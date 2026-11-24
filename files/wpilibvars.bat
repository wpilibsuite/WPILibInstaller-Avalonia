@echo off

pushd "%~dp0..\jdk\@JDK_FOLDER@"
set JAVA_HOME=%CD%
set PATH=%CD%\bin;%PATH%
popd
