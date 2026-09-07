# Deployment Runbook — ecom-new on Kubernetes via Argo CD

This is the practical, step-by-step guide to standing up the full stack from scratch:
SQL Server (with seeded data), the ecom-new-api service, Prometheus + Grafana, and the
optional site_smb UI — all deployed and kept in sync by Argo CD (GitOps, app-of-apps
pattern). It reflects what is actually implemented in this repo today, not a plan.

It's written for a local Docker Desktop Kubernetes cluster, with notes on what changes
for a real cloud cluster (EKS/AKS/GKE) at the end of each section.

---

## 1. Architecture at a glance

| Namespace | What runs there | Why |
|---|---|---|
| `argocd` | Argo CD itself | Standard install, manages everything else via GitOps |
| `monitoring` | kube-prometheus-stack (Prometheus, Grafana, Alertmanager) | Cluster-wide observability, shared across all envs |
| `data` | SQL Server StatefulSet + PVC + seed Job | Stateful, different lifecycle than the app — never torn down when the API redeploys |
| `ecom-dev` | ecom-new-api Deployment/Service, ui (site_smb) Deployment/Service | Per-environment application namespace |

Everything is deployed as Helm charts under [deploy/charts/](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/charts),
with environment-specific value overrides in [deploy/envs/dev/](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/envs/dev),
wired together by Argo CD `Application` manifests in [deploy/argocd/apps/](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/argocd/apps)
via a single "app-of-apps" root ([deploy/argocd/app-of-apps.yaml](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/argocd/app-of-apps.yaml)).

```
deploy/
├── argocd/
│   ├── app-of-apps.yaml          # root Application, watches deploy/argocd/apps/
│   └── apps/
│       ├── 00-namespaces.yaml
│       ├── 01-sealed-secrets.yaml
│       ├── 10-monitoring.yaml    # kube-prometheus-stack
│       ├── 20-mssql.yaml         # SQL Server + seed job
│       ├── 30-ecom-api.yaml      # the .NET API
│       └── 40-ui.yaml            # site_smb frontend (optional)
├── charts/
│   ├── monitoring/               # wraps kube-prometheus-stack
│   ├── mssql/                    # SQL Server StatefulSet + seed Job/Image
│   ├── ecom-api/                 # ecom-new-api Deployment/Service/Ingress/ServiceMonitor
│   └── ui/                       # site_smb Deployment/Service/Ingress
├── envs/dev/                     # per-env values.yaml overrides for each chart above
├── sealed-secrets/                # sealed-secrets controller install
└── secrets-plaintext-DO-NOT-COMMIT/  # local-only source material for sealed secrets (gitignored)
```

`sync-wave` annotations on each Application control ordering: namespaces → sealed-secrets →
monitoring/mssql → ecom-api/ui.

---

## 2. Prerequisites (local)

- Docker Desktop with Kubernetes enabled (Settings → Kubernetes → Enable Kubernetes).
- `kubectl` pointed at the `docker-desktop` context.
- `helm` v3.
- `kubeseal` CLI (for sealed-secrets, only needed if you re-seal a secret).
- A `.bacpac`/seed SQL export of the source database (see [Project_Seed/](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/Project_Seed)).

```powershell
kubectl config use-context docker-desktop
kubectl get nodes   # sanity check
```

**Cloud equivalent:** provision an EKS/AKS/GKE cluster, point `kubectl`/your kubeconfig at
it instead. Everything below is identical except: use a real StorageClass instead of
`hostpath` (see §4), push images to a real registry instead of relying on the shared local
Docker daemon (see §3), and use a LoadBalancer/Ingress controller with a real DNS name
instead of port-forwarding (see §7).

---

## 3. Install Argo CD

```powershell
kubectl create namespace argocd
kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml
kubectl -n argocd rollout status deploy/argocd-server
```

Get the initial admin password:
```powershell
kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath='{.data.password}' |
  ForEach-Object { [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($_)) }
```

Access the UI (see §8 for the port-forward command). Log in as `admin` with that password.
Change it after first login (`argocd account update-password` or via the UI).

### Point Argo CD at this repo

Apply the app-of-apps root once — Argo CD will discover and create every other
`Application` under `deploy/argocd/apps/` automatically from then on:

```powershell
kubectl apply -f deploy/argocd/app-of-apps.yaml
```

