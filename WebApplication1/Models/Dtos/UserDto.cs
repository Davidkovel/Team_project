// ...existing code...
using System;

namespace WebApplication1.Models.Dtos
{
    public record UserDto(string Id, string Email, string? UserName, bool IsBlocked, DateTime CreatedAt);
}
// ...existing code...
