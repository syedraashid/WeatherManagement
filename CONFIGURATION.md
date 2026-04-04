# Configuration & Deployment Guide

Complete guide covering branch strategy, Azure portal setup, Railway setup, errors encountered, and how they were fixed.

---

## Table of Contents

1. [Branch & Environment Strategy](#1-branch--environment-strategy)
2. [Azure Resources Setup](#2-azure-resources-setup)
3. [Railway Setup (Dev)](#3-railway-setup-dev)
4. [Key Vault Secrets](#4-key-vault-secrets)
5. [GitHub Actions Secrets](#5-github-actions-secrets)
6. [Errors Encountered & Fixes](#6-errors-encountered--fixes)

---

## 1. Branch & Environment Strategy

This project uses **two separate branches with different database stacks**. This is not a config switch — the actual NuGet packages and connection code differ between branches.

### Branch Comparison

| | `Develop` branch | `master` branch |
|--|-----------------|----------------|
| **Deploys to** | Railway | Azure App Service |
| **Database** | PostgreSQL | SQL Server |
| **EF Core package** | `Npgsql.EntityFrameworkCore.PostgreSQL` | `Microsoft.EntityFrameworkCore.SqlServer` |
| **Hangfire package** | `Hangfire.PostgreSql` | `Hangfire.SqlServer` |
| **DI method** | `UseNpgsql(...)` | `UseSqlServer(...)` |
| **Hangfire storage** | `UsePostgreSqlStorage(...)` | `UseSqlServerStorage(...)` |
| **Connection string key** | `ConnectionStrings:Dev` | `ConnectionStrings:DefaultConnection` |
| **Key Vault secret** | `ConnectionStrings--Dev` | `ConnectionStrings--DefaultConnection` |
| **DbContext column types** | `double precision`, `timestamptz` | EF Core defaults (no explicit types) |
| **Pipeline file** | `.github/workflows/dev.yml` | `.github/workflows/staging.yml` |

### Why separate branches instead of conditional logic?

Keeping environment-specific database code in the same branch adds risk — a merge conflict or accidental config swap could break both environments. Each branch is a clean, independent deployment target.

### Flow

```
Local Development
├── Any branch
├── docker-compose up --build
├── PostgreSQL runs in Docker container
└── App at http://localhost:8080/swagger

Develop branch → dev.yml pipeline
├── Triggers on: push to Develop
├── Builds Docker image → pushes to Docker Hub as :dev-latest
├── Deploys Azure Function (weather-sync-fn)
├── Railway pulls :dev-latest and restarts
├── App reads ConnectionStrings--Dev from Key Vault
├── Database: PostgreSQL (Railway managed)
└── App URL: https://yourapp.up.railway.app

master branch → staging.yml pipeline
├── Triggers on: push to master
├── Builds Docker image → pushes to Docker Hub as :staging-latest
├── Deploys Azure Function (weather-sync-fn)
├── Sets App Service environment variables (Key Vault references)
├── Azure App Service pulls :staging-latest and restarts
├── App reads ConnectionStrings--DefaultConnection from Key Vault
├── Database: SQL Server (Azure free tier)
└── App URL: https://yourappname.azurewebsites.net
```

### Shared across both environments

- Same Docker image base (only the compiled code differs between branches)
- Same Azure Function (`weather-sync-fn`) — both pipelines deploy to it
- Same Azure Key Vault — secrets are shared, only the connection string secret name differs
- Same OpenWeatherMap API key (`WeatherApi--ApiKey`)
- Same Cosmos DB for sync logs and Serilog sink

---

## 2. Azure Resources Setup

### 1.1 Resource Group
Create one resource group for everything to keep it organised and avoid cross-region charges.

- Portal → **Create a resource** → **Resource Group**
- Name: e.g. `weather-rg`
- Region: pick one and use it for everything (e.g. `East US`)

---

### 1.2 Azure Key Vault

- Portal → Create a resource → **Key Vault**
- Name: e.g. `myweatherkeyvault`
- Region: same as resource group
- Pricing tier: **Standard**
- Permission model: **Azure role-based access control (RBAC)** ← default on new vaults

After creation, note the **Vault URI** from Overview (e.g. `https://myweatherkeyvault.vault.azure.net/`).

**Grant pipeline service principal access:**
- Key Vault → **Access control (IAM)** → **+ Add role assignment**
- Role: **Key Vault Secrets Officer**
- Member: your service principal (used in GitHub Actions)

---

### 1.3 SQL Server + Database (Free Tier)

- Portal → Create a resource → **SQL Database**
- Database name: `WeatherDb`
- Server: **Create new**
  - Server name: e.g. `weather-sqlserver`
  - Authentication: **SQL authentication**
  - Admin login: e.g. `weatheradmin`
  - Password: e.g. `Weather@2024!`
  - Region: same as resource group
- Workload environment: **Development**
- Compute + storage: **Free offer** (32 GB, serverless General Purpose) — shows `$0.00/month`
- Networking tab: **Allow Azure services and resources to access this server** → **Yes**
- Click **Review + Create** → **Create**

**Connection string format:**
```
Server=weather-sqlserver.database.windows.net;Database=WeatherDb;User Id=weatheradmin;Password=Weather@2024!;TrustServerCertificate=True;
```

Add this to Key Vault as secret `ConnectionStrings--DefaultConnection`.

---

### 1.4 Azure App Service (Free Tier)

- Portal → Create a resource → **Web App**

**Basics tab:**

| Field | Value |
|-------|-------|
| Resource Group | `weather-rg` |
| Name | e.g. `weather-api-staging` |
| Publish | **Container** |
| Operating System | **Linux** |
| Region | Same as everything else |
| Linux Plan | Create new → name `weather-plan` |
| Pricing plan | **Free F1** ($0.00/month) |

**Container tab:**

| Field | Value |
|-------|-------|
| Image Source | Docker Hub |
| Access Type | Public |
| Registry server URL | `https://index.docker.io` |
| Image and tag | `yourdockerhubuser/weathermanagementapi:staging-latest` |
| Port | `8080` |
| Startup Command | *(blank)* |

**Monitoring tab:**
- Application Insights → **No** (skip to avoid extra cost)

Click **Review + Create** → confirm **Free F1** → **Create**.

**After creation — critical settings:**

1. App Service → **Settings** → **Environment variables** → add:
   - Name: `WEBSITES_PORT` Value: `8080`
   - Click Apply

2. App Service → **Settings** → **Identity** → System assigned → **On** → Save
   - Copy the **Object (principal) ID** that appears

3. Grant managed identity access to Key Vault:
   - Key Vault → **Access control (IAM)** → **+ Add role assignment**
   - Role: **Key Vault Secrets User**
   - Member: **Managed identity** → select your App Service
   - Review + assign

---

### 1.5 Azure Cosmos DB

- Portal → Create a resource → **Azure Cosmos DB**
- API: **Azure Cosmos DB for NoSQL**
- Account name: e.g. `weathercosmosdb`
- Region: same as everything else
- Capacity mode: **Serverless** (pay per request, no minimum cost)

After creation:
- Note the **URI** (Endpoint) from Overview
- Keys → note **Primary Key**

The app auto-creates the database `weatherlogs` and containers `synclogs` (partition key `/id`) and `applogs` on first run.

Add to Key Vault:
- `CosmosDb--ConnectionString` → primary connection string from Keys blade

---

### 1.6 Azure Function App

- Portal → Create a resource → **Function App**
- Name: `weather-sync-fn`
- Runtime stack: **.NET**
- Version: **8 (LTS), isolated worker model**
- OS: **Linux**
- Hosting: **Consumption (Serverless)** — free tier
- Storage: create new storage account

After creation:
- Identity → System assigned → **On** → Save
- Grant managed identity **Key Vault Secrets User** role on Key Vault (same as App Service above)

Get the function URL after first deploy:
- Function App → Functions → **LogSync** → **Get Function URL** → copy full URL with `?code=...`
- Add this as GitHub secret `AZURE_FUNCTION_SYNC_URL`

---

### 1.7 Service Principal (for CI/CD)

Required so GitHub Actions can authenticate to Azure.

```bash
az ad sp create-for-rbac --name "weather-cicd-sp" --role Contributor --scopes /subscriptions/YOUR_SUBSCRIPTION_ID
```

This outputs:
```json
{
  "appId":       "← AZURE_CLIENT_ID",
  "password":    "← AZURE_CLIENT_SECRET",
  "tenant":      "← AZURE_TENANT_ID"
}
```

Also note your **Subscription ID** from Portal → Subscriptions.

---

## 3. Railway Setup (Dev)

### 2.1 Create Project
- railway.app → New Project → **Empty Project**

### 2.2 Add PostgreSQL Service
- In project → **+ New** → **Database** → **PostgreSQL**
- Railway provisions a managed PostgreSQL instance
- Copy the connection string from the Variables tab

### 2.3 Add API Service
- **+ New** → **Empty Service**
- Settings → **Source** → Docker Image → `yourdockerhubuser/weathermanagementapi:dev-latest`
- Port: `8080`

### 2.4 Set Environment Variables
In your Railway API service → **Variables** tab, add:

| Variable | Value |
|----------|-------|
| `Azure__KeyVaultUri` | `https://yourkeyvault.vault.azure.net/` |
| `AZURE_CLIENT_ID` | Your service principal client ID |
| `AZURE_CLIENT_SECRET` | Your service principal client secret |
| `AZURE_TENANT_ID` | Your tenant ID |

Everything else (connection string, weather API key) comes from Key Vault automatically.

### 2.5 Key Vault Secret for Dev
Add to Key Vault:
- Name: `ConnectionStrings--Dev`
- Value: PostgreSQL connection string from Railway (format: `Host=...;Port=5432;Database=railway;Username=postgres;Password=...`)

### 2.6 Redeploy
The dev pipeline (`dev.yml`) pushes a new Docker image on every push to `Develop`. Railway is configured to watch Docker Hub and pull the latest image automatically, or trigger via Railway CLI in the pipeline.

---

## 4. Key Vault Secrets

All secrets use `--` as separator (maps to `:` in .NET configuration).

| Secret Name | Value Format | Used By |
|-------------|-------------|---------|
| `ConnectionStrings--DefaultConnection` | SQL Server connection string | API (staging) |
| `ConnectionStrings--Dev` | PostgreSQL connection string | API (dev/Railway) |
| `WeatherApi--ApiKey` | OpenWeatherMap API key | API (both envs) |
| `CosmosDb--ConnectionString` | Cosmos DB primary connection string | Azure Function |

---

## 5. GitHub Actions Secrets

Repository → **Settings** → **Secrets and variables** → **Actions**

### Shared (both pipelines)
| Secret | Where to find |
|--------|--------------|
| `DOCKERHUB_USERNAME` | Docker Hub account name |
| `DOCKERHUB_TOKEN` | Docker Hub → Account Settings → Security → New Access Token |
| `AZURE_CLIENT_ID` | Entra ID → App registrations → your app → Overview |
| `AZURE_CLIENT_SECRET` | Entra ID → App registrations → Certificates & secrets |
| `AZURE_TENANT_ID` | Entra ID → Overview → Tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Portal → Subscriptions |
| `AZURE_RESOURCE_GROUP` | Your resource group name |
| `AZURE_KEYVAULT_URI` | Key Vault → Overview → Vault URI |
| `AZURE_KEYVAULT_NAME` | Just the vault name (no URL) |
| `AZURE_FUNCTION_SYNC_URL` | Function App → LogSync → Get Function URL |

### Dev pipeline only
| Secret | Where to find |
|--------|--------------|
| `RAILWAY_TOKEN` | Railway → Account Settings → Tokens |
| `RAILWAY_SERVICE_ID` | Railway → Service → Settings → Service ID |

---

## 6. Errors Encountered & Fixes

### 5.1 PostgreSQL — Host can't be null
**Error:** `ArgumentNullException: Host can't be null`

**Cause:** Key Vault secret `ConnectionStrings--Dev` had `Host=localhost` instead of the Docker service name.

**Fix:** Update Key Vault secret to use `Host=postgres` (the Docker Compose service name) for local, or the Railway host for dev.

---

### 5.2 PostgreSQL — Database does not exist
**Error:** `PostgresException: database "syed" does not exist`

**Cause:** Connection string in Key Vault was missing `Database=postgres` (defaulted to username as DB name).

**Fix:** Add `Database=postgres` explicitly to the connection string in Key Vault.

---

### 5.3 Azure Function — 500 from CosmosDBOutput binding
**Error:** `500 Internal Server Error` from `[CosmosDBOutput]` binding attribute.

**Cause:** The `[CosmosDBOutput]` binding reads from App Settings directly (not `IConfiguration`), so Key Vault references were not resolved.

**Fix:** Removed the binding entirely. Injected `CosmosClient` via DI and wrote to Cosmos DB directly using the SDK. Key Vault secrets flow through `IConfiguration` correctly this way.

---

### 5.4 Cosmos DB — id field missing (400)
**Error:** `BadRequest: The input content is invalid because the required properties - 'id' - are missing`

**Cause:** `CosmosClient` uses Newtonsoft.Json internally. `[JsonPropertyName("id")]` (System.Text.Json) is ignored by it. The property `Id` was serialised as `Id` (capital I) instead of `id`.

**Fix:** Added `CosmosPropertyNamingPolicy.CamelCase` to `CosmosClientOptions`:
```csharp
new CosmosClient(connectionString, new CosmosClientOptions
{
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    }
})
```

---

### 5.5 Cosmos DB — PartitionKey mismatch (400)
**Error:** `PartitionKey extracted from document doesn't match the one specified in the header`

**Cause:** The container `synclogs` was previously created manually with a wrong partition key path. The code used `/id` but the container had a different path.

**Fix:** Delete the old container in Cosmos DB portal. The code uses `CreateContainerIfNotExistsAsync("synclogs", "/id")` which recreates it with the correct partition key.

---

### 5.6 Azure Login — OIDC Forbidden
**Error:** `Error: Forbidden` on `azure/login@v2` step.

**Cause:** `azure/login@v2` defaults to OIDC (federated credentials). The service principal was not configured for OIDC — it uses a client secret instead.

**Fix:** Added `client-secret` parameter to the login action:
```yaml
- uses: azure/login@v2
  with:
    client-id: ${{ secrets.AZURE_CLIENT_ID }}
    tenant-id: ${{ secrets.AZURE_TENANT_ID }}
    subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
    client-secret: ${{ secrets.AZURE_CLIENT_SECRET }}
```

---

### 5.7 Two pipelines triggering on same branch
**Problem:** Both `dev.yml` and `staging.yml` had `pull_request` triggers, causing both to run on every PR regardless of branch.

**Fix:** Removed `pull_request` triggers from both pipelines. Now:
- `dev.yml` triggers only on push to `Develop`
- `staging.yml` triggers only on push to `master`

---

### 5.8 ISyncNotifier circular dependency
**Error:** Build error — circular project reference when `ISyncNotifier` was defined in `WeatherManagement.Core`.

**Cause:** `Core` referenced `Infrastructure` and `Infrastructure` was trying to reference `Core` for the interface.

**Fix:** Moved `ISyncNotifier` to `WeatherManagement.Domain` which has no upstream dependencies.

---

### 5.9 Hangfire startup crash
**Error:** `InvalidOperationException: JobStorage.Current property value has not been initialized`

**Cause:** `RecurringJob.AddOrUpdate` (static API) was called before Hangfire storage was fully initialised at startup.

**Fix:** Used the DI-based `IRecurringJobManager` instead of the static `RecurringJob` class:
```csharp
var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobs.AddOrUpdate<WeatherFetchJob>("weather-fetch", job => job.ExecuteAsync(), cronExpression);
```

---

### 5.10 Azure Function disappeared from portal after deploy
**Problem:** `LogSync` function was not listed in Function App after pipeline ran.

**Cause:** `azure/functions-action` was deploying without a `dotnet publish` step first — it was packaging source code instead of compiled output.

**Fix:** Added explicit publish step before deploy:
```yaml
- name: Publish Azure Function
  run: dotnet publish WeatherManagement.Functions/WeatherManagement.Functions.csproj -c Release -o ./functions-output

- name: Deploy Azure Function
  uses: azure/functions-action@v1
  with:
    app-name: weather-sync-fn
    package: ./functions-output
```

---

### 5.11 `UseHttpsRedirection` breaking Swagger in Docker
**Problem:** Swagger was not accessible when running in Docker — HTTPS redirect was causing a redirect loop.

**Fix:** Wrapped HTTPS redirect in environment check:
```csharp
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
```

---

### 5.12 Duplicate migrations — build failure
**Error:**
```
error CS0579: Duplicate 'DbContext' attribute
error CS0111: Type 'initial' already defines a member called 'Up'
```

**Cause:** Two migration files with the same class name `initial` existed in the Migrations folder (timestamps `20260331` and `20260403`).

**Fix:** Deleted all old PostgreSQL migrations (they were incompatible with SQL Server anyway). Generated a fresh SQL Server migration:
```bash
dotnet ef migrations add InitialCreate --project WeatherManagement.Infrastructure --startup-project WeatherManagement.Api
```

---

### 5.13 App Service container timeout (ContainerTimeout)
**Error:** `Container did not start within expected time limit of 230s`

**Cause:** App Service defaulted to port 80 but the .NET 8 container listens on port 8080.

**Fix:** Added `WEBSITES_PORT = 8080` in App Service → Settings → Environment variables.

---

### 5.14 Key Vault Forbidden on App Service startup
**Error:** `Encountered an error (Forbidden) from extensions API`

**Cause:** App Service managed identity did not have the `Key Vault Secrets User` role on the Key Vault.

**Fix:**
1. App Service → Identity → System assigned → **On** → Save
2. Key Vault → Access control (IAM) → Add role assignment → **Key Vault Secrets User** → select App Service managed identity

---

## Local Development Quick Reference

```bash
# 1. Copy and fill .env
cp .env.example .env

# 2. Start everything
docker-compose up --build

# 3. Access
# Swagger:  http://localhost:8080/swagger
# pgAdmin:  http://localhost:5050
# Hangfire: http://localhost:8080/hangfire
```

## Deployment Quick Reference

```bash
# Deploy to dev (Railway)
git push origin Develop

# Deploy to staging (Azure)
git push origin master
```
