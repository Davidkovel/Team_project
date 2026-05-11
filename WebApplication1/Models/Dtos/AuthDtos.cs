namespace WebApplication1.Models.Dtos
{
    public record RegisterDto(string Email, string Password, string? Role = null);
    public record LoginDto(string Email, string Password);
}
