param(
    [switch]$StartServices
)

$ErrorActionPreference = "Stop"
$script:Passed = 0
$script:Failed = 0
$RunId = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds().ToString()

function Write-Section([string]$Number, [string]$Title) {
    Write-Host "`n[$Number] $Title" -ForegroundColor Yellow
}

function Pass([string]$Name) {
    $script:Passed++
    Write-Host "[PASS] $Name" -ForegroundColor Green
}

function Fail([string]$Name, [string]$Details) {
    $script:Failed++
    Write-Host "[FAIL] $Name" -ForegroundColor Red
    Write-Host "       $Details"
}

function Invoke-Get([string]$Url) {
    try {
        return Invoke-RestMethod -Method Get -Uri $Url -TimeoutSec 10
    }
    catch {
        return $null
    }
}

function Invoke-PostJson([string]$Url, [object]$Body, [switch]$IgnoreErrors) {
    try {
        return Invoke-RestMethod -Method Post -Uri $Url -ContentType "application/json" -Body ($Body | ConvertTo-Json -Compress) -TimeoutSec 10
    }
    catch {
        if ($IgnoreErrors) { return $null }
        throw
    }
}

function To-CompactJson([object]$Value) {
    if ($null -eq $Value) { return "" }
    return $Value | ConvertTo-Json -Depth 10 -Compress
}

function Wait-Until([scriptblock]$Probe, [scriptblock]$Predicate, [int]$TimeoutSeconds = 30) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $last = $null
    while ((Get-Date) -lt $deadline) {
        $last = & $Probe
        if (& $Predicate $last) {
            return $last
        }
        Start-Sleep -Seconds 1
    }
    return $last
}

function Check-Contains([string]$Name, [string]$Expected, [object]$Actual) {
    $json = To-CompactJson $Actual
    if ($json -like "*$Expected*") {
        Pass $Name
    }
    else {
        Fail $Name "Expected '$Expected', actual: $json"
    }
}

if ($StartServices) {
    Write-Host "Building and starting Docker services..." -ForegroundColor Cyan
    docker compose up -d --build
    if ($LASTEXITCODE -ne 0) { throw "docker compose up failed" }
}

$Root = "root_$RunId"
$U1 = "u1_$RunId"
$U2 = "u2_$RunId"
$U3 = "u3_$RunId"
$LinearEvent = "linear_$RunId"
$FibEvent = "fib_$RunId"
$IdempotentEvent = "idempotent_$RunId"
$LossEvent = "loss_$RunId"

