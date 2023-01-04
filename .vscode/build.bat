@echo off

SETLOCAL ENABLEEXTENSIONS

SET TARGET="C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\FarmingHysteresis"

REM build dll
dotnet build .vscode

REM remove mod folder
RMDIR /S /Q %TARGET%

REM make mod folders
MKDIR %TARGET%\1.4\Assemblies
MKDIR %TARGET%\1.4\Defs
MKDIR %TARGET%\1.4\Defs\MainTabDefs

MKDIR %TARGET%\1.4\Languages\English\Keyed
MKDIR %TARGET%\1.4\Languages\English\DefInjected\MainButtonDef

MKDIR %TARGET%\1.4\Languages\Japanese\Keyed

MKDIR %TARGET%\1.4\Languages\Norwegian\Keyed
MKDIR %TARGET%\1.4\Languages\Norwegian\DefInjected\MainButtonDef

MKDIR %TARGET%\1.4\Patches
MKDIR %TARGET%\About

REM copy mod files
COPY 1.4\Assemblies\FarmingHysteresis.dll %TARGET%\1.4\Assemblies
COPY 1.4\Defs\MainTabDefs\MainTabWindow_Hysteresis.xml %TARGET%\1.4\Defs\MainTabDefs

COPY 1.4\Languages\English\Data.xml %TARGET%\1.4\Languages\English
COPY 1.4\Languages\English\Keyed\FarmingHysteresis.xml %TARGET%\1.4\Languages\English\Keyed
COPY 1.4\Languages\English\DefInjected\MainButtonDef\MainButtons.xml %TARGET%\1.4\Languages\English\DefInjected\MainButtonDef

COPY 1.4\Languages\Japanese\Keyed\FarmingHysteresis.xml %TARGET%\1.4\Languages\Japanese\Keyed

COPY 1.4\Languages\Norwegian\Keyed\FarmingHysteresis.xml %TARGET%\1.4\Languages\Norwegian\Keyed
COPY 1.4\Languages\Norwegian\DefInjected\MainButtonDef\MainButtons.xml %TARGET%\1.4\Languages\Norwegian\DefInjected\MainButtonDef

COPY 1.4\Patches\Patches.xml %TARGET%\1.4\Patches
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
