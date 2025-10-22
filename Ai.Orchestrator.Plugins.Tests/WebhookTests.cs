using Ai.Orchestrator.Plugins.Webhook;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ai.Orchestrator.Plugins.Tests
{
    [TestClass]
    public class WebhookTests
    {
        private WebhookCommand _webhookCommand;

        [TestInitialize]
        public void TestInitialize()
        {
            _webhookCommand = new WebhookCommand();
        }

        [TestMethod]
        public async Task DoWork_Post_Throws_Exception()
        {
            // Arrange
            var request = new Models.Interfaces.ServiceRequest
            {
                Method = "post"
            };
            var config = new Models.ServiceConfig();
            var tools = new List<Models.Tools.ToolCall>();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<System.Exception>(() => _webhookCommand.DoWork(request, config, tools));
        }

        [TestMethod]
        public async Task DoWork_Invalid_Method_Throws_Exception()
        {
            // Arrange
            var request = new Models.Interfaces.ServiceRequest
            {
                Method = "invalid"
            };
            var config = new Models.ServiceConfig();
            var tools = new List<Models.Tools.ToolCall>();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<System.Exception>(() => _webhookCommand.DoWork(request, config, tools));
        }
    }
}
