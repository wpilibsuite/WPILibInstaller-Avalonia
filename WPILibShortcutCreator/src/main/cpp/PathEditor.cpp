#include "PathEditor.h"

#include <algorithm>

PathEditor::PathEditor(bool admin)
    : m_admin{admin}, m_hr{S_OK}
{
}

static std::vector<std::wstring> SplitPath(const std::wstring& path) {
  std::vector<std::wstring> result;
  
  auto begin = path.cbegin();
  
  auto end = path.cend();
  
  while (begin != end) {
    const auto foundSplit = std::find(begin, end, L';');
    if (begin != foundSplit) {
      result.emplace_back(begin, foundSplit);
    }
    if (foundSplit == end) {
      break;
    }
    begin = foundSplit + 1;
  }
  
  return result;
}

bool PathEditor::AddToPATH(std::vector<AddedPathVariable> directoriesToAddToPath) const
{
  if (directoriesToAddToPath.empty()) {
    return true;
  }
  
  // length includes null terminator
  DWORD length = GetEnvironmentVariable(L"PATH", NULL, 0);
  auto buf = std::make_unique<WCHAR[]>(length);
  DWORD result = GetEnvironmentVariable(L"PATH", buf.get(), length);
  if (result == 0) return false;
  
  std::wstring toSet{buf.get(), length - 1};
  
  // Ensure PATH does not end with a ;
  if (toSet[toSet.length() - 1] != L';') {
    toSet.pop_back();
  }
  
  // length - 1 because of null terminator
  auto splitPath = SplitPath(toSet);
  

  for(auto&& toAdd : directoriesToAddToPath) {
    for (auto&& exists : splitPath) {
      if (lstrcmpi(toAdd.path.c_str(), exists.c_str()) == 0) {
        toSet.reserve(toSet.length() + toAdd.path.length() + 1);
        toSet.append(L";");
        toSet.append(toAdd.path);
      }
    }
  }
  
  return SetEnvironmentVariable(L"PATH", toSet.c_str());
}

bool PathEditor::AddEnvVariables(std::vector<NewEnvVariable> newEnvironmentalVariables) const {
  bool allSucceeded = true;
  for (auto&& var : newEnvironmentalVariables) {
    if (!AddEnvVariable(var.name, var.value)) {
      allSucceeded = false;
    }
  }
  return allSucceeded;
}

bool PathEditor::AddEnvVariable(const std::wstring& name, const std::wstring& value) const
{
  return SetEnvironmentVariable(name.c_str(), value.c_str());
}
