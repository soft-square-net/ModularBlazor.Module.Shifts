# Clean Architecture in this module

This module follows [Ardalis Clean Architecture](https://github.com/ardalis/CleanArchitecture), adapted to what a
ModularBlazor runtime module is: an assembly set that the host loads into a collectible load context, maps under
`/api/shifts`, and can unload again. The layout is the one the platform's Content module uses.

## The layers

```
src/
  WebApi.Modules.Shifts.Core/            Core       (ModuleLayer Domain)
    SectionAggregate/Section.cs          a desk and the categories it covers; optional timer overrides
    SectionAggregate/Specifications/     SectionByIdSpec, SectionsWithCategoriesSpec
    ShiftAggregate/Shift.cs              a shift on the clock and its rota; rules throw DomainException
    ShiftAggregate/Specifications/       ShiftByIdSpec, ShiftsInWindowSpec, MemberShiftsSpec, OverdueShiftsSpec
  WebApi.Modules.Shifts.UseCases/        UseCases   (ModuleLayer Application)
    ShiftPermissions.cs RoleTemplates.cs ShiftOptions.cs ShiftCaller.cs ResultExtensions.cs
    Behaviors/LoggingBehavior.cs         Mediator pipeline behaviour
    Sections/                            List, Add, Update, GetSettings; SectionDto, SectionSettings
    Shifts/                              List, ListMine, ListOverdue, GetById, GetAutomation, Add, Update, Publish,
                                         CheckOut, CheckOutMember, Delete; ShiftDto and the shared services
                                         ShiftGrants, ShiftDetails, ShiftChanges
  WebApi.Modules.Shifts.Infrastructure/  Infrastructure
    Data/ShiftsDbContext.cs              a TenantDbContext (schema "shifts", tenant filter, domain events)
    Data/Config/                         one IEntityTypeConfiguration per entity
    Data/Migrations/<Provider>/          one migration set per database engine
    InfrastructureServiceExtensions.cs   AddInfrastructureServices()
  WebApi.Modules.Shifts.Web/             Web: the entry assembly and composition root (ModuleLayer Module)
    ShiftsModule.cs                      IRuntimeModule: manifest, permissions, settings, ConfigureServices
    Configurations/                      ServiceConfigs (all registrations), MediatorConfig
    ShiftsRoutes.cs                      the only ICarterModule: the route table
    Sections/  Shifts/                   one endpoint class per route (REPR), and the request bodies
    Extensions/                          ResultExtensions (Result -> HTTP), ShiftsProblemsFilter
  WebApi.Modules.Shifts.Client/          the typed API client (NSwag), generated from src/openapi/shifts.json
  Module.Shifts/                         the Blazor client module (pages over ShiftsClient); outside the layers
tests/
  WebApi.Modules.Shifts.UnitTests/       Core rules; UseCases handlers with NSubstitute
  WebApi.Modules.Shifts.IntegrationTests/ the DbContext, mapping and specifications on SQLite
```

| Layer | Holds | Knows about |
| --- | --- | --- |
| Core | Aggregates and their rules (throwing `DomainException`), entity specifications | the platform's SharedKernel (`AggregateRoot`, `IRepository`) and Ardalis.Specification |
| UseCases | One folder per use case: a command or query and its handler, returning an `Ardalis.Result`; DTOs; permissions; settings; the rota's role templates | Core; the platform's contracts (`IAccessScopes`, `IGrantStore`); Mediator.Abstractions |
| Infrastructure | EF Core: the DbContext, mappings, migrations | Core; the platform's data layer (`TenantDbContext`, `EfRepository`) |
| Web | The module entry, registrations, routes, endpoints, HTTP mapping of results | everything above; ASP.NET Core, Carter, the Mediator source generator |

## Dependency rules

```
Core  <-  UseCases  <-  Web
  ^                      |
  +---  Infrastructure <-+
```

- Core references nothing of the module. UseCases references Core only. Infrastructure references Core only.
- Web references UseCases and Infrastructure and is the only place that wires them together (`ServiceConfigs`).
- UseCases never touches EF Core: handlers get `IRepository<T>` / `IReadRepository<T>` (the platform registers EF
  repositories for them) and query with specifications from Core.
- Endpoints contain no logic: bind the request, `ISender.Send` a command or query, map the `Result` to HTTP.
- Repositories save immediately (`AddAsync`, `UpdateAsync`, `DeleteAsync`); there is no unit of work.
- Failures a caller should see are `Result` statuses: `NotFound` (404), `Forbidden` (403), `Conflict` (409).
  A broken domain rule is a `DomainException`; `ShiftsProblemsFilter` turns it into a 409 problem, and a caller who is
  not signed in (`ShiftAccessDeniedException`) into a 403.

## How the rota becomes rights

This is what the module is for, so it is worth knowing where each part lives:

- **Core** decides who is on a shift and until when: `Shift.SetRota` (exactly one Manager), `CheckOut`,
  `CheckOutMember`, and `EndOf(member)`, the moment a member's access ends.
- **UseCases** turns that into grants. `RoleTemplates` lists the content permissions of each role (plain strings: the
  shifts module never references the content module). `ShiftGrants.WriteAsync` writes every member's permissions on
  every category of the section, valid from the start to `EndOf(member)`, with one
  `IGrantStore.ReplaceSourceAsync("shift:{id}", ...)`. Nothing is written until the shift is published.
- **ShiftChanges** is the shape of every change to a shift: load it, check who may do it, call the aggregate, save, and
  rewrite its grants. Publish, Update, CheckOut and CheckOutMember all go through it, so a published shift's grants
  never lag behind its rota. Changing a section's categories (`UpdateSectionHandler`) rewrites the grants of its
  shifts that have not ended.
- The host evaluates each grant's window on every request, so rights start and stop on the clock without a job.

### Rules that come from the platform

- **No module type in a host-wide cache.** Do not inject `ILogger<T>` with a module type `T` (the host's logger
  factory caches by type): take `ILoggerFactory` and create the logger by name, as `LoggingBehavior` does. Register
  module services in `ConfigureServices` (the module's own container), never in the host's.
- **Host-shared packages are compile-only.** The Module SDK references WebApi.Contracts, SharedKernel, Data, EF Core,
  Ardalis.Specification and Carter with `ExcludeAssets=runtime`; the host provides them. Never ship a copy.
- **Private packages are fine when they keep no host-side state.** Mediator (source-generated: its handler table is
  code in the Web assembly) and Ardalis.Result are copied into the module output and unload with it.
- **Only the entry assembly is scanned.** The runtime maps the `ICarterModule` types of the assembly that holds the
  `IRuntimeModule`, which is Web, so endpoints live there.
- **The API is a contract.** Endpoint names (`Shifts_PublishShift`, ...) are OpenAPI operation ids and the generated
  client's method names; request and response type names are schema names. Keep them stable, and re-export
  `src/openapi/shifts.json` after any change to an endpoint.
- **Check that it unloads.** After the module has served requests, `POST /api/admin/modules/shifts/unload` and then
  `GET /api/admin/modules/unloading` should come back empty within a few seconds.

## Adding a feature

A new action on shifts, say "swap two members":

1. **Core**: the rule on the aggregate, `Shift.Swap(...)`, throwing `DomainException` when not allowed. A new query
   shape is a specification in `ShiftAggregate/Specifications`.
2. **UseCases**: a folder `Shifts/Swap/` with `SwapMembersCommand` (`ICommand<Result<ShiftDto>>`) and its handler,
   which calls `ShiftChanges.ChangeAsync` so the grants follow. Add a permission to `ShiftPermissions` if it needs one.
3. **Web**: `Shifts/Swap.cs`, an `IEndpoint` with a static `HandleAsync` that sends the command and returns
   `Results<Ok<T>, ProblemHttpResult>`; then one line in `ShiftsRoutes`. Name it `Shifts_SwapMembers`.
4. **Infrastructure**, if the model changed: the mapping in `Data/Config`, then a migration for every provider (see
   the README) and `dotnet ef migrations has-pending-model-changes` per provider.
5. **Client**: export the OpenAPI document again (README, "Building") and build: `ShiftsClient` gains
   `SwapMembersAsync`.
6. **Tests**: the rule in `UnitTests/Core`, the handler in `UnitTests/UseCases` (`ShiftsFixture` has the shared
   services over substitutes), a new mapping or specification in `IntegrationTests`.

Handlers are found by the Mediator source generator at compile time; there is nothing to register. Services that
several handlers share are registered in `ServiceConfigs`.
