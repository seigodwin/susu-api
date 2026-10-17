# Susu API

Lightweight backend implementing a "susu" (rotating savings) system.

Projects
- Susu.Domain — domain entities (Group, Member, Contribution, PayoutEntry)
- Susu.Application — application services, repository interfaces, specifications
- Susu.Infrastructure — EF Core DbContext, EF repository, UnitOfWork, data seeder, migrations
- Susu.Api — ASP.NET Core minimal API + controllers
- Susu.Tests — integration tests (EF InMemory)

Requirements
- .NET 10 SDK
- PostgreSQL (for production) or use EF InMemory for tests
- dotnet-ef (if you plan to scaffold or apply migrations locally)

Quick start (development)

1. Set the PostgreSQL connection string in `Susu.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "SusuDatabase": "Host=localhost;Port=5432;Database=susu;Username=postgres;Password=YourPassword"
}
```

2. Create database and apply migrations (requires dotnet-ef):

```powershell
dotnet tool install --global dotnet-ef
dotnet ef database update -p Susu.Infrastructure -s Susu.Api
```

3. Run the API

```powershell
dotnet run --project Susu.Api
```

The project includes a simple DataSeeder that seeds a demo group (Ama, Kofi, Esi) when the database is empty. Seeding runs during infrastructure registration; adjust or disable in Production.

Run tests

```powershell
dotnet test
```

API endpoints (examples)
- POST /api/groups — create group
- GET /api/groups/{id} — get group
- POST /api/groups/{groupId}/members — add member
- POST /api/groups/{groupId}/contributions — record contribution
- POST /api/groups/{groupId}/close — close week
- GET /api/groups/{groupId}/report — get week report

Example curl (record contribution):

```bash
curl -X POST https://localhost:5001/api/groups/{groupId}/contributions \
  -H "Content-Type: application/json" \
  -d '{"memberId":"{memberId}","amount":30.00}'
```

Business rules & assumptions
- Members must be unique by Id or name within a group.
- Members cannot be added once any contribution exists for the group (prevents pot-size changes mid-cycle).
- Payments must be positive and cannot exceed what the member owes for that week.
- Partial payments are allowed; contributions are stored and summed per (group, member, week).
- Payout order is deterministic: members are ordered by `Member.JoinedAt` (set when assigned to a group).
- Money is handled using `decimal` and persisted as `numeric(18,2)` to avoid rounding errors.
- Close-week operations are transactional (payout + group state update happen in one UnitOfWork transaction).

Notes and next steps
- Move seeding to app startup with logging for production readiness.
- Consider adding optimistic concurrency (RowVersion) and stronger validation/error middleware.
- EF repository currently calls SaveChanges inside each method; you may prefer deferring SaveChanges to UnitOfWork.

License: MIT-style (check repo license)
