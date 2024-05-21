$ErrorActionPreference = 'Stop'

$env:RimWorldVersion = $args[0]
$Configuration = 'Release'

$VersionTargetPrefix = "D:\RimWorld"
$VersionTargetSuffix = "Mods\FarmingHysteresis"
#$MainTarget = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\FarmingHysteresis"
$MainTarget = "$VersionTargetPrefix\$env:RimWorldVersion\$VersionTargetSuffix"

#Write-Output $MainTarget
#return

# $env:RimWorldSteamWorkshopFolderPath = "..\.deps\refs"
$env:RimWorldSteamWorkshopFolderPath = "C:\Program Files (x86)\Steam\steamapps\workshop\content\294100"

# build dlls
$env:RimWorldVersion = $args[0] # "1.3"
dotnet build --configuration $Configuration .vscode/mod.csproj
if ($LASTEXITCODE -gt 0) {
    throw "Build failed"
}
# Start-Sleep -Seconds 0.5
# $env:RimWorldVersion = "1.4"
# dotnet build --configuration $Configuration .vscode/mod.csproj
# if ($LASTEXITCODE -gt 0) {
#     throw "Build failed"
# }
# Start-Sleep -Seconds 0.5
# $env:RimWorldVersion = "1.5"
# dotnet build --configuration $Configuration .vscode/mod.csproj
# if ($LASTEXITCODE -gt 0) {
#     throw "Build failed"
# }

# remove pdbs (for release)
if ($Configuration -eq "Release") {
    Remove-Item -Path .\1.5\Assemblies\FarmingHysteresis.pdb -ErrorAction SilentlyContinue
    Remove-Item -Path .\1.4\Assemblies\FarmingHysteresis.pdb -ErrorAction SilentlyContinue
}

# remove mod folder
Remove-Item -Path $MainTarget -Recurse -ErrorAction SilentlyContinue

# copy mod files
Copy-Item -Path 1.3 $MainTarget\1.3 -Recurse
Copy-Item -Path 1.4 $MainTarget\1.4 -Recurse
Copy-Item -Path 1.5 $MainTarget\1.5 -Recurse

Copy-Item -Path Common $MainTarget\Common -Recurse
Copy-Item -Path About $MainTarget\About -Recurse -Exclude *.pdn

Copy-Item -Path CHANGELOG.md $MainTarget
Copy-Item -Path LICENSE $MainTarget
Copy-Item -Path LICENSE.Apache-2.0 $MainTarget
Copy-Item -Path LICENSE.MIT $MainTarget
Copy-Item -Path README.md $MainTarget
