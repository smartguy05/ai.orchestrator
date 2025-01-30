namespace Ai.Orchestrator.Models.Exceptions;

public class OrchestratorException : Exception
{
    public OrchestratorException()
    {
    }

    public OrchestratorException(string message) : base(message)
    {
    }

    public OrchestratorException(string message, Exception inner) : base(message, inner)
    {
    }
}