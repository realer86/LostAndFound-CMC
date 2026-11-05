# Chay tu bat ky thu muc nao: powershell -ExecutionPolicy Bypass -File .\scripts\check-admin.ps1
# Chi chan doan. Khong tu dong dung/xoa containers hoac volumes.
$ErrorActionPreference = 'Continue'
Set-Location (Split-Path -Parent $PSScriptRoot)

Write-Host "=== Foundly Admin: kiem tra Docker ===" -ForegroundColor Cyan
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host 'Khong tim thay Docker CLI.' -ForegroundColor Red
    exit 1
}

docker compose config --quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Compose khong hop le; xem thong bao loi o tren.' -ForegroundColor Red
    exit 1
}

Write-Host "`n--- Compose projects (co the ton tai project cu) ---"
docker compose ls -a
Write-Host "`n--- Trang thai container ---"
docker compose ps
Write-Host "`n--- Log Admin (50 dong cuoi) ---"
docker compose logs admin --tail=50

$mapping = (docker compose port admin 5174 2>$null | Select-Object -First 1)
if (-not $mapping) {
    Write-Host "`nAdmin chua duoc public ra host. Kiem tra 'docker compose ps' va loi bind port." -ForegroundColor Yellow
    Write-Host 'Goi y: docker compose up -d --build --force-recreate admin'
    exit 2
}
$mapping = $mapping.Trim()
Write-Host "`nAnh xa Admin: $mapping" -ForegroundColor Cyan
if ($mapping -notmatch ':(\d+)$') {
    Write-Host 'Khong doc duoc host port tu output Docker.' -ForegroundColor Yellow
    exit 2
}
$hostPort = [int]$Matches[1]
$url = "http://127.0.0.1:$hostPort/"
Write-Host "Thu truy cap $url"
try {
    $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec 6 -UseBasicParsing
    Write-Host "HTTP $($response.StatusCode) - Admin HTTP da phan hoi." -ForegroundColor Green
} catch {
    Write-Host "Khong ket noi duoc Admin: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host 'Neu container Up/healthy nhung khong truy cap duoc, kiem tra proxy/firewall/Docker Desktop cua may.' -ForegroundColor Yellow
    exit 3
}
