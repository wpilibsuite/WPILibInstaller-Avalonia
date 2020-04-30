#pragma once

#include "ShortcutData.h"

#include "Windows.h"

class PathEditor {
public:
  PathEditor(bool admin);

  bool AddToPATH(std::vector<AddedPathVariable> directoriesToAddToPath) const;

  bool AddEnvVariables(std::vector<NewEnvVariable> newEnvironmentalVariables) const;

  bool AddEnvVariable(const std::wstring& name, const std::wstring& value) const;

  operator HRESULT() const { return m_hr; }

private:
  bool m_admin;
  HRESULT m_hr;
};
