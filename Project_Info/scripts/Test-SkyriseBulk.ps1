<#
.SYNOPSIS
  Manually drives the two-call Skyrise flow (SkyIdentity token + /api/v1/bulk createkey)
  from the terminal, mirroring SkyriseKeycodeClient.cs / LegacyAuthTokenSigner.cs, so you can
  inspect the raw HTTP responses without running the xUnit live test.

.USAGE
  pwsh ./Project_Info/scripts/Test-SkyriseBulk.ps1
  pwsh ./Project_Info/scripts/Test-SkyriseBulk.ps1 -Action createmasterkey
#>

param(
	[string]$Action = "createkey",                 # "createkey" or "createmasterkey"
	[string]$ClientId = "EcomQASkyRise",
	[string]$ClientSecret = 'hoW^6jiZrd7uUN#1JmoBX$JkjLpBO9mK',
	[string]$TokenUrl = "https://skyidentity-qa.webrootcloudav.com/connect/token",
	[string]$BaseUrl = "https://skyrise-qa.webrootcloudav.com",
	[string]$SkyriseClientName = "ThundercatsQA",
	[string]$SkyriseAccessKey = "ab387584-11f8-473e-a1e8-e3a4c1784661",
	[string]$SkyriseSecretKey = "vZv4bKfrZA88ZAeuAzC9JZT7Xqu5xFUWuL3-mEg4EahJ"
)

# ---------- Step 1: SkyIdentity OAuth2 client_credentials token ----------
Write-Host "`n=== Step 1: Requesting SkyIdentity bearer token ===" -ForegroundColor Cyan

$tokenResponse = Invoke-RestMethod -Method Post -Uri $TokenUrl -ContentType "application/x-www-form-urlencoded" -Body @{
	client_id     = $ClientId
	client_secret = $ClientSecret
	grant_type    = "client_credentials"
}

$accessToken = $tokenResponse.access_token
Write-Host "Got access_token (expires_in=$($tokenResponse.expires_in)s)" -ForegroundColor Green

# ---------- Step 2: Build the LegacyAuthToken RequestToken (HMAC-SHA256) ----------
Write-Host "`n=== Step 2: Signing inner RequestToken ===" -ForegroundColor Cyan

$keycode = "noKeycode"
$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds().ToString()
$elems = @($SkyriseClientName, $Action, $keycode, $SkyriseAccessKey, $timestamp)

# lowercase + alphabetical sort + concat (matches LegacyAuthTokenSigner.cs)
$signingString = ($elems | ForEach-Object { $_.ToLowerInvariant() } | Sort-Object) -join ""

$hmac = New-Object System.Security.Cryptography.HMACSHA256
$hmac.Key = [System.Text.Encoding]::UTF8.GetBytes($SkyriseSecretKey)
$hashBytes = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($signingString))
$hexHmac = ([System.BitConverter]::ToString($hashBytes) -replace '-', '').ToUpperInvariant()

$requestToken = "$hexHmac,$($elems -join ',')"
Write-Host "RequestToken: $requestToken" -ForegroundColor Green

# ---------- Step 3: Build the inner body + bulk envelope ----------
$path = if ($Action -eq "createmasterkey") { "/master/createmasterkey" } else { "/key/createkey" }
$transactionRefId = "$timestamp$(Get-Random -Minimum 1000 -Maximum 9999)"

$innerBody = [ordered]@{
	LicenseDistCode    = "TEST"
	LicenseCategory    = "SAEP"
	Storage            = "0"
	FirstName          = "Test"
	LastName           = "User"
	Email              = "test@example.com"
	DurationInDays     = "15"
	Seats              = "1"
	IsTrial            = "true"
	LicenseTypeId      = "1"              # adjust to a real LICENSE_TYPE_ID_MAP value if needed
	LicenseCategoryId  = "1"              # adjust to a real PRODUCT_MATRIX value if needed
	LicenseKeycodeTypeId = "1"
	iso                = "US"
	TransactionRefId   = $transactionRefId
	LicenseModules     = ""
	RequestToken       = $requestToken
}

if ($Action -eq "createmasterkey") {
	$innerBody["SeatCount"] = "1"
}

$bulkItem = @{
	method = "POST"
	bulkId = [guid]::NewGuid().ToString()
	path   = $path
	body   = $innerBody
}

$bulkPayload = @($bulkItem)
$bulkJsonInner = ConvertTo-Json -InputObject $bulkItem -Depth 10
$bulkJson = "[$bulkJsonInner]"
Write-Host "`nBulk request payload:" -ForegroundColor Cyan
Write-Host $bulkJson

# ---------- Step 4: Call /api/v1/bulk ----------
Write-Host "`n=== Step 3: Calling $BaseUrl/api/v1/bulk ===" -ForegroundColor Cyan

try {
	$bulkResponse = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/bulk" `
		-Headers @{ Authorization = "Bearer $accessToken" } `
		-ContentType "application/json" `
		-Body $bulkJson

	Write-Host "`nResponse:" -ForegroundColor Green
	$bulkResponse | ConvertTo-Json -Depth 10
}
catch {
	Write-Host "`nRequest failed:" -ForegroundColor Red
	Write-Host $_.Exception.Message
	if ($_.Exception.Response) {
		$stream = $_.Exception.Response.GetResponseStream()
		$reader = New-Object System.IO.StreamReader($stream)
		$errorBody = $reader.ReadToEnd()
		Write-Host "Response body:" -ForegroundColor Yellow
		Write-Host $errorBody
	}
	elseif ($_.ErrorDetails) {
		Write-Host $_.ErrorDetails.Message
	}
}
