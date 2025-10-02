// ==================================================================================
// Harmony Patch: LocalizationManager (Button Labels)
//
// This patch modifies `LocalizationManager.SetActiveLocale` to replace default 
// button labels with custom strings whenever a new locale becomes active.
//
// Key Features:
// - Postfix on SetActiveLocale:
//      • Runs after the game applies the selected locale.
//      • Overrides key-value pairs in `p_locale` for specific UI buttons.
// - Supported Languages:
//      • English (EN): Replaces Play, Editors, Mods, Settings, Quit with numbered variants.
//      • French (FR): Provides equivalent replacements with French translations.
// - Error Handling:
//      • Wraps modifications in a try/catch block.
//      • Logs and displays a UI message if localization patching fails.
// - Debug Logging:
//      • Outputs a confirmation message whenever the patch executes.
//
// Usage:
// Extend or modify the key-value assignments to customize UI labels per language.  
// Ideal for modders who want to alter in-game text dynamically without modifying 
// original localization files.
// ==================================================================================

using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(LocalizationManager))]
static class Patch_LocalizationManager
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(LocalizationManager.SetActiveLocale))]
    static void Patch_Post_SetActiveLocale(Locale_Base p_locale)
    {
        GameLanguages lang = PlayerSettings.GetInstance().Language;

        try
        {
            // Replacing the localization values when english becomes the active language
            if (lang == GameLanguages.EN)
            {
                p_locale.keyValuePairs["btn-play"] = "(1) PLAY (1)";
                p_locale.keyValuePairs["btn-editors"] = "(2) EDITORS (2)";
                p_locale.keyValuePairs["btn-mods"] = "(3) MODS (3)";
                p_locale.keyValuePairs["btn-settings"] = "(4) SETTINGS (4)";
                p_locale.keyValuePairs["btn-quit"] = "(5) QUIT (5)";
            }
            else if (lang == GameLanguages.FR)
            {
                p_locale.keyValuePairs["btn-play"] = "(1) JOUER (1)";
                p_locale.keyValuePairs["btn-editors"] = "(2) ÉDITEURS (2)";
                p_locale.keyValuePairs["btn-mods"] = "(3) MODS (3)";
                p_locale.keyValuePairs["btn-settings"] = "(4) PARAMÈTRES (4)";
                p_locale.keyValuePairs["btn-quit"] = "(5) QUITTER (5)";
            }
        }
        catch (System.Exception ex)
        {
            string message = "Could not patch localization file: " + ex.ToString();

            Debug.LogError(message);

            UIManager.ShowMessage(message);
        }

        Debug.Log($"[MOD:{ExampleMod.modName}] Patching {nameof(LocalizationManager.SetActiveLocale)}");
    }
}