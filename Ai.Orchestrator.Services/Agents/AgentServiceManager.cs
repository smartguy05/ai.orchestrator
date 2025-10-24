using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.Entities;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services.Plugin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ai.Orchestrator.Services.Agents;

/// <summary>
/// Manages service instances per agent
/// Creates and caches services for each configured agent
/// </summary>
public class AgentServiceManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Guid, AgentServiceContainer> _agentServices = new();
    private readonly object _lock = new();

    public AgentServiceManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Initialize services for all configured agents on startup
    /// </summary>
    public async Task InitializeAllAgentsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var agents = await context.Agents
            .Where(a => a.IsActive)
            .ToListAsync();

        foreach (var agent in agents)
        {
            await InitializeAgentServicesAsync(agent);
        }
    }

    /// <summary>
    /// Initialize or reinitialize services for a specific agent
    /// Called when agent is created or updated
    /// </summary>
    public async Task InitializeAgentServicesAsync(Agent agent)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        lock (_lock)
        {
            // Dispose existing services if reinitializing
            if (_agentServices.TryGetValue(agent.Id, out var existing))
            {
                // Dispose old plugin service
                (existing.PluginService as IDisposable)?.Dispose();
                _agentServices.Remove(agent.Id);
            }

            // Create new service container
            var container = new AgentServiceContainer
            {
                AgentId = agent.Id,
                AgentName = agent.Name,
                LoggingService = CreateLoggingService(agent),
                NotificationService = CreateNotificationService(agent),
                PluginService = CreatePluginService(agent),
                TaskScheduler = CreateTaskScheduler(agent),
                Orchestrator = CreateOrchestrator(agent)
            };

            _agentServices[agent.Id] = container;
        }

        // Initialize plugins for the agent
        var services = _agentServices[agent.Id];
        await services.PluginService.InitializePlugins(
            services.LoggingService.Log,
            services.NotificationService
        );
    }

    /// <summary>
    /// Get service container for a specific agent
    /// Returns cached instance or creates new if not exists
    /// </summary>
    public async Task<AgentServiceContainer> GetAgentServicesAsync(Guid agentId)
    {
        // Check cache first
        lock (_lock)
        {
            if (_agentServices.TryGetValue(agentId, out var cached))
            {
                return cached;
            }
        }

        // Not cached, load agent and initialize
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var agent = await context.Agents
            .Include(a => a.PluginConfigurations)
            .FirstOrDefaultAsync(a => a.Id == agentId);

        if (agent == null)
            throw new InvalidOperationException($"Agent with ID '{agentId}' not found");

        if (!agent.IsActive)
            throw new InvalidOperationException($"Agent '{agent.Name}' is not active");

        await InitializeAgentServicesAsync(agent);

        return _agentServices[agentId];
    }

    /// <summary>
    /// Remove and dispose services for an agent
    /// Called when agent is deleted or deactivated
    /// </summary>
    public async Task RemoveAgentServicesAsync(Guid agentId)
    {
        lock (_lock)
        {
            if (_agentServices.TryGetValue(agentId, out var container))
            {
                // Dispose plugin service
                await container.PluginService.DisposePlugins();
                (container.PluginService as IDisposable)?.Dispose();

                _agentServices.Remove(agentId);
            }
        }
    }

    /// <summary>
    /// Get all active agent service containers
    /// </summary>
    public IEnumerable<AgentServiceContainer> GetAllAgentServices()
    {
        lock (_lock)
        {
            return _agentServices.Values.ToList();
        }
    }

    // Private service creation methods

    private ILoggingService CreateLoggingService(Agent agent)
    {
        // Create per-agent logging service with agent-specific logging plugins
        return new LoggingService(agent);
    }

    private INotificationService CreateNotificationService(Agent agent)
    {
        // Create per-agent notification service with agent-specific confirmation plugin
        return new NotificationService(agent);
    }

    private IPluginService CreatePluginService(Agent agent)
    {
        // Create per-agent plugin service
        // This will load plugins from agent.PluginConfigurations
        return new PluginService(agent, _serviceProvider);
    }

    private ITaskScheduler CreateTaskScheduler(Agent agent)
    {
        // Create per-agent task scheduler
        return new TaskScheduler(agent);
    }

    private IOrchestrator CreateOrchestrator(Agent agent)
    {
        // Create per-agent orchestrator
        var services = _agentServices[agent.Id];
        return new Orchestrator(agent, services.PluginService);
    }
}
