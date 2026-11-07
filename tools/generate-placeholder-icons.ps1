# Generates the placeholder catalog icons (one per icon key in BuiltInCatalog) into
# src/ExtensionIcons.App/Assets/Icons: a 64px PNG for the in-app grid and a multi-size
# .ico (16/32/48/256) that is what actually gets written to the registry as a DefaultIcon.
# They are stand-ins: replace a pair with real artwork under the same file names and nothing
# else needs to change.
# Run with Windows PowerShell 5.1:  powershell -File tools\generate-placeholder-icons.ps1

Add-Type -AssemblyName System.Drawing

$outDir = Join-Path $PSScriptRoot '..\src\ExtensionIcons.App\Assets\Icons'
New-Item -ItemType Directory -Force $outDir | Out-Null

# icon key -> label, background colour
$icons = [ordered]@{
    html     = @('HTML', '#E34F26')
    xml      = @('XML',  '#8E44AD')
    csv      = @('CSV',  '#1E8E3E')
    json     = @('{ }',  '#C79100')
    text     = @('TXT',  '#5F6368')
    markdown = @('MD',   '#0B5CAD')
    log      = @('LOG',  '#6D4C41')
    settings = @('INI',  '#455A64')
    yaml     = @('YML',  '#C62828')
}

function New-RoundedRect([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $path
}

# Draws the badge at any size by scaling the 64px design.
function New-Badge([string]$label, [string]$color, [int]$size) {
    $s = $size / 64.0
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    $g.Clear([System.Drawing.Color]::Transparent)

    $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml($color))
    $g.FillPath($brush, (New-RoundedRect (4 * $s) (2 * $s) (56 * $s) (60 * $s) (9 * $s)))

    $fontPx = [Math]::Max(5, 16 * $s)
    $font = New-Object System.Drawing.Font 'Segoe UI', $fontPx, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = 'Center'
    $format.LineAlignment = 'Center'
    $rect = New-Object System.Drawing.RectangleF (4 * $s), (2 * $s), (56 * $s), (60 * $s)
    $g.DrawString($label, $font, [System.Drawing.Brushes]::White, $rect, $format)

    $g.Dispose(); $brush.Dispose(); $font.Dispose()
    $bmp
}

# Writes an .ico whose images are PNG-compressed (supported by Windows Vista and later).
function Save-Ico([string]$path, [string]$label, [string]$color, [int[]]$sizes) {
    $pngs = foreach ($size in $sizes) {
        $bmp = New-Badge $label $color $size
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        , $ms.ToArray()
    }

    $stream = [System.IO.File]::Create($path)
    $w = New-Object System.IO.BinaryWriter $stream
    $w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $dim = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
        $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
        $w.Write([uint16]1); $w.Write([uint16]32)
        $w.Write([uint32]$pngs[$i].Length); $w.Write([uint32]$offset)
        $offset += $pngs[$i].Length
    }
    foreach ($png in $pngs) { $w.Write($png) }
    $w.Dispose(); $stream.Dispose()
}

foreach ($key in $icons.Keys) {
    $label, $color = $icons[$key]

    $bmp = New-Badge $label $color 64
    $bmp.Save((Join-Path $outDir "$key.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()

    Save-Ico (Join-Path $outDir "$key.ico") $label $color @(16, 32, 48, 256)
}

"Wrote $($icons.Count) PNG + ICO pairs to $outDir"
