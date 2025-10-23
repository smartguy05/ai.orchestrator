# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 🚨 CRITICAL: MANDATORY TEST-DRIVEN DEVELOPMENT 🚨

**BEFORE WRITING ANY CODE, YOU MUST:**
1. Write a test that fails
2. Run the test suite to confirm failure
3. Document what you're testing

**ONLY THEN:**
4. Write minimal code to pass the test
5. Verify all tests pass
6. Refactor with tests as safety net

**NO EXCEPTIONS. NO SHORTCUTS. NO EXCUSES.**

### VIOLATIONS OF TDD:
- ❌ Writing implementation code before tests
- ❌ Skipping tests because "it's just a small change"
- ❌ Writing tests after implementation is complete
- ❌ Commenting out failing tests to "fix later"

**This is not a suggestion - this is a requirement for all code contributions.**

## Project Overview

Ai.Orchestrator is a modular, plugin-driven orchestration platform for AI agents built with .NET 8. It provides a central controller for automating and coordinating AI-related tasks across multiple domains and services.

## Build and Development Commands

### Build Commands
```bash
# Build the solution
dotnet build

# Build in Release mode
dotnet build -c Release

# Restore dependencies
dotnet restore
```

### Running the Application
```bash
# Run the main orchestrator
cd Ai.Orchestrator
dotnet run

# Run with specific launch settings profile
dotnet run --launch-profile "Ai.Orchestrator"
```

### Testing Commands (RUN THESE FIRST - ALWAYS!)
```bash
# MANDATORY: Run all tests before ANY code changes
dotnet test

# TDD Watch Mode (KEEP RUNNING DURING DEVELOPMENT)
dotnet watch test

# Run tests for a specific project
dotnet test Ai.Orchestrator.Tests/Ai.Orchestrator.Tests.csproj
dotnet test Ai.Orchestrator.Plugins.Tests/Ai.Orchestrator.Plugins.Tests.csproj

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run a specific test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Run tests with code coverage (MINIMUM 80% REQUIRED)
dotnet test --collect:"XPlat Code Coverage"

# Integration Tests (require external dependencies)
# Run ONLY integration tests
dotnet test --filter "Category=Integration"

# Run ONLY unit tests (exclude integration tests)
dotnet test --filter "Category!=Integration"

# Run integration tests for specific dependency (e.g., ChromaDB)
dotnet test --filter "Dependency=ChromaDB"

# CI/CD should exclude integration tests
dotnet test --filter "Category!=Integration"
```

### TDD Workflow for This Project
1. **Write the test FIRST** - Define expected behavior
2. **Run test and watch it FAIL** - Confirm test is valid
3. **Write MINIMUM code** - Just enough to pass
4. **Run test and watch it PASS** - Verify implementation
5. **Refactor** - Improve code with test safety net
6. **Repeat** - For every feature, bug fix, or change

## High-Level Architecture

### Core Components

1. **Plugin System**: The application uses a dynamic plugin architecture where functionality is provided through plugins loaded at runtime.
   - Plugins are loaded from assemblies in the configured PluginDirectory
   - Each plugin has its own configuration JSON file in the ConfigDirectory
   - Plugin lifecycle is managed by `PluginService` with initialization and disposal hooks

2. **Service Layer** (`Ai.Orchestrator.Services`):
   - `PluginService`: Manages plugin loading, initialization, and lifecycle
   - `LoggingService`: Handles application-wide logging with support for multiple logging plugins
   - `NotificationService`: Manages user notifications and confirmation flows
   - `TaskScheduler`: Handles scheduled and recurring task execution
   - `Orchestrator`: Coordinates between plugins and handles request routing

3. **Controller Layer** (`Ai.Orchestrator/Controllers`):
   - `TextController`: Handles text-based AI interactions and conversations
   - `WebHookController`: Processes incoming webhook events
   - `DataController`: Direct plugin testing and data operations
   - `HealthCheckController`: Application health monitoring

4. **Plugin Types**:
   - **Standard Plugins**: Implement tools and functions (Email, OpenAI, Memories, etc.)
   - **Logging Plugins**: Implement `ILoggingPlugin` for custom logging providers
   - **WebHook Plugins**: Implement `IWebHookPlugin` for handling webhook events
   - **Confirmation Plugins**: Implement `IConfirmationPlugin` for user confirmation flows

### Key Design Patterns

1. **Dependency Injection**: Uses ASP.NET Core's built-in DI container with service registration in `MiddlewareRegistration.cs`

2. **Plugin Loading**: Uses `AssemblyLoadContext` for isolated plugin loading with caching to keep instances alive

3. **Tool Registration**: Plugins expose OpenAI-compatible tool definitions through JSON configuration

4. **Async/Await Pattern**: All plugin operations and service calls are asynchronous

