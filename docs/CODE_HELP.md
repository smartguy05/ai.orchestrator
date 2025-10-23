# Claude Code Technical Guide - Ai.Orchestrator

## 🚨 MANDATORY TDD REQUIREMENTS 🚨

**BEFORE WRITING ANY CODE, YOU MUST:**
1. Write a test that fails
2. Run the test suite to confirm failure
3. Document what you're testing

**ONLY THEN:**
4. Write minimal code to pass the test
5. Verify all tests pass
6. Refactor with tests as safety net

**NO EXCEPTIONS. NO SHORTCUTS. NO EXCUSES.**

## Project Structure Navigation

```
Ai.Orchestrator/
├── Ai.Orchestrator/               # Main API application
│   ├── Controllers/               # API endpoints
│   ├── Middleware/                # Request pipeline
│   ├── Program.cs                 # Application entry
│   └── launchSettings.json       # Dev configurations
├── Ai.Orchestrator.Services/      # Business logic layer
│   ├── Plugin/                    # Plugin management
│   ├── LoggingService.cs         # Logging orchestration
│   ├── NotificationService.cs    # Notification handling
│   ├── Orchestrator.cs           # Core orchestration
│   └── TaskScheduler.cs          # Task scheduling
├── Ai.Orchestrator.Models/        # Shared models/interfaces
│   ├── Interfaces/                # Plugin interfaces
│   ├── Configuration/             # Config models
│   └── Extensions/                # Helper extensions
├── Ai.Orchestrator.Plugins.*/     # Plugin implementations
├── Ai.Orchestrator.Tests/         # Main app tests
├── Ai.Orchestrator.Plugins.Tests/ # Plugin tests
└── docs/                          # Documentation
```

## Testing Commands (RUN THESE FIRST!)

### Before ANY Code Changes
```bash
# Run all tests to ensure clean state
dotnet test

# Watch mode for TDD (KEEP RUNNING)
dotnet watch test

# Run specific test file
dotnet test --filter "FullyQualifiedName~PluginServiceTests"

# Check coverage before starting
dotnet test --collect:"XPlat Code Coverage"
```

### Test-First Development Flow
```bash
# 1. Create your test file first
touch Ai.Orchestrator.Tests/YourFeatureTests.cs

# 2. Write failing test (example below)
# 3. Run test to see it fail
dotnet test --filter "YourFeatureTests"

# 4. Implement minimum code
# 5. Run test to see it pass
dotnet test --filter "YourFeatureTests"

# 6. Run all tests to ensure no regression
dotnet test
```

## Test-First Examples for Common Patterns

### Adding a New Controller Endpoint

**STEP 1: Write the test FIRST**
```csharp
// Ai.Orchestrator.Tests/Controllers/NewEndpointTests.cs
[Fact]
public async Task NewEndpoint_ShouldReturnExpectedData()
{
    // Arrange
    var client = _factory.CreateClient();
    var request = new { data = "test" };

    // Act
    var response = await client.PostAsJsonAsync("/api/new", request);

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    Assert.Contains("expected", content);
}
```

**STEP 2: Run test (it MUST fail)**
```bash
dotnet test --filter "NewEndpoint_ShouldReturnExpectedData"
# Expected: Test fails with 404 or compilation error
```

**STEP 3: Implement endpoint**
```csharp
[ApiController]
[Route("api/[controller]")]
public class NewController : ControllerBase
{
    [HttpPost("new")]
    public async Task<IActionResult> NewEndpoint([FromBody] dynamic request)
    {
        return Ok(new { result = "expected" });
    }
}
```

### Adding a New Service Method

**STEP 1: Write service test FIRST**
```csharp
// Ai.Orchestrator.Tests/Services/ServiceMethodTests.cs
[Fact]
public async Task ProcessData_ShouldTransformCorrectly()
{
    // Arrange
    var service = new DataService();
    var input = "raw data";

    // Act
    var result = await service.ProcessDataAsync(input);

    // Assert
    Assert.Equal("processed: raw data", result);
}
```

**STEP 2: Implement after test fails**
```csharp
public class DataService
{
    public async Task<string> ProcessDataAsync(string input)
    {
        return await Task.FromResult($"processed: {input}");
    }
}
```

### Adding a New Plugin

**STEP 1: Write plugin interface test**
```csharp
[Fact]
public void NewPlugin_ShouldImplementIPlugin()
{
    var plugin = new MyNewPlugin();
    Assert.IsAssignableFrom<IPlugin>(plugin);
}

[Fact]
public async Task NewPlugin_ShouldExecuteTool()
{
    var plugin = new MyNewPlugin();
    var result = await plugin.ExecuteAsync("toolName", new {});
    Assert.NotNull(result);
}
```

