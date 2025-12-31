# Project Constitution: Line Notification Service

> This document defines architectural constraints and technical decisions that MUST be followed throughout the project lifecycle.

## Core Principles

### 1. Simplicity First

- **Single Backend Project**: Use one ASP.NET Core Web API project. No microservices.
- **Single Database**: MySQL only. No polyglot persistence.
- **No Message Queue**: Use .NET BackgroundService for async processing. No RabbitMQ/Kafka.
- **Integrated Frontend**: Razor Pages within the same project. No separate SPA framework.

### 2. Technology Stack (Locked)

| Layer | Technology | Version | Rationale |
|-------|------------|---------|-----------|
| Runtime | .NET | 10.x | Latest LTS-track, C# language features |
| Web Framework | ASP.NET Core | 10.x | Built-in with .NET 10 |
| ORM | Entity Framework Core | 10.x | Code First, MySQL provider |
| Database | MySQL | 8.x | User requirement, Docker containerized |
| Authentication | JWT Bearer | - | Stateless API authentication |
| Password Hashing | BCrypt | - | Industry standard |
| Container | Docker/Podman | - | Containerized deployment |

### 3. Prohibited Patterns

The following patterns are **explicitly prohibited** unless justified in `Complexity Tracking`:

- [ ] Microservices architecture
- [ ] Multiple databases or database types
- [ ] Message queues (RabbitMQ, Kafka, Azure Service Bus)
- [ ] Separate frontend deployment (React, Vue, Angular as standalone)
- [ ] Repository pattern abstraction over EF Core
- [ ] CQRS/Event Sourcing
- [ ] GraphQL (use REST only)
- [ ] gRPC for internal communication

### 4. Required Patterns

The following patterns **MUST** be used:

- [x] Dependency Injection (built-in .NET DI)
- [x] Configuration via appsettings.json + environment variables
- [x] Structured logging (Microsoft.Extensions.Logging)
- [x] Health checks endpoint (`/health`)
- [x] OpenAPI documentation (built-in .NET 10)
- [x] Entity Framework Core migrations (Code First)

## Project Boundaries

### Scale Constraints

| Metric | Target | Hard Limit |
|--------|--------|------------|
| Daily Messages | 10,000+ | 100,000 |
| Concurrent Users | 100 | 500 |
| API Response Time (p95) | <500ms | <2s |
| End-to-End Latency | <3s | <10s |
| Data Retention | 90 days | - |

### External Dependencies

| Service | Purpose | Criticality |
|---------|---------|-------------|
| Line Login API | OAuth authentication | High |
| Line Messaging API | Message delivery | Critical |
| MySQL | Data persistence | Critical |

## Change Control

### Adding New Technology

Before adding any new technology or pattern not listed above:

1. Document the specific problem it solves
2. Explain why existing patterns are insufficient
3. Add entry to `Complexity Tracking` in plan.md
4. Get explicit approval

### Exceptions

Exceptions to this constitution require:

1. Clear justification in `Complexity Tracking`
2. Proof that simpler alternatives were evaluated
3. Documentation of the trade-offs accepted

---

*Last Updated: 2025-12-31*
*Version: 1.0*
