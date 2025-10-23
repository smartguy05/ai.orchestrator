# Testing Strategy - Ai.Orchestrator

## 🚨 MANDATORY TEST-DRIVEN DEVELOPMENT WORKFLOW 🚨

### **CRITICAL REQUIREMENT: TESTS MUST BE WRITTEN BEFORE IMPLEMENTATION**

**THIS IS NOT OPTIONAL - TDD IS REQUIRED FOR ALL CODE GENERATION**

1. ✅ Write the test FIRST
2. ✅ Run the test and watch it FAIL (RED)
3. ✅ Write the MINIMUM code to make the test pass (GREEN)
4. ✅ Refactor while keeping tests green (REFACTOR)
5. ✅ Repeat

**NO CODE SHOULD BE WRITTEN WITHOUT A FAILING TEST FIRST.**

### Enforcement Rules
- ❌ **NEVER** write implementation code before tests
- ❌ **NEVER** skip tests because "it's just a small change"
- ❌ **NEVER** write tests after implementation is complete
- ❌ **NEVER** comment out failing tests to "fix later"
- ❌ **NEVER** commit code without corresponding tests

## Current Test Coverage Analysis

### Test Projects Structure
```
Ai.Orchestrator.Tests/
├── Controllers/
│   ├── DataControllerTests.cs
│   ├── HealthCheckControllerTests.cs
│   ├── TextControllerTests.cs
│   └── WebHookControllerTests.cs
├── Services/
│   ├── LoggingServiceTests.cs
│   ├── NotificationServiceTests.cs
│   ├── OrchestratorTests.cs
│   ├── PluginServiceTests.cs
│   ├── ServiceResolverTests.cs
│   └── TaskSchedulerTests.cs
└── Helpers/
    └── TestHelpers.cs

Ai.Orchestrator.Plugins.Tests/
├── Email/
│   └── EmailCommandTests.cs
├── Memories/
│   └── ChromaServiceTests.cs
├── OpenAi/
│   ├── ChatServiceTests.cs
│   └── ChatCompletionResponseTests.cs
└── Webhook/
    └── WebhookTests.cs
```

### Coverage Gaps - CRITICAL PRIORITY

#### Controllers (Partial Coverage)
- **TextController**: Basic tests exist, needs error handling tests
- **WebHookController**: Missing authentication tests
- **DataController**: Needs negative test cases
- **HealthCheckController**: Minimal coverage

#### Services (Good Coverage)
- **PluginService**: Comprehensive tests added
- **Orchestrator**: Good coverage with mocking
- **NotificationService**: Well-tested async flows
- **LoggingService**: Basic coverage, needs plugin tests
- **TaskScheduler**: Scheduling logic tested

#### Plugins (Needs Expansion)
- **Email Plugin**: Basic functionality tested
- **OpenAI Plugin**: Response parsing tested
- **Memories Plugin**: ChromaDB integration needs tests
- **Webhook Plugin**: Basic validation tested

### Required Tests Before Any New Development

#### PRIORITY 1: Controller Integration Tests
```csharp
// MUST WRITE FIRST before any controller changes
[Fact]
public async Task Controller_ShouldHandleInvalidInput()
{
    // Arrange
    var invalidRequest = new { /* invalid data */ };

    // Act
    var response = await client.PostAsJsonAsync("/api/endpoint", invalidRequest);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
```

#### PRIORITY 2: Plugin Loading Tests
```csharp
// MUST WRITE FIRST before plugin system changes
[Fact]
public void PluginService_ShouldHandleMissingPlugin()
{
    // Test MUST be written before implementing error handling
    Assert.Throws<PluginNotFoundException>(() =>
        pluginService.LoadPlugin("NonExistent"));
}
```

