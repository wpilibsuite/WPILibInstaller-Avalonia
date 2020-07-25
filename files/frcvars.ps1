$frcHome = (Get-Item $PSScriptRoot).Parent.FullName

$javaHome = Join-Path $frcHome "jdk"

$env:JAVA_HOME = $javaHome

$javaBin = Join-Path $javaHome "bin"

$env:PATH = $javaBin + ";" + $env:PATH
