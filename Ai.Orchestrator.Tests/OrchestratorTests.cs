using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ai.Orchestrator.Tests
{
    [TestClass]
    public class OrchestratorTests
    {
        private Mock<IPluginService> _pluginServiceMock;
        private Mock<ILoggingService> _loggingServiceMock;
        private Orchestrator _orchestrator;

        [TestInitialize]
        public void TestInitialize()
        {
            _pluginServiceMock = new Mock<IPluginService>();
            _loggingServiceMock = new Mock<ILoggingService>();
            _orchestrator = new Orchestrator(_pluginServiceMock.Object, _loggingServiceMock.Object);
        }

        [TestMethod]
        public async Task GetPluginContracts_Returns_Contracts()
        {
            // Arrange
            var contracts = new Dictionary<string, IEnumerable<string>>
            {
                { "Plugin1", new[] { "Action1", "Action2" } }
            };
            _pluginServiceMock.Setup(x => x.GetPluginContracts()).Returns(contracts);

            // Act
            var result = await _orchestrator.GetPluginContracts();

            // Assert
            Assert.AreEqual(contracts, result);
        }

        [TestMethod]
        public async Task ProcessRequest_Returns_Response()
        {
            // Arrange
            var request = new Models.OrchestratorRequest();
            var response = new object();
            _pluginServiceMock.Setup(x => x.RunPlugin(request)).ReturnsAsync(response);

            // Act
            var result = await _orchestrator.ProcessRequest(request);

            // Assert
            Assert.AreEqual(response, result);
        }

        [TestMethod]
        public async Task ProcessRequestChain_Returns_Response()
        {
            // Arrange
            var requests = new[]
            {
                new Models.OrchestratorRequest
                {
                    Messages = new List<Models.Chat.ChatMessageHistory>
                    {
                        new Models.Chat.ChatMessageHistory
                        {
                            ToolCalls = new List<Models.Tools.ToolCall>
                            {
                                new Models.Tools.ToolCall
                                {
                                    Id = "1"
                                }
                            }
                        }
                    }
                }
            };
            var response = new object();
            _pluginServiceMock.Setup(x => x.RunPlugin(It.IsAny<Models.OrchestratorRequest>())).ReturnsAsync(response);

            // Act
            var result = await _orchestrator.ProcessRequestChain(requests);

            // Assert
            Assert.AreEqual(response, result);
        }
    }
}
