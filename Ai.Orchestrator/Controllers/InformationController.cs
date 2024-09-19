using System.Collections.Generic;
using System.Threading.Tasks;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Ai.Orchestrator.Controllers;

[Route("info")]
public class InformationController : ControllerBase
{
    private readonly IOrchestrator _orchestrator;

    public InformationController(IOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }
    
    [HttpGet("plugins")]
    public async Task<List<object>> GetAvailablePlugins()
    {
        return await _orchestrator.GetPluginContracts();
    }
}