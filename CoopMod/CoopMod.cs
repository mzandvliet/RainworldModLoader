using System;
using Harmony;
using UnityEngine;

/* Todo:
 * - Provide an in-game (or config file) way of specifying type, like the below code
 * 
 * - Implement a mechanic that improves the play experience for off-screen slugcats. Multiple options:
 *      1. Use a New Super Mario Bros style teleport, bringing the off-screen player to the same position as the on-screen player
 *      2. Use a picture-in-picture or split screen view to show off-screen slugcat what's happening
 *      Both options have very interesting ramifications.
 */

// In RainWorldGame constructor, inside the condition "if (this.setupValues.player2)":
// If you want Player 2 to be Survivor
//    state = new PlayerState(abstractCreature5, 1, 0, false)
// If you want Player 2 to be Monk
//    state = new PlayerState(abstractCreature5, 1, 1, false)
// If you want Player 2 to be Hunter
//    state = new PlayerState(abstractCreature5, 1, 2, false)
// If you want Player 2 to be Shadow
//    state = new PlayerState(abstractCreature5, 1, 3, false)

namespace CoopMod
{
    /// <summary>
    /// Co-op Mod, by OriginalSine
    /// </summary>
    public static class CoopMod
    {
        public static void Initialize() {
            PatchHooks();
        }

        private static void PatchHooks() {
            var harmony = HarmonyInstance.Create("com.originalsine.rainworld.mod.coopmod");

            var original = typeof(RainWorldGame).GetConstructor(new Type[] {typeof(ProcessManager)});
            var hook = typeof(CoopMod).GetMethod("RainWorldGame_CtorPre");
            harmony.Patch(original, new HarmonyMethod(hook), null);

            Debug.Log("Patched methods: ");
            var methods = harmony.GetPatchedMethods();
            foreach (var method in methods) {
                Debug.Log("  " + method);
            }
        }

        public static void RainWorldGame_CtorPre(RainWorldGame __instance, ProcessManager manager) {
            // note: using manager.rainWorld because __instance.rainWorld is still null at this point
            manager.rainWorld.setup.player2 = true;
        }
    }
}
