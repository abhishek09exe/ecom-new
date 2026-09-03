<#
.SYNOPSIS
    Generates synthetic traffic against the ecom-api /license-options and
    /bundle-pricing endpoints to populate the custom Prometheus metrics
    (ecom_db_proc_calls_total, ecom_business_operations_total, ecom_errors_total)
    with both success and error samples, for testing/demoing the Grafana dashboard.

.DESCRIPTION
    Requires the ecom-api service to be reachable, typically via:
        kubectl port-forward svc/ecom-api-dev -n ecom-dev 8081:8080

    Success requests use a real message_key GUID (seeded in the dev DB) and a
    valid bundle-pricing query. "Not found"/validation requests use a random GUID
    and a query missing required fields, which the API resolves as a business-level
    not-found (ecom_business_operations_total{outcome="not_found"}) or a 400
    validation error. Use -SimulateDbOutage to additionally generate genuine
    outcome="error" DB samples (ecom_db_proc_calls_total, ecom_errors_total) by
    briefly scaling SQL Server to zero replicas.

.PARAMETER BaseUrl
    Base URL of the ecom-api service. Defaults to the standard local port-forward.

.PARAMETER MessageKey
    A message_key GUID known to exist in the seeded dev DB, used for success calls.
    Defaults to a value seeded by Project_Seed scripts (a license_key GUID that
    joins to a real license row); override if yours differs.

.PARAMETER Iterations
    Number of request cycles to run. Each cycle fires one success and one error
    request against each endpoint. Defaults to 20.

.PARAMETER DelaySeconds
    Delay between each individual request, in seconds. Defaults to 0.5.

.PARAMETER SimulateDbOutage
    If set, scales the SQL Server StatefulSet in the 'data' namespace to 0 replicas
    before the error requests and scales it back to 1 afterwards. This is the most
    reliable way to generate genuine ecom_db_proc_calls_total{outcome="error"} and
    ecom_errors_total samples, since the seeded dev DB has no other easily reachable
    failure path over HTTP. Requires kubectl access to the cluster. Off by default —
    only use this if you specifically want to see the error-outcome series in Grafana.

.EXAMPLE
    .\simulate-metrics.ps1
    Runs 20 cycles (80 requests total) against http://localhost:8081.

.EXAMPLE
    .\simulate-metrics.ps1 -Iterations 50 -DelaySeconds 0.2
    Runs a larger, faster burst — useful for seeing rate() graphs move in Grafana.

.EXAMPLE
    .\simulate-metrics.ps1 -SimulateDbOutage -Iterations 5
    Also briefly stops SQL Server to generate real DB-error samples.
#>

param(
    [string]$BaseUrl = "http://localhost:8081",
    [string]$MessageKey = "E151E1C7-018B-46EF-93A3-2CB7E01805C8",
    [int]$Iterations = 20,
    [double]$DelaySeconds = 0.5,
    [switch]$SimulateDbOutage
)

function Invoke-Sample {
    param(
        [string]$Label,
        [string]$Url
    )
    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
        Write-Host "[$Label] HTTP $($response.StatusCode)" -ForegroundColor Green
    }
    catch {
        $code = $_.Exception.Response.StatusCode.value__
        if (-not $code) { $code = "ERR" }
        Write-Host "[$Label] HTTP $code" -ForegroundColor Yellow
    }
}

Write-Host "Simulating ecom-api traffic against $BaseUrl for $Iterations iteration(s)..." -ForegroundColor Cyan
Write-Host "Success message_key: $MessageKey`n"

if ($SimulateDbOutage) {
    Write-Host "Scaling down ecom-mssql StatefulSet in 'data' namespace to force real DB errors..." -ForegroundColor Magenta
    kubectl scale statefulset/ecom-mssql -n data --replicas=0 | Out-Null
    Write-Host "Waiting for SQL Server pod to terminate..."
    kubectl wait --for=delete pod/ecom-mssql-0 -n data --timeout=60s 2>$null | Out-Null
}

for ($i = 1; $i -le $Iterations; $i++) {
    Write-Host "--- Cycle $i/$Iterations ---"

    # ── license-options: success (real message_key -> DB proc + fn call succeed) ──
    Invoke-Sample -Label "license-options success" `
        -Url "$BaseUrl/license-options?message_key=$MessageKey&locale=en_US"
    Start-Sleep -Seconds $DelaySeconds

    # ── license-options: not-found (random GUID -> resolves cleanly to 404) ──
    $randomGuid = [guid]::NewGuid().ToString()
    Invoke-Sample -Label "license-options not_found" `
        -Url "$BaseUrl/license-options?message_key=$randomGuid&locale=en_US"
    Start-Sleep -Seconds $DelaySeconds

    # ── bundle-pricing: success (valid category/seats/years -> proc returns rows) ──
    Invoke-Sample -Label "bundle-pricing success" `
        -Url "$BaseUrl/bundle-pricing?Items%5B0%5D.LicenseCategoryName=STD&Items%5B0%5D.LicenseSeats=1&Items%5B0%5D.Years=1&Locale=en_US"
    Start-Sleep -Seconds $DelaySeconds

    # ── bundle-pricing: error (missing required Items -> 400 from model validation) ──
    Invoke-Sample -Label "bundle-pricing bad_request" `
        -Url "$BaseUrl/bundle-pricing?Locale=en_US"
    Start-Sleep -Seconds $DelaySeconds
}

if ($SimulateDbOutage) {
    Write-Host "Restoring ecom-mssql StatefulSet to 1 replica..." -ForegroundColor Magenta
    kubectl scale statefulset/ecom-mssql -n data --replicas=1 | Out-Null
    kubectl rollout status statefulset/ecom-mssql -n data --timeout=120s | Out-Null
}

Write-Host "`nDone. Check the metrics with:" -ForegroundColor Cyan
Write-Host "  curl.exe -s $BaseUrl/metrics | Select-String '^ecom_'"
Write-Host "Or view the 'Ecom API - Overview' dashboard in Grafana."
