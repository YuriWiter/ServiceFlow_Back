# ServiceFlow Backend

ASP.NET Core 10 Web API that powers the ServiceFlow workshop-management app. It implements every business rule server-side (stage transitions, approval flow, validations) so that frontends and third-party clients can never bypass them.

## Stack

- **.NET 10** (C# 14) — [SDK 10.0.203](https://dotnet.microsoft.com/download/dotnet/10.0)
- **ASP.NET Core Minimal APIs** with endpoint grouping, built-in OpenAPI and [Scalar](https://github.com/scalar/scalar) for docs UI
- **Entity Framework Core 10** + **Npgsql** for PostgreSQL
- **FluentValidation** for request validation, centralized via DI
- **BCrypt.Net-Next** for password hashing, **System.IdentityModel.Tokens.Jwt** for access tokens
- **Serilog** for structured logging, **xUnit** + **FluentAssertions** for tests

## Architecture

The solution follows Clean Architecture. Dependencies flow inward only:

```
┌────────────────────────┐
│ ServiceFlow.Api        │  Minimal API endpoints, DI, auth, OpenAPI, error handling
└────────────┬───────────┘
             │
┌────────────▼───────────┐
│ ServiceFlow.Application│  Use-case services (ICustomerService, IServiceOrderService, …),
│                        │  DTOs, FluentValidation validators, abstractions
└────────────┬───────────┘
             │
┌────────────▼───────────┐   ┌──────────────────────────────┐
│ ServiceFlow.Domain     │◄──┤ ServiceFlow.Infrastructure   │
│ Aggregates, enums,     │   │ EF Core DbContext + migs,    │
│ invariants, state      │   │ PostgreSQL, JWT, BCrypt,     │
│ machine                │   │ clock, seeding               │
└────────────────────────┘   └──────────────────────────────┘
```

- `ServiceFlow.Domain` — pure C# with no external dependencies. All invariants raise `DomainException` with a stable error code.
- `ServiceFlow.Application` — exposes interfaces like `ICustomerService`, `IServiceOrderService`. Implementations validate input, call the domain, persist via `IAppDbContext`.
- `ServiceFlow.Infrastructure` — EF Core + Npgsql (`AppDbContext`), BCrypt hasher, JWT generator, system clock, seed initializer.
- `ServiceFlow.Api` — minimal API endpoints grouped by resource, global exception handler that emits RFC 7807 `ProblemDetails`, Serilog, CORS, JWT bearer auth, built-in OpenAPI + Scalar UI at `/docs`.

## Domain model

```
User ──┐
       │ (optional 1:1, customer portal login)
Customer ──< Vehicle ──< ServiceOrder ──< RepairRequest ──< RepairMedia
                              └──< ServiceStatusHistoryEntry
```

**ServiceStage lifecycle** (enforced in `ServiceStageTransitions`):

```
awaiting_pickup → picked_up → in_service → waiting_customer_approval
   → approved ┐
              ├── → in_cleaning → ready_for_delivery → completed
   → rejected ┘
```

Key invariants:

- Stages cannot be skipped or rolled back.
- A service order cannot leave `waiting_customer_approval` while any `RepairRequest` is undecided.
- A customer decision requires at least one `RepairMedia` attached as proof.
- Repair estimates become immutable after the customer decides.
- License plates and emails are unique.

All of these are enforced both by the domain entities (unit-tested) and by EF Core unique indexes.

## Running it locally

### 1. Start PostgreSQL

```bash
docker compose up -d postgres
```

This brings up PostgreSQL 16 on `localhost:5432` (db/user/password all `serviceflow`) and optionally pgAdmin at <http://localhost:5050>.

### 2. Configure secrets (dev-only defaults ship in `appsettings.Development.json`)

```bash
cd src/ServiceFlow.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "replace-with-a-very-long-random-string-at-least-32-chars"
dotnet user-secrets set "Seed:AdminEmail" "admin@serviceflow.local"
dotnet user-secrets set "Seed:AdminPassword" "ChangeMe!123"
```

Omit the seed keys to skip automatic admin creation.

### 3. Run the API

```bash
dotnet run --project src/ServiceFlow.Api
```

The API auto-migrates on startup (`Database:AutoMigrate=true`) and seeds the admin when `Seed:AdminEmail` / `Seed:AdminPassword` are set.

- API: <http://localhost:5066>
- OpenAPI document: <http://localhost:5066/openapi/v1.json>
- Scalar UI: <http://localhost:5066/docs>
- Liveness: <http://localhost:5066/api/v1/health/live>

### 4. Tests

```bash
dotnet test
```

- `ServiceFlow.Domain.Tests` — state-machine, entity invariants, guards.
- `ServiceFlow.Application.Tests` — in-memory DbContext + validators exercise full use cases.

## Database migrations

```bash
# create new migration
dotnet ef migrations add <Name> \
  --project src/ServiceFlow.Infrastructure \
  --startup-project src/ServiceFlow.Api \
  --output-dir Persistence/Migrations

# apply pending migrations (normally done automatically on startup)
dotnet ef database update \
  --project src/ServiceFlow.Infrastructure \
  --startup-project src/ServiceFlow.Api
```

Tables live in the `serviceflow` schema. Snake-case naming is applied automatically via `EFCore.NamingConventions`.

## REST surface

All routes are prefixed with `/api/v1`.

| Method | Path                                         | Auth           | Purpose |
| ------ | -------------------------------------------- | -------------- | ------- |
| POST   | `/auth/register/customer`                    | public         | Create a customer account + portal login |
| POST   | `/auth/register/staff`                       | Admin          | Create a new staff/admin account |
| POST   | `/auth/login`                                | public         | Exchange email + password for a JWT |
| GET    | `/customers?search=&page=&pageSize=`         | Staff/Admin    | Paged, searchable customer list |
| POST   | `/customers`                                 | Staff/Admin    | Register a walk-in customer |
| GET    | `/customers/{id}`                            | Staff/Admin    | Fetch customer |
| PUT    | `/customers/{id}`                            | Staff/Admin    | Update contact info |
| GET    | `/vehicles/{id}`                             | Staff/Admin    | Fetch vehicle |
| GET    | `/vehicles/by-customer/{customerId}`         | Staff/Admin    | All vehicles of a customer |
| POST   | `/vehicles`                                  | Staff/Admin    | Register a new vehicle |
| PUT    | `/vehicles/{id}`                             | Staff/Admin    | Update vehicle details |
| GET    | `/service-orders?stage=&customerId=…`        | authenticated  | List orders (customers see only theirs) |
| GET    | `/service-orders/{id}`                       | authenticated  | Full order + history + repairs |
| POST   | `/service-orders`                            | Staff/Admin    | Open a new service order |
| POST   | `/service-orders/{id}/transition`            | Staff/Admin    | Advance to the next allowed stage |
| POST   | `/service-orders/{id}/assign`                | Staff/Admin    | Assign a staff member |
| GET    | `/repair-requests/{id}`                      | authenticated  | Fetch a repair request |
| GET    | `/repair-requests/by-service-order/{id}`     | authenticated  | All repairs on an order |
| POST   | `/repair-requests`                           | Staff/Admin    | Request approval for a repair |
| PUT    | `/repair-requests/{id}/estimate`             | Staff/Admin    | Revise estimate before decision |
| POST   | `/repair-requests/{id}/media`                | Staff/Admin    | Attach photo/video proof |
| POST   | `/repair-requests/{id}/decision`             | authenticated  | Customer approves/rejects |

Errors follow RFC 7807 ProblemDetails. Validation failures include an `errors` dictionary keyed by field name; domain rule violations include a stable `code` field (`service_order.invalid_stage_transition`, `repair_request.media.required`, …) so clients can localize messages.

## Scaling notes

- Stateless API — scale horizontally behind any load balancer.
- Single PostgreSQL instance is the default; move to a managed cluster (RDS/CloudSQL) and enable pgBouncer. EF Core already has retry-on-failure turned on.
- Concurrency on `service_orders` and `repair_requests` uses Postgres `xmin` as a row version so optimistic concurrency conflicts surface as `DbUpdateConcurrencyException`.
- All services are scoped per-request — swap the DbContext provider for Read Replicas or an async-outbox later without touching Application code.
- JWT is stateless; for refresh tokens or revocation, add a `refresh_tokens` table in Infrastructure without touching the Domain.

## Conventions

- Every Application command/query has a FluentValidation validator registered automatically (`AddApplication()`).
- Domain entities have private setters and only mutate through intention-revealing methods (`Open`, `TransitionTo`, `RegisterDecision`, …).
- Stable error codes live in `DomainErrors` so the UI and logs never drift.
- Central package management via `Directory.Packages.props` keeps versions consistent across projects.
