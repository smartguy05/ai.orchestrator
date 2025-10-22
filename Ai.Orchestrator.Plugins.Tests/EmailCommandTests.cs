using System.Reflection;
using Ai.Orchestrator.Plugins.Email;

namespace Ai.Orchestrator.Plugins.Tests;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void CleanHtmlEmail_Returns_Cleaned_Html()
    {
        // Arrange
        var emailCommand = new EmailCommand();
        var method = emailCommand.GetType().GetMethod("CleanHtmlEmail", BindingFlags.Static | BindingFlags.NonPublic);
        var html = "<html><body><p>Hello World</p></body></html>";
        var expected = "Hello World";

        // Act
        var result = method.Invoke(null, new object[] { html });

        // Assert
        Assert.AreEqual(expected, result);
    }
}
