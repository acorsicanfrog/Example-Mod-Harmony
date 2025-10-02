// ==================================================================================
// Harmony Patch: AI Class
//
// This patch demonstrates how to access and modify private fields of the AI class 
// using Harmony’s `AccessTools.FieldRef` utilities. It alters AI decision-making 
// logic by intercepting internal state after key methods are executed.
//
// Key Features:
// - Private Field Access:
//      • `_numberOfEnemyVPs` (int) — tracks the number of enemy victory points.
//      • `_areWeWinning` (bool) — indicates whether the AI considers itself 
//        to be winning a war.
// - Postfix on GatherEnemyVictoryPoints:
//      • Logs the base game’s calculated enemy VP total.
//      • Overrides the value with 999 for testing/demo purposes.
// - Postfix on CheckWinningWar:
//      • Logs whether the AI believes it is winning.
//      • Forces the AI to always consider itself winning by setting the field to true.
//
// Usage:
// Serves as an example of manipulating private AI state variables to alter 
// gameplay behavior or for debugging/testing AI logic. Replace the hardcoded 
// overrides with conditional logic for more nuanced AI modifications.
// ==================================================================================

using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(AI))]
class Patch_AI
{
    // Most of the AI class variables/methods are marked as PRIVATE, therefore, we need to get a reference to them like so
    static readonly AccessTools.FieldRef<AI, int> _enemyVPsRef = AccessTools.FieldRefAccess<AI, int>("_numberOfEnemyVPs");
    static readonly AccessTools.FieldRef<AI, bool> _winningWarRef = AccessTools.FieldRefAccess<AI, bool>("_areWeWinning");

    [HarmonyPostfix, HarmonyPatch("GatherEnemyVictoryPoints")]
    static void Patch_Post_GatherEnemyVictoryPoints(AI __instance)
    {
        ref int VPs = ref _enemyVPsRef(__instance); // One way to store the variable for numerous re use within this method

        Debug.Log($"Enemy VPs after base game method ran: {VPs}");

        _enemyVPsRef(__instance) = 999; // Another way of directly modifying the value of the variable

        Debug.Log($"Enemy VPs after our modification: {_enemyVPsRef(__instance)}");
    }

    [HarmonyPostfix, HarmonyPatch("CheckWinningWar")]
    static void Patch_Post_CheckWinningWar(AI __instance)
    {
        Debug.Log($"Winning war after base game method ran: {_winningWarRef(__instance)}");

        _winningWarRef(__instance) = true;

        Debug.Log($"Winning war after our modification: {_winningWarRef(__instance)}");
    }
}