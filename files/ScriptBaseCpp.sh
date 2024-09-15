#!/bin/sh

SCRIPT_PATH="$(dirname "$(realpath "$0")")"
SCRIPT_NAME="$(basename "$(realpath "$0")")"
SCRIPT_BASE="$(basename -s .sh "$SCRIPT_NAME")"
OS_NAME="$(uname -s)"

echo "SCRIPT_PATH: $SCRIPT_PATH"
echo "SCRIPT_NAME: $SCRIPT_NAME"
echo "SCRIPT_BASE: $SCRIPT_BASE"
echo "OSTYPE: $OS_NAME"

if [ "$OS_NAME" = "Darwin" ]; then
    EXE_NAME="open $SCRIPT_PATH/$SCRIPT_BASE.app --args"
else
    EXE_NAME="$SCRIPT_PATH/$(echo "$SCRIPT_BASE" | tr '[:upper:]' '[:lower:]')"
fi

echo "EXE_NAME: $EXE_NAME"
echo "ARGUMENTS: $*"

exec "$EXE_NAME" "$*"
