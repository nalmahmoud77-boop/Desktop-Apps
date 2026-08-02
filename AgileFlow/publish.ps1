<#
  Builds AgileFlow for production and zips it for distribution.

  Usage:
      powershell -ExecutionPolicy Bypass -File .\publish.ps1

  Output:
      dist\AgileFlow\AgileFlow.exe        (self-contained single file)
      dist\AgileFlow-win-x64.zip          (ready to send)
#>

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$proj = Join-Path $root 'PersonalTaskManagement\PersonalTaskManagement.csproj'
$out  = Join-Path $root 'dist\AgileFlow'
$zip  = Join-Path $root 'dist\AgileFlow-win-x64.zip'

Write-Host "Cleaning previous output..." -ForegroundColor Cyan
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }

Write-Host "Publishing (self-contained single-file, win-x64)..." -ForegroundColor Cyan
dotnet publish $proj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $out
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

Write-Host "Zipping..." -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip

$exe = Join-Path $out 'AgileFlow.exe'
$sizeMb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host ("  EXE : {0}  ({1} MB)" -f $exe, $sizeMb)
Write-Host ("  ZIP : {0}" -f $zip)
