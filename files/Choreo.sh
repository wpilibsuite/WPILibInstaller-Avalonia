#!/bin/sh

SCRIPT_PATH="$(dirname "$(realpath "$0")")"
SCRIPT_NAME="$(basename "$(realpath "$0")")"
SCRIPT_BASE="$(basename -s .sh "$SCRIPT_NAME")"
OS_NAME="$(uname -s)"
CHOREO_PATH="$(realpath "$SCRIPT_PATH/../choreo")"

exec "$CHOREO_PATH/choreo"
