'Create File System Object for working with directories
Set fso = WScript.CreateObject("Scripting.FileSystemObject")

'Get the folder of this script
toolsFolder = fso.GetParentFolderName(WScript.ScriptFullName)

'Get the AdvantageScope folder
toolsFolder = fso.GetParentFolderName(WScript.ScriptFullName)
yearFolder = fso.GetParentFolderName(toolsFolder)
advantageScopeFolder = fso.BuildPath(yearFolder, "advantagescope")

'Get the full path to the exe
fullExeName = fso.BuildPath(advantageScopeFolder, "AdvantageScope (WPILib).exe")

shellScript = fullExeName

'Create Shell Object
Set objShell = WScript.CreateObject( "WScript.Shell" )
Set objEnv = objShell.Environment("PROCESS")
objEnv.Remove("ELECTRON_RUN_AS_NODE")
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