**STEP 2: Create plugin after tests fail**

## Development Workflow

### Local Setup (Test-Driven)
```bash
# 1. Clone and setup
git clone <repo>
cd Ai.Orchestrator

# 2. Restore packages
dotnet restore

# 3. RUN TESTS FIRST
dotnet test

# 4. Start dependencies
docker-compose -f dependencies/docker-compose.yml up -d

# 5. Run application
dotnet run --project Ai.Orchestrator
```

### Environment Configuration
```bash
# Key environment variables
export PluginDirectory="./plugins"
export ConfigDirectory="./configs"
export ActivePlugins="Ai.Orchestrator.Plugins.OpenAI,Ai.Orchestrator.Plugins.Email"
```

## Common Tasks (WITH TEST-FIRST APPROACH)

### Creating a New Plugin

1. **Write plugin tests first**:
```csharp
// Test file: MyPlugin.Tests.cs
[Fact]
public void Plugin_ShouldLoadConfiguration()
{
    var config = new MyPluginConfig { ApiKey = "test" };
    var plugin = new MyPlugin();
    plugin.InitializeAsync(config);
    Assert.NotNull(plugin.Configuration);
}
```

2. **Create plugin interface**:
```csharp
public class MyPlugin : IPlugin
{
    public string Name => "MyPlugin";
    public string Version => "1.0.0";

    public async Task InitializeAsync(IConfig config)
    {
        // Implementation after test
    }
}
```

3. **Add configuration**:
```json
// Configs/MyPlugin.json
{
    "ApiKey": "your-key",
    "Settings": {}
}
```

### Adding Authentication (TEST FIRST!)

1. **Write auth test**:
```csharp
[Fact]
public async Task Endpoint_ShouldRequireAuthentication()
{
    var response = await _unauthorizedClient.GetAsync("/api/secure");
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

2. **Implement after test fails**:
```csharp
[Authorize]
[HttpGet("secure")]
public async Task<IActionResult> SecureEndpoint()
{
    // Implementation
}
```

## Business Logic Reference

### Core Services

#### PluginService
- **Purpose**: Manages plugin lifecycle
- **Test First**: Write tests for plugin loading errors before handling them
- **Key Methods**:
  - `LoadPluginsAsync()` - Test plugin discovery first
  - `ExecuteToolAsync()` - Test tool execution first
  - `GetPluginTools()` - Test tool registration first

#### Orchestrator
- **Purpose**: Coordinates plugin operations
- **Test First**: Write orchestration tests before implementation
- **Key Methods**:
  - `ProcessRequestAsync()` - Test request routing first
  - `HandleErrorAsync()` - Test error scenarios first

#### NotificationService
- **Purpose**: Handles notifications
- **Test First**: Write notification tests before channels
- **Key Methods**:
  - `SendNotificationAsync()` - Test delivery first
  - `GetConfirmationAsync()` - Test user flow first

### Plugin Patterns

#### Standard Plugin Implementation
```csharp
// ALWAYS TEST FIRST!
[Fact]
public async Task Plugin_ShouldExecuteSuccessfully()
{
    // Test before implementing
}

public class StandardPlugin : IPlugin
{
    public async Task<object> ExecuteAsync(string tool, object parameters)
    {
        // Implement after test
    }
}
```

#### Logging Plugin Implementation
```csharp
// TEST FIRST!
[Fact]
public void LoggingPlugin_ShouldLogMessages()
{
    // Test logging behavior first
}

