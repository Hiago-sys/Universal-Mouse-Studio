param([switch]$FrameworkDependent)
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    $standalone = if ($FrameworkDependent) { 'false' } else { 'true' }
    dotnet publish src/UniversalMouse.App/UniversalMouse.App.csproj -c Release -r win-x64 --self-contained $standalone -p:PublishReadyToRun=false -o artifacts/portable-win-x64
    if ($LASTEXITCODE -ne 0) { throw 'Publicação falhou.' }
    Copy-Item README.md artifacts/portable-win-x64/LEIA-ME.md -Force
    Copy-Item docs artifacts/portable-win-x64/docs -Recurse -Force
    Compress-Archive -Path artifacts/portable-win-x64/* -DestinationPath artifacts/UniversalMouseStudio-win-x64.zip -Force
} finally { Pop-Location }
