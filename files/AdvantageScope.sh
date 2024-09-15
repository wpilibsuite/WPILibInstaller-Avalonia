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
    EXE_NAME="exec $AS_PATH/AdvantageScope\ \(WPILib\).AppImage"
elif [ "$OS_NAME" = "Darwin" ]; then
    EXE_NAME="open AdvantageScope (WPILib).app"
else
    EXE_NAME="exec $AS_PATH/AdvantageScope\ \(WPILib\).AppImage"
fi

echo "EXE_NAME: $EXE_NAME"

unset ELECTRON_RUN_AS_NODE

"$EXE_NAME"

