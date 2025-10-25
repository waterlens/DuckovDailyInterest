@echo off
setlocal enabledelayedexpansion

rem --- Configuration ---
rem !!! IMPORTANT !!!
rem !!! Replace this with the actual path to your game's mod directory !!!
set "GAME_MOD_DIR=C:\Program Files (x86)\Steam\steamapps\common\Escape from Duckov\Duckov_Data\Mods"
set "MOD_NAME=DailyInterest"
set "OUTPUT_DIR=Output"
set "MOD_RELEASE_DIR=%OUTPUT_DIR%\%MOD_NAME%"

rem --- 1. Publish the mod ---
echo 🚀 Publishing the mod...
call publish.bat

rem --- 2. Deploy the mod to the game directory ---
echo 🚚 Deploying the mod to the game directory...

rem Check if the game mod directory exists
if not exist "%GAME_MOD_DIR%" (
    echo ❌ ERROR: Game mod directory not found at '%GAME_MOD_DIR%'
    echo Please update the 'GAME_MOD_DIR' variable in this script.
    exit /b 1
)

rem Copy the mod directory
xcopy /s /e /y "%MOD_RELEASE_DIR%" "%GAME_MOD_DIR%\%MOD_NAME%\"

rem Create the NODEBUG_DAILY_INTEREST file to disable debug features
if /i not "%1" == "debug" (
    type nul > "%GAME_MOD_DIR%\%MOD_NAME%\NODEBUG_DAILY_INTEREST"
) else (
    echo 🐞 Debug mode enabled
)

echo ✅ Mod successfully deployed to '%GAME_MOD_DIR%'

endlocal
