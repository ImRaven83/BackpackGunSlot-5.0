# Builds the client plugin and the server mod and zips them in the SPT install layout.
#   ./build-release.ps1 -SptRoot "D:\SPT 5.0"
# The client build needs BepInEx\interop, which BepInEx generates the first time the game starts.
param(
    [Parameter(Mandatory = $true)][string]$SptRoot,
    [string]$SptServerDir = (Join-Path $SptRoot "SPT_Runtime")
)
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

[xml]$project = Get-Content "BackpackGunSlot.csproj"
$version = ($project.Project.PropertyGroup | Where-Object { $_.AssemblyVersion } | Select-Object -First 1).AssemblyVersion

$root = $SptRoot.TrimEnd('\', '/') + '\'
$server = $SptServerDir.TrimEnd('\', '/') + '\'
dotnet build "BackpackGunSlot.csproj" -c Release "-p:TarkovDir=$root"
if ($LASTEXITCODE -ne 0) { throw "Client build failed" }
dotnet build "server\BackpackGunSlot.Server.csproj" -c Release "-p:SptServerDir=$server"
if ($LASTEXITCODE -ne 0) { throw "Server build failed" }

$stage = "release\stage"
Remove-Item "release" -Recurse -Force -ErrorAction SilentlyContinue
$plugin = New-Item -ItemType Directory "$stage\BepInEx\plugins\BackpackGunSlot"
$mod = New-Item -ItemType Directory "$stage\SPT_Runtime\user\mods\BackpackGunSlot\config"
Copy-Item "bin\Release\net10.0\BackpackGunSlot.dll" $plugin
Copy-Item "server\bin\Release\net10.0\BackpackGunSlot.dll" $mod.Parent.FullName
Copy-Item "server\config\config.json" $mod
Copy-Item "LICENSE", "README.md" "$stage\BepInEx\plugins\BackpackGunSlot"

$zip = "release\BackpackGunSlot-$version-SPT5.0.zip"
Compress-Archive -Path "$stage\*" -DestinationPath $zip
Write-Host "Created $zip"
