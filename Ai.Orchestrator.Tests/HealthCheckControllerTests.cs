using Ai.Orchestrator.Controllers;

namespace Ai.Orchestrator.Tests;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void HealthCheck_Returns_OK()
    {
        // Arrange
        var controller = new HealthCheckController();

        // Act
        var result = controller.Post();

        // Assert
        Assert.AreEqual("OK", result.Status);
    }
}
