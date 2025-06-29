namespace Ai.Orchestrator.Models.Interfaces;

public interface IConfirmationService
{
    Task<object> RequestConfirmation(Confirmation confirmation, OrchestratorRequest request, int timeoutInMinutes = 10);
    Task<object> Confirm(Guid confirmationId, bool confirm);
    public bool DoesConfirmationExist(Guid confirmationId, out Confirmation confirmation);
}