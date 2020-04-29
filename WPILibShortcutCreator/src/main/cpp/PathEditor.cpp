#include "PathEditor.h"

PathEditor::PathEditor(bool admin)
    : m_admin{admin}, m_hr{S_OK}
{
}

bool PathEditor::AddToPATH(std::vector<AddedPathVariable> directoriesToAddToPath) const
{
  return false;
}

bool PathEditor::AddEnvVariables(std::vector<NewEnvVariable> newEnvironmentalVariables) const {
  return false;
}

bool PathEditor::AddEnvVariable(std::wstring name, std::wstring value) const
{
  return false;
}
