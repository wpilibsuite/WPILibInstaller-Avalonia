#!/bin/sh

SCRIPT_PATH="$(dirname "$(realpath "$0")")"
SCRIPT_NAME="$(basename "$(realpath "$0")")"
SCRIPT_BASE="$(basename -s .sh "$SCRIPT_NAME")"
OS_NAME="$(uname -s)"
CHOREO_PATH="$(realpath "$SCRIPT_PATH/../choreo")"

if [ "$OS_NAME" = "Linux" ]; then
    exec "$CHOREO_PATH/Choreo"
elif [ "$OS_NAME" = "Darwin" ]; then
    open "$CHOREO_PATH/Choreo"
else
    exec "$CHOREO_PATH/Choreo"
fi
