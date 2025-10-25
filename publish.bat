@echo off
setlocal enabledelayedexpansion

echo 🚀 Starting build for Duckov++ Mod...

rem --- Configuration ---
set "MOD_NAME=DailyInterest"
set "PROJECT_FILE_PATH=%MOD_NAME%.csproj"
set "BUILD_SOURCE_DIR=bin\Release\netstandard2.1\publish"
set "OUTPUT_DIR=Output"

rem --- 1. Clean previous build ---
echo 🧹 Cleaning old output directory...
if exist "%OUTPUT_DIR%" (
    rmdir /s /q "%OUTPUT_DIR%"
)
mkdir "%OUTPUT_DIR%"

rem --- 2. Build the project ---
echo 🔨 Building the project in Release mode...
dotnet publish "%PROJECT_FILE_PATH%" -c Release

rem --- 3. Create packaging structure ---
echo 📁 Creating packaging directory...
set "MOD_RELEASE_DIR=%OUTPUT_DIR%\%MOD_NAME%"
mkdir "%MOD_RELEASE_DIR%"

rem --- 4. Copy necessary files ---
echo 📋 Copying files to release directory...

rem Copy the main mod DLL
copy "%BUILD_SOURCE_DIR%\%MOD_NAME%.dll" "%MOD_RELEASE_DIR%\"
echo   ✓ %MOD_NAME%.dll

rem Copy info.ini
if exist "info.ini" (
    copy "info.ini" "%MOD_RELEASE_DIR%\"
    echo   ✓ info.ini
) else (
    echo   ⚠️ WARNING: info.ini not found.
)

rem Copy preview.png if it exists
if exist "preview.png" (
    copy "preview.png" "%MOD_RELEASE_DIR%\"
    echo   ✓ preview.png
) else (
    echo   ⚠️ NOTE: preview.png not found. You can add one to the '%MOD_NAME%' folder.
)

rem Copy MathNet.Numerics
if exist "MathNet.Numerics.dll" (
    copy "MathNet.Numerics.dll" "%MOD_RELEASE_DIR%\"
    echo   ✓ preview.png
) else (
    echo   ⚠️ NOTE: MathNet.Numerics.dll not found. You can add one to the '%MOD_NAME%' folder.
)

rem Copy Localization files
if exist "Localization" (
    mkdir "%MOD_RELEASE_DIR%\Localization"
    echo   - Copying localization files...
    for %%L in (ChineseSimplified ChineseTraditional English French German Japanese Korean Portuguese Russian Spanish) do (
        if exist "Localization\%%L.tsv" (
            copy "Localization\%%L.tsv" "%MOD_RELEASE_DIR%\Localization\" /Y > nul
            echo     ✓ %%L.tsv
        )
    )
) else (
    echo   ⚠️ WARNING: Localization directory not found.
)

rem --- 5. Final verification ---
echo.
echo 📦 Release folder content:
dir "%MOD_RELEASE_DIR%"
echo.
echo 🎯 Build complete! To install the mod:
echo 1. Copy the '%MOD_RELEASE_DIR%' folder.
echo 2. Paste it into your game's mod directory.
echo    (e.g., ^<Steam_Path^>/common/Escape from Duckov/Duckov.app/Contents/Mods/)
echo 3. Launch the game and enable '%MOD_NAME%' in the Mods menu.

endlocal
