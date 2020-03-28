#include "ShortcutCreator.h"

#include <optional>


#include "shobjidl.h"
#include "objidl.h"
#include "shlguid.h"
#include "Shlobj.h"
#include "Shlobj_core.h"
#include "ComPtr.h"


#pragma comment(lib, "Shell32.lib")

ShortcutCreator::ShortcutCreator() {
  m_hr = CoGetClassObject(CLSID_ShellLink, CLSCTX_INPROC_SERVER, NULL, IID_PPV_ARGS(shellLinkFactory.GetAddressOf()));
}

bool ShortcutCreator::CreateShortcut(std::wstring path, std::wstring destination) const {
  HRESULT hres;
  com::ComPtr<IShellLink> shellLink;

  hres = shellLinkFactory->CreateInstance(NULL, IID_PPV_ARGS(shellLink.GetAddressOf()));

  if (FAILED(hres)) return false;

  auto persistFile = shellLink.As<IPersistFile>();

  if (!persistFile) return false;

  shellLink->SetPath(path.c_str());

  hres = persistFile->Save(destination.c_str(), true);

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

bool ShortcutCreator::CreateFolder(std::wstring path) const {
  auto ret = CreateDirectory(path.c_str(), NULL);
  return ret == 0;
}

// bool ShortcutCreator::Create() const {
//   HRESULT hres;
//   com::ComPtr<IShellLink> shellLink;

//   hres = CoCreateInstance(CLSID_ShellLink, NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(shellLink.GetAddressOf()));

//   if (FAILED(hres)) return false;


//   auto persistFile = shellLink.As<IPersistFile>();
// }