Everything downstream of this is pure GitOps: **commit + push to the
`agents/k8s-deployment-sqlserver-prometheus-grafana` branch, and Argo CD syncs it.**
(`repoURL`/`targetRevision` are hardcoded in each Application manifest — update them if you
fork the repo or deploy from a different branch.)

Force an immediate resync instead of waiting for the poll interval:
```powershell
kubectl annotate application ecom-app-of-apps -n argocd argocd.argoproj.io/refresh=hard --overwrite
kubectl get applications -n argocd   # watch SYNC STATUS / HEALTH STATUS
```

**Cloud equivalent:** identical. Argo CD itself is cluster-agnostic; only the
`destination.server` in each Application (already `https://kubernetes.default.svc`, i.e.
"this cluster") needs no change for a single-cluster setup.

---

## 4. SQL Server + seed data (`data` namespace, chart: `mssql`)

Chart: [deploy/charts/mssql](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/charts/mssql).
Env values: [deploy/envs/dev/mssql-values.yaml](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/envs/dev/mssql-values.yaml).
Argo Application: [deploy/argocd/apps/20-mssql.yaml](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/argocd/apps/20-mssql.yaml).

Deploys a single-replica StatefulSet running `mcr.microsoft.com/mssql/server`, a PVC for
data, and (if `seedImport.enabled: true`) a one-shot Kubernetes `Job` that runs a custom
seed image (built from [deploy/charts/mssql/seed-image/Dockerfile](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/charts/mssql/seed-image/Dockerfile))
to create the database and run the SQL scripts under [Project_Seed/](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/Project_Seed)
against it (this replaced an earlier bacpac-restore approach — bacpac restore was flaky
under the SQL Server Linux container; plain `.sql` scripts are more reliable).

### One-time setup steps

1. **Build the seed image** (Docker Desktop's daemon is shared with the cluster, so no
   registry push needed locally):
   ```powershell
   docker build -t ecom-mssql-seed:local -f deploy/charts/mssql/seed-image/Dockerfile deploy/charts/mssql/seed-image
   ```

2. **Create the SA password Secret** (never committed in plaintext — see §9 for how this
   is sealed for git):
   ```powershell
   kubectl create secret generic ecom-mssql-sa -n data --from-literal=SA_PASSWORD='<a-strong-password>'
   ```

3. Argo CD (via the app-of-apps) will create the `data` namespace, StatefulSet, PVC, and
   seed Job automatically once `20-mssql.yaml` is synced.

4. Verify:
   ```powershell
   kubectl get pods -n data
   kubectl logs job/<seed-job-name> -n data   # confirm seed scripts ran without error
   ```

### Connecting from SSMS / Azure Data Studio

```powershell
kubectl port-forward svc/ecom-mssql -n data 14330:1433
```
- Server: `localhost,14330`
- Auth: SQL Login
- User: `sa`
- Password: the value you put in the `ecom-mssql-sa` secret
- Database: `ecommerce` (per `targetDatabase` in [mssql-values.yaml](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/envs/dev/mssql-values.yaml))
- Enable "Trust server certificate" (self-signed cert on the container).

Cluster-internal DNS name the API uses: `ecom-mssql.data.svc.cluster.local,1433`.

**Cloud equivalent:** swap `storageClassName: hostpath` in
[mssql-values.yaml](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/envs/dev/mssql-values.yaml)
for your cloud provider's block-storage StorageClass (e.g. `gp3` on EKS, `managed-csi` on
AKS). For production, prefer a managed SQL offering (Azure SQL, RDS for SQL Server) over
running SQL Server yourself in a StatefulSet — this setup is optimized for local dev/demo.

---

## 5. ecom-new-api (`ecom-dev` namespace, chart: `ecom-api`)

Chart: [deploy/charts/ecom-api](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/charts/ecom-api).
Env values: [deploy/envs/dev/ecom-api-values.yaml](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/envs/dev/ecom-api-values.yaml).
Argo Application: [deploy/argocd/apps/30-ecom-api.yaml](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/argocd/apps/30-ecom-api.yaml).

Deploys the .NET 10 API as a Deployment + Service (+ optional Ingress), reads its DB
connection string from a pre-existing Secret (`ecom-db-credentials`, key
`connectionString`), and exposes `/metrics` for Prometheus (via a `ServiceMonitor`) plus
`/healthz` and `/readyz` probes. Also ships a Grafana dashboard ConfigMap (see §6).

