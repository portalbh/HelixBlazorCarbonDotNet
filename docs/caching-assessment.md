# Caching Strategy Assessment — HelixCarbon Blazor WASM Frontend

**Date:** 2026-07-03
**Scope:** `src/HelixCarbon.Client`, `src/HelixCarbon.Client.Charts`, `src/HelixCarbon.Server`, `lib/CarbonBlazor`
**Focus:** stale data, cross-user/cross-tenant leakage, asset versioning, plus a general review of the caching strategy.
**Note:** Findings L1, L2, S1, A2, A3, and the tenant-header trust note were remediated in TASK-01 through TASK-06 in `docs/caching-remediation-plan.md`.

---

## 1. Executive Summary

The template's caching posture is **conservative and mostly safe by default**. Static assets are handled well by .NET 10 `MapStaticAssets` (content fingerprinting, immutable caching, ETags, integrity hashes). There is **no service worker / PWA offline cache**, no client-side data cache beyond one in-memory auth state object, and no server-side caching layer — so most classic staleness and leakage traps simply don't exist yet.

The most important finding is a **latent cross-tenant leakage risk**: API responses carry **no `Cache-Control` headers at all**, and tenant identity is resolved from a request header (`X-Tenant`) with **no `Vary` header emitted**. The template itself ships no shared cache, so nothing leaks today — but the moment this template is deployed behind a CDN, reverse proxy, or output cache (a very common production step), tenant A's `GET /api/products` response could be served to tenant B from the same URL. Because this is a *template* that others will build on, the safe defaults should be baked in now.

Secondary findings: the client's in-memory auth state can go stale relative to the server cookie session, and two CarbonBlazor assets bypass fingerprinting (a performance gap, not a safety one).

---

## 2. Current Caching Inventory (What Exists Today)

### 2.1 Static assets — `MapStaticAssets` + fingerprinting

- The server calls `MapStaticAssets()` (`src/HelixCarbon.Server/Program.cs`), and `App.razor` references assets via `@Assets["..."]`, `<ImportMap />`, and `<ResourcePreloader />`.
- Verified from the publish manifest (`HelixCarbon.Client.staticwebassets.endpoints.json`):
  - **Fingerprinted routes** (e.g. `apexcharts.fipu15mg5j.css`, `dotnet.native.j9yxww2air.js`): `Cache-Control: max-age=31536000, immutable` + ETag + SHA-256 integrity.
  - **Un-fingerprinted routes** (the original file names): `Cache-Control: no-cache` + ETag, i.e. revalidated on every load.
- The Blazor runtime and assemblies (`_framework/*`) use standard HTTP caching with fingerprinted names (modern .NET no longer uses the JS Cache Storage API for boot resources), with integrity checks against the boot manifest.
- The Charts assembly is lazy-loaded (`BlazorWebAssemblyLazyLoad` in `HelixCarbon.Client.csproj`, `LazyAssemblyLoader` in `DashboardMetricsLoader.razor`) and served as a fingerprinted, immutable `_framework` asset.

### 2.2 API / data caching

