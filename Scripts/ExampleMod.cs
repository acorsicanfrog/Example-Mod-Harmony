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

public class ExampleMod : GameModification
{
    public static string modName;
    public static string modInstallLocation;

    Harmony _harmony;

    public ExampleMod(Mod p_mod) : base(p_mod)
    {
        Log("Registering...");
    }

    /// <summary>
    /// Entry point, no more, no less
    /// </summary>
    /// <param name="p_mod"></param>
    public override void OnModInitialization(Mod p_mod)
    {
        Log("Initializing...");

        modName = Mod.Name;
        modInstallLocation = Mod.InstallLocation;

        ApplyPatches();
    }

    /// <summary>
    /// Removes all the modifications your mod applied to the game
    /// </summary>
    public override void OnModUnloaded()
    {
        Log("Unloading...");

        _harmony?.UnpatchAll(_harmony.Id);
    }

    /// <summary>
    /// Applies all of the modifications your mod is designed to perform
    /// </summary>
    void ApplyPatches()
    {
        Log("Applying patches...");

        _harmony = new Harmony("com.hexofsteel." + Mod.Name);
        _harmony.PatchAll();

        RuntimeImageLoader.Initialize();

        PlayerLogOpener.OpenPlayerLogLocation();

        UIExample.StartExample();
    }
}