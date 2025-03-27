namespace Ai.Orchestrator.Services;

public static class ServiceResolver
{
    private static IServiceProvider _serviceProvider;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static T GetService<T>()
    {
        if (_serviceProvider is not null)
        {
            using var scope = _serviceProvider.CreateScope();
            return scope.ServiceProvider.GetService<T>();
        }

        throw new Exception("No Scope Factory Configured");
    }
}