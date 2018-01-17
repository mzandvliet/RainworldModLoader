using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using UnityEngine;

/// In RainWorldGame.SetupValues Constructor:
//    this.devToolsActive = true;
//
//// In RainWorld.Start()
//// IL Edit:
//        this.buildType = RainWorld.BuildType.Distribution;


// change ldc.i4.0 to ldc.i4.2

/* 
 * Note to self: can't patch RainWorld.Start IL if it is currently executing lol
 */

namespace DevToolsMod
{
    public static class DevToolsMod {
        public static void Initialize() {
            PatchHooks();
        }

        private static void PatchHooks() {
            var harmony = HarmonyInstance.Create("com.andrewfm.rainworld.mod.devtools");

            var target = typeof(RainWorldGame).GetConstructor(new Type[] { typeof(ProcessManager) });
            var hook = typeof(DevToolsMod).GetMethod("RainWorldGame_Ctor_Post");
            var transpiler = typeof(DevToolsMod).GetMethod("RainWorldGame_Ctor_Trans");
            
            harmony.Patch(target, null, new HarmonyMethod(hook), new HarmonyMethod(transpiler));
            
            Debug.Log("DevToolsMod Initialized");
        }

        public static void RainWorldGame_Ctor_Post(RainWorldGame __instance) {
            Debug.Log("CROOOTOTOTOROTOTOEOREOOER");
        }

        public static IEnumerable<CodeInstruction> RainWorldGame_Ctor_Trans(IEnumerable<CodeInstruction> instructions) {
//            var codeInstructions = instructions as IList<CodeInstruction> ?? instructions.ToList();
//            foreach (var instruction in codeInstructions) {
//                Debug.Log(instruction.ToString());
//            }
            return instructions;
        }
    }
}
