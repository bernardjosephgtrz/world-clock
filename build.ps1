# Builds World Clock with the C# compiler that ships with Windows (.NET Framework 4.8).
# No SDK, NuGet or internet access needed.
#   .\build.ps1          -> dist\WorldClock.exe
#   .\build.ps1 -Test    -> also builds and runs the unit tests
param([switch]$Test)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$fw   = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc  = Join-Path $fw 'csc.exe'
$wpf  = Join-Path $fw 'WPF'
$src  = Join-Path $root 'src'
$out  = Join-Path $root 'dist'
$obj  = Join-Path $root 'obj'

if (-not (Test-Path $csc)) { throw "C# compiler not found at $csc (.NET Framework 4.x is required)" }
New-Item -ItemType Directory -Force -Path $out, $obj | Out-Null

$refs = @(
    "$wpf\PresentationFramework.dll", "$wpf\PresentationCore.dll", "$wpf\WindowsBase.dll",
    "$fw\System.Xaml.dll", "$fw\System.Windows.Forms.dll", "$fw\System.Drawing.dll", "$fw\System.dll"
) | ForEach-Object { "/r:$_" }

# 1) Generate the .exe icon from the same drawing code the app uses for its tray icon.
$ico = Join-Path $obj 'WorldClock.ico'
& $csc /nologo /target:library /codepage:65001 "/out:$obj\IconGen.dll" /r:"$fw\System.Drawing.dll" "$src\IconArt.cs"
if ($LASTEXITCODE -ne 0) { throw 'Icon generator build failed' }
$gen = @"
`$asm = [Reflection.Assembly]::LoadFile('$obj\IconGen.dll')
`$asm.GetType('WorldClock.IconArt').GetMethod('WriteIco').Invoke(`$null, @('$ico'))
"@
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command $gen
if (-not (Test-Path $ico)) { throw 'Icon generation failed' }

# 2) Compile the app.
$files = Get-ChildItem "$src\*.cs" | ForEach-Object { $_.FullName }
& $csc /nologo /target:winexe /optimize+ /codepage:65001 /platform:anycpu "/out:$out\WorldClock.exe" `
    "/win32icon:$ico" "/win32manifest:$root\app.manifest" $refs $files
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Write-Host "Built $out\WorldClock.exe" -ForegroundColor Green

# 3) Optional unit tests (pure logic: offsets, day labels, DST, search, settings).
if ($Test) {
    $testSrc = @("$src\TimeFormat.cs", "$src\Zones.cs", "$src\Settings.cs", "$root\tests\Tests.cs")
    & $csc /nologo /target:exe /codepage:65001 "/out:$obj\Tests.exe" /r:"$fw\System.dll" $testSrc
    if ($LASTEXITCODE -ne 0) { throw 'Test build failed' }
    & "$obj\Tests.exe"
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
}
