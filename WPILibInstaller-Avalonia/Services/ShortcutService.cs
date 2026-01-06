using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using Newtonsoft.Json;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;

namespace WPILibInstaller.Services
{
    public class ShortcutService : IShortcutService
    {
        private readonly IConfigurationProvider configurationProvider;
        private readonly IToInstallProvider toInstallProvider;
        private readonly IVsCodeInstallLocationProvider vsInstallProvider;
        private readonly IProgramWindow programWindow;

        public ShortcutService(
            IConfigurationProvider configurationProvider,
            IToInstallProvider toInstallProvider,
            IVsCodeInstallLocationProvider vsInstallProvider,
            IProgramWindow programWindow)
        {
            this.configurationProvider = configurationProvider;
            this.toInstallProvider = toInstallProvider;
            this.vsInstallProvider = vsInstallProvider;
            this.programWindow = programWindow;
        }

        private async void CreateLinuxShortcut(String name, String executableName, String frcYear, String wmClass, String iconName, CancellationToken token)
        {
            var launcherFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/applications", $@"{name.Replace(' ', '_').Replace(")", "").Replace("(", "")}_{frcYear}.desktop");
            string contents = $@"#!/usr/bin/env xdg-open
[Desktop Entry]
Version=1.0
Type=Application
Categories=Robotics;Science
Name={name} {frcYear}
Comment={name} tool for the {frcYear} FIRST Robotics Competition season
Exec={configurationProvider.InstallDirectory}/tools/{executableName}
Icon={configurationProvider.InstallDirectory}/icons/{iconName}
Terminal=false
StartupNotify=true
StartupWMClass={wmClass}
";
            var launcherPath = Path.GetDirectoryName(launcherFile);
            if (launcherPath != null)
            {
                Directory.CreateDirectory(launcherPath);
            }
            await File.WriteAllTextAsync(launcherFile, contents, token);
        }

        private void CreateLinuxShortcut(String name, String frcYear, String wmClass, String iconName, CancellationToken token)
        {
            CreateLinuxShortcut(name, name, frcYear, wmClass, iconName, token);
        }

