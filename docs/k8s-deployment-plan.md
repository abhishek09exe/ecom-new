# ecom-new-api — Kubernetes / Argo CD / Observability Deployment Plan

> Reference doc compiled from planning session. This is guidance to execute yourself, not a
> record of changes already made to the repo. Nothing below has been applied to the codebase
> unless you did it manually.

Repo context: `ecom-new-api` is a .NET 10 ASP.NET Core Web API using EF Core + SQL Server
(`AppDbContext`, connection string key `ConnectionStrings:EcomDb`). A bacpac file needs to be
restored into SQL Server. Target: local Kubernetes (Docker Desktop), deployed via Helm charts,
orchestrated by Argo CD (app-of-apps pattern), with Prometheus + Grafana for observability.

---

## 0. Namespace layout

| Namespace | What goes in it | Why |
|---|---|---|
| `argocd` | Argo CD itself | Standard, chart default |
| `monitoring` | kube-prometheus-stack (Prometheus, Grafana, Alertmanager, node-exporter) | Cluster-wide platform concern, one install serves all envs |
| `data` | SQL Server StatefulSet, PVC, bacpac import Job | Stateful, different lifecycle/backup policy than the app; never redeployed when the API redeploys |
| `ecom-dev` | ecom-new-api Deployment/Service/Ingress/ConfigMap/Secret | Per-environment app namespace. Add `ecom-qa` later by pointing a second Argo app at the same chart with different values |

Rule of thumb: shared platform services get their own namespace + own Argo Application;
app services are namespaced per environment.

Cross-namespace DNS the API will use for SQL Server:
`ecom-mssql.data.svc.cluster.local,1433`.

---

## 1. Dockerize the API (multi-stage)

Create `ecom-new-api/Dockerfile` with three stages:

1. `mcr.microsoft.com/dotnet/sdk:10.0` as `build` — copy `.csproj` first, `dotnet restore`,
   then copy the rest and `dotnet publish -c Release -o /app/publish`. Copying the csproj
   alone first preserves Docker layer caching on restore.
2. Optional `test` stage running `dotnet test` against `ecom-new-api.Tests`.
3. `mcr.microsoft.com/dotnet/aspnet:10.0` as final — copy `/app/publish`, run as non-root
   (`USER $APP_UID`), `EXPOSE 8080`, `ENTRYPOINT ["dotnet","ecom-new-api.dll"]`.

Also add a `.dockerignore` (`bin/`, `obj/`, `.git`, `Project_Seed`) — otherwise the build
context is huge.

Gotchas specific to this repo:
- `Program.cs` only enables Swagger in Development — control via `ASPNETCORE_ENVIRONMENT`
  ConfigMap value.
