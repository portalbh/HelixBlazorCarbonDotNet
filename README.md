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

| Parameter | Values | Default |
|-----------|--------|---------|
| `--auth` | `None`, `Azure`, `BFF`, `Advanced` | `None` |

```bash
dotnet new myt3-carbon-saas -n Contoso
dotnet new myt3-carbon-saas -n Contoso --auth BFF
dotnet new myt3-carbon-saas -n Contoso --auth Advanced
dotnet new myt3-carbon-saas -n Contoso --auth Azure
```

## Pack and install

```powershell
cd C:\Repo\template
dotnet pack HelixCarbon.TemplatePack.csproj -o ./artifacts
dotnet new install .\artifacts\PortalBH.HelixCarbon.SaaS.1.0.4.nupkg
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
