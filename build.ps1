# Baut Multikeys.exe mit dem in Windows enthaltenen C#-Compiler (.NET Framework 4).
# Es wird kein zusaetzliches SDK benoetigt.

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) {
    throw "C#-Compiler (csc.exe) nicht gefunden. .NET Framework 4 wird benoetigt."
}

$src = Get-ChildItem -Path (Join-Path $root "src") -Filter *.cs | ForEach-Object { $_.FullName }
$out = Join-Path $root "Multikeys.exe"

$refs = @(
    "/r:System.dll",
    "/r:System.Core.dll",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll",
    "/r:System.Web.Extensions.dll"
)

Write-Host "Kompiliere Multikeys ..." -ForegroundColor Cyan
& $csc /nologo /target:winexe /platform:anycpu /optimize+ "/out:$out" $refs $src

if ($LASTEXITCODE -eq 0) {
    Write-Host "Fertig: $out" -ForegroundColor Green
} else {
    throw "Kompilierung fehlgeschlagen (Exit-Code $LASTEXITCODE)."
}
