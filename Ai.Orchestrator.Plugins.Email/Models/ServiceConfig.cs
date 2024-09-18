using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Plugins.Email.Models;

public record ServiceConfig: IPluginConfig
{
    public object Contract { get; set; }
    public string Description { get; set; }
    public IEnumerable<EmailParameters> EmailAccounts { get; set; }
}