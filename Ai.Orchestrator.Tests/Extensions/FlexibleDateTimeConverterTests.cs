using System.Text.Json;
using Ai.Orchestrator.Models.Extensions;

namespace Ai.Orchestrator.Tests.Extensions;

public class FlexibleDateTimeConverterTests
{
    private readonly JsonSerializerOptions _options;

    public FlexibleDateTimeConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new FlexibleDateTimeConverter());
    }

    [Theory]
    [InlineData("2023-12-25T10:30:45.123Z", 2023, 12, 25, 10, 30, 45)]
    [InlineData("2023-12-25T10:30:45Z", 2023, 12, 25, 10, 30, 45)]
    [InlineData("2023-12-25T10:30:45.123", 2023, 12, 25, 10, 30, 45)]
    [InlineData("2023-12-25T10:30:45", 2023, 12, 25, 10, 30, 45)]
    [InlineData("2023-12-25 10:30:45", 2023, 12, 25, 10, 30, 45)]
    [InlineData("2023-12-25", 2023, 12, 25, 0, 0, 0)]
    public void Read_ShouldParseDateTimeFormats(string dateString, int year, int month, int day, int hour, int minute, int second)
    {
        // Arrange
        var json = $"\"{dateString}\"";

        // Act
        var result = JsonSerializer.Deserialize<DateTime>(json, _options);

        // Assert
        Assert.Equal(year, result.Year);
        Assert.Equal(month, result.Month);
        Assert.Equal(day, result.Day);
        Assert.Equal(hour, result.Hour);
        Assert.Equal(minute, result.Minute);
        Assert.Equal(second, result.Second);
    }

    [Theory]
    [InlineData("12/25/2023", 2023, 12, 25)]
    [InlineData("12/25/2023 10:30:45", 2023, 12, 25)]
    public void Read_ShouldParseUSDateFormats(string dateString, int year, int month, int day)
    {
        // Arrange
        var json = $"\"{dateString}\"";

        // Act
        var result = JsonSerializer.Deserialize<DateTime>(json, _options);

        // Assert
        Assert.Equal(year, result.Year);
        Assert.Equal(month, result.Month);
        Assert.Equal(day, result.Day);
    }

    [Fact]
    public void Read_ShouldThrowException_ForInvalidDateTime()
    {
        // Arrange
        var json = "\"not a date\"";

        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTime>(json, _options));
    }

    [Fact]
    public void Read_ShouldThrowException_ForEmptyString()
    {
        // Arrange
        var json = "\"\"";

        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTime>(json, _options));
    }

    [Fact]
    public void Write_ShouldFormatDateTimeCorrectly()
    {
        // Arrange
        var dateTime = new DateTime(2023, 12, 25, 10, 30, 45, 123);

        // Act
        var json = JsonSerializer.Serialize(dateTime, _options);

        // Assert
        Assert.Contains("2023-12-25T10:30:45.123Z", json);
    }
}

public class FlexibleNullableDateTimeConverterTests
{
    private readonly JsonSerializerOptions _options;

    public FlexibleNullableDateTimeConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new FlexibleNullableDateTimeConverter());
    }

    [Fact]
    public void Read_ShouldReturnNull_ForNullValue()
    {
        // Arrange
        var json = "null";

        // Act
        var result = JsonSerializer.Deserialize<DateTime?>(json, _options);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Read_ShouldParseDateTime_ForValidValue()
    {
        // Arrange
        var json = "\"2023-12-25T10:30:45Z\"";

        // Act
        var result = JsonSerializer.Deserialize<DateTime?>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2023, result.Value.Year);
        Assert.Equal(12, result.Value.Month);
        Assert.Equal(25, result.Value.Day);
    }

    [Fact]
    public void Write_ShouldWriteNull_ForNullValue()
    {
        // Arrange
        DateTime? dateTime = null;

        // Act
        var json = JsonSerializer.Serialize(dateTime, _options);

        // Assert
        Assert.Equal("null", json);
    }

    [Fact]
    public void Write_ShouldFormatDateTime_ForNonNullValue()
    {
        // Arrange
        DateTime? dateTime = new DateTime(2023, 12, 25, 10, 30, 45, 123);

        // Act
        var json = JsonSerializer.Serialize(dateTime, _options);

        // Assert
        Assert.Contains("2023-12-25T10:30:45.123Z", json);
    }
}
