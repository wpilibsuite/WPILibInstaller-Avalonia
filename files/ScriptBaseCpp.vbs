'Create File System Object for working with directories
Set fso = WScript.CreateObject("Scripting.FileSystemObject")

'Get the script name, and from that the jar name
exeName = fso.GetBaseName(WScript.ScriptName) + ".exe"

'Get the folder of this script
toolsFolder = fso.GetParentFolderName(WScript.ScriptFullName)

'Get the full path to the Jar
fullExeName = fso.BuildPath(toolsFolder, exeName)

shellScript = fullExeName

'Create Shell Object
Set objShell = WScript.CreateObject( "WScript.Shell" )
dim runObject
' Allow us to catch a script run failure
On Error Resume Next
Set runObj = objShell.Exec(shellScript)
If Err.Number <> 0 Then
	If WScript.Arguments.Count > 0 Then
		If (WScript.Arguments(0) <> "silent") Then
			WScript.Echo "Error Launching Tool" + vbCrLf + Err.Description
		Else
			WScript.StdOut.Write("Error Launching Tool")
			WScript.StdOut.Write(Error.Description)
		End If
	Else
		WScript.Echo "Error Launching Tool"  + vbCrLf + Err.Description
	End If
	Set runObj = Nothing
	Set objShell = Nothing
	Set fso = Nothing
	WScript.Quit(1)
End If

Set runObj = Nothing
Set objShell = Nothing
Set fso = Nothing
WScript.Quit(0)