5. **Configuration Management**:
   - Main config via environment variables and `launchSettings.json`
   - Plugin configs in separate JSON files per plugin
   - Implements `IConfig` interface for configuration access

### Important Implementation Details

- **Plugin Instance Management**: Plugins are kept alive in `_pluginInstanceCache` to avoid disposal issues
- **Assembly Loading**: Uses custom `PluginLoadContext` for isolated loading and dependency resolution
- **Tool Execution**: Tools are registered with OpenAI function calling format and executed via reflection
- **Memory Systems**: Supports both short-term (Redis) and long-term (ChromaDB) memory stores
- **Mini-Agent System**: Agents can have specific toolsets configured per agent

### Testing Approach (MANDATORY TDD)

**ALL NEW CODE MUST BE TEST-DRIVEN - NO EXCEPTIONS**

The solution uses xUnit as the testing framework with:
- Moq for mocking dependencies
- Microsoft.AspNetCore.Mvc.Testing for integration testing
- Test projects mirror the main project structure
- **Minimum 80% code coverage required**
- **Tests must be written BEFORE implementation**
- **All PRs must include tests or will be rejected**

#### Integration Tests vs Unit Tests

**Unit Tests:**
- Test individual components in isolation
- Use mocks for dependencies
- Run fast, no external dependencies
- Run in CI/CD pipelines

**Integration Tests:**
- Test interaction with external services (ChromaDB, Redis, databases, APIs)
- Require external dependencies to be running
- Use `[Trait("Category", "Integration")]` attribute
- Use `[Trait("Dependency", "ServiceName")]` for specific dependencies
- Run separately from unit tests

**Integration Test Example:**
```csharp
[Fact]
[Trait("Category", "Integration")]
[Trait("Dependency", "ChromaDB")]
public async Task TestConnectionAsync_ShouldConnectToChromaDB()
{
    // Test real ChromaDB connection
    var service = new ChromaService(config, logger);
    var result = await service.TestConnectionAsync();
    Assert.True(result);
}
```

**Running Integration Tests:**
- All integration tests: `dotnet test --filter "Category=Integration"`
- Specific dependency: `dotnet test --filter "Dependency=ChromaDB"`
- Exclude from CI: `dotnet test --filter "Category!=Integration"`

#### Test-First Examples for Common Tasks

```csharp
// STEP 1: Write the test FIRST
[Fact]
public async Task NewFeature_ShouldBehaveAsExpected()
{
    // Arrange
    var service = new MyService();

    // Act
    var result = await service.DoSomethingAsync();

    // Assert
    Assert.NotNull(result);
}

// STEP 2: Run test - it MUST fail
// STEP 3: Implement minimal code to pass
// STEP 4: Refactor with test protection
```

## Configuration

Key environment variables:
- `PluginDirectory`: Directory containing plugin assemblies
- `ConfigDirectory`: Directory containing plugin configuration JSON files
- `ActivePlugins`: Comma-separated list of plugins to load (e.g., "Ai.Orchestrator.Plugins.OpenAI,Ai.Orchestrator.Plugins.Email")

## Development Notes

- The project targets .NET 8.0 with nullable reference types disabled
- Uses implicit usings and global using for xUnit in test projects
- Swagger/OpenAPI is available in DEBUG builds at `/swagger`
- HTTP logging is enabled in DEBUG mode for troubleshooting

## Testing Requirements & Quality Gates

### Pre-Commit Requirements
- [ ] All tests must pass (`dotnet test`)
- [ ] Code coverage must be > 80%
- [ ] No failing tests commented out
- [ ] All new functionality has tests written first

### Pull Request Requirements
- [ ] Tests demonstrate the issue (for bug fixes)
- [ ] Tests validate new behavior (for features)
- [ ] Integration tests for API changes
- [ ] Performance tests for optimization

### CI/CD Quality Gates
- [ ] Build must pass
- [ ] All tests must pass
- [ ] Coverage threshold enforced
- [ ] Security scans pass
- [ ] No critical code quality issues

## Important Files & Locations

### Test Files (CREATE TESTS HERE FIRST!)
- `Ai.Orchestrator.Tests/` - Main application tests
- `Ai.Orchestrator.Plugins.Tests/` - Plugin tests
- Test files should mirror source structure

### Documentation
- `/docs/TESTING.md` - Comprehensive testing guide
- `/docs/CODE_HELP.md` - Development patterns and examples
- `/docs/ARCHITECTURE.md` - System architecture
- `/docs/SECURITY.md` - Security requirements and tests

### Configuration
- `launchSettings.json` - Development configuration
- `Configs/` - Plugin configuration files
- Environment variables for runtime config

---

**FINAL REMINDER**: Write tests first, ALWAYS. No code without failing tests.