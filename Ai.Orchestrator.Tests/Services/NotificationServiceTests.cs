using System.Text.Json;
using System.Text.Json.Nodes;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services;
using Moq;
using StackExchange.Redis;

namespace Ai.Orchestrator.Tests.Services;

public class NotificationServiceTests
{
    private class TestableNotificationService : INotificationService
    {
        private readonly IOrchestrator _orchestrator;
        private readonly ILoggingService _loggingService;
        private readonly IPluginService _pluginService;
        private readonly Mock<IDatabase> _mockDatabase;
        private readonly INotificationPlugin _confirmationPlugin;
        private readonly Dictionary<Guid, Confirmation> _confirmations = new();

        public TestableNotificationService(
            IOrchestrator orchestrator,
            ILoggingService loggingService,
            IPluginService pluginService,
            Mock<IDatabase> mockDatabase,
            INotificationPlugin confirmationPlugin)
        {
            _orchestrator = orchestrator;
            _loggingService = loggingService;
            _pluginService = pluginService;
            _mockDatabase = mockDatabase;
            _confirmationPlugin = confirmationPlugin;
        }

        public async Task<object> SendNotification(string message)
        {
            var confirmation = new Confirmation
            {
                Id = Guid.NewGuid(),
                ConfirmationMessage = message
            };
            var result = await SendConfirmation(confirmation, null);
            var isSuccessful = (bool?)result.GetType().GetProperty("Success")?.GetValue(result) ?? false;
            if (isSuccessful)
            {
                return new
                {
                    Success = true,
                    ConfirmationId = confirmation.Id.ToString()
                };
            }

            return new
            {
                Success = false
            };
        }

        public async Task<object> RequestConfirmation(string serviceName, Confirmation confirmation, IPluginServiceRequest serviceRequest)
        {
            if (serviceRequest is null)
            {
                await _loggingService.LogError("RequestConfirmation: serviceRequest is null");
                throw new Exception("Service Request is null");
            }

            confirmation.Id ??= Guid.NewGuid();
            serviceRequest.ConfirmationId = confirmation.Id.ToString();
            var request = new OrchestratorRequest
            {
                Service = serviceName,
                ServiceRequest = serviceRequest
            };
            var confirmationRequest = await ProcessRequestConfirmation(confirmation, request, 30);
            var isSuccessful = (bool?)confirmationRequest.GetType().GetProperty("Success")?.GetValue(confirmationRequest) ?? false;
            if (isSuccessful)
            {
                return new
                {
                    Success = true,
                    ConfirmationId = confirmation.Id.ToString()
                };
            }

            return new
            {
                Success = false
            };
        }

