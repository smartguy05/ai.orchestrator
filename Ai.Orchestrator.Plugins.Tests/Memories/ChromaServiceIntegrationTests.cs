using Ai.Orchestrator.Models.Enums;
using Ai.Orchestrator.Plugins.Memories;
using Ai.Orchestrator.Plugins.Memories.Models;

namespace Ai.Orchestrator.Plugins.Tests.Memories;

/// <summary>
/// Integration tests for ChromaService that require a running ChromaDB instance.
/// These tests verify the connection to ChromaDB v0.6.3 (v1 API).
///
/// Prerequisites:
/// - ChromaDB must be running at http://localhost:8000
/// - Use: docker compose -f dependencies/docker-compose.yml up chromadb
///
/// Running integration tests:
/// - Run all integration tests: dotnet test --filter Category=Integration
/// - Exclude from CI: dotnet test --filter Category!=Integration
/// </summary>
public class ChromaServiceIntegrationTests
{
    private readonly ServiceConfig _testConfig;

    public ChromaServiceIntegrationTests()
    {
        // Setup test configuration
        _testConfig = new ServiceConfig
        {
            ChromaUrl = "http://localhost:8000/api/v1/",
            DefaultCollectionName = "test-collection",
            OpenAiApiKey = "test-key-not-used-for-connection",
            EmbeddingModel = "text-embedding-3-small"
        };
    }

    private async Task TestLogger(LogLevel level, string message, Exception exception = null)
    {
        // Simple test logger for integration tests
        await Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Dependency", "ChromaDB")]
    public async Task TestConnectionAsync_WithRunningChromaDB_ShouldReturnTrue()
    {
        // Arrange
        using var service = new ChromaService(_testConfig, TestLogger);

        // Act
        var result = await service.TestConnectionAsync();

        // Assert
        Assert.True(result, "ChromaDB connection should succeed when service is running");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Dependency", "ChromaDB")]
    public async Task TestConnectionAsync_WithInvalidUrl_ShouldReturnFalse()
    {
        // Arrange
        var invalidConfig = new ServiceConfig
        {
            ChromaUrl = "http://localhost:9999/api/v1/",
            DefaultCollectionName = "test-collection",
            OpenAiApiKey = "test-key",
            EmbeddingModel = "text-embedding-3-small"
        };
        using var service = new ChromaService(invalidConfig, TestLogger);

        // Act
        var result = await service.TestConnectionAsync();

        // Assert
        Assert.False(result, "ChromaDB connection should fail with invalid URL");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Dependency", "ChromaDB")]
    public async Task DiagnoseChromaDbAsync_WithRunningChromaDB_ShouldReturnSuccessfulDiagnostics()
    {
        // Arrange
        using var service = new ChromaService(_testConfig, TestLogger);

        // Act
        var diagnostics = await service.DiagnoseChromaDbAsync();

        // Assert
        Assert.NotNull(diagnostics);
        Assert.Contains("ChromaDB URL configured:", diagnostics);
        Assert.Contains("✓ Heartbeat successful:", diagnostics);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Dependency", "ChromaDB")]
    public async Task GetOrCreateCollectionClientAsync_CalledTwice_ShouldNotThrowException()
    {
        // This test reproduces the bug where calling GetOrCreateCollectionClientAsync
        // multiple times (simulating app restart) fails due to empty metadata dictionary

        // Arrange
        var uniqueCollectionName = $"test-restart-{Guid.NewGuid().ToString()[..8]}";
        var config = new ServiceConfig
        {
            ChromaUrl = "http://localhost:8000/api/v1/",
            DefaultCollectionName = uniqueCollectionName,
            OpenAiApiKey = "test-key",
            EmbeddingModel = "text-embedding-3-small"
        };

        // Act & Assert - First call (simulates first run)
        using (var service1 = new ChromaService(config, TestLogger))
        {
            var collection1 = await service1.GetOrCreateCollectionClientAsync();
            Assert.NotNull(collection1);
        }

        // Act & Assert - Second call (simulates app restart)
        // This should NOT throw an exception about empty metadata
        using (var service2 = new ChromaService(config, TestLogger))
        {
            var exception = await Record.ExceptionAsync(async () =>
            {
                var collection2 = await service2.GetOrCreateCollectionClientAsync();
                Assert.NotNull(collection2);
            });

            Assert.Null(exception); // Should not throw any exception
        }
    }
}
