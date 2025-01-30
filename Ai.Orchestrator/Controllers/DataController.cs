using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Ai.Orchestrator.Controllers;

[Route("data")]
public class DataController : ControllerBase
{
    private readonly IOrchestrator _orchestrator;

    public DataController(IOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpPost]
    public async Task<object> GetData([FromBody] OrchestratorRequest request)
    {
        return await _orchestrator.ProcessRequest(request);
    }

    [HttpPost("chain")]
    public async Task<object> RunDataChain([FromBody] IEnumerable<OrchestratorRequest> requests)
    {
        return await _orchestrator.ProcessRequestChain(requests);
    }
}