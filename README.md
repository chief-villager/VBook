# Bookkeeping.Api

A modular monolith bookkeeping Web API for Nigerian MSMEs, built in .NET 8. It turns
plain transaction records into double-entry books, financial statements, and a
lender-facing credit readiness report that arranges evidence under the 5Cs of credit.

This is a dissertation artifact. The design favours things that are easy to explain
and defend over things that are merely convenient.

## Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB is fine for development) or another provider you swap in
- Internet access to restore NuGet packages (this repo ships source only)

## Build and run

    dotnet restore
    dotnet build
    dotnet run

On start-up the app creates the database schema with `EnsureCreated()` and serves
Swagger at the launch URL (see `Properties/launchSettings.json`). The connection
string is in `appsettings.json` under `ConnectionStrings:Default`.

## Architecture at a glance

One deployable, one `AppDbContext`, five modules, each behind a service interface:

    Identity  ->  Transactions  ->  Ledger  ->  Reporting
                                              \
                                               ->  Credit Readiness

Modules talk in-process through interfaces, never by reaching into each other's
tables. The data spine is Transactions -> Ledger -> Reporting; Credit Readiness
reads sideways off Reporting and Transactions.

### Single context, schema per module

There is one `AppDbContext`. Each module owns a database schema (`identity`,
`transactions`, `ledger`), so the module boundary is visible in the data layer
without splitting into multiple contexts.

### No foreign keys across module boundaries

A cross-module reference (for example a Ledger entry's `BusinessId`) is stored as a
plain id value with no database foreign key. Within a module, foreign keys are used
freely (a transaction to its category, a posting line to its entry). Referential
integrity across modules is enforced in the application layer through the owning
module's service, not by the database. The tradeoff is an accepted orphan risk in
exchange for genuinely decoupled modules that could later be extracted into services.

### Strongly-typed ids

`BusinessId`, `TransactionId`, `AccountId`, and so on are `readonly record struct`
wrappers over `Guid`, stored via EF value converters registered in
`ConfigureConventions`. A `BusinessId` cannot be passed where a `TransactionId` is
expected.

### In-process domain events

A recorded transaction raises `TransactionRecorded`; the Ledger handles it and posts
the matching balanced journal entry. A new business raises `BusinessRegistered`;
the Ledger seeds the chart of accounts and Transactions seeds default categories.

Dispatch is a small hand-rolled dispatcher (`DomainEventDispatcher`), not a library,
so the flow is fully visible. Events are dispatched inside the overridden
`AppDbContext.SaveChangesAsync`, before the base save, so the change and its
consequences commit in a single transaction. The dispatcher resolves handlers from
the current scope, so every handler shares the same `AppDbContext` instance.

### Repositories

A generic write-side repository (`IGenericRepository<T>` / `GenericRepository<T>`)
provides Add / AddRange / Update / Remove. Each module then has its own repository
that inherits the generic one and adds typed read queries
(`IIdentityRepository`, `ITransactionRepository`, `ILedgerRepository`). The
`AppDbContext` is the unit of work.

### Money

The domain has a `Money` value object, but persistence stores plain `decimal(18,2)`
with NGN assumed, to keep the EF mapping simple. Money is used at the service and
domain boundary. This is a deliberate scaffold simplification.

## The vertical slice that proves the design

The hardest seam is Transactions -> Ledger being event-driven. To see it working:

1. `POST /api/users` with an email and display name -> returns `userId`.
2. `POST /api/businesses` with that `ownerId`, a name, and a sector -> returns
   `businessId`. This one call also seeds the chart of accounts and default
   categories, via `BusinessRegistered`.
3. `GET /api/businesses/{businessId}/categories` -> pick a `categoryId`.
4. `POST /api/businesses/{businessId}/transactions` with an amount, that
   `categoryId`, and a date. Recording the transaction automatically produces a
   balanced journal entry in the Ledger, in the same save. You never post debits
   and credits by hand.
5. `GET /api/businesses/{businessId}/trial-balance?asOf=YYYY-MM-DD` -> the entry is
   already there and the trial balance balances.
6. `GET /api/businesses/{businessId}/reports/profit-and-loss?from=...&to=...`,
   `.../reports/balance-sheet?asOf=...`, `.../reports/cash-flow?from=...&to=...`
7. `GET /api/businesses/{businessId}/credit-readiness?from=...&to=...` -> the
   5Cs-arranged report with evidence and gap flags, and no score.

## Credit Readiness: a report, not a score

The Credit Readiness module produces a lender-facing report, not a creditworthiness
score. It attacks information asymmetry by making the business legible: it arranges
the bookkeeping evidence under the lender's own 5Cs checklist (Character, Capacity,
Capital, Collateral, Conditions) and flags gaps, but it never grades. Conditions is
the weakest fit, since it is mostly external to the books; the module surfaces sector
and trading window and leaves the judgement to the lender.

## Project layout

    Domain/          Entities, value objects, domain events, invariants (no EF, no ASP.NET)
      Common/        Result, Entity/AggregateRoot, strongly-typed ids, Money, DateRange
      Identity/      User, Business, BusinessSector
      Transactions/  Transaction, Category, TransactionType, events
      Ledger/        Account, JournalEntry (aggregate root), PostingLine (owned)
    Application/     Service interfaces + implementations, DTOs, repository interfaces,
                     event handlers, posting rules
    Infrastructure/  AppDbContext, EF configurations, repositories, dispatcher, DI
    Api/Endpoints/   Minimal-API endpoint groups per module

## Notes on running beyond development

- `EnsureCreated()` is used for convenience. For anything past local development,
  switch to EF migrations. A design-time factory (`AppDbContextFactory`) is included
  so `dotnet ef migrations add <Name>` works out of the box.
- To swap SQL Server for PostgreSQL, replace the `UseSqlServer(...)` call in
  `PersistenceRegistration` with `UseNpgsql(...)` and reference the Npgsql provider.
- Trial balances are aggregated in memory (owned posting lines load with their
  entry). This is fine at dissertation scale; a SQL-side GROUP BY is the scaling path.
