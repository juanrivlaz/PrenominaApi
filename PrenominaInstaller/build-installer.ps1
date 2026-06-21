# =============================================================================
#  build-installer.ps1
#  Genera el instalador autocontenido de Prenomina (API + Frontend) en un paso.
#
#  1. Publica el API (self-contained)              -> .\api
#  2. Publica el servidor estático PrenominaWeb    -> .\web
#  3. Compila el frontend Angular (ng build)       -> .\webroot
#  4. Publica el instalador WPF como .exe único    -> .\dist
#
#  Ejecutar en Windows (requiere SDK .NET 8 y Node.js para el frontend):
#     powershell -ExecutionPolicy Bypass -File .\build-installer.ps1
#
#  Parámetros:
#     -SkipWeb        Omite el frontend (solo API + instalador)
#     -FrontendDir    Ruta del proyecto Angular (carpeta con package.json)
# =============================================================================

param(
    [switch]$SkipWeb,
    [string]$FrontendDir = "C:\Develop\Prenomina-front"   # ajustar a la ruta real en Windows
)

$ErrorActionPreference = "Stop"

$here       = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiProject = Join-Path $here "..\PrenominaApi\PrenominaApi.csproj"
$webProject = Join-Path $here "..\PrenominaWeb\PrenominaWeb.csproj"
$installerProject = Join-Path $here "PrenominaInstaller.csproj"
$frontendDir = $FrontendDir

$apiOutput  = Join-Path $here "api"
$webOutput  = Join-Path $here "web"
$webRootOut = Join-Path $here "webroot"
$distOutput = Join-Path $here "dist"

Write-Host "==> 1/4  Publicando el API (self-contained)..." -ForegroundColor Cyan
if (Test-Path $apiOutput) { Remove-Item $apiOutput -Recurse -Force }
dotnet publish $apiProject -c Release -r win-x64 --self-contained true -o $apiOutput

if (-not $SkipWeb) {
    Write-Host "==> 2/4  Publicando el servidor estático del frontend..." -ForegroundColor Cyan
    if (Test-Path $webOutput) { Remove-Item $webOutput -Recurse -Force }
    dotnet publish $webProject -c Release -r win-x64 --self-contained true -o $webOutput

    Write-Host "==> 3/4  Compilando el frontend Angular (ng build)..." -ForegroundColor Cyan
    Push-Location $frontendDir
    try {
        if (-not (Test-Path (Join-Path $frontendDir "node_modules"))) {
            Write-Host "    Instalando dependencias (npm ci)..." -ForegroundColor DarkGray
            npm ci
        }
        npm run build
    } finally {
        Pop-Location
    }

    if (Test-Path $webRootOut) { Remove-Item $webRootOut -Recurse -Force }
    $ngDist = Join-Path $frontendDir "dist\prenomina\browser"
    if (-not (Test-Path $ngDist)) {
        throw "No se encontró el build de Angular en '$ngDist'. Revise el resultado de 'ng build'."
    }
    Copy-Item $ngDist $webRootOut -Recurse -Force
} else {
    Write-Host "==> 2-3/4  Frontend omitido (-SkipWeb)." -ForegroundColor Yellow
}

Write-Host "==> 4/4  Publicando el instalador (.exe único)..." -ForegroundColor Cyan
dotnet publish $installerProject -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $distOutput

Write-Host ""
Write-Host "Listo. Instalador generado en:" -ForegroundColor Green
Write-Host "    $distOutput\PrenominaInstaller.exe"
Write-Host ""
Write-Host "Copie la carpeta 'dist' al servidor y ejecute PrenominaInstaller.exe como administrador." -ForegroundColor Yellow
