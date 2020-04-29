#include "ShortcutData.h"

#include <memory>

#include "nlohmann/json.hpp"

#include "Windows.h"

using json = nlohmann::json;

static std::wstring ToWideString(const std::string& str) {
    constexpr int numCharacters = 512;
    WCHAR buf[numCharacters];
    int length = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), str.length(), buf, numCharacters);
    DWORD lastError = GetLastError();
    if (length != 0) {
        return std::wstring(buf, length);
    }
    else if (lastError == ERROR_INSUFFICIENT_BUFFER) {
        // Call to get length
        length = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), str.length(), nullptr, 0);
        auto bigBuf = std::make_unique<WCHAR[]>(length);
        length = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), str.length(), bigBuf.get(), length);
        return std::wstring(bigBuf.get(), length);
    } else {
        // Actual error, figure out what to do
        return L"";
    }
}

void from_json(const json& j, ShortcutInfo& s) {
    s.path = ToWideString(j.at("Path").get<std::string>());
    s.name = ToWideString(j.at("Name").get<std::string>());
    s.description = ToWideString(j.at("Description").get<std::string>());
}

void from_json(const nlohmann::json& j, NewEnvVariable& s) {
    s.name = ToWideString(j.at("Name").get<std::string>());
    s.value = ToWideString(j.at("Value").get<std::string>());
}

void from_json(const nlohmann::json& j, AddedPathVariable& s) {
    s.path = ToWideString(j.at("Path").get<std::string>());
}

void from_json(const json& j, ShortcutData& s) {
    j.at("IsAdmin").get_to(s.isAdmin);
    j.at("DesktopShortcuts").get_to(s.desktopShortcuts);
    j.at("StartMenuShortcuts").get_to(s.startMenuShortcuts);
    j.at("NewEnvironmentalVariables").get_to(s.newEnvironmentalVariables);
    j.at("AddToPath").get_to(s.addToPath);
    s.iconLocation = ToWideString(j.at("IconLocation").get<std::string>());
}