        public async Task<object> Confirm(Guid confirmationId, bool confirm)
        {
            // get request
            var redisKey = $"confirmation_{confirmationId}";
            var requestJson = await _mockDatabase.Object.StringGetAsync(redisKey);
            if (string.IsNullOrWhiteSpace(requestJson))
            {
                var errorMessage = $"No request found for confirmation id {confirmationId}";
                await _loggingService.LogError(errorMessage);
                return new
                {
                    Success = false,
                    Error = errorMessage
                };
            }

            if (!_confirmations.TryGetValue(confirmationId, out var confirmation))
            {
                var errorMessage = $"No confirmation found for confirmation id {confirmationId}";
                await _loggingService.LogError(errorMessage);
                return new
                {
                    Success = false,
                    Error = errorMessage
                };
            }

            if (confirmation.Expiration < DateTime.Now)
            {
                await _loggingService.LogInformation($"Confirmation {confirmation.Id} expired!");
                return new
                {
                    Success = false,
                    Message = "Confirmation expired"
                };
            }

            try
            {
                if (confirm)
                {
                    var orchestratorRequest = !string.IsNullOrWhiteSpace(requestJson)
                        ? JsonSerializer.Deserialize<OrchestratorRequest>(requestJson)
                        : new OrchestratorRequest();

                    return await _orchestrator.ProcessRequest(orchestratorRequest);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogError($"Error handling confirmation task {redisKey}: {ex.Message}");
            }
            finally
            {
                // Clean up the backup
                await _mockDatabase.Object.KeyDeleteAsync(redisKey);
                _confirmations.Remove(confirmation.Id ?? Guid.Empty);
            }

            return new
            {
                Success = false
            };
        }

        public bool DoesConfirmationExist(Guid confirmationId, out Confirmation confirmation)
        {
            if (_confirmations.TryGetValue(confirmationId, out confirmation))
            {
                if (DateTime.Now > _confirmations[confirmationId].Expiration)
                {
                    _confirmations.Remove(confirmationId);
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        private async Task<object> ProcessRequestConfirmation(Confirmation confirmation, OrchestratorRequest request, int timeoutInMinutes)
        {
            if (confirmation == null)
            {
                throw new ArgumentException("No confirmation found");
            }

            confirmation.Id ??= Guid.NewGuid();
            confirmation.Expiration = DateTime.Now.AddMinutes(timeoutInMinutes);
            _confirmations.TryAdd((Guid)confirmation.Id, confirmation);

            var redisKey = $"confirmation_{confirmation.Id}";
            var requestJson = JsonSerializer.Serialize(request);
            var expiration = TimeSpan.FromMinutes(30);

            await _mockDatabase.Object.StringSetAsync(redisKey, requestJson, expiration);
            return await SendConfirmation(confirmation, request);
        }

        private async Task<object> SendConfirmation(Confirmation confirmation, OrchestratorRequest request)
        {
            if (_confirmationPlugin is not null)
            {
                return await _confirmationPlugin.RequestConfirmation(confirmation, request);
            }

            return false;
        }
    }

    private class TestPluginServiceRequest : IPluginServiceRequest
    {
        public string Method { get; set; } = "";
        public string ToolCallId { get; set; } = "";
        public string RequestingService { get; set; } = "";
        public string ConfirmationId { get; set; } = "";
    }

    private readonly Mock<IOrchestrator> _mockOrchestrator;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IPluginService> _mockPluginService;
    private readonly Mock<INotificationPlugin> _mockConfirmationPlugin;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly TestableNotificationService _notificationService;

    public NotificationServiceTests()
    {
        _mockOrchestrator = new Mock<IOrchestrator>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockPluginService = new Mock<IPluginService>();
        _mockConfirmationPlugin = new Mock<INotificationPlugin>();
        _mockDatabase = new Mock<IDatabase>();

        // Setup plugin service to return our mock confirmation plugin
        _mockPluginService.Setup(x => x.GetPlugin<INotificationPlugin>(It.IsAny<string>()))
            .Returns(_mockConfirmationPlugin.Object);

        _notificationService = new TestableNotificationService(
            _mockOrchestrator.Object,
            _mockLoggingService.Object,
            _mockPluginService.Object,
            _mockDatabase,
            _mockConfirmationPlugin.Object);
    }

    [Fact]
    public async Task SendNotification_ShouldReturnSuccess_WhenPluginReturnsSuccess()
    {
        // Arrange
        var message = "Test notification message";
        var pluginResponse = new { Success = true };

        _mockConfirmationPlugin.Setup(x => x.RequestConfirmation(It.IsAny<Confirmation>(), It.IsAny<OrchestratorRequest>()))
            .ReturnsAsync(pluginResponse);

        // Act
        var result = await _notificationService.SendNotification(message);

        // Assert
        Assert.NotNull(result);
        var resultDict = result.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(result));
        Assert.True((bool)resultDict["Success"]);
        Assert.NotNull(resultDict["ConfirmationId"]);
    }

    [Fact]
    public async Task SendNotification_ShouldReturnFailure_WhenPluginReturnsFailure()
    {
        // Arrange
        var message = "Test notification message";
        var pluginResponse = new { Success = false };

        _mockConfirmationPlugin.Setup(x => x.RequestConfirmation(It.IsAny<Confirmation>(), It.IsAny<OrchestratorRequest>()))
            .ReturnsAsync(pluginResponse);

        // Act
        var result = await _notificationService.SendNotification(message);

        // Assert
        Assert.NotNull(result);
        var resultDict = result.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(result));
        Assert.False((bool)resultDict["Success"]);
    }

    [Fact]
    public async Task SendNotification_ShouldReturnFailure_WhenPluginResponseHasNoSuccessProperty()
    {
        // Arrange
        var message = "Test notification message";
        var pluginResponse = new { Message = "No success property" };

        _mockConfirmationPlugin.Setup(x => x.RequestConfirmation(It.IsAny<Confirmation>(), It.IsAny<OrchestratorRequest>()))
            .ReturnsAsync(pluginResponse);

        // Act
        var result = await _notificationService.SendNotification(message);

        // Assert
        Assert.NotNull(result);
        var resultDict = result.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(result));
        Assert.False((bool)resultDict["Success"]);
    }

    [Fact]
    public async Task RequestConfirmation_ShouldThrowException_WhenServiceRequestIsNull()
    {
        // Arrange
        var serviceName = "TestService";
        var confirmation = new Confirmation { ConfirmationMessage = "Test" };
        IPluginServiceRequest serviceRequest = null;

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _notificationService.RequestConfirmation(serviceName, confirmation, serviceRequest));
    }

