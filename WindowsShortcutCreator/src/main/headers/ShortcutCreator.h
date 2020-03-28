#pragma once

#include <string>
#include <optional>

#include "shobjidl.h"
#include "objidl.h"
#include "shlguid.h"
#include "ComPtr.h"

class ShortcutCreator {
public:
  ShortcutCreator();


  bool CreateShortcut(std::wstring path, std::wstring destination) const;
  bool CreateFolder(std::wstring path) const;

  std::optional<std::wstring> GetLocalStartMenuFolder() const;
  std::optional<std::wstring> GetAllUsersStartMenuFolder() const;

    std::optional<std::wstring> GetPublicDesktopFolder() const;

   operator HRESULT() const { return m_hr; }


private:
  com::ComPtr<IClassFactory> shellLinkFactory;
   HRESULT m_hr;
};
