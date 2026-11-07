# Generates the placeholder catalog icons (one PNG per icon key in BuiltInCatalog)
# into src/ExtensionIcons.App/Assets/Icons. They are stand-ins: replace a PNG with
# real artwork under the same file name and nothing else needs to change.
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

foreach ($key in $icons.Keys) {
    $label, $color = $icons[$key]
    $bmp = New-Object System.Drawing.Bitmap 64, 64
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    $g.Clear([System.Drawing.Color]::Transparent)

    $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml($color))
    $g.FillPath($brush, (New-RoundedRect 4 2 56 60 9))

    $font = New-Object System.Drawing.Font 'Segoe UI', 16, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = 'Center'
    $format.LineAlignment = 'Center'
    $g.DrawString($label, $font, [System.Drawing.Brushes]::White, (New-Object System.Drawing.RectangleF 4, 2, 56, 60), $format)

    $bmp.Save((Join-Path $outDir "$key.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose(); $brush.Dispose(); $font.Dispose()
}

"Wrote $($icons.Count) icons to $outDir"
