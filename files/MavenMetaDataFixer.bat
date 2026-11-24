@echo off
call "%~dp0..\jdk\@JDK_FOLDER@\bin\java.exe" -jar "%~dp0MavenMetaDataFixer.jar" %*