#### PRIORITY 3: Security Tests
```csharp
// MUST WRITE FIRST before security implementations
[Fact]
public async Task API_ShouldRequireAuthentication()
{
    // Write this test BEFORE adding auth
    var response = await unauthorizedClient.GetAsync("/api/secure");
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

## TDD Implementation Roadmap

### Phase 1: Test Infrastructure (IMMEDIATE)
1. **Setup Test Fixtures**
   - Write fixture tests first
   - Implement shared test contexts
   - Add test data builders

2. **Mock Frameworks**
   - Write tests for mock behavior
   - Implement mock factories
   - Create test doubles

### Phase 2: Unit Test Expansion (WEEK 1)
1. **Write failing tests for**:
   - All public methods without coverage
   - Edge cases and error conditions
   - Async operation handling

2. **Then implement**:
   - Minimum code to pass tests
   - Refactor with test safety net

### Phase 3: Integration Tests (WEEK 2)
1. **Write failing integration tests**:
   - API endpoint integration
   - Plugin interaction tests
   - Database integration tests

2. **Implement integration points**:
   - Only after tests are red
   - Verify with green tests

## Testing Framework & Tools

### Current Stack
- **Framework**: xUnit 2.4.2
- **Mocking**: Moq 4.20.72
- **Test Host**: Microsoft.AspNetCore.Mvc.Testing 8.0.11
- **Coverage**: dotnet test --collect:"XPlat Code Coverage"

### Test Execution Commands

```bash
# ALWAYS RUN TESTS FIRST!

# Run all tests (DO THIS BEFORE ANY COMMIT)
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test Ai.Orchestrator.Tests/Ai.Orchestrator.Tests.csproj

# Run specific test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Watch mode for TDD (RECOMMENDED)
dotnet watch test
```

## Test-First Development Examples

### Example 1: Adding a New Service Method

```csharp
// STEP 1: Write the test FIRST
[Fact]
public async Task NewService_ShouldProcessDataCorrectly()
{
    // Arrange
    var service = new MyService();
    var input = "test data";
    var expected = "processed test data";

    // Act
    var result = await service.ProcessAsync(input);

    // Assert
    Assert.Equal(expected, result);
}

// STEP 2: Run test - it MUST fail (compilation error is a failure)

// STEP 3: Write MINIMUM implementation
public class MyService
{
    public async Task<string> ProcessAsync(string input)
    {
        return await Task.FromResult($"processed {input}");
    }
}

// STEP 4: Run test - it should pass

// STEP 5: Refactor if needed
```

### Example 2: Adding a New Plugin

```csharp
// STEP 1: Write plugin interface test FIRST
[Fact]
public void Plugin_ShouldImplementIPlugin()
{
    // This test MUST be written before creating the plugin
    var plugin = new MyNewPlugin();
    Assert.IsAssignableFrom<IPlugin>(plugin);
}

// STEP 2: Write functionality test
[Fact]
public async Task Plugin_ShouldExecuteTool()
{
    // Write before implementing ExecuteAsync
    var plugin = new MyNewPlugin();
    var result = await plugin.ExecuteAsync("tool", new {});
    Assert.NotNull(result);
}

// ONLY THEN implement the plugin
```

## Quality Gates & Enforcement

### Pre-Commit Hooks (MANDATORY)
```bash
#!/bin/bash
# .git/hooks/pre-commit

# Run tests before allowing commit
dotnet test
if [ $? -ne 0 ]; then
    echo "❌ Tests failed. Commit blocked."
    echo "Fix failing tests before committing."
    exit 1
fi

# Check coverage
coverage=$(dotnet test --collect:"XPlat Code Coverage" | grep -oP '(?<=Line coverage: )\d+')
if [ $coverage -lt 80 ]; then
    echo "❌ Coverage below 80%. Write more tests."
    exit 1
fi
```

### CI/CD Pipeline Requirements
```yaml
# All builds MUST include:
- name: Run Tests
  run: |
    dotnet test --no-restore --verbosity normal
    if [ $? -ne 0 ]; then
      echo "Build failed: Tests not passing"
      exit 1
    fi

- name: Check Coverage
  run: |
    dotnet test --collect:"XPlat Code Coverage"
    # Fail if coverage < 80%
```

### Pull Request Checklist
- [ ] All new code has tests written FIRST
- [ ] Tests were RED before implementation
- [ ] Tests are now GREEN
- [ ] No tests were skipped or commented out
- [ ] Coverage increased or maintained
- [ ] Integration tests added for new features

## Test Categories & Organization

### Unit Tests
- **Naming**: `ClassName_MethodName_ExpectedBehavior`
- **Location**: Mirror source structure
- **Scope**: Single class/method
- **Dependencies**: All mocked

### Integration Tests
- **Naming**: `Feature_Scenario_ExpectedOutcome`
- **Location**: `*.IntegrationTests` projects
- **Scope**: Multiple components
- **Dependencies**: Real where possible

### E2E Tests
- **Naming**: `UserFlow_Action_Result`
- **Location**: `*.E2ETests` projects
- **Scope**: Complete workflows
- **Dependencies**: Full stack

## Mock Strategies

### Plugin Mocking
```csharp
// Always mock plugins in unit tests
var mockPlugin = new Mock<IPlugin>();
mockPlugin.Setup(p => p.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>()))
          .ReturnsAsync(new PluginResult());
