#!/bin/sh

SCRIPT_PATH="$(dirname "$(realpath "$0")")"
SCRIPT_NAME="$(basename "$(realpath "$0")")"
SCRIPT_BASE="$(basename -s .sh "$SCRIPT_NAME")"
OS_NAME="$(uname -s)"

if [ "$OS_NAME" = "Darwin" ]; then
    open "$SCRIPT_PATH/$SCRIPT_BASE.app" --args
else
    exec "$SCRIPT_PATH/$(echo "$SCRIPT_BASE" | tr '[:upper:]' '[:lower:]')" "$*"
fi

