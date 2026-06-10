# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A NuGet library (`PetaPoco.Repository.Logging`, `netstandard2.0`) that extends the
`PetaPoco.Repository` stack with two independent logging features. It has no executable
entry point — it plugs into a host app's DI container and `Microsoft.Extensions.Logging`.

## Build / package

```powershell
dotnet build src/PetaPoco.Repository.Logging.sln
dotnet pack  src/PetaPoco.Repository.Logging/PetaPoco.Repository.Logging.csproj -o ./artifacts
```

There are no tests. CI (`azure-pipelines.yml`) only runs `dotnet pack`; the package version
is supplied via the `VersionNumber` pipeline parameter (`byEnvVar` versioning), not from the
`.csproj`.

## Architecture

The two features are wired up separately and can be used independently.

### 1. SQL statement logging — `SqlLoggerHostedService`
Registered as an `IHostedService` by `AddPetaPocoRepositoryLogging` (only when
`PetaPocoLoggingConfiguration.LogSqlStatements` is true). On `StartAsync` it subscribes to
`IDatabaseFactory.DatabaseInstantiated`, and for each new DB subscribes to
`db.CommandExecuted`, logging the SQL text plus parameter values at the configured
`SqlLogLevel`.

Key constraint: there is **no event for a Database being disposed**, so `CommandExecuted`
handlers are never unsubscribed (see comment at `SqlLoggerHostedService.cs:44`). Don't
"fix" this with disposal logic unless `IDatabaseFactory`/`Database` gains such an event.
Both the SQL log call and the parameter-gathering are guarded by `IsEnabled(...)` checks so
nothing is computed when the log level is off, and parameter logging is wrapped in
nested try/catch so a logging failure never breaks the actual DB command.

### 2. Entity CRUD logging — `CrudActionLogger`
Implements `PetaPoco.Repository.Abstractions.ICrudRepositoryService`. It logs an indented
JSON snapshot of the entity (via Newtonsoft.Json) at `Debug` level on the **Before** hooks
of Add/Update/Remove; the **After** hooks are intentionally empty. For Remove it must
`FindById` to obtain the entity, so it skips that fetch entirely when
`EntityLoggingEnabled` is false. Registered via `AddEntityCrudLogging` on the
`ICrudRepositoryServiceCollection` (a different collection than the standard
`IServiceCollection`).

### Registration flow (host app)
1. `services.AddPetaPocoRepositoryLogging(cfg => ...)` — registers config + SQL hosted service.
2. `services.AddEntityCrudLogging(new CrudActionLoggerOptions { ... })` — on the crud repo collection, for entity logging.
3. `serviceProvider.UsePetaPocoRepositoryLogging()` — **must be called after the provider is built.**

Step 3 is required because `CrudActionLogger` resolves its `ILoggerFactory` through the
static `CrudActionLoggerOptions.ServiceProvider`, which is only set inside
`UsePetaPocoRepositoryLogging`. The logger is constructed with options (not via DI), so
without this call `_loggerFactory` is null and entity logging throws. This static
service-locator coupling is the main non-obvious wart of the design — be cautious changing it.

## Dependencies / versioning

All `Microsoft.Extensions.*` references are pinned to `6.0.0` and `PetaPoco.Repository.Abstractions`
to `1.0.11`. The library targets the abstractions package, not concrete PetaPoco — behavior
depends on the `IDatabaseFactory`, `ICrudRepository<,>`, and `ICrudRepositoryService` contracts
defined there.
