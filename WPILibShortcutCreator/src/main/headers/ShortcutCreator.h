#pragma once

#include <string>
#include <vector>
#include <optional>

#include "shobjidl.h"
#include "objidl.h"
#include "shlguid.h"
#include "ComPtr.h"

#include <ShortcutData.h>

class ShortcutCreator {
public:
  ShortcutCreator();

  bool CreateShortcuts(std::vector<ShortcutInfo>& toCreate, const std::wstring& icon, const std::wstring& dest) const;


  bool CreateShortcut(const std::wstring& destination, const std::wstring& icon, const ShortcutInfo& shortcutInfo) const;
  bool CreateFolder(const std::wstring& path) const;

  std::optional<std::wstring> GetLocalStartMenuFolder() const;
  std::optional<std::wstring> GetAllUsersStartMenuFolder() const;

  std::optional<std::wstring> GetLocalDesktopFolder() const;
  std::optional<std::wstring> GetPublicDesktopFolder() const;

   operator HRESULT() const { return m_hr; }


private:
  com::ComPtr<IClassFactory> shellLinkFactory;
  HRESULT m_hr;
};
