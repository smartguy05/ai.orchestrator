namespace Ai.Orchestrator.Models.Interfaces;

public interface IIntializable
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="config">String value of the service config, will be deserialized</param>
    /// <param name="logFunction">The logging function from LoggingService</param>
    /// <param name="confirmationService">The injected Confirmation Service</param>
    /// <remarks>IMPORTANT! Do not use Log() in this method. Log() is not initialized yet. use the provided log parameter for logging</remarks>
    /// <returns></returns>
    public Task<object> Initialize(string config, LogDelegate logFunction, IConfirmationService confirmationService);
}