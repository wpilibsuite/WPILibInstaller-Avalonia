#!/bin/bash
set -e

# Create the Version Number
LATEST_TAG=`git describe --abbrev=0`
LATEST_TAG_TRIMMED=${LATEST_TAG:1}
DATE=`date +"%Y%m%d"`
VERSION="$LATEST_TAG_TRIMMED-$DATE"

echo $VERSION

# Clone GradleRIO and VS Code Extension
function clone() {
  if [ ! -d $1 ] ; then
    git clone "https://github.com/prateekma/$1"
  else
    cd $1
    git reset --hard
    git pull
    cd ..
  fi
}

clone "GradleRIO"
clone "vscode-wpilib"

# Publish Local GradleRIO
cd GradleRIO
git checkout ov-cpp
touch settings.gradle

./gradlew updateVersions -PuseDevelopment
./gradlew publishToMavenLocal -x patchExamples -PpublishVersion=$VERSION
cd ..

# Publish Local VS Code Extension
cd vscode-wpilib
./gradlew build updateVersions updateAllDependencies -PbuildServer -PpublishVersion=$VERSION

cd vscode-wpilib
npm install
npm run gulp
npm run webpack
npm run vscePackage
cd ../..

# Update GradleRIO Version in Installer and Set VS Code Location
echo "gradleRioVersion: $VERSION" > gradle.properties
echo "vscodeLoc: vscode-wpilib/vscode-wpilib/vscode-wpilib-$VERSION.vsix" >> gradle.properties
echo "standaloneVersionOverride: $LATEST_TAG_TRIMMED" >> gradle.properties