- **Server:** none. No `ResponseCaching`, no `OutputCache`, no `IMemoryCache`/`HybridCache`, no `Cache-Control`, `Vary`, or `ETag` headers on any `/api/*` endpoint (`ApiEndpoints.cs`). Every request hits the database via Dapper, including the per-request tenant lookup in `TenantResolutionMiddleware`.
- **Client:** `HelixApiClient` performs plain `HttpClient` GET/POST calls with no client-side caching. Pages (Products, Dashboard, Tenants) re-fetch on every navigation. The only cached data object is `AuthStateService.Profile` (in-memory, lives for the WASM app's lifetime).

### 2.3 Browser storage

- `localStorage`: only the CarbonBlazor theme preference (`cb-theme` in `lib/CarbonBlazor/wwwroot/carbon-blazor.js`). Not sensitive.
- MSAL (AuthAzure variant): tokens cached by `Microsoft.Authentication.WebAssembly.Msal` in its default location (`sessionStorage`) — cleared when the tab closes.
- No app data is persisted to `localStorage`/`sessionStorage`/IndexedDB.

### 2.4 Offline / service worker

- **None.** No `service-worker.js`, no `manifest.json` (an orphaned `icon-192.png` sits in `wwwroot`). No offline asset cache exists, so no "app stuck on old version" service-worker failure mode.

---

## 3. Safety Assessment

### 3.1 Cross-user / cross-tenant leakage


| #   | Finding                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                | Severity            |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------- |
| L1  | **Tenant-scoped API responses are cache-ambiguous.** `/api/products`, `/api/dashboard/metrics`, `/api/auth/profile`, `/api/tenants` return tenant- and user-specific data with **no `Cache-Control: no-store`** and **no `Vary: X-Tenant` / `Vary: Cookie`**. Tenant identity comes from the `X-Tenant` header or subdomain (`TenantResolutionMiddleware.cs`), so two tenants share identical URLs. Today's browser won't cache these responses (no validators are emitted), but any shared cache added later — CDN, `OutputCache`, nginx/Varnish, corporate proxy over plain HTTP — would key on URL alone and **serve tenant A's data to tenant B**. | **High (latent)**   |
| L2  | **Prerendered HTML contains tenant/user data with no explicit cache headers.** Pages prerender on the server (`AddInteractiveWebAssemblyRenderMode` + `ServerRequestForwardingHandler` forwarding cookies), so the initial HTML response embeds tenant-scoped markup. Nothing marks these responses `no-store`, so the same shared-cache scenario as L1 applies to page HTML.                                                                                                                                                                                                                                                                          | **Medium (latent)** |
| L3  | **In-browser cross-user cleanup is handled correctly.** Logout calls `AuthState.Clear()` then `NavigateTo("/login", forceLoad: true)` (`HelixProfileMenuBff.razor`), which fully reloads the WASM app and wipes all in-memory state. No sensitive data sits in `localStorage`. MSAL defaults to `sessionStorage`.                                                                                                                                                                                                                                                                                                                                      | OK                  |
| L4  | **Server-side tenant state is per-request.** `ITenantContext` is scoped, resolved fresh from the DB each request; nothing tenant-specific is cached in singletons. Prerender `HttpClient`s are scoped per request. No server-side bleed.                                                                                                                                                                                                                                                                                                                                                                                                               | OK                  |


*Related non-caching note:* `TenantHeaderHandler` silently injects `X-Tenant: demo` on every client request, and the middleware trusts the header over the subdomain. The code comments acknowledge this is dev-only, but combined with L1 it widens the blast radius of any cache misconfiguration and allows tenant spoofing if it survives into production.

### 3.2 Stale data


| #   | Finding                                                                                                                                                                                                                                                                                                                                                                                                                                    | Severity   |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------- |
| S1  | **Stale auth state on the client.** `AuthStateService` loads the profile once and holds it for the app's lifetime (scoped ≈ singleton in WASM). If the `HelixCarbon.Auth` cookie expires or is revoked server-side, the UI keeps showing the user as signed in. Compounding this, `HelixApiClient.GetJsonOrNullAsync` swallows non-success responses and returns `null`, so a 401 shows up as "empty data" instead of a redirect to login. | **Medium** |
| S2  | **App data staleness is minimal by design.** No data cache exists; every page navigation re-fetches from the API, and mutations (create/delete product) are followed by fresh loads. Fine for a template.                                                                                                                                                                                                                                  | OK         |
| S3  | **No server-side staleness** — no server cache exists to go stale. The cost is a tenant DB lookup plus data queries on every request (see §4).                                                                                                                                                                                                                                                                                             | OK         |


### 3.3 Asset versioning


| #   | Finding                                                                                                                                                                                                                                                                                                                                                                                                                                                               | Severity            |
| --- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------- |
| A1  | **Core strategy is correct.** Fingerprinted file names + `immutable, max-age=1yr` for versioned routes, `no-cache` + ETag revalidation for stable names, SHA-256 integrity on boot assets. Users cannot get a stale `app-shell.css` or framework runtime after a deploy.                                                                                                                                                                                              | OK                  |
| A2  | **Two CarbonBlazor assets bypass fingerprinting.** `App.razor` hard-codes `_content/CarbonBlazor/carbon-blazor.css` (line 9, not wrapped in `@Assets[]`) and the inline module script imports `./_content/CarbonBlazor/carbon-blazor.js` by literal URL (bypassing the import map). Both fall back to the `no-cache` + ETag route — **safe against staleness** but revalidated on every page load, forfeiting immutable caching.                                      | **Low (perf only)** |
| A3  | **Deploy-time version skew for the lazy-loaded Charts assembly.** The fingerprinted URL for `HelixCarbon.Client.Charts.wasm` is fixed by the boot manifest downloaded at app start. If a new version deploys mid-session and the old fingerprinted files are deleted (typical container/replace deploys), a user who *then* visits the Dashboard gets a failed lazy load. `DashboardMetricsLoader` throws `InvalidOperationException` rather than prompting a reload. | **Low**             |
| A4  | **Client `appsettings.json`** is fetched by the WASM host and served `no-cache` + ETag — fresh after deploys. (Unrelated caution: everything in it is world-readable; keep secrets out.)                                                                                                                                                                                                                                                                              | OK                  |


---

## 4. General Strategy Review & Recommendations

The overall philosophy — *aggressively cache immutable assets, cache nothing dynamic* — is the right default for a multi-tenant template. The gaps are about making the "cache nothing dynamic" part **explicit** instead of accidental. Prioritized recommendations:

### Priority 1 — Make tenant/user API responses explicitly uncacheable (fixes L1, L2)

Add middleware (or endpoint filters on the `/api` group) that stamps tenant-resolved responses:

```csharp
// Concept — e.g. inside TenantResolutionMiddleware after tenant resolution
context.Response.Headers.CacheControl = "no-store, no-cache";
context.Response.Headers.Pragma = "no-cache";
context.Response.Headers.Append("Vary", "X-Tenant, Cookie");
```

`no-store` is the primary defense; `Vary` is defense-in-depth for caches that ignore `no-store`. Apply the same to prerendered page responses (a small middleware branch for non-`/api`, non-static paths). This costs nothing today and makes the template safe to put behind any CDN/proxy tomorrow.

### Priority 2 — Handle auth-state staleness (fixes S1)

- Surface 401/403 in `HelixApiClient` (e.g. a delegating handler that calls `AuthStateService.Clear()` and redirects to `/login` on 401) instead of returning `null`.
- Optionally re-validate the profile on tab focus or a timer for long-lived sessions.

### Priority 3 — Tighten asset delivery (fixes A2, A3)

- Reference CarbonBlazor assets through the fingerprint pipeline: `href="@Assets["_content/CarbonBlazor/carbon-blazor.css"]"` and import the JS via the import map (bare/mapped specifier) so both gain `immutable` caching.
- For the lazy-loaded Charts assembly: either retain previous-version assets across deploys (side-by-side publishing) or catch the lazy-load failure and prompt/force a page reload to pick up the new boot manifest.

### Priority 4 — Optional server-side caching (only if/when needed)

If the template gains a caching layer, the tenant lookup in `TenantResolutionMiddleware` (one DB round-trip per request) is the best first candidate — e.g. `HybridCache`/`IMemoryCache` keyed by slug with a short TTL (30–60 s) and invalidation on tenant changes. **Rule for any future server cache in this codebase: every cache key must include the tenant ID** (and user ID where relevant). Never cache by route alone.

### Explicit non-recommendations

- **Don't add a service worker / offline cache.** For a multi-tenant, auth-heavy SaaS template it adds the exact staleness and cross-user risks this app currently avoids, for little benefit.
- **Don't cache API data client-side** (beyond auth state) at the template level. Per-navigation re-fetch is the correct simple default; downstream apps can add scoped memory caches where profiling justifies it.
- **Don't put app data in `localStorage`.** It survives logout and browser restarts and is shared across users of an OS profile.

---

## 5. Summary Risk Table


| ID  | Issue                                                                | Category                | Severity | Effort to fix | Status |
| --- | -------------------------------------------------------------------- | ----------------------- | -------- | ------------- | ------ |
| L1  | No `Cache-Control: no-store` / `Vary` on tenant-scoped API responses | Tenant leakage (latent) | High     | Small         | Fixed in TASK-01 |
| L2  | Prerendered HTML with tenant data lacks explicit cache headers       | Tenant leakage (latent) | Medium   | Small         | Fixed in TASK-02 |
| S1  | Client auth state can outlive server session; 401s swallowed         | Stale data              | Medium   | Small–Medium  | Fixed in TASK-03 |
| A2  | CarbonBlazor CSS/JS referenced without fingerprinting                | Performance             | Low      | Trivial       | Fixed in TASK-04 |
| A3  | Lazy-loaded Charts assembly can 404 after a mid-session deploy       | Versioning              | Low      | Small         | Fixed in TASK-05 |
| —   | `X-Tenant` header trusted by default with baked-in `demo` fallback   | Related (auth)          | Medium   | Small         | Fixed in TASK-06 |


**Bottom line:** nothing leaks or goes stale in the template as it runs today, but the safety currently depends on the *absence* of any shared cache rather than on explicit headers. Priority 1 converts that accidental safety into guaranteed safety and is the one change that should ship with the template.
