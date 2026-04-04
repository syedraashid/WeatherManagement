# WeatherManagement API

A production-grade .NET 8 Web API that fetches real-time weather data from OpenWeatherMap on a scheduled basis, stores it in a relational database, and exposes REST endpoints for querying. Logs every sync operation to Azure Cosmos DB via an Azure Function.

---

## Architecture

```
WeatherManagement.Api          → Presentation layer (REST API, Hangfire, Serilog)
WeatherManagement.Core         → Business logic (IWeatherService, WeatherService)
WeatherManagement.Infrastructure → Data access, EF Core, Hangfire jobs, Refit client
WeatherManagement.Domain       → Entities, contracts, interfaces
WeatherManagement.Functions    → Azure Functions (sync log → Cosmos DB)
WeatherManagement.Test         → NUnit unit tests
```

### Key Technologies

| Concern | Technology |
|---------|-----------|
| Framework | .NET 8 |
| ORM | Entity Framework Core 8 |
| Job Scheduler | Hangfire |
| External API Client | Refit (OpenWeatherMap) |
| Logging | Serilog (Console, File, Azure Cosmos DB) |
| Secrets | Azure Key Vault via `DefaultAzureCredential` |
| Serverless | Azure Functions v4 (isolated worker) |
| Containerisation | Docker + Docker Hub |
| CI/CD | GitHub Actions |

### Environments

| Environment | Branch | Database | Hosting | Pipeline |
|-------------|--------|----------|---------|----------|
| Local | any | PostgreSQL (Docker Compose) | localhost:8080 | none |
| Dev | `Develop` | PostgreSQL (Railway managed service) | Railway | `dev.yml` |
| Staging | `master` | SQL Server (Azure free tier) | Azure App Service | `staging.yml` |

> **Important — branches have different code, not just different config.**
>
> The `Develop` branch uses `Npgsql.EntityFrameworkCore.PostgreSQL` + `Hangfire.PostgreSql` and connects via `ConnectionStrings:Dev`.
>
> The `master` branch uses `Microsoft.EntityFrameworkCore.SqlServer` + `Hangfire.SqlServer` and connects via `ConnectionStrings:DefaultConnection`.
>
> Never merge master into Develop expecting the database code to work — they are intentionally different.

#### Branch → Deploy Flow

```
Develop branch
    │
    ├── push → dev.yml triggers
    │               ├── build & test
    │               ├── push Docker image → Docker Hub (tag: dev-latest)
    │               ├── deploy Azure Function (weather-sync-fn)
    │               └── redeploy Railway service (pulls dev-latest from Docker Hub)
    │
    └── App runs on Railway
            ├── Database: PostgreSQL (Railway managed)
            ├── Secrets: Azure Key Vault (ConnectionStrings--Dev, WeatherApi--ApiKey)
            └── URL: yourapp.up.railway.app

master branch
    │
    ├── push → staging.yml triggers
    │               ├── build & test
    │               ├── push Docker image → Docker Hub (tag: staging-latest)
    │               ├── deploy Azure Function (weather-sync-fn)
    │               ├── configure App Service env vars (Key Vault references)
    │               └── deploy container to Azure App Service
    │
    └── App runs on Azure App Service
            ├── Database: SQL Server (Azure free tier)
            ├── Secrets: Azure Key Vault (ConnectionStrings--DefaultConnection, WeatherApi--ApiKey)
            └── URL: yourappname.azurewebsites.net
```

---

## Project Structure

```
WeatherManagement.Api/
├── Controllers/WeatherController.cs     # All REST endpoints
├── Middleware/GlobalExceptionMiddleware  # Global error handling
├── Program.cs                           # Startup, DI, Key Vault, Serilog
├── appsettings.json                     # App config (secrets left blank)
└── Dockerfile                           # Multi-stage Docker build (port 8080)

WeatherManagement.Core/
└── Service/WeatherService.cs            # Read-side business logic

WeatherManagement.Domain/
├── Entities/Location.cs                 # Location entity
├── Entities/WeatherData.cs              # Weather reading entity
└── Contracts/ISyncNotifier.cs           # Interface for Azure Function call

WeatherManagement.Infrastructure/
├── Data/WeatherDbContext.cs             # EF Core DbContext
├── Data/DataSeed/DbSeeder.cs            # Seeds locations on startup
├── Configuration/WeatherSettings.cs    # Strongly typed settings
├── Integration/IWeatherApi.cs          # Refit client for OpenWeatherMap
├── Integration/WeatherOrchestratorService.cs  # Fetch + store orchestration
├── Job/WeatherFetchJob.cs              # Hangfire scheduled job
├── Repo/GenericRepository.cs           # Generic repository pattern
├── Services/AzureFunctionSyncNotifier  # Notifies Azure Function after sync
├── Migrations/                          # EF Core migrations (SQL Server)
└── DependencyInjection.cs              # All service registrations

WeatherManagement.Functions/
├── SyncLogFunction.cs                   # HTTP trigger → writes to Cosmos DB
└── Program.cs                           # Functions host + Cosmos DI

.github/workflows/
├── dev.yml                              # Dev pipeline (Develop → Railway)
└── staging.yml                          # Staging pipeline (master → Azure)
```

