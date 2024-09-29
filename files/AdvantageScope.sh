#!/bin/sh

SCRIPT_PATH="$(dirname "$(realpath "$0")")"
SCRIPT_NAME="$(basename "$(realpath "$0")")"
SCRIPT_BASE="$(basename -s .sh "$SCRIPT_NAME")"
OS_NAME="$(uname -s)"
AS_PATH="$(realpath "$SCRIPT_PATH/../advantagescope")"

echo "SCRIPT_PATH: $SCRIPT_PATH"
echo "SCRIPT_NAME: $SCRIPT_NAME"
echo "SCRIPT_BASE: $SCRIPT_BASE"
echo "AS_PATH: $AS_PATH"
echo "OSTYPE: $OS_NAME"

if [ "$OS_NAME" = "Linux" ]; then
    EXE_NAME="advantagescope-wpilib"
elif [ "$OS_NAME" = "Darwin" ]; then
    EXE_NAME="AdvantageScope (WPILib).app"
else
    EXE_NAME="advantagescope-wpilib"
fi

unset ELECTRON_RUN_AS_NODE

exec "$AS_PATH/$EXE_NAME"

