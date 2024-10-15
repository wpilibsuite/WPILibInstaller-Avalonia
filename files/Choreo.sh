#!/bin/sh

SCRIPT_PATH="$(dirname "$(realpath "$0")")"
SCRIPT_NAME="$(basename "$(realpath "$0")")"
SCRIPT_BASE="$(basename -s .sh "$SCRIPT_NAME")"
OS_NAME="$(uname -s)"
CHOREO_PATH="$(realpath "$SCRIPT_PATH/../choreo")"

if [ "$OS_NAME" = "Linux" ]; then
    exec "$CHOREO_PATH/Choreo-v2025.0.0-beta-4-Linux-x86_64"
elif [ "$OS_NAME" = "Darwin" ]; then
    open "$CHOREO_PATH/Choreo-v2025.0.0-beta-4-macOS-aarch64"
else
    exec "$CHOREO_PATH/Choreo-v2025.0.0-beta-4-macOS-x86_64"
fi
