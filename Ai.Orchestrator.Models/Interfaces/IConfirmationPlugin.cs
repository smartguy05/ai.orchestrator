namespace Ai.Orchestrator.Models.Interfaces;

public interface IConfirmationPlugin: IIntializable
{
    protected static IPluginConfig Config;
    public Task<object> RequestConfirmation(Confirmation confirmation, OrchestratorRequest request);
    public ValueTask<object> Confirm(string confirmationId, bool confirm);
}