@echo off

SETLOCAL ENABLEEXTENSIONS

SET TARGET="C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\FarmingHysteresis"

REM build dll
dotnet build .vscode

REM remove mod folder
RMDIR /S /Q %TARGET%

REM make mod folders
MKDIR %TARGET%\1.2\Assemblies
MKDIR %TARGET%\1.2\Languages\English\Keyed
MKDIR %TARGET%\1.2\Languages\Japanese\Keyed
MKDIR %TARGET%\1.2\Patches
MKDIR %TARGET%\About

REM copy mod files
COPY 1.2\Assemblies\FarmingHysteresis.dll %TARGET%\1.2\Assemblies
COPY 1.2\Languages\English\Data.xml %TARGET%\1.2\Languages\English
COPY 1.2\Languages\English\Keyed\FarmingHysteresis.xml %TARGET%\1.2\Languages\English\Keyed
COPY 1.2\Languages\Japanese\Keyed\FarmingHysteresis.xml %TARGET%\1.2\Languages\Japanese\Keyed
COPY 1.2\Patches\Patches.xml %TARGET%\1.2\Patches
COPY About\About.xml %TARGET%\About
COPY About\Manifest.xml %TARGET%\About
COPY About\Preview.png %TARGET%\About
COPY About\PublishedFileId.txt %TARGET%\About
COPY CHANGELOG.md %TARGET%
COPY LICENSE %TARGET%
COPY LICENSE.Apache-2.0 %TARGET%
COPY LICENSE.MIT %TARGET%
COPY README.md %TARGET%

ENDLOCAL