- Bind with `ASPNETCORE_URLS=http://+:8080` (non-root user can't bind to 80).
- `appsettings.json` currently has a **live connection string with credentials committed** —
  fix before going further (see Part B).

Verify: `docker build -t ecom-new-api:0.1.0 .` then `docker run -p 8080:8080 ...`.
On Docker Desktop the image is already visible to the cluster — set
`imagePullPolicy: IfNotPresent`, no registry needed yet.

---

## 2. Health checks + `/metrics` endpoints

### Landmine to fix first
`Program.cs` currently calls `app.UseHttpsRedirection()` unconditionally. Inside the cluster,
probes/scrapes arrive as plain HTTP — a 307 redirect still counts as "success" to kubelet
(2xx–3xx both pass), but it **breaks Prometheus scraping of `/metrics` outright**. Fix:
wrap it so it only runs outside Production: `if (!app.Environment.IsProduction()) app.UseHttpsRedirection();`.
TLS terminates at the Ingress anyway, so in-cluster redirect buys nothing.

### 2a. Add packages
```powershell
cd ecom-new-api
dotnet add package AspNetCore.HealthChecks.SqlServer   # or just use AddDbContextCheck below
dotnet add package prometheus-net.AspNetCore
```

### 2b. Register health checks
After the `AddDbContext` block:
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(name: "ecomdb", tags: new[] { "ready" });
```
The `tags` parameter is what lets one registration serve two different endpoints.

### 2c. Map two endpoints with different semantics
| Endpoint | Predicate | DB check? | k8s behavior on failure |
|---|---|---|---|
| `/healthz` (liveness) | `_ => false` | No | Restarts the pod |
| `/readyz` (readiness) | `c => c.Tags.Contains("ready")` | Yes | Removes pod from Service endpoints |

Why liveness must exclude the DB: a DB-backed liveness probe makes every API pod restart
simultaneously during a DB outage — crashloop storm on top of the outage. Liveness = "is the
process wedged?"; readiness = "can I serve traffic right now?".

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
// ... just before app.MapControllers():
app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/readyz",  new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });
```

### 2d. Metrics
```csharp
using Prometheus;
// early in pipeline, before MapControllers, after the dev-only block:
app.UseHttpMetrics();
// next to health maps:
app.MapMetrics();  // exposes /metrics
```
Order matters — `UseHttpMetrics()` must be registered before the routes it should measure.

### 2e. Verify locally before touching Helm
```powershell
dotnet run --project ecom-new-api
curl -i http://localhost:5xxx/healthz   # expect 200 Healthy, instant
curl -i http://localhost:5xxx/readyz    # expect 503 while DB unreachable, 200 when up
curl -s http://localhost:5xxx/metrics | Select-String http_request_duration
```
`/readyz` returning 503 with no DB is the real proof the tag filter works (not a hardcoded 200).

### Checklist
- [ ] Packages added
- [ ] `AddHealthChecks().AddDbContextCheck<AppDbContext>(tags: "ready")`
- [ ] `/healthz` with `Predicate = _ => false`
- [ ] `/readyz` filtered on `ready` tag
- [ ] `UseHttpMetrics()` early, `MapMetrics()` mapped
- [ ] `UseHttpsRedirection` made non-Production
- [ ] Verified `/readyz` 503→200 transition and `/metrics` output

---

## 3. Remove committed connection string / inject via env var

### 3a. Scrub `appsettings.json`
Set the `EcomDb` value to `""`. Keep the key present (self-documenting shape); don't delete it.

### 3b. Config override mechanism
Default chain: `appsettings.json` → `appsettings.{Env}.json` → user-secrets (Dev only) →
**environment variables** → CLI args. Later wins. Env var uses `__` as section separator:
```
ConnectionStrings__EcomDb=Server=ecom-mssql.data.svc.cluster.local,1433;Database=ecommerce;User Id=sa;Password=...;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
```
No code change needed — `GetConnectionString("EcomDb")` picks it up automatically.

### 3c. Local dev secret (no env var needed)
```powershell
cd ecom-new-api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:EcomDb" "Server=localhost,1433;Database=ecommerce;User Id=sa;Password=YourLocalPw;TrustServerCertificate=True;"
```
Note: separator is `:` for user-secrets, `__` only for env vars. Stored outside the repo.

### 3d. Fail fast on missing config
```csharp
var cs = builder.Configuration.GetConnectionString("EcomDb")
    ?? throw new InvalidOperationException("ConnectionStrings__EcomDb is not configured.");
```
Turns a confusing runtime EF error into a clear CrashLoopBackOff message.

### 3e. Credential is already in git history
Scrubbing the file only fixes `HEAD`; the old password is still recoverable from history.
1. **Rotate that SQL login now** — the only step that actually removes the risk.
2. Optionally rewrite history with `git filter-repo`/BFG (needs force-push + team coordination)
   — only if policy demands it; rotation is the priority.

### 3f. How this lands in k8s later
In the `ecom-api` Helm chart:
- A `Secret` (created out-of-band, not in Git) with key `connectionString`.
- Deployment env: `ConnectionStrings__EcomDb` via `secretKeyRef`.
- Same pattern for `ASPNETCORE_ENVIRONMENT` (ConfigMap) and `ASPNETCORE_URLS=http://+:8080`.

### Checklist
- [ ] `appsettings.json` value emptied
- [ ] Null-check throw added
- [ ] user-secrets set for local dev
- [ ] **SQL login rotated**

---

## 4. SQL Server + bacpac import

Chart `charts/mssql`, deployed to `data` namespace:
- `StatefulSet` (not Deployment), image `mcr.microsoft.com/mssql/server:2022-latest`,
  `ACCEPT_EULA=Y`, `MSSQL_SA_PASSWORD` from Secret, `volumeClaimTemplates` for
  `/var/opt/mssql` (Docker Desktop default StorageClass is `hostpath`).
- `securityContext.fsGroup: 10001` — required or the mssql user can't own the PVC → crashloop.
- Headless + ClusterIP Service on 1433.
- Real memory limits (≥2Gi); raise Docker Desktop VM memory to 6–8 GB.

### bacpac import — options, best first
1. **One-shot Job with `sqlpackage`.** Bacpac on a PVC (or baked into a small helper image).
   Job runs:
   ```
   sqlpackage /a:Import /tsn:ecom-mssql.data:1433 /tdn:ecommerce /tu:sa /tp:$PW \
     /sf:/data/ecom.bacpac /TargetTrustServerCertificate:True
   ```
   Guard with a Helm hook (`post-install`) + Argo `PostSync` hook annotation +
   `hook-delete-policy: HookSucceeded`. **Make it idempotent** (exit 0 if DB already exists),
   or every Argo sync retries a 20-minute import.
2. Convert bacpac → `.bak` once locally, restore via `sqlcmd` — faster, but must be produced
   manually first.
3. Init-container variant — avoid; blocks the DB pod from ever becoming Ready.

Getting the file into the cluster: `kubectl cp` into a PVC-mounted helper pod once; don't
commit a multi-GB bacpac to Git.

Verify: exec in, `sqlcmd -S localhost -U sa -Q "SELECT name FROM sys.databases"`.

---

## 5. Helm chart structure

```
deploy/
  charts/
    ecom-api/        # Deployment, Service, Ingress, ConfigMap, Secret, ServiceMonitor, HPA
    mssql/           # StatefulSet, Service, PVC, import Job
  envs/
    dev/ecom-api-values.yaml
    dev/mssql-values.yaml
  argocd/
    app-of-apps.yaml
    apps/*.yaml
```

`ecom-api` values to cover: `image.tag`, `replicaCount`, `env`, `db.secretName`,
`serviceMonitor.enabled`, `ingress.host`. Start from `helm create`, delete unused scaffolding.
Validate with `helm template` + `helm lint` before letting Argo touch it.

For Prometheus/Grafana: **don't write a custom chart** — use
`prometheus-community/kube-prometheus-stack` as its own Argo Application with a values
override. It provides Prometheus Operator; your API just ships a `ServiceMonitor`
(gated by a values flag) inside the `ecom-api` chart.

---

## 6. Argo CD — app-of-apps

1. Install Argo CD into `argocd` namespace (manifest or Helm), port-forward, retrieve the
   initial admin secret.
2. Create one root Application (`app-of-apps.yaml`) pointing at `deploy/argocd/apps/`, which
   holds one Application manifest per component:

| App | Source | Dest namespace | Sync wave |
|---|---|---|---|
| `platform-monitoring` | kube-prometheus-stack chart | `monitoring` | `-2` |
| `ecom-mssql` | `deploy/charts/mssql` | `data` | `-1` |
| `ecom-api-dev` | `deploy/charts/ecom-api` + `envs/dev` values | `ecom-dev` | `0` |

Use `argocd.argoproj.io/sync-wave` annotations so SQL Server + import finish before the API
rolls out. Set `syncPolicy.automated.prune/selfHeal` + `CreateNamespace=true`.

3. Secrets: don't commit the SA password. Locally, create Secrets out-of-band with
   `kubectl create secret`. Later, move to Sealed Secrets or External Secrets Operator so
   Argo can own them declaratively.

---

## 7. Prometheus integration for ecom-api (ServiceMonitor)

Prometheus Operator discovers targets via `ServiceMonitor` CRDs matching a label selector
configured on the `Prometheus` object at install time — it does NOT scrape pods directly.
`ServiceMonitor` → matches a Service by label → reads a **named port** on that Service.
Any broken link (Service selector, port name, ServiceMonitor label, namespace selector) means
Prometheus silently ignores the target — no error, it just never shows up in Targets.

### 7a. Service must expose a named port
```yaml
ports:
  - name: http
    port: 8080
    targetPort: 8080
```

### 7b. ServiceMonitor (ship inside `ecom-api` chart, `templates/servicemonitor.yaml`)
```yaml
{{- if .Values.serviceMonitor.enabled }}
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: {{ include "ecom-api.fullname" . }}
  namespace: {{ .Release.Namespace }}
  labels:
    release: {{ .Values.serviceMonitor.releaseLabel | default "kube-prometheus-stack" }}
spec:
  selector:
    matchLabels:
      app.kubernetes.io/name: {{ include "ecom-api.name" . }}
  namespaceSelector:
    matchNames:
      - {{ .Release.Namespace }}
  endpoints:
    - port: http
      path: /metrics
      interval: 15s
{{- end }}
```

Two common mistakes:
1. **`labels.release`** must match the *monitoring* Helm release name (kube-prometheus-stack's
   default Operator config only watches ServiceMonitors carrying `release: <its-release-name>`),
   not the api chart's release name. Check what's actually configured:
   ```powershell
   kubectl get prometheus -n monitoring -o jsonpath='{.items[0].spec.serviceMonitorSelector}'
   ```
   Empty `{}` = matches everything, label irrelevant. Don't assume — verify.
2. **`namespaceSelector`** must be explicit since the Service lives in `ecom-dev`, not
   `monitoring`.

### 7c. Cross-namespace RBAC / namespace selector restriction
Operator ClusterRole usually already allows cross-namespace list/watch, but check the
Prometheus object isn't restricted to specific namespaces:
```powershell
kubectl get prometheus -n monitoring -o jsonpath='{.items[0].spec.serviceMonitorNamespaceSelector}'
```
Empty `{}` = all namespaces (good). If restricted, add `ecom-dev` to the allowed selector or
label the namespace to match.

### 7d. Verify discovery, in order
1. ```powershell
   kubectl get servicemonitor -n ecom-dev
   kubectl get endpoints -n ecom-dev <svc-name>   # confirm real pod IPs listed
   ```
2. Port-forward Prometheus and check Status → Targets:
   ```powershell
   kubectl port-forward -n monitoring svc/kube-prometheus-stack-prometheus 9090:9090
   ```
   Expect `serviceMonitor/ecom-dev/ecom-api/0` state `UP`. Missing entirely (not just DOWN)
   = selector/label mismatch, not a network problem.
3. Run a query for `http_requests_received_total` or `http_request_duration_seconds_count`
   in the Prometheus Graph tab to confirm real scraped data.

### 7e. Grafana dashboard (after data flows)
Ship a dashboard-as-ConfigMap in the `ecom-api` chart, labelled `grafana_dashboard: "1"` —
Grafana's sidecar auto-discovers these across namespaces if `searchNamespace: ALL` (default
in kube-prometheus-stack). Start from the community ASP.NET Core / prometheus-net dashboard
JSON rather than building from scratch.

