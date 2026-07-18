# Erzeugt icon.ico (mehrere Groessen) fuer Multikeys.
# Zeichnet eine blaue Tastenkappe mit einem weissen "M".
# Benoetigt kein externes Werkzeug (nur System.Drawing).

Add-Type -AssemblyName System.Drawing

function Add-RoundedRect([System.Drawing.Drawing2D.GraphicsPath]$path, [System.Drawing.RectangleF]$r, [single]$radius) {
    $d = $radius * 2
    $path.AddArc($r.X, $r.Y, $d, $d, 180, 90)
    $path.AddArc($r.Right - $d, $r.Y, $d, $d, 270, 90)
    $path.AddArc($r.Right - $d, $r.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($r.X, $r.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()
}

function New-KeyIcon([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.Clear([System.Drawing.Color]::Transparent)

    # Hintergrund: abgerundetes Quadrat mit blauem Verlauf
    $pad = [single]([Math]::Max(1, $size * 0.05))
    $bgRect = New-Object System.Drawing.RectangleF($pad, $pad, ($size - 2 * $pad), ($size - 2 * $pad))
    $bgPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    Add-RoundedRect $bgPath $bgRect ([single]($size * 0.22))
    $c1 = [System.Drawing.Color]::FromArgb(255, 45, 156, 219)
    $c2 = [System.Drawing.Color]::FromArgb(255, 20, 86, 140)
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($bgRect, $c1, $c2, 55.0)
    $g.FillPath($bgBrush, $bgPath)

    # Tastenkappe: helles abgerundetes Rechteck
    $inset = [single]($size * 0.20)
    $capRect = New-Object System.Drawing.RectangleF($inset, ($inset * 0.9), ($size - 2 * $inset), ($size - 1.9 * $inset))
    $capPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    Add-RoundedRect $capPath $capRect ([single]($size * 0.10))
    $capBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 245, 249, 252))
    $g.FillPath($capBrush, $capPath)

    # Buchstabe "M"
    $fontSize = [single]($size * 0.42)
    $font = New-Object System.Drawing.Font("Segoe UI", $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = [System.Drawing.StringAlignment]::Center
    $fmt.LineAlignment = [System.Drawing.StringAlignment]::Center
    $textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 20, 86, 140))
    $g.DrawString("M", $font, $textBrush, $capRect, $fmt)

    $g.Dispose()
    return $bmp
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$pngs = @()
foreach ($s in $sizes) {
    $bmp = New-KeyIcon $s
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += , ($ms.ToArray())
    $ms.Dispose()
    $bmp.Dispose()
}

$outPath = Join-Path (Split-Path $PSScriptRoot -Parent) "icon.ico"
$fs = [System.IO.File]::Create($outPath)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([UInt16]0)              # reserviert
$bw.Write([UInt16]1)              # Typ = Icon
$bw.Write([UInt16]$sizes.Count)   # Anzahl Bilder

$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $data = $pngs[$i]
    $wb = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$wb)          # Breite
    $bw.Write([byte]$wb)          # Hoehe
    $bw.Write([byte]0)            # Farben
    $bw.Write([byte]0)            # reserviert
    $bw.Write([UInt16]1)          # Ebenen
    $bw.Write([UInt16]32)         # Bit pro Pixel
    $bw.Write([UInt32]$data.Length)
    $bw.Write([UInt32]$offset)
    $offset += $data.Length
}
foreach ($data in $pngs) { $bw.Write($data) }
$bw.Flush(); $bw.Dispose(); $fs.Dispose()

Write-Host "icon.ico erstellt: $outPath" -ForegroundColor Green
