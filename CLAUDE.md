# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A bookkeeping / double-entry accounting API for MSMEs, built as a dissertation
project to showcase Domain-Driven Design and a clean, modular architecture. The
focus is the accounting domain (transactions → balanced journal entries →
financial statements → credit-readiness), not infrastructure breadth.

This repo is a **monorepo with two independently deployable apps**: the .NET API
(`Bookkeeping.Api.csproj`, at the repo root) and a **separate `frontend/` SPA**
(React + Vite + TypeScript). They share one git history but build and deploy on
their own pipelines — the API does not build or serve the SPA. The SPA talks to
the API over HTTP using the JWT bearer flow (see Authentication), so CORS on the
API must allow the SPA's origin.

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
  `Program.cs`. A `Migrations/` folder exists (and `AppDbContextFactory` makes
  `dotnet ef migrations add <Name>` work), but **startup still uses `EnsureCreated`,
  not `Migrate`** — the migrations are not applied automatically and the two paths
  are not reconciled.
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
- `Api/` — MVC controllers (`Api/Controllers/*`), one per module, mapped via
  `app.MapControllers()`. (The README still describes the older `Api/Endpoints/*`
  minimal-API layout — that has been migrated to controllers.)

### Modules (bounded contexts)

`Identity`, `Transactions`, `Ledger`, `Reporting`, `CreditReadiness`, `Invoices`.
Each registers its own repository/service/handlers via an `AddXModule()` extension
in `Infrastructure/DependencyInjection/ModuleRegistration.cs`, chained in `Program.cs`.
Two modules carry infrastructure integrations: `Transactions` owns the Mono
bank-feed adapter, and `Invoices` owns PDF generation + object storage (see below).

Cross-module references are **plain id values with no FK constraint**; integrity
across modules is enforced in the application, not the database. The single
`AppDbContext` uses **one schema per module** (`identity`, `transactions`,
`ledger`, `invoices`, plus `auth` for ASP.NET Core Identity tables) to keep
boundaries visible at the data layer. Note `invoice_templates` lives in the
`identity` schema even though invoicing is its own module.

### Frontend (`frontend/`)

A standalone **React + Vite + TypeScript** SPA, deployed separately from the API.
It is a pure API client: it authenticates via the Identity endpoints, stores the
JWT, and sends it as `Authorization: Bearer <token>` on every call.

- `frontend/src/lib/api.ts` — the single fetch wrapper; reads the API base URL
  from `VITE_API_URL` (see `frontend/.env.example`) and attaches the bearer token.
- Build: `cd frontend && npm install && npm run build` → static assets in
  `frontend/dist/` (host on any static host — Vercel/Netlify/Cloudflare Pages).
- Dev: `npm run dev` (Vite dev server). Point `VITE_API_URL` at the running API.
- `node_modules/` and `dist/` are gitignored; the .NET build ignores `frontend/`
  (it only compiles `.cs`).

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
- **Ports & adapters for external services** — `Application/` defines a
  provider-neutral port; `Infrastructure/` holds the vendor adapter, so vendor
  types never leak upward:
  - `IBankFeedProvider` (`Application/Abstractions`) → `MonoBankFeedProvider` +
    `MonoApiClient` (`Infrastructure/Mono`). The `MonoApiClient` `HttpClient` is
    configured with `AddStandardResilienceHandler` (retry + circuit breaker +
    timeout). Mono amounts are in **kobo (minor units)**; the adapter divides by
    100 to reach the `Money` value object. `MonoOptions` is bound + `ValidateOnStart`.
  - `IObjectStore` (`Application/Abstractions`) → `R2ObjectStore`
    (`Infrastructure/Documents`) — the general-purpose object store that owns the
    Cloudflare R2/S3 mechanics (`AWSSDK.S3`, path-style, public-URL-or-presigned).
    Any module stores binary objects through it (invoice PDFs, business logos, …)
    without touching a vendor SDK. Semantic stores build their own key convention
    and delegate here: `IInvoiceDocumentStore` → `R2InvoiceDocumentStore` owns the
    `invoices/{id}.pdf` key; invoice-template logos are uploaded by `IdentityService`
    under a server-derived `logos/{businessId}/{guid}{ext}` key (the endpoint takes an
    `IFormFile`, validates the image type, and never trusts a client filename).
    `IInvoicePdfGenerator` → `InvoicePdfGenerator` (QuestPDF, whose Community license
    is set in `Program.cs`).
  - `ITokenIssuer` → `JwtTokenIssuer` (`Infrastructure/Auth`).
