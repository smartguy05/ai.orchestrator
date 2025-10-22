using Ai.Orchestrator.Plugins.OpenAi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ai.Orchestrator.Plugins.Tests
{
    [TestClass]
    public class OpenAiTests
    {
        private OpenAiCommand _openAiCommand;

        [TestInitialize]
        public void TestInitialize()
        {
            _openAiCommand = new OpenAiCommand();
        }

        [TestMethod]
        public async Task DoWork_Text_Throws_Exception()
        {
            // Arrange
            var request = new Models.Interfaces.ServiceRequest
            {
                Method = "text"
            };
            var config = new Models.ServiceConfig();
            var tools = new List<Models.Tools.ToolCall>();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<System.Exception>(() => _openAiCommand.DoWork(request, config, tools));
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
            await Assert.ThrowsExceptionAsync<System.Exception>(() => _openAiCommand.DoWork(request, config, tools));
        }
    }
}
