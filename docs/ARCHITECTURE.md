# System Architecture - Ai.Orchestrator

## Architecture Overview

Ai.Orchestrator follows a modular, plugin-driven architecture built on .NET 8, designed for extensibility, testability, and scalability.

## System Components Diagram

```mermaid
graph TB
    subgraph "Client Layer"
        A[Web Clients]
        B[API Clients]
        C[Webhook Sources]
    end

    subgraph "API Gateway Layer"
        D[ASP.NET Core API]
        E[Middleware Pipeline]
        F[Authentication]
    end

    subgraph "Controller Layer"
        G[TextController]
        H[WebHookController]
        I[DataController]
        J[HealthCheckController]
    end

    subgraph "Service Layer"
        K[Orchestrator Service]
        L[Plugin Service]
        M[Notification Service]
        N[Task Scheduler]
        O[Logging Service]
    end

    subgraph "Plugin Layer"
        P[Standard Plugins]
        Q[Logging Plugins]
        R[WebHook Plugins]
        S[Confirmation Plugins]
    end

    subgraph "Data Layer"
        T[Redis Cache]
        U[ChromaDB]
        V[Configuration Store]
    end

    subgraph "External Services"
        W[OpenAI API]
        X[Email Services]
        Y[Third-party APIs]
    end

    A --> D
    B --> D
    C --> D
    D --> E
    E --> F
    F --> G
    F --> H
    F --> I
    F --> J
    G --> K
    H --> K
    I --> K
    J --> K
    K --> L
    K --> M
    K --> N
    K --> O
    L --> P
    L --> Q
    L --> R
    L --> S
    P --> T
    P --> U
    P --> V
    P --> W
    P --> X
    P --> Y
```

## Layered Architecture

### 1. Presentation Layer
```
├── Controllers/
│   ├── TextController      # Natural language processing
│   ├── WebHookController   # Webhook event handling
│   ├── DataController      # Direct data operations
│   └── HealthCheckController # System health monitoring
```

**Responsibilities:**
- HTTP request/response handling
- Input validation
- Response formatting
- API versioning
- Rate limiting

### 2. Business Logic Layer
```
├── Services/
│   ├── Orchestrator        # Core orchestration logic
│   ├── PluginService       # Plugin lifecycle management
│   ├── NotificationService # Notification orchestration
│   ├── TaskScheduler       # Background task scheduling
│   └── LoggingService      # Centralized logging
```

**Responsibilities:**
- Business rule implementation
- Workflow orchestration
- Plugin coordination
- Transaction management
- Error handling

### 3. Plugin Layer
```
├── Plugins/
│   ├── IPlugin             # Standard plugin interface
│   ├── ILoggingPlugin      # Logging provider interface
│   ├── IWebHookPlugin      # Webhook handler interface
│   └── IConfirmationPlugin # User confirmation interface
```

**Responsibilities:**
- Feature implementation
- External service integration
- Tool execution
- Custom business logic
- Data transformation

### 4. Data Access Layer
```
├── Repositories/
│   ├── Configuration       # Config data access
│   ├── Memory             # Memory store access
│   └── Persistence        # Persistent storage
```

**Responsibilities:**
- Data persistence
- Query optimization
- Connection management
- Transaction handling
- Caching strategies

## Plugin Architecture

### Plugin Loading Sequence

```mermaid
sequenceDiagram
    participant App as Application
    participant PS as PluginService
    participant PLC as PluginLoadContext
    participant Plugin as Plugin Instance
    participant Config as Configuration

    App->>PS: LoadPluginsAsync()
    PS->>Config: Read ActivePlugins
    loop For Each Plugin
        PS->>PLC: Create LoadContext
        PLC->>PLC: Load Assembly
        PLC->>Plugin: Create Instance
        PS->>Config: Load Plugin Config
        PS->>Plugin: InitializeAsync(config)
        Plugin->>Plugin: Register Tools
        PS->>PS: Cache Instance
    end
    PS->>App: Plugins Loaded
```

### Plugin Execution Flow

