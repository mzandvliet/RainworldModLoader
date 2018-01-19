using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            var methods = typeof(RainWorld).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            var target = typeof(RainWorld).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            var hook = typeof(DevToolsMod).GetMethod("RainWorld_Start_Pre");
            var transpiler = typeof(DevToolsMod).GetMethod("RainWorld_Start_Trans");
            
            harmony.Patch(target, null, new HarmonyMethod(hook), new HarmonyMethod(transpiler));
            
            Debug.Log("DevToolsMod Initialized");
        }

        public static void RainWorld_Start_Pre(RainWorld __instance) {
            Debug.Log("CROOOTOTOTOROTOTOEOREOOER");
        }

        public static IEnumerable<CodeInstruction> RainWorld_Start_Trans(IEnumerable<CodeInstruction> instructions) {
            var codeInstructions = instructions as IList<CodeInstruction> ?? instructions.ToList();
            foreach (var instruction in codeInstructions) {
                Debug.Log(instruction.ToString());
            }
            return instructions;
        }
    }
}
