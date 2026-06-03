# HelixCarbon — T3-Style SaaS `dotnet new` Template

**HelixCarbon** is a modern, T3-inspired multi-tenant SaaS starter for .NET 10:

- **Blazor Web App** with **Interactive WebAssembly** UI
- **[portalbh/CarbonBlazor](https://github.com/portalbh/CarbonBlazor)** (vendored under `lib/CarbonBlazor`)
- **Minimal APIs** + **Dapper**
- **SQLite** (dev) / **PostgreSQL** (prod)
- **Shared-database multi-tenancy** (subdomain + `X-Tenant` header)
- **Blazor-ApexCharts** dashboard
- Optional **Tailwind** utilities (`npm run build:css` in Client)

## Template parameters

| Parameter | CLI flag | Values | Default |
|-----------|----------|--------|---------|
| Authentication | `--auth` | `None`, `BFF`, `Advanced`, `Azure` | `BFF` |
| Database | `--database` | `Sqlite`, `Postgres` | `Sqlite` |
| Seed demo data | `--seedDemoData` | `true` / `false` | `true` |
| Default tenant slug | `--defaultTenant` | any slug | `demo` |
| Enable HTTPS | `--useHttps` | `true` / `false` | `true` |

```bash
dotnet new myt3-carbon-saas -n Contoso
dotnet new myt3-carbon-saas -n Contoso --auth BFF --database Postgres
dotnet new myt3-carbon-saas -n Contoso --auth Advanced --seedDemoData false --useHttps false
dotnet new myt3-carbon-saas -n Contoso --auth Azure --defaultTenant acme
```

## Visual Studio 2022+

1. Install the template pack (once per machine):

   ```powershell
   dotnet pack HelixCarbon.TemplatePack.csproj -o ./artifacts
   dotnet new install .\artifacts\PortalBH.HelixCarbon.SaaS.1.1.0.nupkg
   ```

2. In Visual Studio: **Create a new project** → search **HelixCarbon** or **T3 SaaS**.

3. Use the wizard to pick **Authentication**, **Database**, **Seed demo data**, **Default tenant slug**, and **Enable HTTPS**. The generated solution is ready to open and run.

4. Update `appsettings.json` connection strings (especially PostgreSQL) before deploying. The `App` section (seed, default tenant) is not renamed when you use `-n`; only project/namespace names change.

> VS uses the same `template.json` parameters as `dotnet new`; no separate `.vstemplate` is required.

## Pack and install

```powershell
cd C:\Repo\template
dotnet pack HelixCarbon.TemplatePack.csproj -o ./artifacts
dotnet new install .\artifacts\PortalBH.HelixCarbon.SaaS.1.1.0.nupkg
dotnet new list myt3-carbon-saas
```

Uninstall:

```powershell
dotnet new uninstall PortalBH.HelixCarbon.SaaS
```

## Run generated app

```powershell
cd Contoso\src\HelixCarbon.Server
dotnet run
```

Open `https://localhost:7151` and send tenant context:

- Header: `X-Tenant: demo` (default dev tenant), or
- Subdomain: `demo.localhost` (configure hosts file as needed)

**Demo login** (`--auth BFF` or `Advanced`): `admin@demo.local` / `Admin123!`

## Project layout

```text
src/
  HelixCarbon.Shared/   # DTOs, enums, models
  HelixCarbon.Server/   # APIs, middleware, host
  HelixCarbon.Client/   # Carbon UI, features
lib/CarbonBlazor/       # Vendored Carbon RCL (swap for NuGet when published)
```

## Extending

- **Database per tenant**: resolve connection string in `TenantResolutionMiddleware` instead of `ITenantContext` only.
- **Auth**: replace cookie BFF with [Duende BFF](https://docs.duendesoftware.com/bff/) or keep Entra JWT validation in `AddHelixCarbonAuth`.
- **CarbonBlazor**: when a NuGet package is published, remove `lib/CarbonBlazor` and add `<PackageReference Include="CarbonBlazor" />`.

## CarbonBlazor maintenance

Refresh vendored library from upstream:

```powershell
git clone --depth 1 https://github.com/portalbh/CarbonBlazor.git _carbon_tmp
Copy-Item -Recurse -Force _carbon_tmp\CarbonBlazor lib\CarbonBlazor
```

## Tailwind

```powershell
cd src/HelixCarbon.Client
npm install
npm run build:css
```