```mermaid
sequenceDiagram
    participant Client as Client
    participant Controller as Controller
    participant Orchestrator as Orchestrator
    participant PluginService as PluginService
    participant Plugin as Plugin

    Client->>Controller: API Request
    Controller->>Orchestrator: ProcessRequest()
    Orchestrator->>PluginService: GetPlugin()
    PluginService->>Plugin: ExecuteAsync()
    Plugin->>Plugin: Process
    Plugin->>PluginService: Return Result
    PluginService->>Orchestrator: Return Result
    Orchestrator->>Controller: Return Response
    Controller->>Client: HTTP Response
```

## Service Interactions

### Request Processing Pipeline

```mermaid
graph LR
    A[HTTP Request] --> B[Middleware]
    B --> C{Auth Required?}
    C -->|Yes| D[Authentication]
    C -->|No| E[Controller]
    D --> E
    E --> F[Service Layer]
    F --> G[Plugin Selection]
    G --> H[Plugin Execution]
    H --> I[Response Builder]
    I --> J[HTTP Response]
```

### Notification Flow

```mermaid
graph TB
    A[Event Trigger] --> B[NotificationService]
    B --> C{Confirmation Required?}
    C -->|Yes| D[ConfirmationPlugin]
    C -->|No| E[Send Notification]
    D --> F{User Confirmed?}
    F -->|Yes| E
    F -->|No| G[Cancel Operation]
    E --> H[Log Notification]
```

## Data Flow Architecture

### Memory Management Strategy

```mermaid
graph TB
    A[Request Data] --> B{Cache Check}
    B -->|Hit| C[Return Cached]
    B -->|Miss| D[Process Request]
    D --> E[Plugin Execution]
    E --> F{Cacheable?}
    F -->|Yes| G[Store in Redis]
    F -->|No| H[Return Direct]
    G --> I[Set TTL]
    I --> H
    H --> J[Response]
```

### Persistent Storage Pattern

```mermaid
graph LR
    A[Application] --> B[Repository]
    B --> C{Data Type}
    C -->|Config| D[JSON Files]
    C -->|Vectors| E[ChromaDB]
    C -->|Session| F[Redis]
    C -->|Logs| G[Log Plugins]
```

## Security Architecture

### Authentication & Authorization Flow

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Auth as Auth Middleware
    participant Token as Token Service
    participant Controller

    Client->>API: Request + Token
    API->>Auth: Validate Token
    Auth->>Token: Verify
    Token->>Auth: Claims
    Auth->>Controller: Authorized Request
    Controller->>Controller: Process
    Controller->>Client: Response
```

### Security Layers

```
┌─────────────────────────────────────┐
│         TLS/HTTPS Layer             │
├─────────────────────────────────────┤
│     Authentication Middleware       │
├─────────────────────────────────────┤
│      Authorization Policies         │
├─────────────────────────────────────┤
│        Input Validation            │
├─────────────────────────────────────┤
│      Plugin Sandboxing             │
├─────────────────────────────────────┤
│     Configuration Encryption       │
└─────────────────────────────────────┘
```

## Deployment Architecture

### Container Architecture

```mermaid
graph TB
    subgraph "Docker Host"
        subgraph "Application Container"
            A[Ai.Orchestrator]
            B[Plugin Assemblies]
            C[Configuration]
        end

        subgraph "Redis Container"
            D[Redis Server]
        end

        subgraph "ChromaDB Container"
            E[ChromaDB Server]
        end
    end

    A --> D
    A --> E
    B --> A
    C --> A
```

### Scalability Architecture

```mermaid
graph TB
    subgraph "Load Balancer"
        LB[HAProxy/Nginx]
    end

    subgraph "Application Tier"
        A1[Instance 1]
        A2[Instance 2]
        A3[Instance N]
    end

    subgraph "Cache Tier"
        R[Redis Cluster]
    end

    subgraph "Data Tier"
        C[ChromaDB]
        S[SQL Database]
    end

    LB --> A1
    LB --> A2
    LB --> A3
    A1 --> R
    A2 --> R
    A3 --> R
    A1 --> C
    A2 --> C
    A3 --> C
    A1 --> S
    A2 --> S
    A3 --> S
