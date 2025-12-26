#Si el servidor tiene proteccion de script ejecutar este comando
#Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned -Force
#powershell -ExecutionPolicy Bypass -File .\PublishIIS.ps1

#instalar dotnet-hosting-9.0.6-win
#reiniciar iis
#iisreset

# Variables de configuración
$sourcePath = "."
$publishPath = "C:\inetpub\wwwroot\PrenominaApi"
$siteName = "PrenominaApi"
$port = 5000
$appPoolName = "PrenominaApiAppPool"
$runtime = "win-x64"

#Set Env
[System.Environment]::SetEnvironmentVariable("SERVER_DB", "DESKTOP-5S0U1TN", "Machine")
[System.Environment]::SetEnvironmentVariable("NAME_APSI_DB", "apsisistemas", "Machine")
[System.Environment]::SetEnvironmentVariable("NAME_PRENOMINA_DB", "PrenominaApi", "Machine")
[System.Environment]::SetEnvironmentVariable("USER_DB", "sa", "Machine")
[System.Environment]::SetEnvironmentVariable("PASSWORD_DB", "desarrollo", "Machine")

# Publicar proyecto
Write-Host "Preparando carpeta de publicación..."
if (!(Test-Path $publishPath)) {
    New-Item -Path $publishPath -ItemType Directory
}

Write-Host "Copiando archivos desde $sourcePath..."
Remove-Item "$publishPath\*" -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item "$sourcePath\*" $publishPath -Recurse -Force

# Importar módulo WebAdministration si no está
Import-Module WebAdministration

# Crear Application Pool
Write-Host "Creando Application Pool..."
if (-not (Test-Path IIS:\AppPools\$appPoolName)) {
    Write-Host "Creando Application Pool $appPoolName"
    New-WebAppPool -Name $appPoolName
} else {
    Write-Host "El Application Pool $appPoolName ya existe"
}
Set-ItemProperty IIS:\AppPools\$appPoolName -Name managedRuntimeVersion -Value ""

# Crear el sitio web en IIS
Write-Host "Creando el sitio en IIS..."
if (!(Test-Path "IIS:\Sites\$siteName")) {
    New-Item "IIS:\Sites\$siteName" -bindings @{protocol="http";bindingInformation="*:${port}:"} -physicalPath $publishPath
    Set-ItemProperty "IIS:\Sites\$siteName" -Name applicationPool -Value $appPoolName
} else {
    Write-Host "El sitio '$siteName' ya existe, actualizando ruta física y pool..."
    Set-ItemProperty "IIS:\Sites\$siteName" -Name physicalPath -Value $publishPath
    Set-ItemProperty "IIS:\Sites\$siteName" -Name applicationPool -Value $appPoolName
}

# Permisos de carpeta
Write-Host "Asignando permisos de lectura a IIS_IUSRS..."
$acl = Get-Acl $publishPath
$permission = "IIS_IUSRS","ReadAndExecute","ContainerInherit,ObjectInherit","None","Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl $publishPath $acl

# Reiniciar sitio
Restart-WebAppPool $appPoolName
Restart-WebItem "IIS:\Sites\$siteName"

New-NetFirewallRule -DisplayName "Abrir puerto 5000 TCP" `
                    -Direction Inbound `
                    -LocalPort 5000 `
                    -Protocol TCP `
                    -Action Allow `
                    -Profile Any

Write-Host "Despliegue completado. Accede a http://localhost:$port"