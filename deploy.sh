#!/bin/bash
# Exit immediately if a command exits with a non-zero status.
set -e

# --- Configuration ---
# !!! IMPORTANT !!!
# !!! Replace this with the actual path to your game's mod directory !!!
GAME_MOD_DIR="$HOME/Library/Application Support/Steam/steamapps/common/Escape from Duckov/Duckov.app/Contents/Mods"
MOD_NAME="DailyInterest"
OUTPUT_DIR="Output"
MOD_RELEASE_DIR="$OUTPUT_DIR/$MOD_NAME"

# --- 1. Publish the mod ---
echo "🚀 Publishing the mod..."
./publish.sh

# --- 2. Deploy the mod to the game directory ---
echo "🚚 Deploying the mod to the game directory..."

# Check if the game mod directory exists
if [ ! -d "$GAME_MOD_DIR" ]; then
    echo "❌ ERROR: Game mod directory not found at '$GAME_MOD_DIR'"
    echo "Please update the 'GAME_MOD_DIR' variable in this script."
    exit 1
fi

# Copy the mod directory
cp -r "$MOD_RELEASE_DIR" "$GAME_MOD_DIR/"

echo "✅ Mod successfully deployed to '$GAME_MOD_DIR'"
