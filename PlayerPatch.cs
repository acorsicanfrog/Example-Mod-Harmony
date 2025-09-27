// ==================================================================================
// Harmony Patch: Player Class
//
// This patch modifies the behavior of the `Player` class by intercepting and 
// extending several key gameplay methods.
//
// Key Features:
// - War Declaration Control (Prefix on AddWarWith):
//      • Blocks declaring war on Italy before 1940, showing a UI warning instead.
//      • Returns false to skip the original method when conditions are met.
// - War Rewards (Postfix on AddWarWith):
//      • If the USA declares war on Italy, grants 1000 money and adds 3 Light 
//        Infantry units to reserves with proper setup.
//      • Refreshes the deployment notification panel.
// - AI Manpower Growth Boost (Postfix on GetManpowerGrowth):
//      • Increases AI manpower growth based on difficulty level (x2 to x5).
//      • Logs the modified values for debugging.
// - Manpower Growth Override (Transpiler on GetManpowerGrowth):
//      • Replaces the hardcoded manpower growth rate constant (0.03f) with 0.5f.
//      • This affects all players globally.
//      • Includes debug logging when the transpiler applies successfully.
//
// Usage: 
// Demonstrates how to use Harmony Prefixes, Postfixes, and Transpilers to alter 
// strategic gameplay mechanics such as war declarations, rewards, and growth 
// rates, while preserving or bypassing vanilla behavior where appropriate.
// ==================================================================================

using System;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

class PlayerPatch
{
    // Patching the Player class
    [HarmonyPatch(typeof(Player))]
    static class Patch_Player
    {
        // Running our custom code before the game runs the the AddWarWith function
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.AddWarWith))]
        static bool Prefix(Player __instance, Player p_player, ref bool p_sendRPC)
        {
            // Example: Blocking declaration of war on specific player before specific date
            if (p_player != null && p_player.Country == "Italy")
            {
                // Simple date check
                if (GameData.Instance.map.Date.Year < 1940)
                {
                    // Showing a message on the UI to the user
                    UIManager.ShowMessage("You cannot go to war with Italy until 1940.");

                    return false; // skip the original method
                }
            }

            // Let vanilla function run
            return true;
        }

        // Running our custom code right after the game runs the the AddWarWith function
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Player.AddWarWith))]
        static void Postfix(Player __instance, Player p_player, bool p_sendRPC)
        {
            if (__instance.Country == "USA")
            {
                if (p_player.Country == "Italy")
                {
                    // Giving 1000 money to the USA player
                    __instance.AddMoney(1000, false);

                    // Adding 3 light infantry units to the USA reserve
                    for (int i = 0; i < 3; i++)
                    {
                        // Generating a new unit
                        Unit unit = Utils.GenerateNewUnit("Light infantry", "USA");

                        // Adding the unit to the US player army
                        __instance.AddUnit(unit, false);

                        // Sending the unit to the player's reserve
                        unit.IsReserve = true;

                        // Using the vanilla production time
                        unit.SetDefaultProductionTime();

                        // Or setting the value manually
                        // unit.TurnToProduce = 1;

                        ReservePanelGO.instance.RefreshDeployNotification();
                    }
                }
            }

            Debug.Log($"[WarDeclarationPatch] {__instance?.Name} war with {p_player?.Name} (RPC sent? {p_sendRPC})");
        }

        // Using regular Postfix to modify the manpower growth value
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Player.GetManpowerGrowth))]
        static void Postfix(Player __instance, ref int __result)
        {
            if (!__instance.IsComputer) // Only applying the patch to AI controlled players
                return;

            float modifier = 1f; // Modifier applied to base game manpower growth value

            switch (PlayerSettings.GetInstance().DifficultyLevel)
            {
                default:
                case 1:
                    modifier *= 2f; // Doubling base game manpower growth
                    break;

                case 2:
                    modifier *= 3f;  // Tripling base game manpower growth
                    break;

                case 3:
                    modifier *= 4f;  // Quadrupling base game manpower growth
                    break;

                case 4:
                    modifier *= 5f;  // Quintupling base game manpower growth
                    break;
            }

            int backupValue = __result;

            __result = (int)Math.Round((float)modifier * __result, MidpointRounding.ToEven);

            Debug.Log($"Manpower growth value for {__instance.Country} changed from {backupValue} to {__result}");
        }

        // Using transpiler to modify a very specific variable value (manpower growth value)
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Player.GetManpowerGrowth))]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            const float newManpowerGrowth = 0.5f;

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == System.Reflection.Emit.OpCodes.Ldc_R4 && (float)instruction.operand == 0.03f)
                {
                    instruction.operand = newManpowerGrowth; // Setting our own manpower growth value here, applies to absolutely everyone

                    Debug.Log($"[MOD:{ExampleMod.modName}] Patched {nameof(Player.GetManpowerGrowth)} with new value={newManpowerGrowth}");
                }

                yield return instruction;
            }
        }
    }
}