```

## Communication Patterns

### Synchronous Communication
```
Client -> API -> Controller -> Service -> Plugin -> External Service
                                              ↓
Client <- API <- Controller <- Service <- Response
```

### Asynchronous Communication
```
Client -> API -> Controller -> Service -> Task Queue
   ↓                                          ↓
Response (Task ID)                    Background Worker
   ↓                                          ↓
Polling/Webhook                        Plugin Execution
   ↓                                          ↓
Final Result <------------------------  Complete
```

## Error Handling Architecture

### Error Propagation Flow

```mermaid
graph TD
    A[Plugin Error] --> B[Plugin Service]
    B --> C{Retry?}
    C -->|Yes| D[Retry Logic]
    C -->|No| E[Service Error Handler]
    D --> F{Success?}
    F -->|Yes| G[Continue]
    F -->|No| E
    E --> H[Controller Error Handler]
    H --> I[Global Exception Handler]
    I --> J[Error Response]
    I --> K[Error Logging]
```

## Monitoring Architecture

### Observability Stack

```
┌──────────────────────────────────────┐
│         Application Metrics          │
│     (Performance, Throughput)        │
├──────────────────────────────────────┤
│          Business Metrics            │
│    (Plugin Usage, Success Rate)      │
├──────────────────────────────────────┤
│         System Metrics               │
│      (CPU, Memory, Network)          │
├──────────────────────────────────────┤
│            Log Aggregation           │
│        (Errors, Warnings, Info)      │
├──────────────────────────────────────┤
│            Health Checks             │
│      (Liveness, Readiness)           │
└──────────────────────────────────────┘
```

## Technology Stack Details

### Core Technologies
- **.NET 8.0**: Latest LTS framework
- **ASP.NET Core**: Web API framework
- **C# 12**: Latest language features
- **Entity Framework Core**: Future ORM
- **Docker**: Containerization

### Testing Stack
- **xUnit**: Test framework
- **Moq**: Mocking library
- **TestContainers**: Integration testing
- **BenchmarkDotNet**: Performance testing

### Infrastructure
- **Redis**: Caching and session
- **ChromaDB**: Vector database
- **PostgreSQL**: Future relational DB
- **RabbitMQ/Kafka**: Future message queue

## Performance Architecture

### Caching Strategy

```mermaid
graph TB
    A[Request] --> B{L1 Cache}
    B -->|Hit| C[Return]
    B -->|Miss| D{L2 Cache}
    D -->|Hit| E[Update L1]
    D -->|Miss| F[Compute]
    E --> C
    F --> G[Update L1 & L2]
    G --> C
```

### Optimization Points
1. **Plugin Caching**: Instance reuse
2. **Response Caching**: Idempotent operations
3. **Connection Pooling**: Database and external APIs
4. **Async Operations**: Non-blocking I/O
5. **Lazy Loading**: On-demand plugin loading

## Future Architecture Evolution

### Microservices Migration Path

```mermaid
graph LR
    A[Monolithic] --> B[Modular Monolith]
    B --> C[Service Extraction]
    C --> D[Microservices]

    style A fill:#f9f,stroke:#333,stroke-width:2px
    style B fill:#ff9,stroke:#333,stroke-width:2px
    style D fill:#9f9,stroke:#333,stroke-width:2px
```

### Event-Driven Architecture

```mermaid
graph TB
    A[Event Producer] --> B[Event Bus]
    B --> C[Plugin Service]
    B --> D[Notification Service]
    B --> E[Audit Service]
    B --> F[Analytics Service]
```

## Architecture Decision Records (ADRs)

### ADR-001: Plugin Architecture
- **Decision**: Use dynamic plugin loading
- **Rationale**: Maximum extensibility
- **Consequences**: Complex lifecycle management

### ADR-002: Async Everything
- **Decision**: All operations async
- **Rationale**: Scalability and performance
- **Consequences**: Increased complexity

### ADR-003: TDD Mandatory
- **Decision**: Test-driven development required
- **Rationale**: Quality and maintainability
- **Consequences**: Slower initial development

---

**Note**: All architectural changes must be test-driven. Write tests that validate architectural constraints before implementation.