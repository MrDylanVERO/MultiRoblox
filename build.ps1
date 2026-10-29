$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $csc)) { throw 'Compilatore C# non trovato.' }

$app = Join-Path $dist 'MultiRoblox-v3.2.5.exe'
& $csc /nologo /codepage:65001 /target:winexe /optimize+ `
    "/win32icon:$root\assets\MultiRoblox.ico" `
    "/resource:$root\assets\MultiRoblox.ico,icon.ico" `
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll `
    "/out:$app" "$root\src\MultiRoblox.cs" "$root\src\Comune.cs"
if ($LASTEXITCODE -ne 0) { throw 'Compilazione programma fallita.' }

$iscc = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
if (-not (Test-Path -LiteralPath $iscc)) { throw 'Inno Setup 6 non trovato.' }
& $iscc "$root\installer\MultiRoblox.iss"
if ($LASTEXITCODE -ne 0) { throw 'Compilazione installer fallita.' }
