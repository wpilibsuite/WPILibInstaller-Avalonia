#!/bin/sh

SCRIPT_PATH="$(dirname "$(realpath -s "$0")")"
SCRIPT_NAME="$(basename "$(realpath -s "$0")")"
SCRIPT_BASE="$(basename -s .sh "$SCRIPT_NAME")"
OS_NAME="$(uname -s)"

echo "SCRIPT_PATH: $SCRIPT_PATH"
echo "SCRIPT_NAME: $SCRIPT_NAME"
echo "SCRIPT_BASE: $SCRIPT_BASE"
echo "OSTYPE: $OS_NAME"

if [ "$OS_NAME" = "Linux" ]; then
    EXE_NAME="$SCRIPT_BASE"
elif [ "$OS_NAME" = "Darwin" ]; then
    EXE_NAME="open $SCRIPT_BASE.app --args"
else
    EXE_NAME="$SCRIPT_BASE"
fi

EXE_NAME="$(echo "$EXE_NAME" | tr '[:upper:]' '[:lower:]')"

echo "EXE_NAME: $EXE_NAME"
echo "ARGUMENTS: $*"

exec "$SCRIPT_PATH/$EXE_NAME" "$*"
