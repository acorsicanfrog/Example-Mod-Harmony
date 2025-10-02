// ==================================================================================
// Example Mod Entry Point
//
// Provides a basic template for integrating a mod into the game’s lifecycle using 
// the `GameModification` base class and Harmony for runtime patching.
//
// Key Features:
// - Mod Initialization (OnModInitialization):
//      • Stores the mod’s install location for later use (e.g., loading assets).
//      • Applies all Harmony patches defined in this mod assembly.
// - Mod Unloading (OnModUnloaded):
//      • Safely reverts all patches applied by this mod via `UnpatchAll`.
// - Harmony Integration:
//      • Uses a unique Harmony ID (`com.hexofsteel.<modName>`) for scoping patches.
//      • Calls `PatchAll()` to apply every Harmony patch in the assembly.
// - Logging:
//      • Provides detailed debug logs for registration, initialization, patching, 
//        and unloading steps to ease development and troubleshooting.
//
// Usage:
// Extend this class as a starting point when creating new mods.  
// Add Harmony patch classes elsewhere in your project, and they will be 
// automatically applied when the mod is initialized.
// ==================================================================================

using HarmonyLib;
using UnityEngine;

public class ExampleMod : GameModification
{
    public static string modName;
    public static string modInstallLocation;

    Harmony _harmony;

    public ExampleMod(Mod p_mod) : base(p_mod)
    {
        Debug.Log($"[MOD:{p_mod.name}] Registering...");
    }

    public override void OnModInitialization(Mod p_mod)
    {
        Debug.Log($"[MOD:{p_mod.name}] Initializing...");

        mod = p_mod;

        modName = mod.name;
        modInstallLocation = mod.installLocation;

        ApplyPatches();
    }

    public override void OnModUnloaded()
    {
        Debug.Log($"[MOD:{modName}] Unloading...");

        _harmony?.UnpatchAll(_harmony.Id);
    }

    /// <summary>
    /// Applying all of our patches to the game
    /// </summary>
    void ApplyPatches()
    {
        Debug.Log($"[MOD:{modName}] Applying Patches...");

        _harmony = new Harmony("com.hexofsteel." + mod.name);
        _harmony.PatchAll();

        RuntimeImageLoader.Initialize();

        PlayerLogOpener.OpenPlayerLogLocation();

        UIExample.Initialize();
        UIExample.CreateExampleMenu();
    }
}