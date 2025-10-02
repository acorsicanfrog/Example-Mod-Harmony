// ==================================================================================
// Harmony Patch: Custom Data Storage & Victory Points (Simplified)
//
// Shows how to embed mod-specific data into the game’s save system with 
// `GameData.ModDataBag`. This example tracks a custom “diplomacy score” per player, 
// rewarding points for kills and for capturing tiles.
//
// Key Features:
// - Unique Mod Data Key:
//      • Stores mod data under a unique key (e.g., "modAuthorName.modName.modData") 
//        to avoid conflicts between mods.
// - Patch: GameData.SetData (Prefix):
//      • Ensures `MyModData` exists when a game is initialized or loaded.  
//      • Initializes new data or validates existing data integrity.
// - Patch: UnitGO.Attack (Postfix):
//      • Awards points (`POINTS_PER_KILL`) when an enemy unit is destroyed.  
// - Patch: UnitGO.TryCaptureTile (Postfix):
//      • Awards points (`POINTS_PER_VP_CAPTURE`) when capturing a tile.  
//      • Applies an additional multiplier if the tile is a major objective.
// - MyModData (Serializable):
//      • Tracks diplomacy scores in a dictionary keyed by player name.  
//      • Provides methods to initialize, validate, and update scores.  
//      • Declares a win condition when a player reaches `MAX_DIPLOMATIC_SCORE`.  
//      • Displays an in-game message when the win condition is met.
// - Debugging & Feedback:
//      • Logs injections and point changes with `[MOD:<modName>]` prefix.  
//      • Warns and auto-registers missing players in the score dictionary.
//
// Usage:
// Use this approach to integrate lightweight, persistent custom systems into 
// saves. Keep stored data minimal to prevent bloated save files and maintain 
// smooth performance.
// ==================================================================================

using System;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

class CustomData
{
    // Your keys should be as unique as possible to prevent collision with other mods
    // If two keys are identical, they will be referencing the same underlying data and will be overwriting each other
    const string modDataKey = "modAuthorName.modName.modData"; // The text is an example and can be anything, as long as it is unique

    const int POINTS_PER_KILL = 1;
    const int POINTS_PER_VP_CAPTURE = 5;
    const int MAJOR_VP_CAPTURE_MODIFIER = 2;

    [HarmonyPatch(typeof(GameData))]
    class Patch_GameData
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameData.SetData))]
        static bool Patch_SetData(GameData p_gameData)
        {
            // Our mod data was already registered for that GameData instance, maybe we should check that it contains everything we need
            if (p_gameData.ModDataBag.TryGet(modDataKey, out MyModData o_myModData))
            {
                Debug.Log($"[MOD:{ExampleMod.modName}] Mod data is present!");

                o_myModData.CheckValidData(p_gameData);
            }
            // The GameData does not contain our mod data yet, we need to add it now
            else
            {
                Debug.Log($"[MOD:{ExampleMod.modName}] Injecting mod data into GameData...");

                MyModData myModData = new MyModData();
                myModData.InitializeData(p_gameData);

                p_gameData.ModDataBag.TrySet(modDataKey, myModData);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(UnitGO))]
    class Patch_UnitGO
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(UnitGO.AttackUnit))]
        static void Patch_AttackUnit(UnitGO __instance, UnitGO p_unitGO, bool p_isRetaliationPossible, bool p_isSupportPossible, bool p_isAmbush)
        {
            // Simple failsafes just in case
            if (__instance == null) return;
            if (__instance.unit == null) return;
            if (__instance.unit.OwnerPlayer == null) return;

            if (p_unitGO == null) return;
            if (p_unitGO.unit == null) return;

            // Target unit is still alive after the attack
            if (p_unitGO.unit.CurrHP > 0) return;

            if (GameData.Instance.ModDataBag.TryGet(modDataKey, out MyModData o_myModData))
                o_myModData.AddPointsTowardVictory(__instance.unit.OwnerPlayer, POINTS_PER_KILL);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(UnitGO.TryCaptureTile))]
        static void Patch_CaptureTile(UnitGO __instance, Tile p_tile, ref bool __result)
        {
            // Capturing the tile failed
            if (!__result) return;

            if (GameData.Instance.ModDataBag.TryGet(modDataKey, out MyModData o_myModData))
            {
                int points = POINTS_PER_VP_CAPTURE;

                // Applying a bonus for capturing a major victory point
                if (p_tile.PrimaryObjective)
                    points *= MAJOR_VP_CAPTURE_MODIFIER;

                o_myModData.AddPointsTowardVictory(__instance.unit.OwnerPlayer, points);
            }
        }
    }

    // The general data your mod will be storing in every game save file
    // Keep the data stored as lightweight as possible, as it will have an impact on the save file size, and saving/loading times
    [Serializable]
    class MyModData
    {
        const int MAX_DIPLOMATIC_SCORE = 10;

        // Keeping a 'diplomacy score' per player
        // Capturing victory points or destroying enemy units will increase 
        public Dictionary<string, int> playersDiplomacyScore = new Dictionary<string, int>();

        public void InitializeData(GameData p_gameData)
        {
            foreach (Player player in p_gameData.listOfPlayers)
            {
                if (!playersDiplomacyScore.ContainsKey(player.Name))
                    playersDiplomacyScore.Add(player.Name, 0);
            }
        }

        public void CheckValidData(GameData p_gameData)
        {
            // Check your data integrity here, make sure everything is properly initialized, etc...
            foreach (Player player in p_gameData.listOfPlayers)
            {
                if (!playersDiplomacyScore.ContainsKey(player.Name))
                    playersDiplomacyScore.Add(player.Name, 0);
            }
        }

        public void AddPointsTowardVictory(Player p_player, int p_amount)
        {
            if (p_player == null)
            {
                Debug.LogWarning("Player is null.");
                return;
            }

            Debug.Log($"[MOD:{ExampleMod.modName}] Adding {p_amount} diplomacy points to {p_player.Country}");

            playersDiplomacyScore[p_player.Name] += p_amount;

            if (playersDiplomacyScore[p_player.Name] >= MAX_DIPLOMATIC_SCORE)
            {
                // This should obviously be localized by your mod and not harcoded into english
                string message = LocalizationManager.GetLocalizedCountry(p_player.Country) + " has enough diplomacy points to force peace.";

                UIManager.ShowMessage(message);
            }
        }

        public bool HasWon(Player p_player)
        {
            if (p_player == null)
            {
                Debug.LogWarning("Player is null.");
                return false;
            }

            if (playersDiplomacyScore.TryGetValue(p_player.Name, out int o_points))
                return o_points >= MAX_DIPLOMATIC_SCORE;

            Debug.LogWarning($"[MOD:{ExampleMod.modName}] Player was not found. Registering player now.");

            playersDiplomacyScore.Add(p_player.Name, 0);

            return false;
        }
    }
}