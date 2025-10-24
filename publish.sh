#!/bin/bash
# Exit immediately if a command exits with a non-zero status.
set -e

echo "🚀 Starting build for Duckov++ Mod..."

# --- Configuration ---
MOD_NAME="DailyInterest"
PROJECT_FILE_PATH="$MOD_NAME.csproj"
BUILD_SOURCE_DIR="bin/Release/netstandard2.1/publish"
OUTPUT_DIR="Output"

# --- 1. Clean previous build ---
echo "🧹 Cleaning old output directory..."
if [ -d "$OUTPUT_DIR" ]; then
    rm -rf "$OUTPUT_DIR"
fi
mkdir "$OUTPUT_DIR"

# --- 2. Build the project ---
echo "🔨 Building the project in Release mode..."
dotnet publish "$PROJECT_FILE_PATH" -c Release

# --- 3. Create packaging structure ---
echo "📁 Creating packaging directory..."
MOD_RELEASE_DIR="$OUTPUT_DIR/$MOD_NAME"
mkdir -p "$MOD_RELEASE_DIR"

# --- 4. Copy necessary files ---
echo "📋 Copying files to release directory..."

# Copy the main mod DLL
cp "$BUILD_SOURCE_DIR/$MOD_NAME.dll" "$MOD_RELEASE_DIR/"
echo "  ✓ $MOD_NAME.dll"

# Copy info.ini
if [ -f "info.ini" ]; then
    cp "info.ini" "$MOD_RELEASE_DIR/"
    echo "  ✓ info.ini"
else
    echo "  ⚠️ WARNING: info.ini not found."
fi

# Copy preview.png if it exists
if [ -f "preview.png" ]; then
    cp "preview.png" "$MOD_RELEASE_DIR/"
    echo "  ✓ preview.png"
else
    echo "  ⚠️ NOTE: preview.png not found. You can add one to the '$MOD_NAME' folder."
fi

# --- 5. Final verification ---
echo ""
echo "📦 Release folder content:"
ls -l "$MOD_RELEASE_DIR"
echo ""
echo "🎯 Build complete! To install the mod:"
echo "1. Copy the '$MOD_RELEASE_DIR' folder."
echo "2. Paste it into your game's mod directory."
echo "   (e.g., <Steam_Path>/common/Escape from Duckov/Duckov.app/Contents/Mods/)"
echo "3. Launch the game and enable '$MOD_NAME' in the Mods menu."