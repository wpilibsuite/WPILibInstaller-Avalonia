#!/bin/sh

SCRIPT_PATH="$(dirname "$(realpath "$0")")"
SCRIPT_NAME="$(basename "$(realpath "$0")")"
SCRIPT_BASE="$(basename -s .sh "$SCRIPT_NAME")"
OS_NAME="$(uname -s)"
ELASTIC_PATH="$(realpath "$SCRIPT_PATH/../elastic")"

if [ "$OS_NAME" = "Darwin" ]; then
    open "$ELASTIC_PATH/elastic_dashboard.app"
else
    exec "$ELASTIC_PATH/elastic_dashboard"
fi
