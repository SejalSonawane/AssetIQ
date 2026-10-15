# AssetIQ

Industrial Asset Intelligence Platform — ASP.NET Core MVC foundation for predictive maintenance, reliability, failure analytics, energy analytics, maintenance workflows, and operational alerts.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (compatible with the installed SDK on this machine)
- Git (optional)

PostgreSQL is planned for a future stage. The foundation UI runs **without** a database connection.

## Solution structure

```
AssetIQ/
  AssetIQ.sln
  src/
    AssetIQ.Web/          MVC UI (controllers, views, view models)
    AssetIQ.Business/     Business services and DTOs
    AssetIQ.Data/         Domain entities and repository interfaces
  tests/
    AssetIQ.Tests/
  data/
    rolling-mill/         Rolling Mill CSV location (manual copy)
  docs/
```

## Run locally

From the repository root (`D:\AssetIQ`):

```powershell
dotnet restore AssetIQ.slnx
dotnet build AssetIQ.slnx
dotnet run --project src/AssetIQ.Web/AssetIQ.Web.csproj
```

Then open the HTTPS URL shown in the console (typically `https://localhost:7xxx`).

Default route: **Dashboard** (`/Dashboard`).

## Rolling Mill demonstration dataset

The application currently loads:

`data/rolling-mill/AssetIQ_RollingMill_2000Rows_Demo.csv`

It contains 2,000 rows: 500 rows matching the original dataset values, with timestamps reformatted, and 1,500 additional simulated demonstration rows. The original `AssetIQ_RollingMill_500Rows.csv` is retained. These demonstration labels are not validated real-world predictions.

The configured path is relative to `src/AssetIQ.Web/`, as set in `appsettings.json`.

Do not commit secrets in `appsettings.Development.json`; use user secrets or local config for future database credentials.

## Current stage

**CSV integration (Rolling Mill demonstration dataset)**

- Loads the configured 2,000-row demonstration CSV at startup (cached in memory)
- Validates headers, types, duplicates, energy intensity consistency, and health-score / risk bands
- Dashboard, Equipment, Alerts, Energy Analytics, Predictive Maintenance, and Failure Analytics use imported dataset values
- Maintenance recommendations are imported CSV fields; application actions are temporary in-memory records and are cleared on restart
- Dataset values are not live SCADA, confirmed failure events, engineering-approved limits, or validated ML predictions
- No PostgreSQL/EF Core, ML training, or Power Automate integration yet
