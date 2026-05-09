# TuanTranCodeLeap Architecture

## System Overview

```mermaid
graph TB
    subgraph "Client"
        WEB[Web Browser / API Consumer]
    end

    subgraph "API Layer (TuanTranCodeLeap.API)"
        CTRL[Controllers]
        SWG[Swagger / OpenAPI]
        MIDDLE[GlobalExceptionMiddleware]
        API_RESP[ApiResponse<T>]
    end

    subgraph "Application Layer (TuanTranCodeLeap.Application)"
        MEDIATR[MediatR]
        CMD[Commands]
        QRY[Queries]
        HANDLERS[Command/Query Handlers]
        VAL[FluentValidation]
        DTO[DTOs]
        EXC[Custom Exceptions]
        AUTH[Auth Handlers]
    end

    subgraph "Domain Layer (TuanTranCodeLeap.Domain)"
        ENT[Entities]
        REPO_I[Repository Interfaces]
        CONST[Constants]
    end

    subgraph "Infrastructure Layer (TuanTranCodeLeap.Infrastructure)"
        DB[ApplicationDbContext]
        REPO_IMPL[Repository Implementations]
        JWT[JwtService]
        SEED[DataSeeder]
        IDENTITY[ASP.NET Identity]
    end

    subgraph "External Systems"
        SQL[(SQL Server)]
        CACHE[(Memory Cache)]
        LOG[(Serilog Logs)]
    end

    WEB -->|HTTP Request| CTRL
    CTRL -->|Returns| API_RESP
    CTRL -->|Sends| MEDIATR
    SWG -->|Documents| CTRL
    MIDDLE -->|Catches Errors| CTRL

    MEDIATR -->|Validates| VAL
    MEDIATR -->|Dispatches| HANDLERS
    HANDLERS -->|Uses| REPO_I
    HANDLERS -->|Throws| EXC
    CMD --> MEDIATR
    QRY --> MEDIATR
    DTO --> CTRL
    DTO --> HANDLERS
    AUTH -->|Generates| JWT

    REPO_I -.->|Implemented by| REPO_IMPL
    REPO_IMPL -->|Uses| DB
    DB -->|Connects to| SQL
    DB -->|Configured by| IDENTITY
    DB -->|Seeded by| SEED
    JWT -->|Signs| CACHE
    CTRL -->|Logs to| LOG
    HANDLERS -->|Logs to| LOG
```

---

## Layer Responsibilities

| Layer | Project | Responsibility |
|-------|---------|---------------|
| **API** | `TuanTranCodeLeap.API` | HTTP endpoints, middleware, DI registration, Swagger docs |
| **Application** | `TuanTranCodeLeap.Application` | Business logic via CQRS handlers, validation, DTOs, auth commands |
| **Domain** | `TuanTranCodeLeap.Domain` | Entities, repository contracts, business rules/constants |
| **Infrastructure** | `TuanTranCodeLeap.Infrastructure` | Data access, EF Core, Identity, JWT implementation, seeding |

---

## Dependency Direction

```mermaid
graph LR
    API[TuanTranCodeLeap.API] --> APP[TuanTranCodeLeap.Application]
    APP --> DOM[TuanTranCodeLeap.Domain]
    INFRA[TuanTranCodeLeap.Infrastructure] --> APP
    INFRA --> DOM
    API --> INFRA

    style DOM fill:#e1f5fe
    style APP fill:#fff3e0
    style INFRA fill:#e8f5e9
    style API fill:#fce4ec
```

**Rule**: Dependencies point inward. Domain has zero external dependencies.

---

## Request Flow (Create Product Example)

```mermaid
sequenceDiagram
    participant C as Client
    participant PC as ProductsController
    participant M as MediatR
    participant V as CreateProductValidator
    participant H as CreateProductHandler
    participant R as ProductRepository
    participant DB as EF Core / SQL Server

    C->>PC: POST /api/products {JSON}
    PC->>M: Send(CreateProductCommand)
    
    M->>V: Validate(command)
    
    alt Validation Fails
        V-->>M: ValidationException
        M-->>PC: 400 Bad Request
        PC-->>C: ApiResponse with errors
    else Validation Passes
        V-->>M: Valid
        M->>H: Handle(command)
        H->>R: CheckDuplicateSku()
        R->>DB: SELECT WHERE SKU = ?
        DB-->>R: No results
        R-->>H: No duplicate
        H->>R: Add(product)
        R->>DB: INSERT INTO Products
        DB-->>R: Success
        R-->>H: Product entity
        H-->>M: ProductDto
        M-->>PC: ProductDto
        PC-->>C: 201 Created + ApiResponse
    end
```

---

## Authentication Flow

