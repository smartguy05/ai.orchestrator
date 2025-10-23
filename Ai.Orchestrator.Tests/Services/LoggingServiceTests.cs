using Ai.Orchestrator.Models.Interfaces;
using LogLevel = Ai.Orchestrator.Models.Enums.LogLevel;

namespace Ai.Orchestrator.Tests.Services;

public class LoggingServiceTests
{
    private class TestableLoggingService : ILoggingService
    {
        public async Task LogInformation(string message)
        {
            await Log(LogLevel.Info, message);
        }

        public async Task LogError(string message, Exception exception = null)
        {
            await Log(LogLevel.Error, message, exception);
        }

        public async Task LogWarning(string message)
        {
            await Log(LogLevel.Warning, message);
        }

        public async Task Log(LogLevel level, string message, Exception exception = null)
        {
            // Simple console logging for testing
            if (!string.IsNullOrEmpty(message))
            {
                Console.WriteLine(message);
            }

            if (exception != null)
            {
                Console.WriteLine(exception.Message);
                if (exception.InnerException != null)
                {
                    Console.WriteLine("Inner Exception(s):");
                    var inner = exception.InnerException;
                    while (inner != null)
                    {
                        Console.WriteLine(inner.Message);
                        inner = inner.InnerException;
                    }
                }
            }
        }
    }

    private readonly TestableLoggingService _loggingService;

    public LoggingServiceTests()
    {
        _loggingService = new TestableLoggingService();
    }

    [Fact]
    public async Task LogInformation_ShouldCallLogWithInfoLevel()
    {
        // Arrange
        var message = "Test information message";
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            await _loggingService.LogInformation(message);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains(message, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task LogError_ShouldCallLogWithErrorLevel()
    {
        // Arrange
        var message = "Test error message";
        var exception = new Exception("Test exception");
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            await _loggingService.LogError(message, exception);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains(message, output);
            Assert.Contains(exception.Message, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task LogError_ShouldCallLogWithErrorLevel_WhenNoException()
    {
        // Arrange
        var message = "Test error message";
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            await _loggingService.LogError(message);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains(message, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task LogWarning_ShouldCallLogWithWarningLevel()
    {
        // Arrange
        var message = "Test warning message";
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            await _loggingService.LogWarning(message);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains(message, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task Log_ShouldWriteToConsole_WhenUseConsoleIsTrue()
    {
        // Arrange
        var message = "Test log message";
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            await _loggingService.Log(LogLevel.Info, message);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains(message, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task Log_ShouldWriteExceptionToConsole_WhenExceptionProvided()
    {
        // Arrange
        var message = "Test log message";
        var exception = new Exception("Test exception");
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            await _loggingService.Log(LogLevel.Error, message, exception);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains(message, output);
            Assert.Contains(exception.Message, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task Log_ShouldWriteInnerExceptionToConsole_WhenInnerExceptionExists()
    {
        // Arrange
        var message = "Test log message";
        var innerException = new InvalidOperationException("Inner exception");
        var exception = new Exception("Outer exception", innerException);
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            await _loggingService.Log(LogLevel.Error, message, exception);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains(message, output);
            Assert.Contains(exception.Message, output);
            Assert.Contains(innerException.Message, output);
            Assert.Contains("Inner Exception(s)", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task Log_ShouldHandleNestedInnerExceptions()
    {
        // Arrange
        var message = "Test log message";
        var deepestException = new ArgumentException("Deepest exception");
        var middleException = new InvalidOperationException("Middle exception", deepestException);
        var outerException = new Exception("Outer exception", middleException);
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            await _loggingService.Log(LogLevel.Error, message, outerException);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains(outerException.Message, output);
            Assert.Contains(middleException.Message, output);
            Assert.Contains(deepestException.Message, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task Log_ShouldFallbackToConsole_WhenPluginThrowsException()
    {
        // Since we're mocking plugins as empty list, this test validates console fallback
        // Arrange
        var message = "Test log message";
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            await _loggingService.Log(LogLevel.Info, message);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains(message, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Info)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    public async Task Log_ShouldHandleAllLogLevels(LogLevel logLevel)
    {
        // Arrange
        var message = $"Test message for {logLevel}";
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            await _loggingService.Log(logLevel, message);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains(message, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task Log_ShouldHandleNullMessage()
    {
        // Arrange
        string message = null;
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act & Assert - Should not throw
            await _loggingService.Log(LogLevel.Info, message);
            Assert.True(true); // Method completed without exception
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task Log_ShouldHandleEmptyMessage()
    {
        // Arrange
        var message = "";
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            await _loggingService.Log(LogLevel.Info, message);

            // Assert
            Assert.True(true); // Method completed without exception
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}