- **Outbox + background processing (invoice PDFs)** — creating an invoice raises
  `InvoiceCreated`; `InvoiceCreatedHandler` stages an `InvoicePdfJob` (the
  `invoices.pdf_jobs` table) in the **same** transaction and does no I/O. The
  hosted `InvoicePdfOutboxProcessor` (`Infrastructure/Documents`) drains the queue
  out of band, each iteration in its own DI scope / fresh `AppDbContext`: it renders
  the PDF, uploads to R2, and writes the URL back **through the `Invoice` aggregate**
  (`AttachPdf`) so the aggregate stays the sole writer of `PdfUrl`. This keeps
  external I/O out of the request path.
- **Authentication** — JWT bearer is wired in `Program.cs` (validated against the
  `Jwt` config section), backed by ASP.NET Core Identity (`ApplicationUser`, `auth`
  schema). `BookkeepingUserStore` disables the store's auto-save so credential
  creation **joins the caller's unit of work** rather than committing on its own —
  the domain user and its credentials commit together, linked by a shared `Guid`
  id (no FK). **Enforcement is on:** a deny-by-default fallback policy in `Program.cs`
  requires an authenticated caller on every endpoint except those marked
  `[AllowAnonymous]` (login, the registration flows, refresh/logout, the Mono
  webhook), and fine-grained `[Authorize(Policy = ...)]` permission checks layer on top.
