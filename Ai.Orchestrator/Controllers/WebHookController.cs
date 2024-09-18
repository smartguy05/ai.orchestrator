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
    public async Task<ActionResult> ProcessWebHook([FromBody] Webhook request)
    {
        await _orchestrator.ProcessWebHook(request);
        return Ok();
    }
    
    [HttpPost("chain")]
    public async Task<ActionResult> ProcessWebHookChain([FromBody] IEnumerable<Webhook> requests)
    {
        await _orchestrator.ProcessWebHookChain(requests);
        return Ok();
    }

    [HttpGet]
    public async Task<List<object>> GetAvailablePlugins()
    {
        return await _orchestrator.GetPluginContracts();
    }
}