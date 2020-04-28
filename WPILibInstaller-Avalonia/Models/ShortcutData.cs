using System.Collections.Generic;

namespace WPILibInstaller_Avalonia.Models {
  public class ShortcutData {
    public bool IsAdmin {get;set;}
    public List<ShortcutInfo> DesktopShortcuts {get;set;} = new List<ShortcutInfo>();
    public List<ShortcutInfo> StartMenuShortcuts {get;set;} = new List<ShortcutInfo>();
    public string IconLocation {get;set;} = "";
  }

  public class ShortcutInfo {
    public string Path {get;set;} = "";
    public string Name {get;set;} = "";
    public string Description {get;set;} = "";
  }
}
