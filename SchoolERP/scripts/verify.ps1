$ErrorActionPreference = 'Stop'
Write-Host 'For full persistence verification, set SCHOOLERP_TEST_USE_CONTAINERS=true (Docker) or SCHOOLERP_TEST_POSTGRES (native PostgreSQL).'
Push-Location "$PSScriptRoot/../backend"
try { dotnet restore; if ($LASTEXITCODE) { throw 'Restore failed' }; dotnet build --no-restore; if ($LASTEXITCODE) { throw 'Build failed' }; dotnet test --no-build; if ($LASTEXITCODE) { throw 'Tests failed' } } finally { Pop-Location }
Push-Location "$PSScriptRoot/../frontend"
try { npm ci; if ($LASTEXITCODE) { throw 'Install failed' }; npm run build; if ($LASTEXITCODE) { throw 'Frontend build failed' } } finally { Pop-Location }
