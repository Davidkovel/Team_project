using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using WebApplication1.Models;
using WebApplication1.Models.Dtos;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _cfg;

        public AuthController(UserManager<ApplicationUser> userManager, IConfiguration cfg)
        {
            _userManager = userManager;
            _cfg = cfg;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null) return BadRequest("Email already in use");

            var user = new ApplicationUser { Email = dto.Email, UserName = dto.Email };
            var res = await _userManager.CreateAsync(user, dto.Password);
            if (!res.Succeeded) return BadRequest(res.Errors);

            var role = string.IsNullOrEmpty(dto.Role) ? "User" : dto.Role;
            if (await _userManager.IsInRoleAsync(user, role)) { /* already in role */ }
            else
            {
                try { await _userManager.AddToRoleAsync(user, role); } catch { /* ignore if role missing */ }
            }

            return CreatedAtAction(null, new { user.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized();
            if (user.IsBlocked) return Forbid();

            var passOk = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passOk) return Unauthorized();

            var key = Encoding.UTF8.GetBytes(_cfg["Jwt:Key"] ?? "very_long_default_dev_key_change_me");
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? "")
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenStr = tokenHandler.WriteToken(token);
            return Ok(new { token = tokenStr });
        }
    }
}
