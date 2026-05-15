using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.Dtos;
using System.Security.Claims;
using System.Linq;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SurveysController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public SurveysController(ApplicationDbContext db) { _db = db; }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateSurveyDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Forbid();

            var s = new Survey { Title = dto.Title, Description = dto.Description, IsPublished = dto.IsPublished, CreatedById = userId };
            _db.Surveys.Add(s);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = s.Id }, s);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> Get(int id)
        {
            var s = await _db.Surveys.Include(x => x.Questions).ThenInclude(q => q.Options).FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();
            return Ok(s);
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var q = _db.Surveys.Include(s => s.Questions).AsQueryable();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return Ok(items);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] CreateSurveyDto dto)
        {
            var s = await _db.Surveys.FindAsync(id); if (s == null) return NotFound();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Forbid();
            if (s.CreatedById != userId && !User.IsInRole("Admin")) return Forbid();

            s.Title = dto.Title; s.Description = dto.Description; s.IsPublished = dto.IsPublished;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _db.Surveys.FindAsync(id); if (s == null) return NotFound();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Forbid();
            if (s.CreatedById != userId && !User.IsInRole("Admin")) return Forbid();

            s.IsDeleted = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // Questions
        [HttpPost("{surveyId}/questions")]
        [Authorize]
        public async Task<IActionResult> AddQuestion(int surveyId, [FromBody] CreateQuestionDto dto)
        {
            var s = await _db.Surveys.FindAsync(surveyId); if (s == null) return NotFound();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; if (userId == null) return Forbid();
            if (s.CreatedById != userId && !User.IsInRole("Admin")) return Forbid();

            var q = new Question { SurveyId = surveyId, Text = dto.Text, QuestionType = (QuestionType)dto.QuestionType, IsRequired = dto.IsRequired };
            _db.Questions.Add(q); await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = surveyId }, q);
        }

        [HttpPost("questions/{questionId}/options")]
        [Authorize]
        public async Task<IActionResult> AddOption(int questionId, [FromBody] CreateOptionDto dto)
        {
            var q = await _db.Questions.Include(x => x.Survey).FirstOrDefaultAsync(x => x.Id == questionId); if (q == null) return NotFound();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; if (userId == null) return Forbid();
            if (q.Survey.CreatedById != userId && !User.IsInRole("Admin")) return Forbid();

            var o = new Option { QuestionId = questionId, Text = dto.Text };
            _db.Options.Add(o); await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = q.SurveyId }, o);
        }

        // Vote
        [HttpPost("questions/{questionId}/vote")]
        [Authorize]
        public async Task<IActionResult> Vote(int questionId, [FromBody] VoteDto dto)
        {
            var q = await _db.Questions.Include(x => x.Survey).Include(x => x.Options).FirstOrDefaultAsync(x => x.Id == questionId); if (q == null) return NotFound();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; if (userId == null) return Forbid();
            var survey = q.Survey;
            var now = DateTime.UtcNow;
            if ((survey.StartAt.HasValue && now < survey.StartAt.Value) || (survey.EndAt.HasValue && now > survey.EndAt.Value)) return BadRequest("Survey not active");

            if (q.QuestionType == QuestionType.Open)
            {
                var vote = new Vote { UserId = userId, SurveyId = q.SurveyId, QuestionId = q.Id, OpenAnswer = dto.OpenAnswer };
                _db.Votes.Add(vote);
                await _db.SaveChangesAsync();
                return Ok();
            }

            var option = q.Options.FirstOrDefault(o => o.Id == dto.OptionId);
            if (option == null) return NotFound("Option not found");

            // check duplicate
            if (!survey.AllowRepeatVoting)
            {
                var existing = await _db.Votes.FirstOrDefaultAsync(v => v.UserId == userId && v.QuestionId == q.Id);
                if (existing != null) return BadRequest("User already voted for this question");
            }

            var voteRecord = new Vote { UserId = userId, SurveyId = q.SurveyId, QuestionId = q.Id, OptionId = option.Id };
            option.VotesCount += 1;
            _db.Votes.Add(voteRecord);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("{id}/results")]
        [Authorize]
        public async Task<IActionResult> Results(int id)
        {
            var s = await _db.Surveys.Include(x => x.Questions).ThenInclude(q => q.Options).FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();

            var result = s.Questions.Select(q => new
            {
                q.Id,
                q.Text,
                Options = q.Options.Select(o => new { o.Id, o.Text, Votes = o.VotesCount, Percent = q.Options.Sum(x => x.VotesCount) == 0 ? 0 : Math.Round(100.0 * o.VotesCount / (double)System.Math.Max(1, q.Options.Sum(x => x.VotesCount)), 2) })
            });

            return Ok(result);
        }
        
        [HttpGet("{id}/statistics")]
        [Authorize]
        public async Task<IActionResult> Statistics(int id)
        {
            var survey = await _db.Surveys
                .Include(x => x.Questions)
                .ThenInclude(x => x.Options)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (survey == null)
                return NotFound();

            var result = survey.Questions.Select(q =>
            {
                var totalVotes = q.Options.Sum(o => o.VotesCount);

                return new
                {
                    QuestionId = q.Id,
                    Question = q.Text,
                    TotalVotes = totalVotes,

                    Options = q.Options.Select(o => new
                    {
                        OptionId = o.Id,
                        Option = o.Text,
                        Votes = o.VotesCount,

                        Percentage = totalVotes == 0
                            ? 0
                            : Math.Round(
                                (double)o.VotesCount /
                                totalVotes * 100,
                                2)
                    })
                };
            });

            return Ok(result);
        }
    }
}
