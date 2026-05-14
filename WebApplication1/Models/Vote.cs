using System;

namespace WebApplication1.Models
{
    public class Vote
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public int SurveyId { get; set; }
        public int QuestionId { get; set; }
        public int? OptionId { get; set; }
        public string? OpenAnswer { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