- **Refresh-token rotation** — sign-in returns a **short-lived access token**
  (`Jwt:AccessTokenMinutes`, default 15) plus a **refresh token**; `POST /api/auth/refresh`
  exchanges the refresh token for a new pair and `POST /api/auth/logout` ends the session
  (both `[AllowAnonymous]`, since the access token is usually expired by then). Refresh
  tokens live in `auth.refresh_tokens` (`RefreshToken` + `IRefreshTokenStore`/`RefreshTokenStore`
  in `Infrastructure/Auth`) — only the **SHA-256 hash** is stored, never the raw value.
  Tokens are **single-use and rotated**: each refresh revokes the presented token and
  mints a successor in the same `FamilyId` (one login session's lineage). Presenting an
  already-rotated token is treated as **theft** — the whole family is revoked (reuse
  detection). Logout revokes the family too. Access-token lifetime and refresh-token
  lifetime (`Jwt:RefreshTokenDays`, default 14) are config, both with in-code fallbacks.
  Adds a table, so **drop the dev DB** for `EnsureCreated` to create `auth.refresh_tokens`.
- **Per-business permission authorization** — the JWT carries one `business_role`
  claim per membership (value `"{businessId}:{role}"`, stamped by `AuthService` at
  login). `PermissionPolicyProvider` turns a permission name used as a policy
  (`[Authorize(Policy = Permissions.Invoices.Create)]`) into a `PermissionRequirement`;
  `PermissionAuthorizationHandler` reads the `businessId` off the route, finds the
  caller's role for **that** business from the claims, and grants access if
  `RolePermissions.Map[role]` contains the permission. So the same user can pass on
  one business and be denied on another. `Domain/Permissions.cs` is the capability
  catalog; `RolePermissions.Map` (Owner/Admin/Accountant) is the single source of
  truth the handler reads at runtime and the `IdentitySeeder` seeds role claims from —
  so adding a permission needs no DB reset for enforcement to pick it up.

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

- **Authorization is route-scoped, not resource-scoped (IDOR).** Enforcement is now
  wired — every endpoint requires authentication (deny-by-default) and business
  actions carry `[Authorize(Policy = ...)]` permission checks keyed on the route's
  `businessId`. The remaining gap: the handler proves the caller has the permission
  for the `businessId` **in the route**, but the services still load invoices, staged
  bank rows, and linked accounts by their **own id** without confirming that resource
  belongs to that business. So a caller with rights on business A can pass their own
  `businessId` in the route alongside an `invoiceId`/`stagedId`/`accountId` from
  business B and operate on it. Closing this is the `EnsureOwnershipAsync` /
  resource-scoping work (still **not called anywhere**). The anonymous onboarding
  endpoints also still trust the `ownerId` in the body.
- **Bank feed webhook auth is secret-header only, not HMAC.** The Mono bank feed
  is implemented end to end — link (`BankAccountsController` → `BankAccountService`
  exchanges the widget code via `IBankFeedProvider`/`MonoBankFeedProvider`), pull +
  stage + approve/discard into the ledger (`BankImportsController` →
  `BankImportService`, `StagedBankTransaction`), and `MonoWebhookController`. The Mono
  config section is present in `appsettings.example.json`. The remaining caveat: the
  webhook authenticates with a shared **secret header**, not an HMAC signature, so it
  doesn't verify the payload came from Mono unmodified.
- **CORS is wide open (`AllowAnyOrigin`).** `Program.cs` defines a default policy
  with `AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()` and applies it via
  `app.UseCors()`. This unblocks the `frontend/` SPA from any origin, but is too
  permissive for production and is incompatible with credentialed requests — before
  going live, replace it with a named policy that lists the SPA's real origin(s)
  (and `AllowCredentials()` if refresh tokens ever move into cookies).
- **No account/category editing endpoints** (consistent with reference-data scope).
- **Migrations exist but are not applied at startup** — `Program.cs` uses
  `EnsureCreated`, not `Migrate` (see Runtime & tooling).
- **The invoice PDF outbox assumes a single consumer.**
  `InvoicePdfJobRepository.ClaimNextPendingAsync` is named "Claim" but does not
  actually claim/lock a row — it just reads the oldest `Pending` job. There is no
  intermediate `Processing` status and no row lock, and `InvoicePdfOutboxProcessor`
  only marks the job `Done`/`Failed` after the work. This is safe for the current
  single hosted instance, but two processors would both pick the same row and render
  the PDF twice. True concurrency would need a claiming transition (a `Processing`
  status) or a DB-level claim (e.g. SQL Server `UPDATE ... WITH (UPDLOCK, READPAST)`).
- **Invoice numbers are generated per-business but not concurrency-safe.** Invoice
  numbers are assigned server-side (`InvoiceService` formats `INV-00001`), so the
  client never supplies one. The sequence is derived from the current per-business
  invoice count (`InvoiceRepository.CountForBusinessAsync`) + 1, with a
  `NumberExistsAsync` loop as a defensive guard and the unique index on
  `(BusinessId, InvoiceNumber)` as the ultimate backstop. This is correct for the
  current single hosted instance (invoices are never deleted, so count + 1 is always
  the next number), but two concurrent creates — or a second API instance — could both
  read the same count and race for the same number, and the loser fails on the unique
  index rather than retrying. True concurrency would replace the count with a
  **persisted per-business counter** (`invoices.invoice_counters`, one row per
  business) incremented under a row lock, e.g. SQL Server
  `UPDATE ... WITH (UPDLOCK, ROWLOCK) ... OUTPUT INSERTED.LastNumber`, or a DB sequence.
  Note this is `UPDLOCK, ROWLOCK` (block and wait on the *one* counter row), **not**
  the `UPDLOCK, READPAST` of the outbox above — READPAST skips locked rows, which is
  right for a queue of interchangeable jobs but wrong for a single row every create
  must serialise on.

## Conventions

- Match surrounding style: explicit constructors, `sealed` classes, file-scoped
  namespaces, `async`/`ct` parameters, XML-free explanatory comments where a
  decision isn't obvious.
- Keep `Application/` free of EF Core; seeding/`HasData` belongs in Infrastructure.
- Commit or push only when asked.
