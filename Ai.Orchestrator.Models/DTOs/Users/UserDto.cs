namespace Ai.Orchestrator.Models.DTOs.Users;

/// <summary>
/// User data transfer object (excludes password hash)
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}
