using Newtonsoft.Json.Linq;

namespace WPILibInstaller.Utils
{
    public static class VsCodeSettingsUtils
    {
        public static void SetIfNotSet(string key, object value, JObject settingsJson)
        {
            if (!settingsJson.ContainsKey(key))
            {
                settingsJson[key] = JToken.FromObject(value);
            }
        }

        public static void SetIfNotSetIgnoreSync(string key, object value, JObject settingsJson)
        {
            SetIfNotSet(key, value, settingsJson);
            IgnoreSync(key, settingsJson);
        }

        public static void IgnoreSync(string key, JObject settingsJson)
        {
            if (settingsJson.ContainsKey("settingsSync.ignoredSettings"))
            {
                JArray ignoredSettings = (JArray)settingsJson["settingsSync.ignoredSettings"]!;
                bool keyFound = false;
                foreach (JToken result in ignoredSettings)
                {
                    if (result.Value<string>() != null)
                    {
                        if (result.Value<string>() == key)
                        {
                            keyFound = true;
                        }
                    }
                }
                if (!keyFound)
                {
                    ignoredSettings.Add(key);
                    settingsJson["settingsSync.ignoredSettings"] = ignoredSettings;
                }
            }
            else
            {
                JArray ignoredSettings = new JArray(key);
                settingsJson["settingsSync.ignoredSettings"] = ignoredSettings;
            }
        }
    }
}
