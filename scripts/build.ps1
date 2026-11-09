$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    dotnet build UniversalMouseStudio.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Compilação falhou.' }
    dotnet run --project tests/UniversalMouse.Tests -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Testes de movimento falharam.' }
    dotnet run --project tests/UniversalMouse.ProfileTests -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Testes de perfis falharam.' }
    if ($env:OS -eq 'Windows_NT') {
        dotnet run --project tests/UniversalMouse.WpfTests -c Release --no-build
        if ($LASTEXITCODE -ne 0) { throw 'Testes do laboratório WPF falharam.' }
    }
} finally { Pop-Location }
