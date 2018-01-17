using System;
using System.IO;
using Harmony;
using UnityEngine;

namespace ReplayGhostMod {
    /// <summary>
    /// Replay Ghost Mod, by mzandvliet
    /// </summary>
    public static class ReplayGhostMod {
        private const string RecordingFolder =
            "D:\\Games\\SteamLibrary\\steamapps\\common\\Rain World\\Mods\\ReplayGhostMod\\Replays";

        private static AbstractCreature _player;
        private static TextWriter _writer;

        public static void Initialize() {
            PatchHooks();
        }

        private static void PatchHooks() {
            var harmony = HarmonyInstance.Create("com.mzandvliet.rainworld.mod.replayghostmod");

            var ctor = typeof(RainWorldGame).GetConstructor(new Type[] { typeof(ProcessManager) });
            var hook = typeof(ReplayGhostMod).GetMethod("RainWorldGame_Ctor_Post");
            harmony.Patch(ctor, null, new HarmonyMethod(hook));

            var update = typeof(RainWorldGame).GetMethod("Update");
            hook = typeof(ReplayGhostMod).GetMethod("RainWorldGame_Update_Post");
            harmony.Patch(update, null, new HarmonyMethod(hook));

            var exitGame = typeof(RainWorldGame).GetMethod("ExitGame");
            hook = typeof(ReplayGhostMod).GetMethod("RainWorldGame_ExitGame_Pre");
            harmony.Patch(exitGame, new HarmonyMethod(hook), null);
        }

        public static void RainWorldGame_Ctor_Post(RainWorldGame __instance, ProcessManager manager) {
            _player = __instance.session.Players[0];

            if (!Directory.Exists(RecordingFolder)) {
                Directory.CreateDirectory(RecordingFolder);
            }
            _writer = new StreamWriter(Path.Combine(RecordingFolder, GetNewReplayFileName()), false);
        }

        private static string GetNewReplayFileName() {
            DateTime now = DateTime.Now;
            return $"Replay_{now.Day}-{now.Month}-{now.Year}-{now.Hour}-{now.Minute}-{now.Ticks}.txt";
        }

        // Todo: Make sure this gets called, and that the writer closes before any new recording starts
        public static void RainWorldGame_ExitGame_Pre(RainWorldGame __instance, ProcessManager manager) {
            _player = __instance.session.Players[0];

            _writer.Close();
        }

        public static void RainWorldGame_Update_Post(RainWorldGame __instance) {
            _writer.WriteLine(_player.realizedCreature.coord.SaveToString());
        }
    }
}
