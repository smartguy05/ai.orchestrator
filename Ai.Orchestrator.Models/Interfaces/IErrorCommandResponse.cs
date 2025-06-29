namespace Ai.Orchestrator.Models.Interfaces;

public interface IErrorCommandResponse: ICommandResponse
{
    public string Error { get; set; }
}