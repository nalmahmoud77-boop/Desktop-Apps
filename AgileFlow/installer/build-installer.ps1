<#
  Builds AgileFlow and packages it into a Windows installer.

  Usage:
      powershell -ExecutionPolicy Bypass -File .\installer\build-installer.ps1
      # skip the (re)build and just repackage an existing dist\AgileFlow:
      powershell -ExecutionPolicy Bypass -File .\installer\build-installer.ps1 -SkipPublish

  Output:
      dist\AgileFlow-Setup-<version>.exe   (the installer to send to customers)

  Requires Inno Setup 6 (ISCC.exe). Install once with:
      winget install -e --id JRSoftware.InnoSetup
#>

param(
    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'
$installerDir = $PSScriptRoot
$root = Split-Path $installerDir -Parent
$iss  = Join-Path $installerDir 'AgileFlow.iss'
$exe  = Join-Path $root 'dist\AgileFlow\AgileFlow.exe'

# 1. Build the self-contained exe (unless told to skip).
if (-not $SkipPublish) {
    Write-Host "Publishing AgileFlow..." -ForegroundColor Cyan
    & (Join-Path $root 'publish.ps1')
    if ($LASTEXITCODE -ne 0) { throw "publish.ps1 failed." }
}

if (-not (Test-Path $exe)) {
    throw "Expected $exe but it does not exist. Run without -SkipPublish first."
}

# 2. Locate the Inno Setup compiler (ISCC.exe).
$iscc = (Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue).Source
if (-not $iscc) {
    foreach ($p in @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
    )) {
        if (Test-Path $p) { $iscc = $p; break }
    }
}
if (-not $iscc) {
    throw "ISCC.exe (Inno Setup 6) not found. Install it with:`n" +
          "    winget install -e --id JRSoftware.InnoSetup"
}

# 3. Compile the installer.
Write-Host "Compiling installer with $iscc..." -ForegroundColor Cyan
& $iscc $iss
if ($LASTEXITCODE -ne 0) { throw "ISCC failed with exit code $LASTEXITCODE." }

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Get-ChildItem (Join-Path $root 'dist\AgileFlow-Setup-*.exe') |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 |
    ForEach-Object { Write-Host ("  Installer : {0}  ({1:N1} MB)" -f $_.FullName, ($_.Length / 1MB)) }
