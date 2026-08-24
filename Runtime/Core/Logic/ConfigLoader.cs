#nullable enable
using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FTFoundation.Core
{
    // Loads and merges layered appsettings JSON files at startup, then serves config values
    // to services via [Config]-decorated properties.
    //
    // Override chain (lowest → highest priority):
    //   appsettings.builtin  — package-provided defaults for built-in services
    //   appsettings          — user's main config
    //   appsettings.{profile} — e.g. appsettings.editor, appsettings.development
    //   appsettings.local    — machine-local overrides (add to .gitignore; safe for secrets)
    //
    // All files are loaded via Resources.Load<TextAsset> and silently skipped if absent.
    // Files must therefore reside under a Resources/ folder in the project.
    internal static class ConfigLoader
    {
        private static JObject _config = new();

        internal static void Clear()
        {
            _config = new JObject();
        }

        internal static void Initialize(BuildTargetProfile currentProfile)
        {
            LoadAndMerge("appsettings.builtin");
            LoadAndMerge("appsettings");
            LoadAndMerge($"appsettings.{currentProfile.ToString().ToLower()}");
            LoadAndMerge("appsettings.local");
        }

        private static void LoadAndMerge(string resourceName)
        {
            var asset = Resources.Load<TextAsset>(resourceName);
            if (asset == null) return;

            try
            {
                var obj = JObject.Parse(asset.text);
                _config.Merge(obj, new Newtonsoft.Json.Linq.JsonMergeSettings
                {
                    MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Replace
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FTFoundation] Failed to parse config file '{resourceName}': {e.Message}");
            }
        }

        internal static bool TryGetValue(Type serviceType, string propertyName, out string? value)
        {
            value = null;
            string sectionKey = GetServiceKey(serviceType);
            string propertyKey = LowercaseFirst(propertyName);

            if (_config[sectionKey] is JObject section && section[propertyKey] is JToken token)
            {
                value = token.ToString();
                return true;
            }

            return false;
        }

        // Called from within compiled injection actions for each [Config] property.
        internal static void ApplyConfigValue(object instance, PropertyInfo property, Type serviceType, bool required)
        {
            if (!TryGetValue(serviceType, property.Name, out string? raw))
            {
                if (required)
                    throw new UnityException(
                        $"[FTFoundation] Required config value '{GetServiceKey(serviceType)}.{LowercaseFirst(property.Name)}' " +
                        $"is not defined in any appsettings file.");
                return;
            }

            try
            {
                // Convert.ChangeType can't target an enum or a Nullable<T> directly — unwrap to the
                // underlying type (int for a non-nullable enum, T for Nullable<T>) before converting,
                // then let SetValue's implicit T → Nullable<T> boxing handle re-wrapping if needed.
                Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                object converted = targetType.IsEnum
                    ? Enum.Parse(targetType, raw!, ignoreCase: true)
                    : Convert.ChangeType(raw, targetType)!;
                property.SetValue(instance, converted);
            }
            catch (Exception e)
            {
                throw new UnityException(
                    $"[FTFoundation] Failed to apply config value for '{serviceType.Name}.{property.Name}': {e.Message}");
            }
        }

        // Derives the JSON section key from a service type:
        //   ConsoleLoggerService → strip "Service" → "ConsoleLogger" → lowercase first char → "consoleLogger"
        internal static string GetServiceKey(Type serviceType)
        {
            string name = serviceType.Name;
            if (name.EndsWith("Service", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "Service".Length);
            return LowercaseFirst(name);
        }

        internal static string LowercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToLower(s[0]) + s.Substring(1);
        }
    }
}
