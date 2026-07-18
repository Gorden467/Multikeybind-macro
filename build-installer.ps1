# Baut alle Distributionsdateien nach dist\:
#   - Multikeys.exe        (die App)
#   - Setup-Multikeys.exe  (der Installer zum Doppelklicken)
#   - icon.ico             (App-Symbol)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

# 1) App bauen
& (Join-Path $root "build.ps1")

# 2) dist vorbereiten
$dist = Join-Path $root "dist"
New-Item -ItemType Directory -Force -Path $dist | Out-Null
Copy-Item (Join-Path $root "Multikeys.exe") (Join-Path $dist "Multikeys.exe") -Force
Copy-Item (Join-Path $root "icon.ico") (Join-Path $dist "icon.ico") -Force

# 3) Installer bauen
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" }
if (-not (Test-Path $csc)) { throw "csc.exe nicht gefunden (.NET Framework 4 benoetigt)." }

Write-Host "Kompiliere Setup-Multikeys.exe ..." -ForegroundColor Cyan
& $csc /nologo /target:exe /optimize+ "/out:$(Join-Path $dist 'Setup-Multikeys.exe')" `
    /r:System.dll (Join-Path $root "installer\Setup.cs")
if ($LASTEXITCODE -ne 0) { throw "Setup-Kompilierung fehlgeschlagen." }

Write-Host "Fertig. Distributionsdateien in: $dist" -ForegroundColor Green
Get-ChildItem $dist | Select-Object Name, Length
