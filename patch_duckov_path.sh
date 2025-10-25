#!/bin/bash

CSPROJ_FILE="DailyInterest.csproj"

# Determine the operating system
OS="$(uname -s)"

# Define DuckovPath based on OS
DUCKOV_PATH=""
if [[ "$OS" == "Darwin" ]]; then
    # macOS path
    DUCKOV_PATH="/Users/waterlens/Library/Application Support/Steam/steamapps/common/Escape from Duckov/Duckov.app/Contents/Resources/Data"
elif [[ "$OS" == "MINGW"* || "$OS" == "CYGWIN"* || "$OS" == "MSYS"* ]]; then
    # Windows path (assuming Git Bash or similar environment)
    DUCKOV_PATH="C:\\Program Files (x86)\\Steam\\steamapps\\common\\Escape from Duckov\\Duckov_Data"
else
    echo "Unsupported operating system: $OS"
    exit 1
fi

echo "Detected OS: $OS"
echo "Setting DuckovPath to: $DUCKOV_PATH"

# Escape backslashes for sed on Windows if necessary
if [[ "$OS" == "MINGW"* || "$OS" == "CYGWIN"* || "$OS" == "MSYS"* ]]; then
    DUCKOV_PATH_SED=$(echo "$DUCKOV_PATH" | sed 's/\\/\\\\/g')
else
    DUCKOV_PATH_SED="$DUCKOV_PATH"
fi

# Use sed to update the DuckovPath in the .csproj file
# The pattern looks for <DuckovPath>...</DuckovPath>
sed -i '' "s|<DuckovPath>.*</DuckovPath>|<DuckovPath>$DUCKOV_PATH_SED</DuckovPath>|g" "$CSPROJ_FILE"

if [ $? -eq 0 ]; then
    echo "Successfully patched DuckovPath in $CSPROJ_FILE"
else
    echo "Failed to patch DuckovPath in $CSPROJ_FILE"
    exit 1
fi
