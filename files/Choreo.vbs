'Create File System Object for working with directories
Set fso = WScript.CreateObject("Scripting.FileSystemObject")

'Get the folder of this script
toolsFolder = fso.GetParentFolderName(WScript.ScriptFullName)

'Get the Choreo folder
toolsFolder = fso.GetParentFolderName(WScript.ScriptFullName)
yearFolder = fso.GetParentFolderName(toolsFolder)
choreoFolder = fso.BuildPath(yearFolder, "choreo")

'Get the full path to the exe
fullExeName = fso.BuildPath(choreoFolder, "choreo.exe")

shellScript = fullExeName

'Create Shell Object
Set objShell = WScript.CreateObject( "WScript.Shell" )
objShell.Run(shellScript)
