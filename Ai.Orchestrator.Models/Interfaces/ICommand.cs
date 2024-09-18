using System.Threading.Tasks;

namespace Ai.Orchestrator.Models.Interfaces;

public interface ICommand
{
    public string Name { get; }
    public string Description { get; }

    Task<object> Execute(object request, string config);
}