### Checklist
- [ ] Service port named `http`
- [ ] Confirmed `serviceMonitorSelector` and `serviceMonitorNamespaceSelector` on `Prometheus`
- [ ] `ServiceMonitor` added with matching `release` label + explicit `namespaceSelector`
- [ ] `kubectl get endpoints` shows real pod IPs
- [ ] Prometheus Targets shows `UP`
- [ ] Test query returns data
- [ ] (later) Grafana dashboard ConfigMap added

---

## 8. Other observability wiring
- Grafana: get admin password from `monitoring` secret, port-forward 3000.
- SQL Server metrics: add `awaragi/prometheus-mssql-exporter` (sidecar or separate Deployment
  in `data`), with its own ServiceMonitor.
- Add a couple of `PrometheusRule` alerts (5xx rate, pod restarts, DB down) to prove the stack
  end-to-end.

---

## Suggested overall order of work

1. `.dockerignore` + multi-stage Dockerfile → build & run locally
2. Strip committed connection string; move to env var (Part 3 above) + rotate SQL login
3. Health + `/metrics` endpoints (Part 2 above)
4. `mssql` chart → manual `helm install` into `data`, confirm persistence across pod delete
5. bacpac import Job → confirm tables exist
6. `ecom-api` chart → manual `helm install` into `ecom-dev`, confirm DB connectivity
7. kube-prometheus-stack into `monitoring`, confirm ServiceMonitor scraped (Targets UP)
8. Only then install Argo CD and convert steps 4–7 into declarative Applications

Doing 4–7 manually first avoids debugging Helm chart bugs indirectly through Argo sync errors.