```

### Service Mocking
```csharp
// Mock external dependencies
var mockLogger = new Mock<ILoggerService>();
var mockNotifier = new Mock<INotificationService>();
```

### Data Mocking
```csharp
// Use builders for test data
var testData = new TestDataBuilder()
    .WithValidConfig()
    .WithPlugins(3)
    .Build();
```

## Testing Anti-Patterns to Avoid

### ❌ DON'T: Write Tests After Code
```csharp
// WRONG: Implementation exists, test written after
public class Service
{
    public int Calculate() => 42; // Code written first
}

[Fact]
public void Test() // Test written after
{
    Assert.Equal(42, new Service().Calculate());
}
```

### ✅ DO: Write Tests First
```csharp
// RIGHT: Test first, then implementation
[Fact]
public void Service_Calculate_Returns42() // Test written first
{
    Assert.Equal(42, new Service().Calculate());
}
// THEN implement Service
```

### ❌ DON'T: Test Implementation Details
```csharp
// WRONG: Testing private methods
[Fact]
public void TestPrivateMethod()
{
    // Don't test internals
}
```

### ✅ DO: Test Public Behavior
```csharp
// RIGHT: Test public API
[Fact]
public void PublicMethod_WhenCalled_ProducesExpectedResult()
{
    // Test behavior, not implementation
}
```

## Performance Testing

### Load Testing Requirements
- Test MUST be written before optimization
- Baseline performance MUST be established
- Performance regression tests MUST exist

### Example Performance Test
```csharp
[Fact]
public async Task API_ShouldHandleLoad()
{
    // Write this BEFORE optimizing
    var tasks = Enumerable.Range(0, 100)
        .Select(_ => client.GetAsync("/api/health"));

    var responses = await Task.WhenAll(tasks);
    Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
}
```

## Test Data Management

### Test Database
- In-memory database for unit tests
- Docker container for integration tests
- Separate test data per test

### Test Configuration
```json
{
  "TestSettings": {
    "UseInMemoryDatabase": true,
    "MockExternalServices": true,
    "TestDataPath": "./TestData"
  }
}
```

## Coverage Requirements

### Minimum Coverage Thresholds (ENFORCED)
- **Overall**: 80% minimum
- **Business Logic**: 95% minimum
- **Controllers**: 85% minimum
- **Services**: 90% minimum
- **Plugins**: 85% minimum

### Coverage Exceptions (MUST BE DOCUMENTED)
- Auto-generated code
- Configuration classes
- DTOs/Models (if no logic)

## Continuous Testing

### Development Workflow
1. **Write failing test**
2. **Run test to confirm failure**
3. **Write minimum code**
4. **Run test to confirm pass**
5. **Refactor with confidence**
6. **Commit only when all tests pass**

### Watch Mode for TDD
```bash
# Keep this running during development
dotnet watch test

# It will:
# - Run tests on file changes
# - Show failures immediately
# - Keep you in TDD flow
```

## Test Documentation

### Each Test Must Have
- Clear naming indicating what is tested
- Arrange-Act-Assert structure
- Comments for complex scenarios
- Links to requirements/issues

### Test Documentation Example
```csharp
/// <summary>
/// Tests that plugin service correctly handles missing plugins
/// Requirement: #123 - Graceful plugin failure handling
/// </summary>
[Fact]
public void PluginService_WhenPluginMissing_ThrowsPluginNotFoundException()
{
    // Arrange: Setup service without plugin
    var service = new PluginService();

    // Act & Assert: Verify exception
    Assert.Throws<PluginNotFoundException>(() =>
        service.LoadPlugin("NonExistent"));
}
```

## Remember: NO EXCEPTIONS TO TDD

**Every single line of production code MUST have a failing test written first. This is not a suggestion, it's a requirement.**