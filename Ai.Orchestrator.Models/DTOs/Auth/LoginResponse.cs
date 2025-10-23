namespace Ai.Orchestrator.Models.DTOs.Auth;

/// <summary>
/// Response model for successful login
/// </summary>
public class LoginResponse
{
    public string Token { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}