---

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/weather` | List all weather readings |
| GET | `/weather/{id}` | Get weather by ID |
| GET | `/weather/current` | Current weather for all locations |
| GET | `/weather/history` | Historical weather readings |
| GET | `/weather/compare` | Compare weather across cities |
| GET | `/weather/summary` | Aggregated statistics |
| POST | `/weather/sync` | Manually trigger a weather sync |
| GET | `/` | Redirects to `/swagger` |
| GET | `/hangfire` | Hangfire dashboard (local only) |

---

## Running Locally

### Prerequisites
- Docker Desktop
- .NET 8 SDK
- A free OpenWeatherMap API key (openweathermap.org)

### 1. Create `.env` file in solution root

```env
POSTGRES_USER=syed
POSTGRES_PASSWORD=syedpass
POSTGRES_DB=postgres
PGADMIN_DEFAULT_EMAIL=admin@admin.com
PGADMIN_DEFAULT_PASSWORD=admin
WEATHER_API_KEY=your_openweathermap_api_key
Azure__KeyVaultUri=
AZURE_CLIENT_ID=
AZURE_CLIENT_SECRET=
AZURE_TENANT_ID=
```

> Leave Azure fields blank for local dev — Key Vault is skipped when `Azure__KeyVaultUri` is empty.

### 2. Start containers

```bash
docker-compose up --build
```

### 3. Open

- API + Swagger: http://localhost:8080/swagger
- pgAdmin: http://localhost:5050 (admin@admin.com / admin)
- Hangfire: http://localhost:8080/hangfire

### What happens on startup
1. EF Core runs pending migrations against PostgreSQL
2. `DbSeeder` inserts 5 default locations (Chennai, London, New York, Tokyo, Sydney) if not present
3. Hangfire registers a recurring job to fetch weather every 10 minutes

---

## CI/CD Pipelines

### Dev Pipeline (`dev.yml`)
**Trigger:** Push to `Develop` branch

```
Build & Test → Push Docker image (dev-latest) → Deploy Azure Function → Redeploy Railway
```

### Staging Pipeline (`staging.yml`)
**Trigger:** Push to `master` branch

```
Build & Test → Push Docker image (staging-latest) → Deploy Azure Function → Configure App Service → Deploy to Azure App Service
```

---

## GitHub Secrets Required

### Docker Hub
| Secret | Description |
|--------|-------------|
| `DOCKERHUB_USERNAME` | Docker Hub username |
| `DOCKERHUB_TOKEN` | Docker Hub access token |

### Azure Identity
| Secret | Description |
|--------|-------------|
| `AZURE_CLIENT_ID` | Service principal app (client) ID |
| `AZURE_CLIENT_SECRET` | Service principal client secret |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |

### Azure Resources
| Secret | Description |
|--------|-------------|
| `AZURE_RESOURCE_GROUP` | Resource group name |
| `AZURE_WEBAPP_NAME` | App Service name |
| `AZURE_KEYVAULT_URI` | Key Vault URI (e.g. `https://mykeyvault.vault.azure.net/`) |
| `AZURE_KEYVAULT_NAME` | Key Vault name (no URL, no suffix) |
| `AZURE_FUNCTION_SYNC_URL` | Full Azure Function URL with `?code=...` |

### Railway (Dev only)
| Secret | Description |
|--------|-------------|
| `RAILWAY_TOKEN` | Railway API token |
| `RAILWAY_SERVICE_ID` | Railway service ID |

---

## Azure Key Vault Secrets

All secrets in deployed environments come from Key Vault. Secret names use `--` as the `:` separator.

| Key Vault Secret Name | Maps to Config Key | Description |
|----------------------|-------------------|-------------|
| `ConnectionStrings--DefaultConnection` | `ConnectionStrings:DefaultConnection` | SQL Server connection string (staging) |
| `ConnectionStrings--Dev` | `ConnectionStrings:Dev` | PostgreSQL connection string (dev/Railway) |
| `WeatherApi--ApiKey` | `WeatherApi:ApiKey` | OpenWeatherMap API key |
| `CosmosDb--ConnectionString` | `CosmosDb:ConnectionString` | Cosmos DB connection string (Functions) |

---

## Azure Function — Sync Log

Every time a weather sync completes (via API call or Hangfire job), the app calls the Azure Function:

```
POST https://weather-sync-fn.azurewebsites.net/api/sync/log?code=...
Content-Type: application/json

{
  "triggeredBy": "HangfireJob",
  "locationCount": 5
}
```

The function writes a `SyncLogEntry` to Cosmos DB:
- Database: `weatherlogs`
- Container: `synclogs`
- Partition key: `/id`

---

## Configuration Flow

```
Local:   .env → docker-compose.override.yml → app reads config
Dev:     GitHub Secrets → pipeline sets Railway env vars + Function App settings → Key Vault for rest
Staging: GitHub Secrets → pipeline sets App Service settings → Key Vault references resolve at runtime
```

The Key Vault URI is the only secret set directly as an env var. All others are pulled from Key Vault at runtime.
