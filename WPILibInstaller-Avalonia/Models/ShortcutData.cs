﻿using System.Collections.Generic;

namespace WPILibInstaller.Models
{
    public class ShortcutData
    {
        public bool IsAdmin { get; set; }
        public List<ShortcutInfo> DesktopShortcuts { get; set; } = new List<ShortcutInfo>();
        public List<NewEnvVariable> NewEnvironmentalVariables { get; set; } = new();
        public List<ShortcutInfo> StartMenuShortcuts { get; set; } = new List<ShortcutInfo>();
        public List<AddedPathVariable> AddToPath { get; set; } = new();

        public string IconLocation { get; set; } = "";
    }

    public class ShortcutInfo
    {
        public ShortcutInfo() { }

        public ShortcutInfo(string path, string name, string description)
        {
            Path = path;
            Name = name;
            Description = description;
        }

        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class NewEnvVariable
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public class AddedPathVariable
    {
        public string Path { get; set; } = "";
    }
}
