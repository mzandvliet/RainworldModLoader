using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;

namespace DevToolsMod
{
    public static class DevToolsMod {
        public static void Initialize() {
            PatchHooks();
        }

        private static void PatchHooks() {
            var harmony = HarmonyInstance.Create("com.andrewfm.rainworld.mod.devtools");

            var start = typeof(RainWorld).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            var startTrans = typeof(DevToolsMod).GetMethod("RainWorld_Start_Trans");
            harmony.Patch(start, null, null, new HarmonyMethod(startTrans));

            var update = typeof(RainWorldGame).GetMethod("Update", BindingFlags.Instance | BindingFlags.Public);
            var updatePre = typeof(DevToolsMod).GetMethod("RainWorldGame_Update_Pre");
            harmony.Patch(update, new HarmonyMethod(updatePre), null);

            var ctor = typeof(RainWorldGame).GetConstructor(new [] { typeof(ProcessManager)});
            var ctorTrans = typeof(DevToolsMod).GetMethod("RainWorldGame_Ctor_Trans");
            harmony.Patch(ctor, null,  null, new HarmonyMethod(ctorTrans));

            Debug.Log("DevToolsMod Initialized");
        }

        public static void RainWorldGame_Update_Pre(RainWorldGame __instance) {
            Debug.Log("DevTools Active? " + __instance.setupValues.devToolsActive);
            Debug.Log("Build Type? " + __instance.rainWorld.buildType);
        }

        // TODO
        // In RainWorld.Start()
        // this.buildType = RainWorld.BuildType.Distribution;
        // change ldc.i4.0 to ldc.i4.2
        public static IEnumerable<CodeInstruction> RainWorld_Start_Trans(IEnumerable<CodeInstruction> instructions) {
            var codeInstructions = new List<CodeInstruction>(instructions);
            codeInstructions[1].operand = 2;
            return codeInstructions.AsEnumerable();
        }


        // In RainWorldGame.SetupValues Constructor:
        //  this.devToolsActive = true;
        public static IEnumerable<CodeInstruction> RainWorldGame_Ctor_Trans(IEnumerable<CodeInstruction> instructions) {
            var codeInstructions = new List<CodeInstruction>(instructions);
            
            codeInstructions.RemoveRange(145, 6); // Disable code that loads setup values by removing it
            codeInstructions.Insert(145, new CodeInstruction(OpCodes.Ldarg_0)); // load this
            codeInstructions.Insert(146, new CodeInstruction(OpCodes.Ldc_I4_1)); // load true


            return codeInstructions.AsEnumerable();

            // Compile and inspect this to see IL for setting member bool to a constant
            //    public class MyClass {
            //        private bool _myBool;
            //
            //        public void SetMyBool() {
            //            _myBool = true;
            //        }
            //    }
        }
    }


}
