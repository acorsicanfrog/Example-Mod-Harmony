// ==================================================================================
// Harmony Patch: Custom Localization Entries
//
// This patch extends the game's `LocalizationManager.Translate` method to inject 
// custom translations for specific keys across multiple languages.
//
// Key Features:
// - Maintains a dictionary of custom translations per supported language.
// - Hooks into `LocalizationManager.Translate`:
//      • If a matching key exists in the custom dictionary, the translation is 
//        returned immediately (skipping the game’s default lookup).
//      • Falls back to English if the requested language does not contain the key.
//      • Otherwise, defers to the game’s original translation logic.
// - Example entries provided for EN, FR, RU, ES, DE, and CNS.
//
// Usage: 
// Add or modify entries in `_entries` to define new policies, items, or UI strings 
// in multiple languages. This ensures consistency and enables easy extension of 
// localized content without modifying the game’s base files.
// ==================================================================================

using HarmonyLib;
using System.Collections.Generic;

class Localization
{
    static readonly Dictionary<GameLanguages, Dictionary<string, string>> _entries = new Dictionary<GameLanguages, Dictionary<string, string>>
    {
        [GameLanguages.EN] = new Dictionary<string, string>
        {
            ["policy.name.Example Policy"] = "Example policy",
            ["policy.description.Example Policy"] = "This is the english description for this example policy.",
        },
        [GameLanguages.FR] = new Dictionary<string, string>
        {
            ["policy.name.Example Policy"] = "Police d'exemple",
            ["policy.description.Example Policy"] = "Voici la description française de la police d'exemple.",
        },
        [GameLanguages.RU] = new Dictionary<string, string>
        {
            ["policy.name.Example Policy"] = "Пример политики",
            ["policy.description.Example Policy"] = "Это описание на русском языке для этой примерной политики.",
        },
        [GameLanguages.ES] = new Dictionary<string, string>
        {
            ["policy.name.Example Policy"] = "Política de ejemplo",
            ["policy.description.Example Policy"] = "Esta es la descripción en español para esta política de ejemplo.",
        },
        [GameLanguages.DE] = new Dictionary<string, string>
        {
            ["policy.name.Example Policy"] = "Beispielrichtlinie",
            ["policy.description.Example Policy"] = "Dies ist die deutsche Beschreibung für diese Beispielrichtlinie.",
        },
        [GameLanguages.CNS] = new Dictionary<string, string>
        {
            ["policy.name.Example Policy"] = "示例政策",
            ["policy.description.Example Policy"] = "这是此示例政策的中文说明。",
        },
    };

    [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.Translate))]
    static class Patch_Translate
    {
        static bool Prefix(string p_key, ref string __result)
        {
            GameLanguages lang = PlayerSettings.GetInstance().Language;

            if (TryGet(lang, p_key, out var val))
            {
                __result = val;
                return false; // skip original, we provided the answer
            }

            return true; // let the game handle it
        }
    }

    static bool TryGet(GameLanguages p_lang, string p_key, out string o_value)
    {
        if (_entries.TryGetValue(p_lang, out var map) && map.TryGetValue(p_key, out o_value))
            return true;

        if (_entries.TryGetValue(GameLanguages.EN, out var en) && en.TryGetValue(p_key, out o_value))
            return true;

        o_value = null;
        return false;
    }
}