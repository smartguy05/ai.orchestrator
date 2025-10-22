using Ai.Orchestrator.Plugins.Memories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ai.Orchestrator.Plugins.Tests
{
    [TestClass]
    public class MemoriesTests
    {
        private MemoryCommand _memoryCommand;

        [TestInitialize]
        public void TestInitialize()
        {
            _memoryCommand = new MemoryCommand();
        }

        [TestMethod]
        public async Task DoWork_Search_Throws_Exception()
        {
            // Arrange
            var request = new Models.Interfaces.ServiceRequest
            {
                Method = "search"
            };
            var config = new Models.ServiceConfig();
            var tools = new List<Models.Tools.ToolCall>();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<System.Exception>(() => _memoryCommand.DoWork(request, config, tools));
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
            await Assert.ThrowsExceptionAsync<System.Exception>(() => _memoryCommand.DoWork(request, config, tools));
        }
    }
}