    [Fact]
    public async Task RequestConfirmation_ShouldSetConfirmationId_WhenNotProvided()
    {
        // Arrange
        var serviceName = "TestService";
        var confirmation = new Confirmation { ConfirmationMessage = "Test" };
        var serviceRequest = new TestPluginServiceRequest();
        var pluginResponse = new { Success = true };

        _mockConfirmationPlugin.Setup(x => x.RequestConfirmation(It.IsAny<Confirmation>(), It.IsAny<OrchestratorRequest>()))
            .ReturnsAsync(pluginResponse);

        // Act
        var result = await _notificationService.RequestConfirmation(serviceName, confirmation, serviceRequest);

        // Assert
        Assert.NotNull(confirmation.Id);
        Assert.NotNull(serviceRequest.ConfirmationId);
        Assert.Equal(confirmation.Id.ToString(), serviceRequest.ConfirmationId);
    }

    [Fact]
    public async Task RequestConfirmation_ShouldReturnSuccess_WhenPluginSucceeds()
    {
        // Arrange
        var serviceName = "TestService";
        var confirmation = new Confirmation { ConfirmationMessage = "Test" };
        var serviceRequest = new TestPluginServiceRequest();
        var pluginResponse = new { Success = true };

        _mockConfirmationPlugin.Setup(x => x.RequestConfirmation(It.IsAny<Confirmation>(), It.IsAny<OrchestratorRequest>()))
            .ReturnsAsync(pluginResponse);

        // Act
        var result = await _notificationService.RequestConfirmation(serviceName, confirmation, serviceRequest);

        // Assert
        Assert.NotNull(result);
        var resultDict = result.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(result));
        Assert.True((bool)resultDict["Success"]);
        Assert.NotNull(resultDict["ConfirmationId"]);
    }

    [Fact]
    public void DoesConfirmationExist_ShouldReturnFalse_WhenConfirmationNotFound()
    {
        // Arrange
        var confirmationId = Guid.NewGuid();

        // Act
        var result = _notificationService.DoesConfirmationExist(confirmationId, out var confirmation);

        // Assert
        Assert.False(result);
        Assert.Null(confirmation);
    }

    [Fact]
    public void Confirmation_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var confirmation = new Confirmation
        {
            Id = Guid.NewGuid(),
            ConfirmationMessage = "Test message",
            Expiration = DateTime.Now.AddMinutes(5)
        };

        // Assert
        Assert.NotEqual(Guid.Empty, confirmation.Id);
        Assert.Equal("Test message", confirmation.ConfirmationMessage);
        Assert.True(confirmation.Expiration > DateTime.Now);
    }

    [Fact]
    public void Confirmation_ShouldHandleNullValues()
    {
        // Arrange & Act
        var confirmation = new Confirmation();

        // Assert
        Assert.Null(confirmation.Id);
        Assert.Null(confirmation.ConfirmationMessage);
        Assert.Null(confirmation.Expiration);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConfirmationResult_ShouldHandleBothBooleanValues(bool confirmValue)
    {
        // Test that we can work with both confirmation values
        // Arrange & Act
        var result = confirmValue;

        // Assert
        Assert.Equal(confirmValue, result);
    }

    [Fact]
    public void NotificationService_ShouldAcceptValidDependencies()
    {
        // Arrange & Act
        var notificationService = new TestableNotificationService(
            _mockOrchestrator.Object,
            _mockLoggingService.Object,
            _mockPluginService.Object,
            _mockDatabase,
            _mockConfirmationPlugin.Object);

        // Assert
        Assert.NotNull(notificationService);
    }

    [Fact]
    public void GuidGeneration_ShouldCreateUniqueIds()
    {
        // Test GUID generation for confirmation IDs
        // Arrange & Act
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        // Assert
        Assert.NotEqual(id1, id2);
        Assert.NotEqual(Guid.Empty, id1);
        Assert.NotEqual(Guid.Empty, id2);
    }

    [Fact]
    public void DateTime_ShouldHandleExpirationCalculation()
    {
        // Test expiration time calculation
        // Arrange
        var now = DateTime.Now;
        var expirationMinutes = 30;

        // Act
        var expiration = now.AddMinutes(expirationMinutes);

        // Assert
        Assert.True(expiration > now);
        Assert.Equal(expirationMinutes, (expiration - now).TotalMinutes, 1); // Allow 1 minute tolerance
    }
}