namespace Ai.Orchestrator.Models.Interfaces;

public interface ITaskScheduler
{
    Task AddScheduledTask(ScheduledTask task);
}