### One-time setup steps

1. **Build the image:**
   ```powershell
   docker build -t ecom-new-api:0.2.0 -f ecom-new-api/Dockerfile ecom-new-api
   ```

2. **Create the DB connection string Secret** (references the `data` namespace SQL Server):
   ```powershell
   kubectl create secret generic ecom-db-credentials -n ecom-dev `
     --from-literal=connectionString="Server=ecom-mssql.data.svc.cluster.local,1433;Database=ecommerce;User Id=sa;Password=<same-as-SA_PASSWORD>;TrustServerCertificate=True;"
   ```

3. Argo CD deploys everything else once `30-ecom-api.yaml` is synced.

### Redeploying after a code change (the actual GitOps loop)

```powershell
# 1. Bump the image tag in BOTH files (same tag == "IfNotPresent" won't re-pull)
#    - deploy/charts/ecom-api/values.yaml
#    - deploy/envs/dev/ecom-api-values.yaml

# 2. Build the new image with that tag
docker build -t ecom-new-api:<new-tag> -f ecom-new-api/Dockerfile ecom-new-api

# 3. Commit + push
git add -A
git commit -m "..."
git push

# 4. Force Argo to pick it up immediately (otherwise it polls every ~3 min)
kubectl annotate application ecom-api-dev -n argocd argocd.argoproj.io/refresh=hard --overwrite

