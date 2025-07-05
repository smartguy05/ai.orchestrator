namespace Ai.Orchestrator.Models.Interfaces;

public interface INotificationService
{
    Task<object> Confirm(Guid confirmationId, bool confirm);
    public bool DoesConfirmationExist(Guid confirmationId, out Confirmation confirmation);
    Task<object> RequestConfirmation(string serviceName, Confirmation confirmation, IPluginServiceRequest serviceRequest);
    Task<object> SendNotification(string message);
}