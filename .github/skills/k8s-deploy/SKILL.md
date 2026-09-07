---
name: k8s-deploy
description: 'Deploy, redeploy, verify, and troubleshoot the ecom-new stack on Kubernetes via Argo CD GitOps. Use when the user mentions deploy, redeploy, rollout, ship a change, bump the image tag, Argo CD, argocd sync/refresh, Helm chart or values changes under deploy/, sealed secrets, kubeseal, SQL Server seed job, Prometheus/Grafana/ServiceMonitor wiring, port-forward, pods CrashLoopBackOff/ImagePullBackOff, OutOfSync applications, or rebuilding the cluster from scratch.'
argument-hint: 'What to deploy or troubleshoot (e.g. "redeploy the API", "mssql seed job failing")'
---

# Kubernetes Deployment (Argo CD GitOps)

Everything in this repo is deployed by Argo CD from git. **Never `helm install` or
`kubectl apply` a chart directly** — change the manifest/values, commit, push, then refresh
the Argo CD Application. `kubectl` is for reading state and forcing refreshes only.

Full narrative reference: [docs/deployment-runbook.md](../../../docs/deployment-runbook.md).
Read it when you need cloud-vs-local differences, secrets policy details, or first-time setup.

## Topology

| Argo Application | Chart | Namespace | Sync wave |
|---|---|---|---|
| `ecom-namespaces` | `deploy/argocd/namespaces.yaml` | — | 0 |
| `ecom-sealed-secrets` | `deploy/sealed-secrets/` | `kube-system` | 1 |
| `platform-monitoring` | `deploy/charts/monitoring` | `monitoring` | 2 |
| `ecom-mssql` | `deploy/charts/mssql` | `data` | 2 |
| `ecom-api-dev` | `deploy/charts/ecom-api` | `ecom-dev` | 3 |
| `ui-dev` | `deploy/charts/ui` | `ecom-dev` | 3 |

Root: `ecom-app-of-apps` watches `deploy/argocd/apps/`.
Branch every Application tracks: `agents/k8s-deployment-sqlserver-prometheus-grafana`.

Per-env overrides live in `deploy/envs/dev/<chart>-values.yaml` and win over the chart's
own `values.yaml`. Change both only when bumping an image tag (see below); otherwise edit
the env file.

## Procedure: redeploy after an app code change

1. Pick a **new** image tag. Reusing a tag does nothing — `imagePullPolicy: IfNotPresent`
   will not re-pull.
2. Bump `image.tag` in **both** `deploy/charts/ecom-api/values.yaml` and
   `deploy/envs/dev/ecom-api-values.yaml`.
3. Build against the Docker Desktop daemon (shared with the cluster, no registry push):
   ```powershell
   docker build -t ecom-new-api:<new-tag> -f ecom-new-api/Dockerfile ecom-new-api
   ```
4. Commit and push to the tracked branch.
5. Force the sync: `kubectl annotate application ecom-api-dev -n argocd argocd.argoproj.io/refresh=hard --overwrite`
6. Watch: `kubectl get pods -n ecom-dev -w`

For the UI, the same loop with `site-smb:<tag>`, `deploy/envs/dev/ui-values.yaml`, and
Application `ui-dev`. UI source lives in `UI_DONT_COMMIT/` and is not in git — there is no
automatic rebuild trigger for it.

## Procedure: chart or values change only

Edit → commit → push → `kubectl annotate application <app> -n argocd argocd.argoproj.io/refresh=hard --overwrite`.
Before committing, validate rendering locally:

```powershell
helm template deploy/charts/<chart> -f deploy/envs/dev/<chart>-values.yaml | Out-Null
```

## Procedure: add a secret

Decide which pattern applies (see runbook §9):
- **Needs to survive a cluster rebuild via GitOps** → sealed secret. Write plaintext into
  `deploy/secrets-plaintext-DO-NOT-COMMIT/` (gitignored), seal with `kubeseal` into
  `deploy/sealed-secrets/`, commit only the sealed output.
- **Tied to excluded source or ad-hoc** → manual `kubectl create secret generic ...`,
  documented in the runbook, never committed.

Never write a password, connection string, or API key into a chart, values file, or
committed manifest. If asked to, stop and propose the sealed-secret path instead.

## Verification checklist

```powershell
kubectl get applications -n argocd          # all Synced / Healthy
kubectl get pods -A | Select-String -NotMatch "Running|Completed"
kubectl port-forward svc/ecom-api-dev -n ecom-dev 8081:8080
# then: /healthz, /readyz, /metrics, /swagger
```

Port-forwards die when the target pod restarts — just re-run.
Full access table (Argo CD, Grafana, Prometheus, SQL Server): runbook §8.

## Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| App stuck `OutOfSync` | Changes not pushed to the tracked branch, or Argo polling (~3 min). Push, then hard-refresh the Application. |
| Pod `ImagePullBackOff` | Image tag doesn't exist in the local Docker daemon. Rebuild with the exact tag in the values file. |
| New code not picked up | Image tag was not bumped, or bumped in only one of the two values files. |
| API pod not ready | DB unreachable. Check the `ecom-db-credentials` secret in `ecom-dev` and that `ecom-mssql` is Running in `data`. Readiness is a real DB check. |
| Seed job failed | `kubectl logs job/<seed-job-name> -n data`. Seeding runs plain `.sql` scripts from `Project_Seed/`, not a bacpac restore. |
| No metrics in Prometheus | `ServiceMonitor` release label must match `kube-prometheus-stack`. Confirm the target in Prometheus → Status → Targets. |
| Dashboard missing in Grafana | ConfigMap must carry label `grafana_dashboard: "1"`; the sidecar watches all namespaces. |
| PVC won't bind (cloud) | `storageClassName: hostpath` is local-only. Swap for the provider's StorageClass. |

## Cluster rebuild from zero

Order matters: install Argo CD → apply `deploy/argocd/app-of-apps.yaml` → build local
images (`ecom-mssql-seed:local`, `ecom-new-api:<tag>`, optionally `site-smb:<tag>`) →
recreate the manually-managed secrets → hard-refresh `ecom-app-of-apps`. Exact commands:
runbook §10.
