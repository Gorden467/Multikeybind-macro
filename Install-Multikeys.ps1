# =====================================================================
#  Multikeys - Einrichtung / Starter
#
#  Prueft die Voraussetzungen (.NET Framework 4), installiert sie bei
#  Bedarf automatisch, baut die App falls noetig, legt Verknuepfungen
#  an und startet Multikeys.
#
#  Hinweis: Die App selbst benoetigt .NET Framework 4 zum Laufen und
#  kann das daher nicht selbst pruefen - dieses Skript uebernimmt das
#  vor dem Start.
# =====================================================================

param(
    [switch]$NoLaunch,      # App am Ende nicht starten
    [switch]$NoShortcuts,   # keine Verknuepfungen anlegen
    [switch]$NoAutostart    # keine Autostart-Verknuepfung anlegen
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

function Write-Step($text) { Write-Host ""; Write-Host "==> $text" -ForegroundColor Cyan }
function Write-Ok($text)   { Write-Host "    [OK] $text" -ForegroundColor Green }
function Write-Warn($text) { Write-Host "    [!]  $text" -ForegroundColor Yellow }

# --- Voraussetzung 1: .NET Framework 4 ---------------------------------

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

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    return (New-Object Security.Principal.WindowsPrincipal($id)).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Install-DotNet4 {
    # Offizieller Microsoft-Link (.NET Framework 4.8 Web-Installer)
    $url = "https://go.microsoft.com/fwlink/?LinkId=2085155"
    $tmp = Join-Path $env:TEMP "ndp48-web-setup.exe"

    Write-Host "    Lade .NET Framework 4.8 von Microsoft herunter ..."
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $url -OutFile $tmp -UseBasicParsing

    Write-Host "    Installiere .NET Framework 4.8 (kann einige Minuten dauern) ..."
    $proc = Start-Process -FilePath $tmp -ArgumentList "/q", "/norestart" -Wait -PassThru
    Remove-Item $tmp -ErrorAction SilentlyContinue

    # 0 = OK, 3010/1641 = OK aber Neustart noetig
    if ($proc.ExitCode -notin @(0, 3010, 1641)) {
        throw ".NET-Installation fehlgeschlagen (Exit-Code $($proc.ExitCode))."
    }
    if ($proc.ExitCode -in @(3010, 1641)) {
        Write-Warn "Ein Windows-Neustart ist noetig, um .NET fertig einzurichten."
    }
}

Write-Host "======================================" -ForegroundColor White
Write-Host "  Multikeys - Einrichtung" -ForegroundColor White
Write-Host "======================================" -ForegroundColor White

Write-Step "Pruefe .NET Framework 4 ..."
if (Test-DotNet4) {
    Write-Ok ".NET Framework 4 ist installiert."
}
else {
    Write-Warn ".NET Framework 4 wurde nicht gefunden."

    if (-not (Test-Admin)) {
        Write-Host "    Fuer die Installation werden Administratorrechte benoetigt."
        Write-Host "    Starte das Skript mit erhoehten Rechten neu ..."
        try {
            Start-Process -FilePath "powershell.exe" -Verb RunAs -ArgumentList @(
                "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "`"$PSCommandPath`"")
        }
        catch {
            Write-Warn "Erhoehte Rechte wurden abgelehnt. Bitte .NET Framework 4.8 manuell installieren:"
            Write-Host "    https://dotnet.microsoft.com/download/dotnet-framework/net48"
        }
        return
    }

    try {
        Install-DotNet4
        if (Test-DotNet4) { Write-Ok ".NET Framework 4 wurde installiert." }
        else { throw "Nach der Installation weiterhin nicht gefunden." }
    }
    catch {
        Write-Warn "Automatische Installation nicht moeglich: $($_.Exception.Message)"
        Write-Host "    Bitte .NET Framework 4.8 manuell installieren:"
        Write-Host "    https://dotnet.microsoft.com/download/dotnet-framework/net48"
        Read-Host "    Mit Enter beenden"
        return
    }
}

# --- App bereitstellen -------------------------------------------------

$exe = Join-Path $root "Multikeys.exe"
if (-not (Test-Path $exe)) {
    Write-Step "Multikeys.exe fehlt - baue die App ..."
    & (Join-Path $root "build.ps1")
    if (-not (Test-Path $exe)) { throw "Build fehlgeschlagen - Multikeys.exe wurde nicht erzeugt." }
    Write-Ok "Multikeys.exe wurde erstellt."
}
else {
    Write-Ok "Multikeys.exe ist vorhanden."
}

# --- Verknuepfungen ----------------------------------------------------

function New-Shortcut($linkPath, $target, $workdir, $icon, $desc) {
    $sh = New-Object -ComObject WScript.Shell
    $lnk = $sh.CreateShortcut($linkPath)
    $lnk.TargetPath = $target
    $lnk.WorkingDirectory = $workdir
    if ($icon -and (Test-Path $icon)) { $lnk.IconLocation = "$icon,0" }
    $lnk.Description = $desc
    $lnk.Save()
}

if (-not $NoShortcuts) {
    Write-Step "Lege Verknuepfungen an ..."
    $icon = Join-Path $root "icon.ico"

    $desktop = [Environment]::GetFolderPath("Desktop")
    New-Shortcut (Join-Path $desktop "Multikeys.lnk") $exe $root $icon "Multikeys - Tastatur-Makros"
    Write-Ok "Desktop-Verknuepfung erstellt."

    if (-not $NoAutostart) {
        $startup = [Environment]::GetFolderPath("Startup")
        New-Shortcut (Join-Path $startup "Multikeys.lnk") $exe $root $icon "Multikeys - startet mit Windows"
        Write-Ok "Autostart-Verknuepfung erstellt (startet mit Windows)."
    }
}

# --- Start -------------------------------------------------------------

if (-not $NoLaunch) {
    Write-Step "Starte Multikeys ..."
    Start-Process -FilePath $exe -WorkingDirectory $root
    Write-Ok "Multikeys wurde gestartet."
}

Write-Host ""
Write-Host "Fertig. Viel Spass mit Multikeys!" -ForegroundColor Green
