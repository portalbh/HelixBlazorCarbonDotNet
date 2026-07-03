# Caching Remediation Plan — AI Agent Task Cards

**Source:** `docs/caching-assessment.md` (2026-07-03)
**Repo:** HelixCarbon template (`C:\Repo\template`)
**Purpose:** Executable work plan for an AI coding agent. Each task card is self-contained: context, exact files, implementation steps, acceptance criteria, and verification commands.

## Agent Ground Rules

1. Execute tasks in order (TASK-01 → TASK-06). TASK-03/04/05 are independent of each other and may be done in any order after TASK-01 and TASK-02.
2. After each task: build the solution and run verification steps before marking the card done. Build with `dotnet build HelixCarbon.app.slnx`.
3. This is a **`dotnet new` template pack**. Preserve all template preprocessor directives (`#if AuthBFF`, `<!--#if (AuthAzure)-->`, `__DEFAULT_TENANT_SLUG__` placeholders). Any change must compile under every auth variant: `AuthNone`, `AuthAzure`, `AuthBFF`, `AuthAdvanced`.
4. Do not add new NuGet packages unless a card explicitly says so. Use central package management (`Directory.Packages.props`) if one is added.
5. Do not create a service worker, offline cache, client-side data cache, or `localStorage` persistence — explicitly out of scope (see assessment §4, non-recommendations).
6. Update the Status line on each card as you go: `NOT STARTED` → `IN PROGRESS` → `DONE` (or `BLOCKED` with a note).

---

## TASK-01 — Stamp `no-store` + `Vary` on tenant-resolved API responses

- **Status:** DONE
- **Priority:** P1 (High — latent cross-tenant leakage, finding L1)
- **Estimated size:** Small
- **Files:**
  - `src/HelixCarbon.Server/Middleware/TenantResolutionMiddleware.cs` (edit)

### Context

Tenant-scoped API responses (`/api/products`, `/api/dashboard/metrics`, `/api/auth/profile`, `/api/tenants`) carry no `Cache-Control` header. Tenant identity comes from the `X-Tenant` header or subdomain, so two tenants share identical URLs. Any shared cache (CDN, reverse proxy, output cache) added downstream would serve tenant A's data to tenant B.

### Implementation steps

