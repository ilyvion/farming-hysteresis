#!/usr/bin/env bash
set -e

CONFIGURATION="Debug"
TARGET="$HOME/.var/app/com.valvesoftware.Steam/.local/share/Steam/steamapps/common/RimWorld/Mods/FarmingHysteresis"

export RimWorldSteamWorkshopFolderPath="../../.deps/refs"

mkdir -p .savedatafolder/1.6
mkdir -p .savedatafolder/1.5
mkdir -p .savedatafolder/1.4

# build dlls
export RimWorldVersion="1.3"
dotnet build --configuration "$CONFIGURATION" FarmingHysteresis.sln
export RimWorldVersion="1.4"
dotnet build --configuration "$CONFIGURATION" FarmingHysteresis.sln
export RimWorldVersion="1.5"
dotnet build --configuration "$CONFIGURATION" FarmingHysteresis.sln
export RimWorldVersion="1.6"
#dotnet build --configuration "$CONFIGURATION" FarmingHysteresis.sln
dotnet build --configuration "$CONFIGURATION" Source/FarmingHysteresis/FarmingHysteresis.csproj

# remove mod folder
rm -rf "$TARGET"

# copy mod files
mkdir -p "$TARGET"
cp -r 1.3 "$TARGET/1.3"
cp -r 1.4 "$TARGET/1.4"
cp -r 1.5 "$TARGET/1.5"
cp -r 1.6 "$TARGET/1.6"

# copy interop mod files
cp -r "1.3_VanillaPlantsExpandedMorePlants" "$TARGET/1.3_VanillaPlantsExpandedMorePlants"
cp -r "1.4_VanillaPlantsExpandedMorePlants" "$TARGET/1.4_VanillaPlantsExpandedMorePlants"
cp -r "1.5_VanillaPlantsExpandedMorePlants" "$TARGET/1.5_VanillaPlantsExpandedMorePlants"
# cp -r "1.6_VanillaPlantsExpandedMorePlants" "$TARGET/1.6_VanillaPlantsExpandedMorePlants"
cp -r Common "$TARGET/Common"

rsync -av --exclude='*.pdn' --exclude='*.svg' --exclude='*.ttf' About "$TARGET/"

cp CHANGELOG.md "$TARGET/"
cp LICENSE "$TARGET/"
cp LICENSE.Apache-2.0 "$TARGET/"
cp LICENSE.MIT "$TARGET/"
cp README.md "$TARGET/"
cp LoadFolders.xml "$TARGET/"

# Trigger auto-hotswap
mkdir -p "$TARGET/1.3/Assemblies"
touch "$TARGET/1.3/Assemblies/FarmingHysteresis.dll.hotswap"
mkdir -p "$TARGET/1.4/Assemblies"
touch "$TARGET/1.4/Assemblies/FarmingHysteresis.dll.hotswap"
mkdir -p "$TARGET/1.5/Assemblies"
touch "$TARGET/1.5/Assemblies/FarmingHysteresis.dll.hotswap"
mkdir -p "$TARGET/1.6/Assemblies"
touch "$TARGET/1.6/Assemblies/FarmingHysteresis.dll.hotswap"
