# Giwu HRMS — Backend (Giwu.Api)

ASP.NET Core 10 + EF Core 10 + PostgreSQL + Hangfire. Clean Architecture with vertical slices.

## Solution layout
```
src/
├── Giwu.Api/             ASP.NET entry point: endpoints, middleware, JWT, Hangfire, OpenAPI
├── Giwu.Application/     CQRS handlers (MediatR), validators, abstractions
├── Giwu.Domain/          Entities, value objects, enums, domain events. Zero deps.
├── Giwu.Infrastructure/  EF Core DbContext, migrations, JWT/password services, tenant context
└── Giwu.Contracts/       Wire DTOs — also referenced by the MAUI client
```

## Run locally

### 1. Postgres
Either point to **Neon** (recommended free tier, Singapore region) and update
`appsettings.Development.json` connection strings, OR run a local instance:

```bash
docker run -d --name giwu-pg -p 5432:5432 \
  -e POSTGRES_USER=hrms -e POSTGRES_PASSWORD=hrms -e POSTGRES_DB=giwu_hrms \
  postgres:17-alpine
docker exec giwu-pg psql -U hrms -c "CREATE DATABASE giwu_hrms_jobs;"
```

### 2. JWT secret
```bash
cd src/Giwu.Api
dotnet user-secrets init
dotnet user-secrets set "Auth:Key" "$(openssl rand -base64 48)"
```

### 3. Migrations + seed
The dev environment auto-migrates and seeds on startup.

To create migrations manually:
```bash
dotnet ef migrations add Initial \
  --project src/Giwu.Infrastructure \
  --startup-project src/Giwu.Api
dotnet ef database update \
  --project src/Giwu.Infrastructure \
  --startup-project src/Giwu.Api
```

### 4. Run
```bash
dotnet run --project src/Giwu.Api
```
- API at `http://localhost:5080`
- Scalar docs at `http://localhost:5080/scalar/v1`
- Hangfire dashboard at `/hangfire` (requires `Settings.Manage` permission — login first)

### 5. Login
Demo HR Admin (seeded automatically in Development):
- Email: `admin@giwu.ph`
- Password: `ChangeMe!123`

```bash
curl -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@giwu.ph","password":"ChangeMe!123"}'
```

## Deploy (Coolify on a VPS)

1. Connect GitHub repo in Coolify, point to this folder.
2. Set required env vars: `DB_PASSWORD`, `JWT_KEY` (32+ chars).
3. Coolify reads `deploy/docker-compose.yml` → builds image → deploys.
4. Caddy auto-issues HTTPS cert when DNS for `api.giwu-hrms.com` resolves.

## Cross-cutting features baked in

| Concern | Where |
|---|---|
| Multi-tenancy | `ITenantContext` + global query filter on every aggregate |
| Soft delete | `AuditableEntity.DeletedAt` + filter |
| Audit columns | `SaveChangesAsync` interceptor in `ApplicationDbContext` |
| Optimistic concurrency | Postgres `xmin` system column |
| Outbox pattern | `OutboxMessage` table + `OutboxDispatcherJob` (1-min Hangfire) |
| JWT auth | `JwtTokenService`; access 15 min + refresh 7 days |
| Permission policies | One policy per `Permissions.*` constant |
| Idempotent seeding | `Seeder.SeedAsync` runs every dev startup |
| Health checks | `/health/live`, `/health/ready` (covers Postgres) |

## Add a new feature endpoint

1. Domain entity in `Giwu.Domain/<Feature>/`.
2. DbSet in `ApplicationDbContext` + EF config in `OnModelCreating`.
3. DTOs in `Giwu.Contracts/<Feature>/`.
4. MediatR command/query + validator + handler in `Giwu.Application/<Feature>/`.
5. Endpoint class implementing `IEndpoint` in `Giwu.Api/Endpoints/<Feature>/`.
6. New migration: `dotnet ef migrations add Add_<Feature>`.

That's the whole loop. Endpoints are auto-discovered by `EndpointExtensions.AddEndpoints`.

## Frontend integration

The MAUI client should:
1. Add a project reference to `Giwu.Contracts` (or copy its DTOs).
2. Replace the in-memory `AuthService` body with `HttpClient`-based calls to `/api/auth/login`, `/api/auth/me`, `/api/auth/refresh`.
3. Read JWT permissions into `RoleAuthService.Permissions` after login. Existing pages and `RoleGuard` keep working unchanged.
