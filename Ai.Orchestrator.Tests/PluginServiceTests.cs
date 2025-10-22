using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services.Plugin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ai.Orchestrator.Tests
{
    [TestClass]
    public class PluginServiceTests
    {
        private PluginService _pluginService;

        [TestInitialize]
        public void TestInitialize()
        {
            _pluginService = new PluginService();
        }

        [TestMethod]
        public void GetTools_Returns_Tools()
        {
            // Arrange

            // Act
            var result = _pluginService.GetTools();

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task RunPlugin_Throws_Exception()
        {
            // Arrange
            var request = new Models.OrchestratorRequest();
            var loggerMock = new Mock<Models.Interfaces.LogDelegate>();
            var notificationServiceMock = new Mock<Models.Interfaces.INotificationService>();
            var field = typeof(PluginService).GetField("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            field.SetValue(null, loggerMock.Object);
            field = typeof(PluginService).GetField("_notificationService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            field.SetValue(null, notificationServiceMock.Object);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<System.Exception>(() => _pluginService.RunPlugin(request));
        }
    }
}