public class CustomLogger : ILoggingPlugin
{
    public async Task LogAsync(LogLevel level, string message)
    {
        // Implement after test
    }
}
```

## API Patterns

### Request/Response Formats

#### Text Processing (Test Examples First!)
```csharp
// Test the expected format first
[Fact]
public async Task TextAPI_ShouldAcceptFormat()
{
    var request = new
    {
        text = "process this",
        agentId = "agent-1",
        tools = new[] { "tool1", "tool2" }
    };

    var response = await client.PostAsJsonAsync("/api/text", request);
    Assert.True(response.IsSuccessStatusCode);
}
```

#### Webhook Handling
```csharp
// Test webhook validation first
[Fact]
public async Task Webhook_ShouldValidateSignature()
{
    var webhook = new { event = "test", data = new {} };
    var response = await client.PostAsJsonAsync("/api/webhook", webhook);
    // Assert validation behavior
}
```

## Database Schema

### Configuration Storage (Test Data Access First!)
```csharp
// Test repository before implementing
[Fact]
public async Task Repository_ShouldStoreConfig()
{
    var repo = new ConfigRepository();
    var config = new PluginConfig();
    await repo.SaveAsync(config);
    var loaded = await repo.GetAsync(config.Id);
    Assert.Equal(config.Id, loaded.Id);
}
```

## Code Style and Standards

### Test Naming Convention
```csharp
// MethodName_StateUnderTest_ExpectedBehavior
Plugin_WhenInitialized_ShouldLoadConfiguration()
Service_WhenErrorOccurs_ShouldRetryThreeTimes()
Controller_WithInvalidInput_ShouldReturnBadRequest()
```

### Code Organization
- Tests mirror source structure
- One test class per source class
- Helper methods in TestHelpers
- Shared fixtures in Fixtures folder

### Async/Await Pattern
```csharp
// Always test async behavior
[Fact]
public async Task Method_ShouldCompleteAsynchronously()
{
    var task = service.MethodAsync();
    Assert.False(task.IsCompleted);
    await task;
    Assert.True(task.IsCompleted);
}
```

## Debugging and Troubleshooting

### Test Debugging
```bash
# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Debug specific test
dotnet test --filter "TestName" --logger "console;verbosity=detailed"

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport"
```

### Common Issues and Solutions

#### Plugin Not Loading
1. **Write test for plugin discovery**
2. Check PluginDirectory path
3. Verify plugin implements IPlugin
4. Check configuration file exists

#### Test Failures
1. Run single test in isolation
2. Check test data setup
3. Verify mocks configured correctly
4. Check async/await usage

## Performance Considerations

### Performance Tests (Write First!)
```csharp
[Fact]
public async Task API_ShouldRespondWithin100ms()
{
    var sw = Stopwatch.StartNew();
    var response = await client.GetAsync("/api/health");
    sw.Stop();
    Assert.True(sw.ElapsedMilliseconds < 100);
}
```

### Optimization Process
1. Write performance test with target
2. Run test to see current performance
3. Optimize until test passes
4. Add regression test

## Security Best Practices

### Security Testing (MANDATORY)
```csharp
// Test security BEFORE implementing
[Fact]
public async Task API_ShouldSanitizeInput()
{
    var maliciousInput = "<script>alert('xss')</script>";
    var response = await client.PostAsJsonAsync("/api/text",
        new { text = maliciousInput });
    var content = await response.Content.ReadAsStringAsync();
    Assert.DoesNotContain("<script>", content);
}
```

### Authentication Testing
```csharp
[Fact]
public async Task SecureEndpoint_ShouldRejectInvalidToken()
{
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", "invalid");
    var response = await client.GetAsync("/api/secure");
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

## Deployment

### Pre-Deployment Checklist
- [ ] All tests passing (`dotnet test`)
- [ ] Coverage > 80% verified
- [ ] Performance tests passing
- [ ] Security tests passing
- [ ] Integration tests passing

### Docker Deployment
```dockerfile
# Multi-stage build with tests
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS test
COPY . .
RUN dotnet test # Tests MUST pass for build to continue

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY . .
RUN dotnet publish -c Release
```

## CI/CD Integration

### GitHub Actions Example
```yaml
- name: Test First
  run: |
    dotnet test --no-restore
    if [ $? -ne 0 ]; then
      echo "Tests failed - blocking deployment"
      exit 1
    fi

- name: Check Coverage
  run: |
    dotnet test --collect:"XPlat Code Coverage"
    # Parse and verify > 80%
```

## Quick Reference

### Essential Commands
```bash
# Testing (ALWAYS FIRST!)
dotnet test                          # Run all tests
dotnet watch test                     # TDD watch mode
dotnet test --filter "NameSpace"     # Run specific tests

# Building (AFTER TESTS PASS)
dotnet build                          # Build solution
dotnet run                           # Run application

# Debugging
dotnet test --logger "console;verbosity=detailed"
```

### Key Files
- `/Ai.Orchestrator/launchSettings.json` - Dev config
- `/Ai.Orchestrator/Program.cs` - App entry
- `/**/*.Tests.cs` - Test files (CREATE FIRST!)
- `/docs/TESTING.md` - Testing guide

### Important Patterns
1. **Always test first** - No exceptions
2. **Mock external dependencies** - Use Moq
3. **Test behavior, not implementation**
4. **One assertion per test when possible**
5. **Fast tests** - Mock I/O operations

---

**FINAL REMINDER**: If you write code without a test first, you're doing it wrong. Stop and write the test.