        public async Task RunShortcutCreator(CancellationToken token)
        {
            var shortcutData = new ShortcutData();

            var frcHomePath = configurationProvider.InstallDirectory;
            var frcYear = configurationProvider.UpgradeConfig.FrcYear;

            var iconLocation = Path.Join(frcHomePath, "icons");
            var wpilibIconLocation = Path.Join(iconLocation, "wpilib-256.ico");

            shortcutData.IsAdmin = toInstallProvider.Model.InstallAsAdmin;

            if (vsInstallProvider.Model.InstallingVsCode)
            {
                // Add VS Code Shortcuts
                shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "vscode", "Code.exe"), $"{frcYear} WPILib VS Code", $"{frcYear} WPILib VS Code", wpilibIconLocation));
                shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "vscode", "Code.exe"), $"Programs/{frcYear} WPILib VS Code", $"{frcYear} WPILib VS Code", wpilibIconLocation));
            }

            // Add Tool Shortcuts
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "glass.exe"), $"{frcYear} WPILib Tools/Glass {frcYear}", $"Glass {frcYear}", ""));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "outlineviewer.exe"), $"{frcYear} WPILib Tools/OutlineViewer {frcYear}", $"OutlineViewer {frcYear}", ""));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "PathWeaver.exe"), $"{frcYear} WPILib Tools/PathWeaver {frcYear}", $"PathWeaver {frcYear}", wpilibIconLocation));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "RobotBuilder.exe"), $"{frcYear} WPILib Tools/RobotBuilder {frcYear}", $"RobotBuilder {frcYear}", Path.Join(iconLocation, "robotbuilder.ico")));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "ShuffleBoard.exe"), $"{frcYear} WPILib Tools/Shuffleboard {frcYear}", $"Shuffleboard {frcYear}", wpilibIconLocation));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "SmartDashboard.exe"), $"{frcYear} WPILib Tools/SmartDashboard {frcYear}", $"SmartDashboard {frcYear}", wpilibIconLocation));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "sysid.exe"), $"{frcYear} WPILib Tools/SysId {frcYear}", $"SysId {frcYear}", ""));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "roborioteambumbersetter.exe"), $"{frcYear} WPILib Tools/roboRIO Team Number Setter {frcYear}", $"roboRIO Team Number Setter {frcYear}", ""));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "datalogtool.exe"), $"{frcYear} WPILib Tools/Data Log Tool {frcYear}", $"Data Log Tool {frcYear}", ""));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "wpical.exe"), $"{frcYear} WPILib Tools/WPIcal {frcYear}", $"WPIcal {frcYear}", ""));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "advantagescope", "AdvantageScope (WPILib).exe"), $"{frcYear} WPILib Tools/AdvantageScope (WPILib) {frcYear}", $"AdvantageScope (WPILib) {frcYear}", ""));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "elastic", "elastic_dashboard.exe"), $"{frcYear} WPILib Tools/Elastic (WPILib) {frcYear}", $"Elastic (WPILib) {frcYear}", ""));

            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "glass.exe"), $"Programs/{frcYear} WPILib Tools/Glass {frcYear}", $"Glass {frcYear}", ""));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "outlineviewer.exe"), $"Programs/{frcYear} WPILib Tools/OutlineViewer {frcYear}", $"OutlineViewer {frcYear}", ""));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "PathWeaver.exe"), $"Programs/{frcYear} WPILib Tools/PathWeaver {frcYear}", $"PathWeaver {frcYear}", wpilibIconLocation));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "RobotBuilder.exe"), $"Programs/{frcYear} WPILib Tools/RobotBuilder {frcYear}", $"RobotBuilder {frcYear}", Path.Join(iconLocation, "robotbuilder.ico")));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "ShuffleBoard.exe"), $"Programs/{frcYear} WPILib Tools/Shuffleboard {frcYear}", $"Shuffleboard {frcYear}", wpilibIconLocation));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "SmartDashboard.exe"), $"Programs/{frcYear} WPILib Tools/SmartDashboard {frcYear}", $"SmartDashboard {frcYear}", wpilibIconLocation));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "sysid.exe"), $"Programs/{frcYear} WPILib Tools/SysId {frcYear}", $"SysId {frcYear}", ""));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "roborioteamnumbersetter.exe"), $"Programs/{frcYear} WPILib Tools/roboRIO Team Number Setter {frcYear}", $"roboRIO Team Number Setter {frcYear}", ""));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "datalogtool.exe"), $"Programs/{frcYear} WPILib Tools/Data Log Tool {frcYear}", $"Data Log Tool {frcYear}", ""));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "wpical.exe"), $"Programs/{frcYear} WPILib Tools/WPIcal {frcYear}", $"WPIcal {frcYear}", ""));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "advantagescope", "AdvantageScope (WPILib).exe"), $"Programs/{frcYear} WPILib Tools/AdvantageScope (WPILib) {frcYear}", $"AdvantageScope (WPILib) {frcYear}", ""));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "elastic", "elastic_dashboard.exe"), $"Programs/{frcYear} WPILib Tools/Elastic (WPILib) {frcYear}", $"Elastic (WPILib) {frcYear}", ""));

            if (toInstallProvider.Model.InstallEverything)
            {
                // Add Documentation Shortcuts
                shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "documentation", "frc-docs", "index.html"), $"{frcYear} WPILib Documentation", $"{frcYear} WPILib Documentation", wpilibIconLocation));
                shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "documentation", "frc-docs", "index.html"), $"Programs/{frcYear} WPILib Documentation", $"{frcYear} WPILib Documentation", wpilibIconLocation));
            }

            var serializedData = JsonConvert.SerializeObject(shortcutData);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Run windows shortcut creater
                var tempFile = Path.GetTempFileName();
                await File.WriteAllTextAsync(tempFile, serializedData, token);
                var shortcutCreatorPath = Path.Combine(configurationProvider.InstallDirectory, "installUtils", "WPILibShortcutCreator.exe");

                do
                {
                    var startInfo = new ProcessStartInfo(shortcutCreatorPath, $"\"{tempFile}\"")
                    {
                        WorkingDirectory = Environment.CurrentDirectory
                    };
                    if (shortcutData.IsAdmin)
                    {
                        startInfo.UseShellExecute = true;
                        startInfo.Verb = "runas";
                    }
                    else
                    {
                        startInfo.UseShellExecute = false;
                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        startInfo.CreateNoWindow = true;
                        startInfo.RedirectStandardOutput = true;
                    }
                    var exitCode = await Task.Run(() =>
                    {
                        try
                        {
                            var proc = Process.Start(startInfo);
                            proc!.WaitForExit();
                            return proc.ExitCode;
                        }
                        catch (Win32Exception ex)
                        {
                            return ex.NativeErrorCode;
                        }
                    });

                    if (exitCode == 1223) // ERROR_CANCELLED
                    {
                        var results = await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                        {
                            ContentTitle = "UAC Prompt Cancelled",
                            ContentMessage = "UAC Prompt Cancelled or Timed Out. Would you like to retry?",
                            Icon = MsBox.Avalonia.Enums.Icon.Info,
                            ButtonDefinitions = MsBox.Avalonia.Enums.ButtonEnum.YesNo
                        }).ShowWindowDialogAsync(programWindow.Window);
                        if (results == MsBox.Avalonia.Enums.ButtonResult.Yes)
                        {
                            continue;
                        }
                        break;
                    }

                    if (exitCode != 0)
                    {
                        await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                        {
                            ContentTitle = "Shortcut Creation Failed",
                            ContentMessage = $"Shortcut creation failed with error code {exitCode}",
                            Icon = MsBox.Avalonia.Enums.Icon.Warning,
                            ButtonDefinitions = MsBox.Avalonia.Enums.ButtonEnum.Ok
                        }).ShowWindowDialogAsync(programWindow.Window);
                        break;
                    }
                    break;
                } while (true);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (vsInstallProvider.Model.InstallingVsCode)
                {
                    // Create Linux desktop shortcut
                    var desktopFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", $@"FRC VS Code {frcYear}.desktop");
                    var launcherFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/applications", $@"FRC_VS_Code_{frcYear}.desktop");
                    string contents = $@"#!/usr/bin/env xdg-open
[Desktop Entry]
Version=1.0
Type=Application
Categories=Development
Name=FRC VS Code {frcYear}
Comment=Official C++/Java IDE for the FIRST Robotics Competition
Exec={configurationProvider.InstallDirectory}/frccode/frccode{frcYear}
Icon={configurationProvider.InstallDirectory}/icons/wpilib-icon-256.png
Terminal=false
StartupNotify=true
StartupWMClass=Code
";

                    var desktopPath = Path.GetDirectoryName(desktopFile);
                    if (desktopPath != null)
                    {
                        Directory.CreateDirectory(desktopPath);
                    }
                    var launcherPath = Path.GetDirectoryName(launcherFile);
                    if (launcherPath != null)
                    {
                        Directory.CreateDirectory(launcherPath);
                    }
                    await File.WriteAllTextAsync(desktopFile, contents, token);
                    await File.WriteAllTextAsync(launcherFile, contents, token);
                    await Task.Run(() =>
                    {
                        var startInfo = new ProcessStartInfo("chmod", $"+x \"{desktopFile}\"")
                        {
                            UseShellExecute = false,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true
                        };
                        var proc = Process.Start(startInfo);
                        proc!.WaitForExit();
                    }, token);
                }

                CreateLinuxShortcut("AdvantageScope (WPILib)", "AdvantageScope", frcYear, "AdvantageScope (WPILib)", "advantagescope.png", token);
                CreateLinuxShortcut("Elastic (WPILib)", "Elastic", frcYear, "elastic_dashboard", "elastic.png", token);
                CreateLinuxShortcut("Glass", "glass", frcYear, "Glass - DISCONNECTED", "glass.png", token);
                CreateLinuxShortcut("OutlineViewer", "outlineviewer", frcYear, "OutlineViewer - DISCONNECTED", "outlineviewer.png", token);
                CreateLinuxShortcut("DataLogTool", "datalogtool", frcYear, "Datalog Tool", "datalogtool.png", token);
                CreateLinuxShortcut("SysId", "sysid", frcYear, "System Identification", "sysid.png", token);
                CreateLinuxShortcut("SmartDashboard", frcYear, "edu-wpi-first-smartdashboard-SmartDashboard", "wpilib-icon-256.png", token);
                CreateLinuxShortcut("RobotBuilder", frcYear, "robotbuilder-RobotBuilder", "robotbuilder.png", token);
                CreateLinuxShortcut("PathWeaver", frcYear, "edu.wpi.first.pathweaver.PathWeaver", "wpilib-icon-256.png", token);
                CreateLinuxShortcut("roboRIOTeamNumberSetter", "roborioteamnumbersetter", frcYear, "roboRIO Team Number Setter", "roborioteamnumbersetter.png", token);
                CreateLinuxShortcut("Shuffleboard", frcYear, "edu.wpi.first.shuffleboard.app.Shuffleboard", "wpilib-icon-256.png", token);
                CreateLinuxShortcut("WPIcal", "wpical", frcYear, "WPIcal", "wpical.png", token);
            }
        }
    }
}
