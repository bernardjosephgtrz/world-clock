# Renders README screenshots straight from the app's UI (offscreen, transparent background),
# so nothing from your desktop ends up in the images. Does not touch your saved settings.
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\screenshots.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$out  = Join-Path $root 'docs'
New-Item -ItemType Directory -Force -Path $out | Out-Null

Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Xaml
$asm = [Reflection.Assembly]::LoadFile((Join-Path $root 'dist\WorldClock.exe'))
$flags = [Reflection.BindingFlags]'Public,NonPublic,Static,Instance'

$app = [System.Windows.Application]::Current
if (-not $app) { $app = New-Object System.Windows.Application }
$styles = $asm.GetType('WorldClock.Theme').GetMethod('LoadStyles', $flags).Invoke($null, @())
$app.Resources.MergedDictionaries.Add($styles)

function Render($name, [bool]$compact, [double]$scale, $zones) {
    $s = New-Object WorldClock.AppSettings
    $s.FirstRun = $false; $s.Compact = $compact; $s.Scale = $scale
    foreach ($z in $zones) {
        $sz = New-Object WorldClock.SavedZone
        $sz.TzId = $z[0]; $sz.City = $z[1]; $sz.Country = $z[2]
        $s.Zones.Add($sz)
    }
    $wt = $asm.GetType('WorldClock.MainWindow')
    $ctor = $wt.GetConstructor([Type[]]@($s.GetType()))
    $w = $ctor.Invoke([object[]]@($s.psobject.BaseObject))
    $content = $w.Content
    $w.Content = $null                     # detach so it can be rendered on its own
    $content.Measure([System.Windows.Size]::new([double]::PositiveInfinity, [double]::PositiveInfinity))
    $content.Arrange([System.Windows.Rect]::new($content.DesiredSize))
    $content.UpdateLayout()
    $dpi = 144                              # 1.5x for sharp images on GitHub
    $px = [int][Math]::Ceiling($content.DesiredSize.Width * $dpi / 96)
    $py = [int][Math]::Ceiling($content.DesiredSize.Height * $dpi / 96)
    $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap $px, $py, $dpi, $dpi, ([System.Windows.Media.PixelFormats]::Pbgra32)
    $rtb.Render($content)
    $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))
    $fs = [IO.File]::Create((Join-Path $out "$name.png")); $enc.Save($fs); $fs.Close()
    Write-Host "docs\$name.png  ${px}x${py}"
}

# The window is only constructed (never shown or interacted with), so no settings are saved.
Render 'full'    $false 1.0 @(@('W. Europe Standard Time','Hamburg','Germany'), @('Eastern Standard Time','New York','United States'))
Render 'compact' $true  1.0 @(@('W. Europe Standard Time','Hamburg','Germany'), @('Eastern Standard Time','New York','United States'))
