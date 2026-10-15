$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install the .NET 10 SDK first.' }
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) { throw 'Install Node.js 22.12+ (or 24 LTS) first.' }
Push-Location $root
try {
    dotnet build InventoryReservation.sln
    if ($LASTEXITCODE -ne 0) { throw '.NET build failed.' }
    Push-Location (Join-Path $root 'InventoryReservation.Web')
    try {
        npm ci
        if ($LASTEXITCODE -ne 0) { throw 'npm dependency installation failed.' }
    } finally { Pop-Location }
    function Open-PocWindow([string]$directory, [string]$command) {
        $escaped = $directory.Replace("'", "''")
        $script = "Set-Location -LiteralPath '$escaped'; $command"
        $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
        Start-Process powershell.exe -ArgumentList @('-NoExit', '-EncodedCommand', $encoded)
    }
    Open-PocWindow (Join-Path $root 'InventoryReservation.Api') 'dotnet run --no-build'
    Open-PocWindow (Join-Path $root 'InventoryReservation.Web') 'npm run dev'
    Write-Host 'Open http://localhost:5173 once Vite is ready. Close the two windows to stop the Inventory Application.'
} finally { Pop-Location }
