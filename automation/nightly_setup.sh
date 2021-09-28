#!/bin/bash

# This script sets up the nightly build, including building development
# artifacts for GradleRIO and VS Code Extension from their respective main
# branches.

set -e

# Detect the operating system and store OS-specific commands in variables
# to be evaluated later.
NPM_PACKAGE_CMD=""
NPM_PACKAGE_LOC=""
SED_CMD="sed -i"
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
  NPM_PACKAGE_CMD="npm run packageLinux && tar -C build/wpilibutility-linux-x64 -pcvzf wpilibutility-linux.tar.gz ."
  NPM_PACKAGE_LOC="wpilibutility-linux.tar.gz"
elif [[ "$OSTYPE" == "darwin"* ]]; then
  NPM_PACKAGE_CMD="npm run packageMac && tar -C build/wpilibutility-darwin-x64 -pcvzf wpilibutility-mac.tar.gz ."
  NPM_PACKAGE_LOC="wpilibutility-mac.tar.gz"
  SED_CMD="sed -i {}"
else
  NPM_PACKAGE_CMD="npm run packageWindows && powershell -Command \"Compress-Archive -Path build\wpilibutility-win32-ia32\* -DestinationPath wpilibutility-windows.zip\""
  NPM_PACKAGE_LOC="wpilibutility-windows.zip"
fi

# First, a version number for the nightly is generated.
LATEST_TAG=$(git describe --abbrev=0)
LATEST_TAG_TRIMMED=${LATEST_TAG:1}
DATE=$(date +"%Y%m%d")
VERSION="$LATEST_TAG_TRIMMED-$DATE"

# Next, we will clone the latest versions of GradleRIO and VS Code
# Extension repositories.
function clone() {
  if [ ! -d $1 ]; then
    git clone "https://github.com/wpilibsuite/$1"
  else
    cd $1
    git reset --hard
    git pull
    cd ..
  fi
}
clone "GradleRIO"
clone "vscode-wpilib"

# Publish a development version of GradleRIO.
cd GradleRIO

# Temporarily use 2020 ni-libraries while 2022 runtime is still not available.
# eval "$SED_CMD \"13s/+/2020.+/\"" versionupdates.gradle

./gradlew updateVersions -PuseDevelopment
./gradlew publishToMavenLocal -x patchExamples -PpublishVersion=$VERSION
cd ..

# Publish a development version of the VS Code Extension.
cd vscode-wpilib
echo "$VERSION" > vscode-wpilib/resources/gradle/version.txt
./gradlew updateVersions updateAllDependencies -PbuildServer -PpublishVersion=$VERSION

cd vscode-wpilib
npm install
npm run gulp
npm run webpack
npm run vscePackage

cd ../wpilib-utility-standalone
npm install
npm run compile
eval "$NPM_PACKAGE_CMD"
cd ../..

# Update the version numbers in the installer and set the VS Code Extension
# and Standalone Utility location.
echo "gradleRioVersion: $VERSION" >gradle.properties
echo "vscodeLoc: vscode-wpilib/vscode-wpilib/vscode-wpilib-$VERSION.vsix" >>gradle.properties
echo "standaloneLoc: vscode-wpilib/wpilib-utility-standalone/$NPM_PACKAGE_LOC" >> gradle.properties
