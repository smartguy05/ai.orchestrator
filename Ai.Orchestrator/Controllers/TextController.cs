using System.Text.Json;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Models.Text;
using Microsoft.AspNetCore.Mvc;

namespace Ai.Orchestrator.Controllers;

[Route("text")]
public class TextController : ControllerBase
{
    private readonly IOrchestrator _orchestrator;

    public TextController(IOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpPost]
    public async Task<object> ProcessTextRequest([FromBody] TextRequest textRequest)
    {
        var serviceRequest = new
        {
            textRequest.SystemPrompt,
            textRequest.UserPrompt,
            Model = "gpt-4o"
        };
        var stringified = JsonSerializer.Serialize(serviceRequest);
        var request = new OrchestratorRequest
        {
            Service = "Ai.Orchestrator.Plugins.OpenAI",
            ServiceRequest = stringified
        };
        return await _orchestrator.ProcessRequest(request);
    }
}