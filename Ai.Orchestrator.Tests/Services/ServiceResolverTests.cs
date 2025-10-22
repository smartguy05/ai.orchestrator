using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Ai.Orchestrator.Tests.Services;

public class ServiceResolverTests
{
    [Fact]
    public void GetService_ShouldThrowException_WhenNotInitialized()
    {
        // Arrange
        // Reset the service provider by creating a new instance
        // Note: In a real scenario, you'd need a way to reset the static field

        // Act & Assert
        Assert.Throws<Exception>(() => ServiceResolver.GetService<IOrchestrator>());
    }

    [Fact]
    public void GetOrchestrator_ShouldReturnOrchestrator_WhenInitialized()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockOrchestrator = new Mock<IOrchestrator>();
        services.AddSingleton(mockOrchestrator.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        ServiceResolver.Initialize(serviceProvider);
        var result = ServiceResolver.GetOrchestrator();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockOrchestrator.Object, result);
    }

    [Fact]
    public void GetPluginService_ShouldReturnPluginService_WhenInitialized()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPluginService = new Mock<IPluginService>();
        services.AddSingleton(mockPluginService.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        ServiceResolver.Initialize(serviceProvider);
        var result = ServiceResolver.GetPluginService();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPluginService.Object, result);
    }

    [Fact]
    public void GetTaskScheduler_ShouldReturnTaskScheduler_WhenInitialized()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockTaskScheduler = new Mock<ITaskScheduler>();
        services.AddSingleton(mockTaskScheduler.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        ServiceResolver.Initialize(serviceProvider);
        var result = ServiceResolver.GetTaskScheduler();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockTaskScheduler.Object, result);
    }

    [Fact]
    public void GetService_ShouldReturnService_WhenRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockService = new Mock<ILoggingService>();
        services.AddSingleton(mockService.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        ServiceResolver.Initialize(serviceProvider);
        var result = ServiceResolver.GetService<ILoggingService>();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockService.Object, result);
    }
}
