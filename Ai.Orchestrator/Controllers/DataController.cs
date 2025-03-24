using System.Text.Json;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Dto;
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
    public async Task<object> GetData([FromBody] DataControllerRequest dataRequest)
    {
        var stringified = dataRequest?.ServiceRequest as string ?? JsonSerializer.Serialize(dataRequest?.ServiceRequest);
        if (dataRequest is null || string.IsNullOrWhiteSpace(stringified))
        {
            return new
            {
                Success = false,
                Message = "Request is empty"
            };
        }
        var request = new OrchestratorRequest
        {
            Service = "Ai.Orchestrator.Plugins.PythonRunner",
            ServiceRequest = stringified
        };
        return await _orchestrator.ProcessRequest(request);
    }

    [HttpPost("chain")]
    public async Task<object> RunDataChain([FromBody] IEnumerable<OrchestratorRequest> requests)
    {
        return await _orchestrator.ProcessRequestChain(requests);
    }
}