# 5. Watch it roll out
kubectl get pods -n ecom-dev -w
```

### Access it locally

```powershell
kubectl port-forward svc/ecom-api-dev -n ecom-dev 8081:8080
```
- Swagger UI: http://localhost:8081/swagger (Development environment only)
- Health: http://localhost:8081/healthz, http://localhost:8081/readyz
- Metrics: http://localhost:8081/metrics

Simulate traffic against it (see [scripts/simulate-metrics.ps1](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/scripts/simulate-metrics.ps1)):
```powershell
.\scripts\simulate-metrics.ps1 -Continuous -DelaySeconds 1     # run until Ctrl+C
.\scripts\simulate-metrics.ps1 -SimulateDbOutage -Iterations 5 # also generate real DB-error samples
```

**Cloud equivalent:** push the image to a real registry (ECR/ACR/GCR/Docker Hub) instead
of relying on the shared local Docker daemon, update `image.repository` accordingly, and
set `imagePullPolicy: Always` (or keep unique tags per build, which is the safer pattern
either way). Expose it externally via an Ingress + real DNS + TLS instead of
`port-forward` (flip `ingress.enabled: true` in the values file).

---

## 6. Observability — Prometheus + Grafana (`monitoring` namespace, chart: `monitoring`)

Chart: [deploy/charts/monitoring](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/charts/monitoring)
(thin wrapper around the community `kube-prometheus-stack` chart).
Env values: [deploy/envs/dev/monitoring-values.yaml](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/envs/dev/monitoring-values.yaml).
Argo Application: [deploy/argocd/apps/10-monitoring.yaml](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/argocd/apps/10-monitoring.yaml).

Installs Prometheus, Grafana, Alertmanager, and node-exporter as one release. `ecom-api`'s
`ServiceMonitor` (in its own chart) is auto-discovered by the Prometheus Operator's
`serviceMonitorSelector` — no manual wiring needed as long as the release label matches
(`serviceMonitor.releaseLabel: kube-prometheus-stack` in
[ecom-api values.yaml](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/charts/ecom-api/values.yaml)).

Custom app metrics (`ecom_db_proc_calls_total`, `ecom_db_proc_duration_seconds`,
`ecom_business_operations_total`, `ecom_errors_total`) are emitted from
[ecom-new-api/Observability/AppMetrics.cs](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/ecom-new-api/Observability/AppMetrics.cs)
and instrumented at every DB call site + business outcome.

A pre-built Grafana dashboard ("Ecom API - Overview") ships as a ConfigMap
([deploy/charts/ecom-api/templates/dashboard-configmap.yaml](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/charts/ecom-api/templates/dashboard-configmap.yaml),
sourced from [deploy/charts/ecom-api/dashboards/ecom-api-overview.json](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/charts/ecom-api/dashboards/ecom-api-overview.json))
labeled `grafana_dashboard: "1"`. Grafana's sidecar container (`searchNamespace: ALL`,
already configured in the monitoring chart values) auto-discovers and loads it — no manual
import step.

### Access Grafana / Prometheus locally

```powershell
kubectl port-forward svc/monitoring-grafana -n monitoring 3000:80
kubectl port-forward svc/monitoring-kube-prometheus-prometheus -n monitoring 9090:9090
```
- Grafana: http://localhost:3000 (default admin/admin per Helm values — change on first
  login; if login fails with correct credentials, the password may have drifted from a
  prior UI change — reset it with `kubectl exec` into the Grafana pod and
  `grafana-cli admin reset-admin-password <new>`).
- Prometheus: http://localhost:9090 — useful for ad-hoc PromQL and checking what labels a
  target actually exposes (`namespace`, not `kubernetes_namespace`; `exported_endpoint` for
  unmatched HTTP routes).

**Cloud equivalent:** identical chart/values. For production, put Grafana behind an
Ingress + SSO/OAuth instead of the default admin/admin, and consider remote-write to a
managed long-term-storage backend (Thanos, Mimir, or a cloud vendor's managed Prometheus)
instead of relying on local PVC retention.

---

## 7. UI — site_smb frontend (`ecom-dev` namespace, chart: `ui`) — optional

Chart: [deploy/charts/ui](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/charts/ui).
Env values: [deploy/envs/dev/ui-values.yaml](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/envs/dev/ui-values.yaml).
Argo Application: [deploy/argocd/apps/40-ui.yaml](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/argocd/apps/40-ui.yaml).

This deploys a pre-built Docker image of `site_smb`, a Next.js 14 SSR frontend backed by
Optimizely CMS/GraphQL. **The frontend source lives in `UI_DONT_COMMIT/opentext-brands/`,
which is intentionally excluded from this repository** (separate product, not owned by
this repo). Only the generic Helm chart (Deployment/Service/ConfigMap/Ingress) is
committed — it has no knowledge of the UI's source code, only a pre-built image name/tag
and a reference to a Secret.

### One-time setup steps

1. **Build the image** (from inside `UI_DONT_COMMIT/opentext-brands/`):
   ```powershell
   cd UI_DONT_COMMIT/opentext-brands
   docker build -f site_smb/Dockerfile -t site-smb:0.1.0 .
   ```
   Notable Dockerfile decisions (see
   [UI_DONT_COMMIT/opentext-brands/site_smb/Dockerfile](</C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/UI_DONT_COMMIT/opentext-brands/site_smb/Dockerfile>)):
   - Multi-stage build; the yarn workspace root (`common_lib`, `common_ui`, `tsconfig.base.json`)
     must be copied in alongside `site_smb` since it depends on them via workspace symlinks.
   - `output: "standalone"` in `next.config.mjs` for a slim runtime image.
   - The `[lang]/[[...path]]` catch-all CMS route was switched from `force-static`
     (pre-render ~292 pages at build time) to `force-dynamic` (render on request) — the
     build-time approach was hitting out-of-memory crashes and per-route timeouts against
     the live CMS in this environment. Since the container has a live network path to the
     CMS at request time anyway, on-demand SSR is the pragmatic choice here.
   - CMS/Graph credentials are passed as Docker `ARG`/`ENV` with real defaults baked in —
     acceptable only because this whole directory is excluded from git.

2. **Create the CMS credentials Secret** (matches the keys the chart expects, see
   [deploy/charts/ui/values.yaml](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/charts/ui/values.yaml) `secrets.keys`):
   ```powershell
   kubectl create secret generic ui-smb-credentials -n ecom-dev `
     --from-literal=OPTIMIZELY_CMS_SITE_ID=<...> `
     --from-literal=OPTIMIZELY_GRAPH_SINGLE_KEY=<...> `
     --from-literal=OPTIMIZELY_GRAPH_APP_KEY=<...> `
     --from-literal=OPTIMIZELY_GRAPH_SECRET=<...> `
     --from-literal=OPTIMIZELY_GRAPH_GATEWAY=https://cg.optimizely.com `
     --from-literal=NEXT_PUBLIC_OPTIMIZELY_CMS_URL=<cms-url> `
     --from-literal=NEXT_PUBLIC_SITE_DOMAIN=localhost:3000 `
     --from-literal=NEXT_PUBLIC_SITE_PRIMARY=<cms-primary-domain>
   ```

3. Argo CD deploys the Deployment/Service once `40-ui.yaml` is synced (the chart itself
   *is* committed to git, just not the app source).

### Access it locally

```powershell
kubectl port-forward svc/ui-dev -n ecom-dev 3001:3000
```
Then browse http://localhost:3001/en/.

**Cloud equivalent:** same as ecom-api (§5) — push to a real registry, use
`imagePullPolicy: Always`/unique tags, expose via Ingress. Because the UI source isn't in
this repo, there's no GitOps auto-rebuild trigger for it today — image rebuilds and
redeploys are a manual `docker build` + bump `image.tag` in
[ui-values.yaml](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/envs/dev/ui-values.yaml) + Argo refresh, same pattern as §5 step "Redeploying after a code change".

---

## 8. Quick reference — accessing everything locally

| Service | Command | URL |
|---|---|---|
| Argo CD UI | `kubectl port-forward svc/argocd-server -n argocd 8080:443` | https://localhost:8080 |
| ecom-new-api | `kubectl port-forward svc/ecom-api-dev -n ecom-dev 8081:8080` | http://localhost:8081/swagger |
| site_smb UI | `kubectl port-forward svc/ui-dev -n ecom-dev 3001:3000` | http://localhost:3001/en/ |
| Grafana | `kubectl port-forward svc/monitoring-grafana -n monitoring 3000:80` | http://localhost:3000 |
| Prometheus | `kubectl port-forward svc/monitoring-kube-prometheus-prometheus -n monitoring 9090:9090` | http://localhost:9090 |
| SQL Server | `kubectl port-forward svc/ecom-mssql -n data 14330:1433` | `localhost,14330` (SSMS) |

All `port-forward` processes die when their target pod restarts (rollout, Argo sync,
crash) — just re-run the command.

---

## 9. Secrets policy

Nothing that looks like a password, connection string, or API key is ever committed in
plaintext. Two patterns are used, depending on whether the secret needs to survive a
cluster rebuild via GitOps or not:

1. **Sealed Secrets** (survives via git): for secrets that Argo CD needs to (re)create
   automatically on every environment rebuild. Plaintext source material lives only in
   [deploy/secrets-plaintext-DO-NOT-COMMIT/](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/secrets-plaintext-DO-NOT-COMMIT)
   (gitignored), sealed with `kubeseal` into
   [deploy/sealed-secrets/](/C:/ECOM/ecom-new.worktrees/k8s-deployment-sqlserver-prometheus-grafana/deploy/sealed-secrets)
   (safe to commit — only decryptable by the sealed-secrets controller in *this* cluster).

2. **Manually created Secrets** (not in git at all): for anything tied to a
   deliberately-excluded piece of source (like the UI's CMS credentials) or created
   ad-hoc during setup (`ecom-mssql-sa`, `ecom-db-credentials`, `ui-smb-credentials`).
   These must be recreated by hand (`kubectl create secret generic ...`, see each
   section above) any time the cluster or namespace is rebuilt from scratch.

---

## 10. Rebuilding the whole cluster from zero (local)

```powershell
# 1. Enable/reset Kubernetes in Docker Desktop, or delete+recreate the docker-desktop cluster

# 2. Install Argo CD (§3)
kubectl create namespace argocd
kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml
kubectl -n argocd rollout status deploy/argocd-server

# 3. Apply the sealed-secrets controller + app-of-apps root
kubectl apply -f deploy/argocd/app-of-apps.yaml

# 4. Build local images the cluster needs (shared Docker daemon, no registry push needed)
docker build -t ecom-mssql-seed:local -f deploy/charts/mssql/seed-image/Dockerfile deploy/charts/mssql/seed-image
docker build -t ecom-new-api:<tag> -f ecom-new-api/Dockerfile ecom-new-api
# optional: docker build -t site-smb:0.1.0 -f UI_DONT_COMMIT/opentext-brands/site_smb/Dockerfile UI_DONT_COMMIT/opentext-brands

# 5. Recreate the manually-managed Secrets (§9) — SA password, DB connection string,
#    UI CMS credentials (if deploying the UI)

# 6. Force a full refresh and watch everything come up
kubectl annotate application ecom-app-of-apps -n argocd argocd.argoproj.io/refresh=hard --overwrite
kubectl get applications -n argocd -w
```

Sealed Secrets (bacpac-restore era) are the exception — they're recreated automatically by
Argo CD from git, no manual step needed for those.
