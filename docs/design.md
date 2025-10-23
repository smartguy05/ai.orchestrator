# Technical Design Document - Ai.Orchestrator

## System Overview

Ai.Orchestrator is a modular, plugin-driven orchestration platform for AI agents built with .NET 8. It provides a central controller for automating and coordinating AI-related tasks across multiple domains and services through a flexible plugin architecture.

## Architecture Overview

### High-Level Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                        API Layer                              │
├──────────────────────────────────────────────────────────────┤
│  TextController  │  WebHookController  │  DataController     │
│                  │                      │  HealthCheckController│
├──────────────────────────────────────────────────────────────┤
│                     Service Layer                             │
├──────────────────────────────────────────────────────────────┤
│  Orchestrator    │  PluginService      │  LoggingService     │
│  TaskScheduler   │  NotificationService │  ServiceResolver   │
├──────────────────────────────────────────────────────────────┤
│                      Plugin Layer                             │
├──────────────────────────────────────────────────────────────┤
│  Standard        │  Logging            │  WebHook            │
│  Plugins         │  Plugins            │  Plugins            │
│  (Email, OpenAI) │  (ILoggingPlugin)   │  (IWebHookPlugin)   │
├──────────────────────────────────────────────────────────────┤
│                    Infrastructure                             │
├──────────────────────────────────────────────────────────────┤
│  Redis           │  ChromaDB           │  External APIs      │
└──────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. Plugin System Architecture

The plugin system is the heart of the Ai.Orchestrator, providing dynamic loading and execution of functionality.

#### Plugin Loading Mechanism
- **AssemblyLoadContext**: Custom `PluginLoadContext` for isolated assembly loading
- **Dependency Resolution**: Automatic resolution of plugin dependencies
- **Instance Caching**: Plugin instances kept alive in `_pluginInstanceCache` to prevent disposal issues
- **Configuration**: Each plugin has its own JSON configuration file in the ConfigDirectory

#### Plugin Types
1. **Standard Plugins**: Implement `IPlugin` interface for general functionality
2. **Logging Plugins**: Implement `ILoggingPlugin` for custom logging providers
3. **WebHook Plugins**: Implement `IWebHookPlugin` for webhook event handling
4. **Confirmation Plugins**: Implement `IConfirmationPlugin` for user confirmation flows

### 2. Service Layer

#### Orchestrator Service
- **Purpose**: Central coordination point for all plugin interactions
- **Responsibilities**:
  - Request routing to appropriate plugins
  - Tool execution management
  - Response aggregation
  - Error handling and recovery

#### PluginService
- **Purpose**: Manages plugin lifecycle
- **Key Features**:
  - Dynamic plugin discovery and loading
  - Plugin initialization and disposal
  - Tool registration and management
  - Reflection-based method invocation

#### LoggingService
- **Purpose**: Centralized logging with plugin support
- **Features**:
  - Multiple logging provider support
  - Plugin-based logging extensions
  - Structured logging support
  - Log level configuration

#### NotificationService
- **Purpose**: User notification management
- **Features**:
  - Multi-channel notifications
  - Confirmation flow management
  - Plugin-based notification providers
  - Async notification handling

#### TaskScheduler
- **Purpose**: Background task management
- **Features**:
  - Scheduled task execution
  - Recurring task support
  - Task priority management
  - Error handling and retry logic

### 3. Controller Layer

#### TextController
- **Endpoint**: `/api/text`
- **Purpose**: Handle text-based AI interactions
- **Features**:
  - Natural language processing
  - Tool execution based on text commands
  - Conversation context management

#### WebHookController
- **Endpoint**: `/api/webhook`
- **Purpose**: Process incoming webhook events
- **Features**:
  - Plugin-based webhook routing
  - Event validation
  - Async webhook processing

#### DataController
- **Endpoint**: `/api/data`
- **Purpose**: Direct plugin data operations
- **Features**:
  - Plugin testing interface
  - Direct data manipulation
  - Debug and development support

#### HealthCheckController
- **Endpoint**: `/health`
- **Purpose**: Application health monitoring
- **Features**:
  - Service availability checks
  - Plugin health status
  - System resource monitoring

## Technology Stack

### Core Technologies
- **Framework**: .NET 8.0
- **Web Framework**: ASP.NET Core
- **Language**: C# (nullable reference types disabled)
- **Testing**: xUnit, Moq, Microsoft.AspNetCore.Mvc.Testing
- **API Documentation**: Swagger/OpenAPI (DEBUG builds only)

### Infrastructure Dependencies
- **Memory Store (Short-term)**: Redis
- **Memory Store (Long-term)**: ChromaDB
- **Container**: Docker support with multi-stage builds
- **Configuration**: JSON-based configuration files

