namespace Ai.Orchestrator.Plugins.Email.Models;

public record EmailParameters
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Email { get; set; }
    public bool Default { get; set; }
    public string Imap { get; set; }
    public int ImapPort { get; set; }
    public string Smtp { get; set; }
    public int SmtpPort { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public bool UseSsl { get; set; }
}