Write-Section "1/8" "Health checks"
$services = @(
    @{ Name = "UserService"; Url = "http://localhost:5001/health" },
    @{ Name = "EventService"; Url = "http://localhost:5002/health" },
    @{ Name = "CommissionService"; Url = "http://localhost:5003/health" },
    @{ Name = "WalletService"; Url = "http://localhost:5004/health" }
)
foreach ($service in $services) {
    $result = Wait-Until `
        { try { (Invoke-WebRequest -UseBasicParsing -Uri $service.Url -TimeoutSec 5).Content } catch { "" } } `
        { param($value) $value -like "*Healthy*" } `
        90
    if ($result -like "*Healthy*") { Pass "$($service.Name) health check" }
    else { Fail "$($service.Name) health check" "Last response: $result" }
}

Write-Section "2/8" "Create partner hierarchy"
Invoke-PostJson "http://localhost:5001/api/users" @{ externalId = $Root } | Out-Null
Invoke-PostJson "http://localhost:5001/api/users" @{ externalId = $U1; parentExternalId = $Root } | Out-Null
Invoke-PostJson "http://localhost:5001/api/users" @{ externalId = $U2; parentExternalId = $U1 } | Out-Null
Invoke-PostJson "http://localhost:5001/api/users" @{ externalId = $U3; parentExternalId = $U2 } | Out-Null

$chain = Invoke-Get "http://localhost:5001/api/users/$U3/chain"
Check-Contains "Upward chain contains level-1 partner" $U2 $chain
Check-Contains "Upward chain contains level-2 partner" $U1 $chain
Check-Contains "Upward chain contains level-3 partner" $Root $chain

Write-Section "3/8" "Downline projection"
$downline = Invoke-Get "http://localhost:5001/api/users/$Root/downline"
Check-Contains "Root downline contains first child" $U1 $downline
Check-Contains "Root downline contains second-level child" $U2 $downline
Check-Contains "Root downline contains third-level child" $U3 $downline

Write-Section "4/8" "Linear commission calculation and multi-partner payout"
Invoke-PostJson "http://localhost:5003/api/admin/schema" @{ schema = "Linear" } | Out-Null
Invoke-PostJson "http://localhost:5002/api/events" @{
    eventExternalId = $LinearEvent
    userExternalId = $U3
    profit = 1000
    occurredAt = [DateTime]::UtcNow.ToString("o")
} | Out-Null

$linear = Wait-Until `
    { Invoke-Get "http://localhost:5003/api/commissions/$LinearEvent" } `
    { param($value) (To-CompactJson $value) -like '*"amount":30*' } `
    30
Check-Contains "Linear level 1 amount is 10" '"amount":10' $linear
Check-Contains "Linear level 2 amount is 20" '"amount":20' $linear
Check-Contains "Linear level 3 amount is 30" '"amount":30' $linear
Check-Contains "Commission scheme is persisted as Linear" '"schemaType":"Linear"' $linear

$balanceU2 = Wait-Until `
    { Invoke-Get "http://localhost:5004/api/wallets/$U2/balance" } `
    { param($value) $null -ne $value -and $value.balance -eq 10 } `
    30
if ($null -ne $balanceU2 -and $balanceU2.balance -eq 10) { Pass "Level-1 partner payout is deposited" }
else { Fail "Level-1 partner payout is deposited" (To-CompactJson $balanceU2) }

$balanceU1 = Wait-Until `
    { Invoke-Get "http://localhost:5004/api/wallets/$U1/balance" } `
    { param($value) $null -ne $value -and $value.balance -eq 20 } `
    30
if ($null -ne $balanceU1 -and $balanceU1.balance -eq 20) { Pass "Level-2 partner payout is deposited" }
else { Fail "Level-2 partner payout is deposited" (To-CompactJson $balanceU1) }

$balanceRoot = Wait-Until `
    { Invoke-Get "http://localhost:5004/api/wallets/$Root/balance" } `
    { param($value) $null -ne $value -and $value.balance -eq 30 } `
    30
if ($null -ne $balanceRoot -and $balanceRoot.balance -eq 30) { Pass "Level-3 partner payout is deposited" }
else { Fail "Level-3 partner payout is deposited" (To-CompactJson $balanceRoot) }

Write-Section "5/8" "Fibonacci commission scheme"
Invoke-PostJson "http://localhost:5003/api/admin/schema" @{ schema = "Fibonacci" } | Out-Null
Invoke-PostJson "http://localhost:5002/api/events" @{
    eventExternalId = $FibEvent
    userExternalId = $U3
    profit = 1000
    occurredAt = [DateTime]::UtcNow.ToString("o")
} | Out-Null

$fib = Wait-Until `
    { Invoke-Get "http://localhost:5003/api/commissions/$FibEvent" } `
    { param($value) $null -ne $value -and $value.commissions.Count -eq 3 } `
    30
Check-Contains "Fibonacci event uses Fibonacci scheme" '"schemaType":"Fibonacci"' $fib
if ($null -ne $fib -and @($fib.commissions | Where-Object { $_.amount -eq 10 }).Count -eq 2) {
    Pass "Fibonacci levels 1 and 2 both produce amount 10"
}
else {
    Fail "Fibonacci levels 1 and 2 both produce amount 10" (To-CompactJson $fib)
}

Write-Section "6/8" "Historical scheme immutability"
$oldLinear = Invoke-Get "http://localhost:5003/api/commissions/$LinearEvent"
Check-Contains "Existing Linear commissions remain Linear" '"schemaType":"Linear"' $oldLinear
Invoke-PostJson "http://localhost:5003/api/admin/schema" @{ schema = "Linear" } | Out-Null

Write-Section "7/8" "Idempotency"
$idempotentPayload = @{
    eventExternalId = $IdempotentEvent
    userExternalId = $U3
    profit = 100
    occurredAt = [DateTime]::UtcNow.ToString("o")
}
1..3 | ForEach-Object {
    Invoke-PostJson "http://localhost:5002/api/events" $idempotentPayload -IgnoreErrors | Out-Null
}
$idempotent = Wait-Until `
    { Invoke-Get "http://localhost:5003/api/commissions/$IdempotentEvent" } `
    { param($value) $null -ne $value -and $value.commissions.Count -gt 0 } `
    30
if ($null -ne $idempotent -and $idempotent.commissions.Count -eq 3) {
    Pass "Duplicate requests create exactly one commission per partner level"
}
else {
    Fail "Duplicate requests create exactly one commission per partner level" (To-CompactJson $idempotent)
}

Write-Section "8/8" "Negative profit"
Invoke-PostJson "http://localhost:5002/api/events" @{
    eventExternalId = $LossEvent
    userExternalId = $U3
    profit = -500
    occurredAt = [DateTime]::UtcNow.ToString("o")
} | Out-Null
Start-Sleep -Seconds 2
$loss = Invoke-Get "http://localhost:5003/api/commissions/$LossEvent"
if ($null -ne $loss -and $loss.commissions.Count -eq 0) { Pass "Loss event produces no commissions" }
else { Fail "Loss event produces no commissions" (To-CompactJson $loss) }

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " Test summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Passed: $script:Passed" -ForegroundColor Green
Write-Host "Failed: $script:Failed" -ForegroundColor Red

if ($script:Failed -eq 0) {
    Write-Host "All end-to-end checks passed." -ForegroundColor Green
    exit 0
}

Write-Host "Some checks failed. Inspect logs with: docker compose logs --tail=200" -ForegroundColor Red
exit 1
