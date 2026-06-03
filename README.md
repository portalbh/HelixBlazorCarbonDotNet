# HelixCarbon — T3-Style SaaS `dotnet new` Template

**HelixCarbon** is a modern, T3-inspired multi-tenant SaaS starter for .NET 10:

- **Blazor Web App** with **Interactive WebAssembly** UI
- **[portalbh/CarbonBlazor](https://github.com/portalbh/CarbonBlazor)** (vendored under `lib/CarbonBlazor`)
- **Minimal APIs** + **Dapper**
- **SQLite** (dev) / **PostgreSQL** (prod)
- **Shared-database multi-tenancy** (subdomain + `X-Tenant` header)
- **Blazor-ApexCharts** dashboard
- **Carbon theme switcher** in the header (White / Gray 10 / 90 / 100)

## Template parameters

| Parameter | CLI flag | Values | Default |
|-----------|----------|--------|---------|
| Authentication | `--auth` (`authMode`) | `None`, `BFF`, `Advanced`, `Azure` | `BFF` |
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

The generated output is a full solution at `{Name}.slnx` (CLI) with `src/{Name}.Shared`, `src/{Name}.Client`, `src/{Name}.Server`, and `lib/CarbonBlazor`, plus `Directory.Build.props`, `Directory.Packages.props`, and `global.json`.

## Visual Studio 2022+

1. Install the template pack (once per machine):

   ```powershell
   dotnet pack HelixCarbon.TemplatePack.csproj -o ./artifacts
   dotnet new install .\artifacts\PortalBH.HelixCarbon.SaaS.1.1.3.nupkg
   ```

2. In Visual Studio: **Create a new project** → search **HelixCarbon** or **T3 SaaS**.

3. Use the wizard to pick **Authentication**, **Database**, **Seed demo data**, **Default tenant slug**, and **Enable HTTPS**. Visual Studio creates a **solution with all projects** (Shared, Client, Server, and vendored CarbonBlazor) and opens it automatically.

4. Update `appsettings.json` connection strings (especially PostgreSQL) before deploying. The `App` section (seed, default tenant) is not renamed when you use `-n`; only project/namespace names change.

> VS uses the same `template.json` parameters as `dotnet new` (via `ide.host.json`). The parameter is named `authMode` (not `auth`) so Visual Studio shows a normal dropdown instead of the built-in Entra/Individual auth picker.

After reinstalling the template pack, restart Visual Studio or run `devenv /updateconfiguration` if options do not appear immediately.

Visual Studio runs **Server** as the startup project via the bundled `*.slnLaunch` profile (Blazor host). The default project name is **HelixCarbon** unless you change it on the first wizard screen.

## Pack and install

```powershell
cd C:\Repo\template
dotnet pack HelixCarbon.TemplatePack.csproj -o ./artifacts
dotnet new install .\artifacts\PortalBH.HelixCarbon.SaaS.1.1.3.nupkg
dotnet new list myt3-carbon-saas
```

Uninstall:

```powershell
dotnet new uninstall PortalBH.HelixCarbon.SaaS
```

## Run generated app

```powershell
cd Contoso
dotnet build Contoso.slnx
dotnet run --project src/Contoso.Server
```

Open `https://localhost:7151` and send tenant context:

- Header: `X-Tenant: demo` (default dev tenant), or
- Subdomain: `demo.localhost` (configure hosts file as needed)

**Demo login** (`--auth BFF` or `Advanced`): `admin@demo.local` / `Admin123!`

## Project layout

```text
HelixCarbon.app.slnx    # Solution (renamed to {Name}.slnx when created via CLI)
src/
  HelixCarbon.Shared/   # DTOs, enums, models
  HelixCarbon.Server/   # APIs, middleware, host
  HelixCarbon.Client/   # Carbon UI, features
lib/CarbonBlazor/       # Vendored Carbon RCL (swap for NuGet when published)
Directory.Build.props
Directory.Packages.props
global.json
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

