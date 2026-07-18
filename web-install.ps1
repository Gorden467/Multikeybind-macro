# =====================================================================
#  Multikeys - PowerShell-Web-Installer
#
#  Installiert Multikeys mit einem Befehl:
#
#    irm https://raw.githubusercontent.com/Gorden467/Multikeybind-macro/main/web-install.ps1 | iex
#
#  Laedt die App nach %LOCALAPPDATA%\Multikeys, legt Verknuepfungen
#  (Desktop + Autostart) an und startet sie.
# =====================================================================

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$base = "https://raw.githubusercontent.com/Gorden467/Multikeybind-macro/main/dist"
$dir = Join-Path $env:LOCALAPPDATA "Multikeys"

Write-Host ""
Write-Host "======================================" -ForegroundColor White
Write-Host "  Multikeys - Installation" -ForegroundColor White
Write-Host "======================================" -ForegroundColor White

# .NET Framework 4 pruefen (dass dieses Skript laeuft, spricht bereits dafuer).
function Test-DotNet4 {
    foreach ($k in @(
            "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full",
            "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client")) {
        if (Test-Path $k) {
            $p = Get-ItemProperty $k -ErrorAction SilentlyContinue
            if ($p.Install -eq 1 -or $p.Release) { return $true }
        }
    }
    return $false
}
if (Test-DotNet4) { Write-Host "  [OK] .NET Framework 4 vorhanden." -ForegroundColor Green }
else { Write-Host "  [!]  .NET Framework 4 nicht gefunden - bitte ggf. .NET 4.8 nachinstallieren." -ForegroundColor Yellow }

New-Item -ItemType Directory -Force -Path $dir | Out-Null

$exe = Join-Path $dir "Multikeys.exe"
$ico = Join-Path $dir "icon.ico"

Write-Host "  Lade Multikeys ..."
Invoke-WebRequest -Uri "$base/Multikeys.exe" -OutFile $exe -UseBasicParsing
Invoke-WebRequest -Uri "$base/icon.ico" -OutFile $ico -UseBasicParsing

# Verknuepfungen
$sh = New-Object -ComObject WScript.Shell
function New-Sc($linkPath, $target, $workdir, $icon, $desc) {
    $l = $sh.CreateShortcut($linkPath)
    $l.TargetPath = $target
    $l.WorkingDirectory = $workdir
    if (Test-Path $icon) { $l.IconLocation = "$icon,0" }
    $l.Description = $desc
    $l.Save()
}
New-Sc (Join-Path ([Environment]::GetFolderPath("Desktop")) "Multikeys.lnk") $exe $dir $ico "Multikeys - Tastatur-Makros"
New-Sc (Join-Path ([Environment]::GetFolderPath("Startup")) "Multikeys.lnk") $exe $dir $ico "Multikeys - startet mit Windows"
Write-Host "  [OK] Verknuepfungen erstellt (Desktop + Autostart)." -ForegroundColor Green

Start-Process -FilePath $exe -WorkingDirectory $dir

Write-Host ""
Write-Host "Fertig! Multikeys wurde installiert und gestartet." -ForegroundColor Green
Write-Host "Ordner: $dir"