```mermaid
sequenceDiagram
    participant C as Client
    participant TC as TokenController
    participant M as MediatR
    participant H as LoginCommandHandler
    participant UM as UserManager
    participant SM as SignInManager
    participant JS as JwtService
    participant DB as Identity DB

    C->>TC: POST /api/token/login {email, password}
    TC->>M: Send(LoginCommand)
    M->>H: Handle(command)
    H->>UM: FindByEmailAsync(email)
    UM->>DB: SELECT FROM Users
    DB-->>UM: User record
    UM-->>H: User
    H->>SM: CheckPasswordSignInAsync(user, password)
    SM->>DB: Verify password hash
    DB-->>SM: Valid
    SM-->>H: Succeeded
    H->>JS: GenerateToken(user, roles)
    JS-->>H: JWT token string
    H-->>M: AuthResponseDto
    M-->>TC: AuthResponseDto
    TC-->>C: 200 OK {token, email, roles}

    Note over C,DB: Subsequent requests include token in Authorization: Bearer header
```

---

## Database Schema

```mermaid
erDiagram
    USERS ||--o{ USER_ROLES : has
    ROLES ||--o{ USER_ROLES : assigned_to
    USERS ||--o{ REFRESH_TOKENS : owns
    USERS ||--o{ USER_CLAIMS : has
    ROLES ||--o{ ROLE_CLAIMS : has
    USERS ||--o{ USER_LOGINS : has
    USERS ||--o{ USER_TOKENS : has

    USERS {
        int Id PK
        string UserName UK
        string Email UK
        string FirstName
        string LastName
        string PasswordHash
        datetime CreatedAt
    }

    ROLES {
        int Id PK
        string Name UK
        string NormalizedName
    }

    USER_ROLES {
        int UserId FK
        int RoleId FK
    }

    REFRESH_TOKENS {
        int Id PK
        int UserId FK
        string Token UK
        datetime ExpiresAt
        bool IsRevoked
    }

    REVOKED_TOKENS {
        int Id PK
        string Jti UK
        datetime RevokedAt
    }

    PRODUCTS {
        int Id PK
        string Name
        string SKU UK
        string Description
        decimal Price
        int StockQuantity
        bool IsActive
        datetime CreatedAt
        datetime UpdatedAt
    }
```

---

## Test Architecture

```mermaid
graph TB
    subgraph "Unit Tests (Mocked)"
        UT1[CreateProductCommandHandlerTests]
        UT2[GetProductsQueryHandlerTests]
        UT3[GetProductByIdQueryHandlerTests]
        UT4[UpdateProductCommandHandlerTests]
        UT5[DeleteProductCommandHandlerTests]
        UT6[ValidatorsTests]
    end

    subgraph "Integration Tests (Real Pipeline)"
        IT1[TokenControllerIntegrationTests]
        IT2[ProductsControllerIntegrationTests]
    end

    subgraph "Test Infrastructure"
        MOCK[Moq]
        FA[FluentAssertions]
        FV[FluentValidation.TestHelper]
        FACT[CustomWebApplicationFactory]
    end

    subgraph "Real Dependencies Under Test"
        API[ASP.NET Core Host]
        MED[MediatR + Validators]
        DB[(In-Memory DB)]
        JWT[JWT Service]
        CONT[Controllers]
    end

    UT1 --> MOCK
    UT1 --> FA
    UT2 --> MOCK
    UT3 --> MOCK
    UT4 --> MOCK
    UT5 --> MOCK
    UT6 --> FV
    UT6 --> FA

    IT1 --> FACT
    IT2 --> FACT
    FACT --> API
    API --> CONT
    API --> MED
    API --> DB
    API --> JWT
```

---

## CQRS Pattern Detail

```mermaid
graph LR
    subgraph "Write Side (Commands)"
        C1[CreateProductCommand]
        C2[UpdateProductCommand]
        C3[DeleteProductCommand]
        H1[CreateProductHandler]
        H2[UpdateProductHandler]
        H3[DeleteProductHandler]
    end

    subgraph "Read Side (Queries)"
        Q1[GetProductsQuery]
        Q2[GetProductByIdQuery]
        Q3[SearchProductsQuery]
        HQ1[GetProductsHandler]
        HQ2[GetProductByIdHandler]
        HQ3[SearchProductsHandler]
    end

    subgraph "Shared Infrastructure"
        REPO[Repositories]
        CACHE[Output Cache]
    end

    C1 --> H1 --> REPO
    C2 --> H2 --> REPO
    C3 --> H3 --> REPO

    Q1 --> HQ1 --> REPO
    Q2 --> HQ2 --> REPO
    Q3 --> HQ3 --> REPO

    HQ1 --> CACHE
    HQ2 --> CACHE
    HQ3 --> CACHE

    C1 -.->|Invalidates| CACHE
    C2 -.->|Invalidates| CACHE
    C3 -.->|Invalidates| CACHE
```

---

## File Locations

| Diagram | File |
|---------|------|
| System Overview | `ARCHITECTURE.md` (this file) |
| High-Level Architecture | `README.md` > `## Architecture` |
| Test Architecture | `README.md` > `### Integration Test Architecture` |
