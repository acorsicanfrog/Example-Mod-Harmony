// ==================================================================================
// Harmony Patch: Policies & Units
//
// This patch introduces a custom policy into the game and modifies related 
// behaviors, such as icon handling and unit upkeep costs.
//
// Key Features:
// - Custom Policy Injection (Postfix on Policies.GeneratePolicies):
//      • Automatically adds `ExamplePolicy` to the list of available policies 
//        whenever the game generates them.
// - Custom Policy Icons (Prefix on Policies.GetPolicyIcon):
//      • Overrides the icon of `ExamplePolicy` with a sprite loaded from disk.
//      • Looks for an image in: <modInstallLocation>/Graphics/UI/Policies/<PolicyName>.png
//      • Falls back to default behavior if the custom sprite cannot be loaded.
// - Policy-Dependent Unit Upkeep (Postfix on Unit.GetUpkeepCost):
//      • Doubles the upkeep cost of infantry units if the owning or viewing player 
//        has the `ExamplePolicy` active.
//      • Ensures policy effects apply dynamically during gameplay.
//
// Usage: 
// Demonstrates how to extend the policy system with new content, 
// integrate custom UI assets, and apply policy-driven gameplay 
// modifications through Harmony patches.
// ==================================================================================

using System.IO;
using HarmonyLib;
using UnityEngine;

class PolicyPatch
{
    static readonly Policy ExamplePolicy = new Policy("Example Policy", "Description goes here.", Policies.Categories.Doctrines, (Policies.Type)99, 20);

    // Injecting our own custom policy every time the game tries to generate its list of policies
    [HarmonyPatch(typeof(Policies), nameof(Policies.GeneratePolicies))]
    static class Patch_GeneratePolicies
    {
        static void Postfix() => Policies.AddPolicy(ExamplePolicy);
    }

    // Telling the game to use our own icon when it tries to get a policy's icon
    [HarmonyPatch(typeof(Policies), nameof(Policies.GetPolicyIcon))]
    static class Patch_GetPolicyIcon
    {
        static bool Prefix(Policy p_policy, ref Sprite __result)
        {
            if (p_policy.Name != ExamplePolicy.Name) // Only handling our own policy, but you could override all base game's policies icons by removing this line
                return true;

            // Building path to file
            string fileName = p_policy.Name + ".png";
            string filePath = Path.Combine(ExampleMod.modInstallLocation, "Graphics", "UI", "Policies", fileName);

            // Loading the image from disk
            Sprite sprite = RuntimeImageLoader.LoadSprite(filePath, p_policy.Name, RuntimeImageLoader.SpriteLoadOptions.UI);

            // For some reason there is no generated sprite (error handling gracefully)
            if (sprite == null)
                return true; // Letting the game fall back to its default behavior

            __result = sprite;

            return false; // Use our icon
        }
    }

    // Patching the Unit class
    [HarmonyPatch(typeof(Unit))]
    class Patch_Unit
    {
        // Patching the upkeep cost calculation function
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Unit.GetUpkeepCost))]
        static void Postfix(Unit __instance, bool p_getReserveCost, ref int __result)
        {
            // Only affecting infantry units
            if (__instance.Type == "Infantry") // And only if our policy is active for the player viewing/owning the unit
                if (TurnManager.currPlayer != null && TurnManager.currPlayer.HasPolicy(ExamplePolicy.Type) || (__instance.OwnerPlayer != null && __instance.OwnerPlayer.HasPolicy(ExamplePolicy.Type)))
                    __result *= 2; // Doubling the upkeep cost
        }
    }
}