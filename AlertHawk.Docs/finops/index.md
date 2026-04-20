# FinOps

**AlertHawk.FinOps** is an ASP.NET Core service that analyzes Azure subscriptions for cost and utilization signals, persists results in SQL Server, optionally calls an external AI API for optimization recommendations, and exposes a REST API for dashboards and automation.

The solution is defined in [`AlertHawk.FinOps.slnx`](https://github.com/thiagoloureiro/AlertHawk/blob/main/AlertHawk.FinOps/AlertHawk.FinOps.slnx) at the repository root of the FinOps folder. It includes:

| Project | Role |
|---------|------|
| **AlertHawk.FinOps** | Web API, Azure data collection, analysis orchestration, optional weekly scheduler |
| **AlertHawk.FinOps.Tests** | Unit and integration tests |

All controller routes are prefixed with **`/finops`** (see `GlobalRoutePrefixConvention` in the service). Example: `/finops/api/Version`. Most endpoints require **JWT Bearer** or **Azure AD**; exceptions are called out below.

---

## Overview

- **Azure**: Uses a service principal (`Azure__*`) to read subscriptions listed in `Azure__SubscriptionIds` (comma-separated). Typical roles include **Reader** on the subscription and **Cost Management Reader** where cost APIs are used.
- **SQL Server**: Stores analysis runs, resource snapshots, cost details, historical cost rows, AI recommendations, and optional subscription metadata.
- **Analysis**: On-demand (sync or async job) or **weekly** (UTC) via `WeeklyAnalysis` options for every configured subscription id.
- **AI**: Optional; URL, key, and header name come from `AI` configuration (used when generating recommendations).
- **Observability**: Sentry is wired in `Program.cs`; Swagger UI is enabled only when `ASPNETCORE_ENVIRONMENT` is **Development**.

---

## Environment variables

Configuration is read from `appsettings.json` and **environment variables**. Nested keys use `__` (double underscore), e.g. `ConnectionStrings__SqlConnectionString`.

### General

| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Development` enables Swagger / Swagger UI; use `Production` in deployed environments |
| `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` | Set to `false` in Docker images that need full culture data (see service `Dockerfile`) |

### Database

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__SqlConnectionString` | **Required.** SQL Server connection string for `FinOpsDbContext` |

On startup the app applies **EF Core migrations** when pending migrations exist; otherwise it may **`EnsureCreated`** when no migrations are present (see `Program.cs`).

### Azure (service principal)

| Variable | Description |
|----------|-------------|
| `Azure__TenantId` | Azure AD tenant id |
| `Azure__ClientId` | Application (client) id |
| `Azure__ClientSecret` | Client secret |
| `Azure__SubscriptionIds` | Comma-separated Azure subscription GUIDs to analyze |

### Weekly analysis (background)

| Variable | Description |
|----------|-------------|
| `WeeklyAnalysis__Enabled` | `true` to run the hosted scheduler; `false` to disable |
| `WeeklyAnalysis__DayOfWeekUtc` | Day name in English, e.g. `Sunday` |
| `WeeklyAnalysis__HourUtc` | 0–23 (UTC) |
| `WeeklyAnalysis__MinuteUtc` | 0–59 (UTC) |

### AI recommendations (optional)

| Variable | Description |
|----------|-------------|
| `AI__ApiUrl` | HTTP endpoint for the recommendation agent |
| `AI__ApiKey` | API key value |
| `AI__ApiKeyHeaderName` | Header name sent with the key (e.g. vendor-specific header) |

### Authentication

The API uses a **combined default policy**: an authenticated user via **JWT Bearer** (`JwtBearer` scheme) **or** **Azure AD** (`AzureAd` scheme from Microsoft.Identity.Web).

| Variable | Description |
|----------|-------------|
| `Jwt__Key` | Symmetric key for JWT validation (replace insecure defaults in production) |
| `Jwt__Issuers` | Comma-separated valid issuers |
| `Jwt__Audiences` | Comma-separated valid audiences |
| `AzureAd__Instance`, `AzureAd__TenantId`, `AzureAd__ClientId`, `AzureAd__ClientSecret`, … | Standard **Microsoft.Identity.Web** / Azure AD app registration settings (see [Authentication](/authentication/) patterns in other services) |

If `Jwt__*` or `AzureAd__*` values are missing, the application falls back to **development placeholders** in code; production deployments must set real secrets.

### Sentry

| Variable | Description |
|----------|-------------|
| `Sentry__Dsn`, `Sentry__Environment`, `Sentry__SendDefaultPii`, … | Sentry SDK options (see `appsettings.json` in the project for the full set used there) |

---

## API controllers

Base path: **`/finops/api`**. Unless noted, endpoints use **`[Authorize]`** and accept **JWT Bearer** or **Azure AD** tokens.

### Version — `/finops/api/Version`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/` | **None** | Returns the entry assembly version string |

### Analysis — `/finops/api/Analysis/*`

| Method | Route | Description |
|--------|--------|-------------|
| `POST` | `/start` | Body: JSON string (Azure subscription id). Runs analysis synchronously; returns summary including `AnalysisRunId` |
| `POST` | `/start-async` | Body: JSON string (subscription id). Returns `202 Accepted` with `JobId`; poll `GET jobs/{jobId}` |
| `GET` | `/jobs/{jobId}` | Job status for async analysis |
| `POST` | `/cleanup` | Deletes old analysis runs, keeping the latest run per subscription (and related rows) |

### Analysis runs — `/finops/api/AnalysisRuns/*`

| Method | Route | Description |
|--------|--------|-------------|
| `GET` | `/` | List analysis runs |
| `GET` | `/{id}` | Run by id |
| `GET` | `/latest` | Latest run |
| `GET` | `/latest-per-subscription` | Latest run per subscription |
| `GET` | `/subscription/{subscriptionId}` | Runs for a subscription |
| `DELETE` | `/{id}` | Delete a run |

### Subscription summaries (from runs) — `/finops/api/Subscription/*`

| Method | Route | Description |
|--------|--------|-------------|
| `GET` | `/` | Distinct subscriptions derived from stored analysis runs (name from latest run) |

### Subscriptions (CRUD metadata) — `/finops/api/Subscriptions/*`

| Method | Route | Description |
|--------|--------|-------------|
| `GET` | `/` | All `Subscription` rows |
| `GET` | `/{id}` | By primary key |
| `GET` | `/by-subscription-id/{subscriptionId}` | By Azure subscription id |
| `POST` | `/` | Create or update description metadata |
| `PUT` | `/{id}` | Update |
| `DELETE` | `/{id}` | Delete |

### Resources — `/finops/api/Resources/*`

| Method | Route | Description |
|--------|--------|-------------|
| `GET` | `/analysis/{analysisRunId}` | Resources for a run |
| `GET` | `/analysis/{analysisRunId}/type/{resourceType}` | Filter by resource type |
| `GET` | `/analysis/{analysisRunId}/resourcegroup/{resourceGroup}` | Filter by resource group |
| `GET` | `/analysis/{analysisRunId}/flags` | Resources with flags |
| `GET` | `/analysis/{analysisRunId}/summary/types` | Summary by type |
| `GET` | `/analysis/{analysisRunId}/summary/resourcegroups` | Summary by resource group |
| `GET` | `/analysis/{analysisRunId}/search` | Query: `searchTerm` — search name, group, or type |

### Cost details — `/finops/api/CostDetails/*`

| Method | Route | Description |
|--------|--------|-------------|
| `GET` | `/analysis/{analysisRunId}` | Cost lines for a run |
| `GET` | `/analysis/{analysisRunId}/type/{costType}` | Filter by cost type |
| `GET` | `/analysis/{analysisRunId}/top/{count}` | Top contributors |
| `GET` | `/analysis/{analysisRunId}/summary/resourcegroups` | Aggregated by resource group |
| `GET` | `/analysis/{analysisRunId}/summary/services` | Aggregated by service |

### Historical costs — `/finops/api/HistoricalCosts/*`

| Method | Route | Description |
|--------|--------|-------------|
| `GET` | `/analysis/{analysisRunId}` | Historical cost rows for a run |
| `GET` | `/subscription/{subscriptionId}` | By subscription |
| `GET` | `/analysis/{analysisRunId}/daily-totals` | Daily totals |
| `GET` | `/analysis/{analysisRunId}/by-resourcegroup` | By resource group |
| `GET` | `/analysis/{analysisRunId}/by-service` | By service |
| `GET` | `/analysis/{analysisRunId}/trend` | Trend payload for charts |

### Recommendations — `/finops/api/Recommendations/*`

| Method | Route | Description |
|--------|--------|-------------|
| `GET` | `/analysis/{analysisRunId}` | Recommendations for a run |
| `GET` | `/analysis/{analysisRunId}/latest` | Latest recommendations for that run context |
| `GET` | `/{id}/formatted` | Single recommendation, formatted (e.g. markdown-friendly) |
| `GET` | `/` | List recommendations |

### Dashboard — `/finops/api/Dashboard/*`

| Method | Route | Description |
|--------|--------|-------------|
| `GET` | `/summary` | Aggregated dashboard summary |
| `GET` | `/cost-trends` | Cost trend data |
| `GET` | `/resource-distribution` | Resource distribution |
| `GET` | `/optimization-opportunities` | Optimization-oriented summary |

---

## Helm chart reference

The main [Helm chart](/helm/) deploys FinOps as **`finops-api`**: set `finops-api.replicas` and **`finops-api.env`** in `values.yaml`, and the container image under **`image.finops-api`**.

Configure SQL, JWT, Azure AD, Sentry, and Swagger credentials the same way as other AlertHawk APIs. Add Azure data collection (`Azure__*`), optional AI (`AI__ApiUrl`, `AI__ApiKey`, `AI__ApiKeyHeaderName`), and optional weekly runs (`WeeklyAnalysis__*`). Full variable list: [Environment variables — finops-api](/helm/environment-variables#finops-api).

---

## Local development

From the repository folder that contains `AlertHawk.FinOps.slnx`:

```bash
dotnet restore AlertHawk.FinOps/AlertHawk.FinOps.slnx
dotnet run --project AlertHawk.FinOps/AlertHawk.FinOps/AlertHawk.FinOps.csproj
```

With `ASPNETCORE_ENVIRONMENT=Development`, open Swagger at the URL shown in `Properties/launchSettings.json` (e.g. `https://localhost:5001/swagger`).

Run tests:

```bash
dotnet test AlertHawk.FinOps/AlertHawk.FinOps.slnx
```

---

## Further reading

- In-repo overview and analysis modules: [`AlertHawk.FinOps/README.md`](https://github.com/thiagoloureiro/AlertHawk/blob/main/AlertHawk.FinOps/AlertHawk.FinOps/README.md)
