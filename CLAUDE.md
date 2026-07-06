# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A bookkeeping / double-entry accounting API for MSMEs, built as a dissertation
project to showcase Domain-Driven Design and a clean, modular architecture. The
focus is the accounting domain (transactions → balanced journal entries →
financial statements → credit-readiness), not infrastructure breadth.

Single project, single deployable: `Bookkeeping.Api.csproj`.

## Runtime & tooling

- **.NET 10** (`net10.0`) — the only runtime installed on this machine.
- **EF Core 10.0.9** with the **SQL Server (MSSQL)** provider.
- Database connection string: `ConnectionStrings:Default` in `appsettings.json`.
  The default points at `(localdb)\MSSQLLocalDB` (Windows-only); on macOS point
  it at a reachable instance (e.g. SQL Server in Docker).

### Common commands

```bash
dotnet build                       # compile
dotnet run                         # start the API (Swagger UI in Development)
DOTNET_ROLL_FORWARD=Major dotnet ef dbcontext info   # validate the EF model builds
```

- The schema is created at startup via `db.Database.EnsureCreated()` in
  `Program.cs` (no EF migrations in the repo).
- **After any schema change, drop the dev database** so `EnsureCreated` rebuilds
  it with the seed data — `EnsureCreated` does not migrate an existing database.

## Architecture

Layered, dependency arrow points inward: **Api → Application → Domain**, with
**Infrastructure** implementing Application-defined interfaces.

- `Domain/` — aggregates, entities, value objects, domain events. No framework
  dependencies. Business rules live here.
- `Application/` — services, DTOs, repository/service interfaces, event handlers.
  **Kept free of EF Core** (persistence is an interface concern).
- `Infrastructure/` — EF Core `AppDbContext`, configurations, repositories, the
  domain-event dispatcher, DI registration.
- `Api/` — minimal-API endpoint definitions (`Api/Endpoints/*`).

### Modules (bounded contexts)

`Identity`, `Transactions`, `Ledger`, `Reporting`, `CreditReadiness`. Each
registers its own repository/service/handlers via an `AddXModule()` extension in
`Infrastructure/DependencyInjection/ModuleRegistration.cs`, chained in `Program.cs`.

Cross-module references are **plain id values with no FK constraint**; integrity
across modules is enforced in the application, not the database. The single
`AppDbContext` uses **one schema per module** (`identity`, `transactions`,
`ledger`) to keep boundaries visible at the data layer.

## Key patterns

- **Aggregates & domain events:** `AggregateRoot<TId>` (`Domain/Common/Entity.cs`)
  holds pending events via `Raise(...)`. Events are dispatched from the overridden
  `AppDbContext.SaveChangesAsync` **before** `base.SaveChangesAsync`, so handler
  side effects commit in the **same transaction** as the change that raised them.
  Flow: `Raise` → `SaveChanges` collects tracked aggregates' events →
  `DomainEventDispatcher` resolves `IDomainEventHandler<T>` from the current DI
  scope (shared `AppDbContext`) → handlers stage more changes → one physical write.
  Worked example: `TransactionRecorded` → `TransactionRecordedHandler` posts a
  balanced `JournalEntry`.
- **Result type** (`Domain/Common/Result.cs`) — operations return `Result` /
  `Result<T>` instead of throwing for expected failures; endpoints map to
  `Ok` / `BadRequest`.
- **Strongly-typed ids** (`Domain/Common/Identifiers.cs`) — `record struct` wrappers
  (`BusinessId`, `TransactionId`, …) with EF value converters registered in
  `AppDbContext.ConfigureConventions`.
- **Repository + Unit of Work** — `IUnitOfWork` is the `AppDbContext`; calling
  `SaveChangesAsync` is the single save/commit point (and the event-dispatch trigger).

## Reference data: chart of accounts & categories

The chart of accounts (`ledger.accounts`) and transaction categories
(`transactions.categories`) are **global shared reference data** — no `BusinessId`,
identical for every business. Per-business customization is **out of scope**.

- Seeded via EF Core **`HasData`** in `AccountConfiguration` / `CategoryConfiguration`
  with **fixed GUIDs** (so `EnsureCreated` is deterministic and `Transaction`→
  `Category` FKs resolve).
- Account **codes** the posting rules key on live in `Application/Ledger/ChartOfAccounts.cs`
  (`Cash` = 1000, `SalesRevenue` = 4000, `GeneralExpenses` = 5000).
- Per-business balances are still correct because they derive from each business's
  journal entries, not from the shared accounts.

## Known gaps / not implemented

- **No authentication or authorization.** No ASP.NET Core Identity, no auth
  middleware, no login/passwords. Endpoints are anonymous and trust the
  `businessId`/`ownerId` passed in the route/body. `IdentityService.EnsureOwnershipAsync`
  exists but is **not called anywhere** — the enforcement seam is empty.
- **No account/category editing endpoints** (consistent with reference-data scope).
- **No EF migrations** — `EnsureCreated` only (dev convenience).

## Conventions

- Match surrounding style: explicit constructors, `sealed` classes, file-scoped
  namespaces, `async`/`ct` parameters, XML-free explanatory comments where a
  decision isn't obvious.
- Keep `Application/` free of EF Core; seeding/`HasData` belongs in Infrastructure.
- Commit or push only when asked.
