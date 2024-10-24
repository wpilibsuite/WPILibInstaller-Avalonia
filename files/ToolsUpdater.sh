#!/bin/sh

SCRIPT_PATH="$(dirname "$(realpath "$0")")"
SCRIPT_NAME="$(basename "$(realpath "$0")")"
SCRIPT_BASE="$(basename -s .sh "$SCRIPT_NAME")"
JAR_NAME="$SCRIPT_BASE.jar"
JDK_DIR="$(realpath "$SCRIPT_PATH/../jdk/bin/java")"

if ! "$JDK_DIR" -jar "$JAR_NAME"; then
    echo "ERROR launching $SCRIPT_BASE using $JDK_DIR"
    if ! "$JAVA_HOME/bin/java" -jar "$JAR_NAME"; then
        echo "ERROR launching $SCRIPT_BASE using $JAVA_HOME"
        if ! java -jar "$JAR_NAME"; then
            echo "ERROR launching $SCRIPT_BASE using java from path: $PATH"
        fi
    fi
fi
