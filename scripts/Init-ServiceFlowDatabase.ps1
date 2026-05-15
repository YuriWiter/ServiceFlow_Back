#requires -Version 5.1
<#
Creates the `serviceflow` login role and database expected by appsettings.json.
Run once against your local PostgreSQL install (must use a superuser such as postgres).

Non-interactive:
  $env:POSTGRES_SUPERUSER_PASSWORD = '<postgres user password>'
  .\scripts\Init-ServiceFlowDatabase.ps1

Interactive (prompts for password):
  .\scripts\Init-ServiceFlowDatabase.ps1
#>
param(
    [string]$PostgresHost = "127.0.0.1",
    [int]$PostgresPort = 5432,
    [string]$AdminUser = "postgres"
)

$ErrorActionPreference = "Stop"

function Resolve-PsqlPath {
    $roots = @(
        (Join-Path $env:ProgramFiles "PostgreSQL")
    )
    $pf86 = [Environment]::GetEnvironmentVariable("ProgramFiles(x86)")
    if ($pf86) { $roots += (Join-Path $pf86 "PostgreSQL") }

    $roots = $roots | Where-Object { $_ -and (Test-Path $_) }

    foreach ($root in $roots) {
        $candidate = Get-ChildItem -Path $root -Directory -ErrorAction SilentlyContinue |
            ForEach-Object { Join-Path $_.FullName "bin\psql.exe" } |
            Where-Object { Test-Path $_ } |
            Sort-Object { [version](Split-Path (Split-Path $_) -Leaf) } -Descending |
            Select-Object -First 1
        if ($candidate) { return $candidate }
    }

    throw "psql.exe not found under Program Files\PostgreSQL. Add the PostgreSQL bin folder to PATH or install PostgreSQL."
}

$psql = Resolve-PsqlPath

$adminPassword = $env:POSTGRES_SUPERUSER_PASSWORD
if (-not $adminPassword) {
    $secure = Read-Host -AsSecureString -Prompt "Password for PostgreSQL user '$AdminUser'"
    if ($secure.Length -eq 0) { throw "No password supplied." }
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { $adminPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
}

$env:PGPASSWORD = $adminPassword

$roleSql = @'
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'serviceflow') THEN
    CREATE ROLE serviceflow LOGIN PASSWORD 'serviceflow';
  ELSE
    ALTER ROLE serviceflow WITH LOGIN PASSWORD 'serviceflow';
  END IF;
END
$$;
'@

& $psql -h $PostgresHost -p $PostgresPort -U $AdminUser -d postgres -v ON_ERROR_STOP=1 -c $roleSql

$dbExists = (& $psql -h $PostgresHost -p $PostgresPort -U $AdminUser -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = 'serviceflow'").Trim()
if ($dbExists -ne "1") {
    & $psql -h $PostgresHost -p $PostgresPort -U $AdminUser -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE serviceflow OWNER serviceflow"
}

Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue

Write-Host "Database 'serviceflow' and role 'serviceflow' are ready (password: serviceflow)."
