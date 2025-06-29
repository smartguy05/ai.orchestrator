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
        var options = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var stringified = dataRequest?.ServiceRequest as string ?? JsonSerializer.Serialize(dataRequest?.ServiceRequest, options);
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
            Service = dataRequest.Plugin,
            ServiceRequest = stringified
        };
        return await _orchestrator.ProcessRequest(request);
    }

    [HttpPost("chain")]
    public async Task<object> RunDataChain([FromBody] IEnumerable<OrchestratorRequest> requests)
    {
        return await _orchestrator.ProcessRequestChain(requests);
    }

    [HttpGet("agents")]
    public async Task<dynamic> GetAgents()
    {
        var serviceRequest = new
        {
            Method = "get_list_of_available_agents",
            Agent = (string)null,
        };
        var options = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var stringified = JsonSerializer.Serialize(serviceRequest, options);
        var request = new OrchestratorRequest
        {
            Service = "Ai.Orchestrator.Plugins.OpenAI",
            ServiceRequest = stringified
        };
        return await _orchestrator.ProcessRequest(request);
    }
}