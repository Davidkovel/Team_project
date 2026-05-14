using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Models.Dtos;
using System.Threading.Tasks;
using System.Linq;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public UsersController(UserManager<ApplicationUser> userManager) { _userManager = userManager; }

        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _userManager.Users.Select(u => new UserDto(u.Id, u.Email ?? "", u.UserName, u.IsBlocked, u.CreatedAt)).ToList();
            return Ok(users);
        }

        [HttpPost("block/{id}")]
        public async Task<IActionResult> Block(string id)
        {
            var u = await _userManager.FindByIdAsync(id); if (u == null) return NotFound();
            u.IsBlocked = true; await _userManager.UpdateAsync(u); return NoContent();
        }

        [HttpPost("unblock/{id}")]
        public async Task<IActionResult> Unblock(string id)
        {
            var u = await _userManager.FindByIdAsync(id); if (u == null) return NotFound();
            u.IsBlocked = false; await _userManager.UpdateAsync(u); return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var u = await _userManager.FindByIdAsync(id); if (u == null) return NotFound();
            await _userManager.DeleteAsync(u); return NoContent();
        }
    }
}