## Design Patterns

### 1. Dependency Injection
- ASP.NET Core's built-in DI container
- Service registration in `MiddlewareRegistration.cs`
- Scoped, Singleton, and Transient service lifetimes

### 2. Plugin Pattern
- Dynamic loading of functionality
- Interface-based contracts
- Configuration-driven behavior

### 3. Async/Await
- All plugin operations are asynchronous
- Non-blocking I/O operations
- Task-based asynchronous pattern (TAP)

### 4. Repository Pattern (via Plugins)
- Data access abstracted through plugins
- Multiple data source support
- Consistent data access interface

### 5. Factory Pattern
- Plugin instance creation
- Service resolution
- Configuration object creation

## Data Flow

### Request Processing Flow
1. HTTP Request received by Controller
2. Controller delegates to appropriate Service
3. Service loads and invokes relevant Plugins
4. Plugins execute business logic
5. Results aggregated and returned
6. Response sent to client

### Plugin Execution Flow
1. Plugin discovery in configured directory
2. Assembly loading via PluginLoadContext
3. Configuration loading from JSON file
4. Plugin initialization
5. Tool registration with OpenAI format
6. Method invocation via reflection
7. Result serialization and return

## Configuration Architecture

### Environment Configuration
- Primary configuration via environment variables
- Launch settings for development profiles
- Override hierarchy: Environment > LaunchSettings > Defaults

### Plugin Configuration
- Individual JSON files per plugin
- Located in ConfigDirectory
- Dynamic configuration reloading support
- Schema validation for plugin configs

### Key Configuration Parameters
- `PluginDirectory`: Plugin assembly location
- `ConfigDirectory`: Plugin configuration location
- `ActivePlugins`: Comma-separated list of plugins to load
- `LogLevel`: Application logging verbosity

## OpenAI Integration

### Tool Registration
- Plugins expose tools in OpenAI function calling format
- JSON schema-based tool definitions
- Parameter validation and type conversion
- Async tool execution support

### Mini-Agent System
- Per-agent tool configuration
- Agent-specific context management
- Tool restriction by agent
- Conversation history per agent

## Memory Architecture

### Short-term Memory (Redis)
- Session-based storage
- Fast access for recent context
- TTL-based expiration
- Conversation state management

### Long-term Memory (ChromaDB)
- Persistent storage
- Vector-based similarity search
- Knowledge base integration
- Historical context retrieval

## Security Considerations

### Authentication & Authorization
- Plugin-based authentication providers
- Role-based access control (RBAC)
- API key management for external services
- Token-based authentication support

### Data Protection
- Configuration encryption for sensitive data
- Secure credential storage
- HTTPS enforcement in production
- Input validation and sanitization

### Plugin Security
- Isolated assembly loading contexts
- Permission-based plugin execution
- Sandboxed plugin environments
- Resource usage limitations

## Performance Optimizations

### Caching Strategy
- Plugin instance caching
- Configuration caching
- Response caching for idempotent operations
- Distributed caching with Redis

### Async Processing
- Non-blocking I/O operations
- Parallel plugin execution where applicable
- Background task processing
- Connection pooling for external services

### Resource Management
- Proper disposal of plugin resources
- Memory leak prevention through instance caching
- Connection pool management
- Garbage collection optimization

## Scalability Design

### Horizontal Scaling
- Stateless service design
- Session state in Redis
- Load balancer compatibility
- Distributed task processing

### Vertical Scaling
- Async operations for CPU efficiency
- Memory-efficient plugin loading
- Resource pooling
- Lazy loading of plugins

## Error Handling

### Global Error Handling
- Centralized exception middleware
- Structured error responses
- Error logging and tracking
- Graceful degradation

### Plugin Error Isolation
- Plugin failures don't crash the system
- Error boundaries for plugin execution
- Retry logic for transient failures
- Fallback mechanisms

## Monitoring & Observability

### Logging
- Structured logging with correlation IDs
- Multiple log providers via plugins
- Log aggregation support
- Debug logging in development

### Metrics
- Performance counters
- Plugin execution metrics
- API endpoint metrics
- Resource usage tracking

### Health Checks
- Liveness probes
- Readiness probes
- Plugin health status
- Dependency health checks

## Development Workflow

### Local Development
- Docker Compose for dependencies
- Launch profiles for different configurations
- Hot reload support
- Swagger UI for API testing

### Testing Strategy
- Unit tests for business logic
- Integration tests for plugin interactions
- End-to-end tests for API workflows
- Performance testing for scalability

### Deployment
- Docker containerization
- Multi-stage builds for optimization
- Environment-based configuration
- CI/CD pipeline support