$ErrorActionPreference = 'Stop'

$env:RimWorldVersion = $args[0]
$Configuration = 'Debug'

$VersionTargetPrefix = "D:\RimWorld"
$VersionTargetSuffix = "Mods\FarmingHysteresis"
$Target = "$VersionTargetPrefix\$env:RimWorldVersion\$VersionTargetSuffix"

$env:RimWorldSteamWorkshopFolderPath = "..\.deps\refs"
#$env:RimWorldSteamWorkshopFolderPath = "C:\Program Files (x86)\Steam\steamapps\workshop\content\294100"

# build dlls
dotnet build --configuration $Configuration .vscode/mod.csproj
if ($LASTEXITCODE -gt 0) {
    throw "Build failed"
}
dotnet build --configuration $Configuration .vscode/vanillaplantsexpandedmoreplants.interop.csproj
if ($LASTEXITCODE -gt 0) {
    throw "Build failed"
}

# remove pdbs (for release)
if ($Configuration -eq "Release") {
    Remove-Item -Path .\$env:RimWorldVersion\Assemblies\FarmingHysteresis.pdb -ErrorAction SilentlyContinue

    Remove-Item -Path .\$($env:RimWorldVersion)_VanillaPlantsExpandedMorePlants\Assemblies\FarmingHysteresis.VanillaPlantsExpandedMorePlants.pdb -ErrorAction SilentlyContinue
}

# remove mod folder
Remove-Item -Path $Target -Recurse -ErrorAction SilentlyContinue

# copy mod files
Copy-Item -Path $env:RimWorldVersion $Target\$env:RimWorldVersion -Recurse

Copy-Item -Path "$($env:RimWorldVersion)_VanillaPlantsExpandedMorePlants" "$Target\$($env:RimWorldVersion)_VanillaPlantsExpandedMorePlants" -Recurse

Copy-Item -Path Common $Target\Common -Recurse
Copy-Item -Path About $Target\About -Recurse -Exclude *.pdn

Copy-Item -Path CHANGELOG.md $Target
Copy-Item -Path LICENSE $Target
Copy-Item -Path LICENSE.Apache-2.0 $Target
Copy-Item -Path LICENSE.MIT $Target
Copy-Item -Path README.md $Target
Copy-Item -Path LoadFolders.xml $Target
