# Run from any working directory: powershell -ExecutionPolicy Bypass -File .\scripts\check-services.ps1
# Read-only diagnostics: does not stop containers, overwrite files, or delete volumes.
$ErrorActionPreference = 'Continue'
Set-Location (Split-Path -Parent $PSScriptRoot)

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host 'Docker CLI not found.' -ForegroundColor Red
    exit 1
}

docker compose config --quiet
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host '=== Compose status ===' -ForegroundColor Cyan
docker compose ps

$failed = $false
$services = @(
    @{ Name = 'admin'; Inside = 5174; Path = '/' },
    @{ Name = 'web'; Inside = 5175; Path = '/' },
    @{ Name = 'api'; Inside = 8080; Path = '/api/health' },
    @{ Name = 'postgres'; Inside = 5432; Path = $null }
)

foreach ($service in $services) {
    # Resolve hashtable members into scalar variables before calling native executables.
    $name = [string]$service['Name']
    $inside = [int]$service['Inside']
    $mappingLines = @(docker compose port $name $inside 2>$null)
    $portExitCode = $LASTEXITCODE
    $mapping = [string]($mappingLines | Select-Object -First 1)
    $portMatch = [regex]::Match($mapping, ':(\d+)\s*$')
    if ($portExitCode -ne 0 -or -not $portMatch.Success) {
        Write-Host "FAIL $($service.Name): no host port mapping" -ForegroundColor Red
        $failed = $true
        continue
    }
    $port = [int]$portMatch.Groups[1].Value
    if ($null -ne $service.Path) {
        $url = "http://127.0.0.1:$port$($service.Path)"
        try {
            $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec 6 -UseBasicParsing
            Write-Host "PASS $($service.Name): HTTP $($response.StatusCode) $url" -ForegroundColor Green
        } catch {
            Write-Host "FAIL $($service.Name): $url - $($_.Exception.Message)" -ForegroundColor Red
            $failed = $true
            docker compose logs $name --tail=20
        }
    } else {
        # Database has no HTTP endpoint. Verify that its published TCP port accepts connections.
        $client = New-Object System.Net.Sockets.TcpClient
        try {
            $pending = $client.BeginConnect('127.0.0.1', $port, $null, $null)
            if (-not $pending.AsyncWaitHandle.WaitOne(3000, $false)) {
                throw 'TCP connect timed out'
            }
            $client.EndConnect($pending)
            Write-Host "PASS postgres: TCP localhost:$port" -ForegroundColor Green
        } catch {
            Write-Host "FAIL postgres: TCP localhost:$port - $($_.Exception.Message)" -ForegroundColor Red
            $failed = $true
            docker compose logs postgres --tail=20
        } finally {
            $client.Close()
        }
    }
}
if ($failed) { exit 2 }
Write-Host 'All 4 service endpoints are reachable.' -ForegroundColor Green
