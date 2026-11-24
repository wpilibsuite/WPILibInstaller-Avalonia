$wpilibHome = (Get-Item $PSScriptRoot).Parent.FullName

$javaHome = Join-Path $wpilibHome "jdk\@JDK_FOLDER@"

$env:JAVA_HOME = $javaHome

$javaBin = Join-Path $javaHome "bin"

$env:PATH = $javaBin + ";" + $env:PATH
