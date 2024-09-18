using Ai.Orchestrator.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Ai.Orchestrator.Controllers;

public class HealthCheckController : ControllerBase
{
    public HealthCheck Post()
    {
        return new HealthCheck
        {
            Status = "OK"
        };
    }
}