1. In `TenantResolutionMiddleware.InvokeAsync`, after the tenant is successfully resolved (right before `await next(context)`), register `context.Response.OnStarting(...)` that sets:
   - `Cache-Control: no-store, no-cache`
   - `Pragma: no-cache`
   - `Vary: X-Tenant, Cookie` (append, don't overwrite an existing `Vary`)
2. Also stamp the two error responses in the middleware (400 tenant-not-specified, 404 tenant-not-found) with `Cache-Control: no-store` before writing the JSON body.
3. Do NOT stamp bypassed paths (`ShouldBypass` returns true) — static assets must keep their `MapStaticAssets` headers.
4. Use `Microsoft.Net.Http.Headers.HeaderNames` constants where available.

### Acceptance criteria

- [ ] Every response that goes through tenant resolution carries `Cache-Control: no-store, no-cache` and `Vary: X-Tenant, Cookie`.
- [ ] Static asset responses (`/_framework/*`, `/_content/*`, `*.css`, `*.js`) are unchanged (still `immutable` or `no-cache` + ETag from `MapStaticAssets`).
- [ ] Solution builds for all template auth variants.

### Verification

```powershell
dotnet build HelixCarbon.app.slnx
dotnet run --project src/HelixCarbon.Server --launch-profile http
# In a second shell:
curl.exe -si -H "X-Tenant: demo" http://localhost:5000/api/products | Select-String "Cache-Control|Vary"
curl.exe -si http://localhost:5000/_content/CarbonBlazor/carbon-blazor.css | Select-String "Cache-Control"
```

Expected: API response shows `no-store`; CSS response shows `no-cache` (or `immutable` for fingerprinted URL), not `no-store`.

---

## TASK-02 — Prevent caching of prerendered HTML pages

- **Status:** DONE
- **Priority:** P1 (Medium — latent leakage via prerendered tenant/user markup, finding L2)
- **Estimated size:** Small
- **Depends on:** TASK-01 (same middleware area; avoid conflicting edits)
- **Files:**
  - `src/HelixCarbon.Server/Program.cs` (edit) — or fold into `TenantResolutionMiddleware` if cleaner

### Context

Pages prerender on the server (`AddInteractiveWebAssemblyRenderMode`), embedding tenant-scoped markup in the initial HTML. These responses have no explicit cache headers.

### Implementation steps

1. Add a small inline middleware (registered before `MapRazorComponents`) that, for responses with `Content-Type: text/html`, sets `Cache-Control: no-store, no-cache` via `Response.OnStarting`.
2. Alternatively, if TASK-01's middleware already covers all non-bypassed paths (page requests do flow through tenant resolution), confirm that coverage and only add handling for any HTML paths that bypass it (e.g. `/not-found`, `/Error`). Choose the simpler implementation, avoid double-stamping.
3. Keep static assets and framework files untouched.

### Acceptance criteria

- [ ] `GET /` (and `/dashboard`, `/login`) responses carry `Cache-Control: no-store, no-cache`.
- [ ] Static assets unchanged.
- [ ] Builds under all auth variants.

### Verification

```powershell
curl.exe -si -H "X-Tenant: demo" http://localhost:5000/ | Select-String "Cache-Control|Content-Type"
```

---

## TASK-03 — Surface 401s and clear stale client auth state

- **Status:** DONE
- **Priority:** P2 (Medium — stale auth UI, finding S1)
- **Estimated size:** Small–Medium
- **Files:**
  - `src/HelixCarbon.Client/Services/AuthStateService.cs` (edit)
  - `src/HelixCarbon.Client/Services/HelixApiClient.cs` (edit)
  - `src/HelixCarbon.Client/Services/UnauthorizedResponseHandler.cs` (new)
  - `src/HelixCarbon.Client/HelixCarbonClientServiceCollectionExtensions.cs` (edit)
  - `src/HelixCarbon.Server/Extensions/ClientServiceCollectionExtensions.cs` (edit — keep prerender handler chain consistent)

### Context

`AuthStateService` caches the profile for the WASM app's lifetime. If the auth cookie expires or is revoked, the UI still shows the user as signed in, and `HelixApiClient.GetJsonOrNullAsync` swallows 401s as `null` (rendered as "empty data").

### Implementation steps

1. Create `UnauthorizedResponseHandler : DelegatingHandler` in `src/HelixCarbon.Client/Services/`. On a `401 Unauthorized` response, raise an event/callback (e.g. a static or injected `Action`) — do NOT reference `AuthStateService` directly from the handler to avoid a circular DI chain; use an intermediary (e.g. a small `AuthSessionSignal` singleton with an `Unauthorized` event).
2. Wire the handler into the client `HttpClient` pipeline in `HelixCarbonClientServiceCollectionExtensions.AddHelixCarbonWasmClient` (chain: `TenantHeaderHandler` → `UnauthorizedResponseHandler` → `HttpClientHandler`).
3. In `AuthStateService`, subscribe to the signal: on unauthorized, call `Clear()` so `IsAuthenticated` flips and layout components re-render. Guard the auth-variant behavior with the existing `#if AuthBFF || AuthAdvanced` pattern where appropriate.
4. Mirror the handler registration in the server prerender chain (`ClientServiceCollectionExtensions.AddHelixCarbonClientForServerPrerender`) so behavior matches, or document why prerender skips it (prerender 401s already yield anonymous state).
5. Do not force navigation from inside the handler (no `NavigationManager` in a `DelegatingHandler`); components reacting to `AuthState.Changed` handle presentation. Optionally: `MainLayout`/`HelixSideNavAuthBff` may redirect to `/login` when auth state transitions to unauthenticated on a protected page — only if it can be done without touching template `#if` structure badly.

### Acceptance criteria

- [ ] A 401 on any API call results in `AuthStateService.IsAuthenticated == false` and the profile menu switching to "Sign in" without a manual reload.
- [ ] No circular DI: `HttpClient` construction does not require `AuthStateService`.
- [ ] Compiles under `AuthNone` (handler present but inert is fine) and `AuthAzure` (MSAL flow untouched).

### Verification

```powershell
dotnet build HelixCarbon.app.slnx
# Manual: run app, log in, delete the HelixCarbon.Auth cookie in browser devtools,
# navigate to Products — UI must flip to signed-out state instead of showing empty data.
```

---

## TASK-04 — Fingerprint CarbonBlazor CSS/JS references

- **Status:** DONE
- **Priority:** P3 (Low — performance only, finding A2)
- **Estimated size:** Trivial
- **Files:**
  - `src/HelixCarbon.Server/Components/App.razor` (edit)

### Context

`App.razor` line 9 hard-codes `_content/CarbonBlazor/carbon-blazor.css` (bypasses `@Assets[]`), and the inline module script imports `./_content/CarbonBlazor/carbon-blazor.js` by literal URL (bypasses the import map). Both fall to the `no-cache` + ETag route: safe, but revalidated on every load instead of served immutable from cache.

### Implementation steps

1. Change the stylesheet link to `href="@Assets["_content/CarbonBlazor/carbon-blazor.css"]"`.
2. For the JS module: import via the import-map-resolved URL. Preferred: `import { initAppShell } from '@Assets["_content/CarbonBlazor/carbon-blazor.js"]'` inside the inline script (Razor interpolates the fingerprinted URL), keeping `<ImportMap />` in place.
3. Confirm the CarbonBlazor RCL assets are part of the static web assets pipeline (they are — served under `_content/CarbonBlazor/`); if a fingerprinted variant is not generated for the RCL asset, leave the reference as-is and mark this card `BLOCKED` with a note rather than inventing a manual versioning scheme.

### Acceptance criteria

- [ ] Rendered HTML references fingerprinted URLs (e.g. `carbon-blazor.{hash}.css`) for both assets.
- [ ] App shell still initializes (`initAppShell` runs, theme switcher works, no console errors).

### Verification

```powershell
dotnet run --project src/HelixCarbon.Server --launch-profile http
curl.exe -s -H "X-Tenant: demo" http://localhost:5000/ | Select-String "carbon-blazor"
# Expect hashed filenames in the output; then load the app in a browser and check console for errors.
```

---

## TASK-05 — Graceful recovery when the lazy-loaded Charts assembly fails to load

- **Status:** DONE
- **Priority:** P3 (Low — deploy-time version skew, finding A3)
- **Estimated size:** Small
- **Files:**
  - `src/HelixCarbon.Client/Features/Dashboard/DashboardMetricsLoader.razor` (edit)

### Context

The fingerprinted URL for `HelixCarbon.Client.Charts.wasm` is fixed by the boot manifest downloaded at app start. If a new version deploys mid-session and old assets are removed, `LazyAssemblyLoader.LoadAssembliesAsync` fails; the component currently throws `InvalidOperationException`.

### Implementation steps

1. Wrap the `LoadAssembliesAsync` call (and the subsequent assembly lookup) in a `try/catch`.
2. On failure, render a `CbNotification` (Error kind, consistent with `LoginPage.razor` usage) explaining a new app version is available, with a button that calls `NavigationManager.NavigateTo(Navigation.Uri, forceLoad: true)` to reload and pick up the new boot manifest.
3. Keep the happy path unchanged; do not retry-loop.

### Acceptance criteria

- [ ] Lazy-load failure renders the notification + reload button instead of an unhandled exception.
- [ ] Normal dashboard load (charts render) still works.
- [ ] Builds under all auth variants.

### Verification

```powershell
dotnet build HelixCarbon.app.slnx
# Manual simulation: run the app, load any non-dashboard page, block
# **/HelixCarbon.Client.Charts*.wasm* via browser devtools network request blocking,
# then navigate to /dashboard — expect the error notification with a working reload button.
```

---

## TASK-06 — Guard against `X-Tenant` header spoofing in production

- **Status:** DONE
- **Priority:** P2 (Medium — related auth hardening; widens blast radius of L1)
- **Estimated size:** Small
- **Files:**
  - `src/HelixCarbon.Server/Middleware/TenantResolutionMiddleware.cs` (edit)
  - `src/HelixCarbon.Server/appsettings.json` / `appsettings.Development.json` (edit — add flag)
  - `README.md` (edit — document the flag)

### Context

`TenantResolutionMiddleware.ResolveSlug` trusts the `X-Tenant` header **before** the subdomain, and the client's `TenantHeaderHandler` injects `X-Tenant: demo` on every request. The code comments say header resolution is dev-only, but nothing enforces that. In production, any caller could switch tenants by setting a header.

### Implementation steps

1. Add a config flag `App:AllowTenantHeader` (default `false`; set `true` in `appsettings.Development.json`).
2. In `ResolveSlug`, only consult the `X-Tenant` header when the flag is enabled (inject `IConfiguration` or bind an options class — follow the existing template style; note the middleware currently has no config dependency, constructor injection of `IConfiguration` alongside `RequestDelegate` is fine).
3. Subdomain resolution remains the production path and stays unchanged.
4. Document the flag in `README.md` next to the existing tenant-routing notes, and keep template placeholder conventions intact.

### Acceptance criteria

- [ ] With flag off, an `X-Tenant` header is ignored and subdomain resolution applies; requests without a resolvable subdomain get the existing 400.
- [ ] With flag on (Development), current dev workflow (`X-Tenant: demo`) works exactly as before.
- [ ] Builds under all auth variants; template pack (`HelixCarbon.TemplatePack.csproj`) still packs.

### Verification

```powershell
dotnet build HelixCarbon.app.slnx
# Dev profile (flag on):
curl.exe -si -H "X-Tenant: demo" http://localhost:5000/api/health   # bypassed, 200
curl.exe -si -H "X-Tenant: demo" http://localhost:5000/api/products # tenant resolved
# Simulate production (flag off in config): same request must return 400 "Tenant not specified".
```

---

## Completion Checklist (run after all tasks)

- [ ] `dotnet build HelixCarbon.app.slnx` — clean build, no new warnings.
- [ ] Template pack builds: `dotnet pack HelixCarbon.TemplatePack.csproj`.
- [ ] Manual smoke test per auth variant if feasible (at minimum the default variant): login → dashboard (charts) → products CRUD → logout.
- [ ] Response-header spot checks from TASK-01/02 verifications pass.
- [ ] Update `docs/caching-assessment.md` §5 risk table: mark remediated rows (L1, L2, S1, A2, A3, header-trust row) as fixed with task IDs.
- [ ] Do not commit unless the user asks.
