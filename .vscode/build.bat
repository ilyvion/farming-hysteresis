@echo off

SETLOCAL ENABLEEXTENSIONS

SET TARGET="C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\FarmingHysteresis"

REM build dll
dotnet build .vscode

REM remove mod folder
RMDIR /S /Q %TARGET%

REM make mod folders
MKDIR %TARGET%\1.4\Assemblies
MKDIR %TARGET%\Common\Defs
MKDIR %TARGET%\Common\Defs\MainTabDefs

MKDIR %TARGET%\Common\Languages\English\Keyed
MKDIR %TARGET%\Common\Languages\English\DefInjected\MainButtonDef

MKDIR %TARGET%\Common\Languages\Japanese\Keyed

MKDIR %TARGET%\Common\Languages\Norwegian\Keyed
MKDIR %TARGET%\Common\Languages\Norwegian\DefInjected\MainButtonDef

MKDIR %TARGET%\Common\Patches
MKDIR %TARGET%\About

REM copy mod files
COPY 1.4\Assemblies\FarmingHysteresis.dll %TARGET%\1.4\Assemblies
COPY Common\Defs\MainTabDefs\MainTabWindow_Hysteresis.xml %TARGET%\Common\Defs\MainTabDefs

COPY Common\Languages\English\Data.xml %TARGET%\Common\Languages\English
COPY Common\Languages\English\Keyed\FarmingHysteresis.xml %TARGET%\Common\Languages\English\Keyed
COPY Common\Languages\English\DefInjected\MainButtonDef\MainButtons.xml %TARGET%\Common\Languages\English\DefInjected\MainButtonDef

COPY Common\Languages\Japanese\Keyed\FarmingHysteresis.xml %TARGET%\Common\Languages\Japanese\Keyed

COPY Common\Languages\Norwegian\Keyed\FarmingHysteresis.xml %TARGET%\Common\Languages\Norwegian\Keyed
COPY Common\Languages\Norwegian\DefInjected\MainButtonDef\MainButtons.xml %TARGET%\Common\Languages\Norwegian\DefInjected\MainButtonDef

COPY Common\Patches\Patches.xml %TARGET%\Common\Patches
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
