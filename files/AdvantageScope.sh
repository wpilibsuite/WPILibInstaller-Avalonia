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

unset ELECTRON_RUN_AS_NODE

if [ "$OS_NAME" = "Linux" ]; then
    exec "$AS_PATH/advantagescope-wpilib"
elif [ "$OS_NAME" = "Darwin" ]; then
    open "$AS_PATH/AdvantageScope (WPILib).app"
else
    exec "$AS_PATH/advantagescope-wpilib"
fi


