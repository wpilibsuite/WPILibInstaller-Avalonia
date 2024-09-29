#include "ShortcutCreator.h"

#include <optional>


#include "shobjidl.h"
#include "objidl.h"
#include "shlguid.h"
#include "Shlobj.h"
#include "Shlobj_core.h"
#include "ComPtr.h"

#include "CreateDirectoryRecursively.h"

#pragma comment(lib, "Shell32.lib")

ShortcutCreator::ShortcutCreator() {
  m_hr = CoGetClassObject(CLSID_ShellLink, CLSCTX_INPROC_SERVER, NULL, IID_PPV_ARGS(shellLinkFactory.GetAddressOf()));
}

bool ShortcutCreator::CreateShortcut(const std::wstring& destination, const ShortcutInfo& shortcutInfo) const {
  HRESULT hres;
  com::ComPtr<IShellLink> shellLink;

  hres = shellLinkFactory->CreateInstance(NULL, IID_PPV_ARGS(shellLink.GetAddressOf()));

  if (FAILED(hres)) return false;

  auto persistFile = shellLink.As<IPersistFile>();

  if (!persistFile) return false;

  shellLink->SetPath(shortcutInfo.path.c_str());

  shellLink->SetDescription(shortcutInfo.description.c_str());

  if (!shortcutInfo.iconLocation.empty()) {
    shellLink->SetIconLocation(shortcutInfo.iconLocation.c_str(), 0);
  }

  std::wstring finalPath = destination + L'\\' + shortcutInfo.name + L".lnk";

  hres = persistFile->Save(finalPath.c_str(), true);

  if (hres == HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND)) {
    static const std::wstring separators(L"\\/");
    std::size_t slashIndex = finalPath.find_last_of(separators);
    if(slashIndex != std::wstring::npos) {
      bool wasSuccessful = createDirectoryRecursively(finalPath.substr(0, slashIndex));
      if (wasSuccessful) {
        hres = persistFile->Save(finalPath.c_str(), true);
      } else {
        return false;
      }
    } else {
      return false;
    }
  }

  return SUCCEEDED(hres);
}

std::optional<std::wstring> ShortcutCreator::GetLocalStartMenuFolder() const {
  PWSTR path;
  if (SUCCEEDED(SHGetKnownFolderPath(FOLDERID_StartMenu, 0, NULL, &path))) {
    std::wstring retVal{path};
    CoTaskMemFree(path);
    return retVal;
  }
  return nullptr;
}

std::optional<std::wstring> ShortcutCreator::GetAllUsersStartMenuFolder() const {
  PWSTR path;
  if (SUCCEEDED(SHGetKnownFolderPath(FOLDERID_CommonStartMenu, 0, NULL, &path))) {
    std::wstring retVal{path};
    CoTaskMemFree(path);
    return retVal;
  }
  return nullptr;
}

std::optional<std::wstring> ShortcutCreator::GetPublicDesktopFolder() const {
  PWSTR path;
  if (SUCCEEDED(SHGetKnownFolderPath(FOLDERID_PublicDesktop, 0, NULL, &path))) {
    std::wstring retVal{path};
    CoTaskMemFree(path);
    return retVal;
  }
  return nullptr;
}

std::optional<std::wstring> ShortcutCreator::GetLocalDesktopFolder() const {
  PWSTR path;
  if (SUCCEEDED(SHGetKnownFolderPath(FOLDERID_Desktop, 0, NULL, &path))) {
    std::wstring retVal{path};
    CoTaskMemFree(path);
    return retVal;
  }
  return nullptr;
}

bool ShortcutCreator::CreateFolder(const std::wstring& path) const {
  auto ret = CreateDirectory(path.c_str(), NULL);
  return ret == 0;
}

bool ShortcutCreator::CreateShortcuts(std::vector<ShortcutInfo>& toCreate, const std::wstring& dest) const {
  bool allCompleted = true;

  for (auto&& info : toCreate) {
    if (!CreateShortcut(dest, info)) {
      allCompleted = false;
    }
  }

  return allCompleted;
}

// bool ShortcutCreator::Create() const {
//   HRESULT hres;
//   com::ComPtr<IShellLink> shellLink;

//   hres = CoCreateInstance(CLSID_ShellLink, NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(shellLink.GetAddressOf()));

//   if (FAILED(hres)) return false;


//   auto persistFile = shellLink.As<IPersistFile>();
// }
