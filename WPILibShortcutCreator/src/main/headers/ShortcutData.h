#pragma once

#include <string>
#include <vector>

#include "nlohmann/json_fwd.hpp"

struct ShortcutInfo {
    std::wstring path;
    std::wstring name;
    std::wstring description;
    std::wstring iconLocation;
};

struct NewEnvVariable {
    std::wstring name;
    std::wstring value;
};

struct AddedPathVariable {
    std::wstring path;
};

struct ShortcutData {
    bool isAdmin;
    std::vector<ShortcutInfo> desktopShortcuts;
    std::vector<ShortcutInfo> startMenuShortcuts;
    std::vector<NewEnvVariable> newEnvironmentalVariables;
    std::vector<AddedPathVariable> addToPath;
};

void from_json(const nlohmann::json& j, ShortcutInfo& s);

void from_json(const nlohmann::json& j, NewEnvVariable& s);

void from_json(const nlohmann::json& j, AddedPathVariable& s);

void from_json(const nlohmann::json& j, ShortcutData& s);
