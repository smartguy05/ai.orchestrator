using System.Collections.Generic;
using System.Threading.Tasks;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Models.Webhook;
using Microsoft.AspNetCore.Mvc;

namespace Ai.Orchestrator.Controllers;

[Route("hook")]
public class WebHookController: ControllerBase
{
    private readonly IOrchestrator _orchestrator;

    public WebHookController(IOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpPost]
    public async Task<ActionResult> ProcessWebHook([FromBody] OrchestratorRequest request)
    {
        await _orchestrator.ProcessRequest(request);
        return Ok();
    }
    
    [HttpPost("chain")]
    public async Task<ActionResult> ProcessWebHookChain([FromBody] IEnumerable<OrchestratorRequest> requests)
    {
        await _orchestrator.ProcessRequestChain(requests);
        return Ok();
    }
}