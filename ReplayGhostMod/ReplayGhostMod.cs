using System;
using System.IO;
using System.Reflection;
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
        private static ReplayGhost _ghost;
        private static ReplayGhostGraphics _ghostGraphics;

        private static TextWriter _writer;
        private static TextWriter _reader;

        private static StreamReader _replay;

        public static void Initialize() {
            PatchHooks();
        }

        private static void PatchHooks() {
            var harmony = HarmonyInstance.Create("com.mzandvliet.rainworld.mod.replayghostmod");

            var ctor = typeof(RainWorldGame).GetConstructor(new Type[] { typeof(ProcessManager) });
            var hook = typeof(ReplayGhostMod).GetMethod("RainWorldGame_Ctor_Post");
            harmony.Patch(ctor, null, new HarmonyMethod(hook));

            var update = typeof(RainWorldGame).GetMethod("Update", BindingFlags.Instance | BindingFlags.Public);
            hook = typeof(ReplayGhostMod).GetMethod("RainWorldGame_Update_Post");
            harmony.Patch(update, null, new HarmonyMethod(hook));

            var exitGame = typeof(RainWorldGame).GetMethod("ExitGame", BindingFlags.Instance | BindingFlags.NonPublic);
            hook = typeof(ReplayGhostMod).GetMethod("RainWorldGame_ExitGame_Pre");
            harmony.Patch(exitGame, new HarmonyMethod(hook), null);
        }

        public static void RainWorldGame_Ctor_Post(RainWorldGame __instance, ProcessManager manager) {
            _player = __instance.session.Players[0];

            if (!Directory.Exists(RecordingFolder)) {
                Directory.CreateDirectory(RecordingFolder);
            }
            
            var replays = Directory.GetFiles(RecordingFolder);
            var replay = replays[0];

            _replay = File.OpenText(replay);

            _writer = new StreamWriter(Path.Combine(RecordingFolder, GetNewReplayFileName()), false);

            /* 
             * Want to add ghost as abstractphysicalobject
             * Need to fix Realize so its extensible
             */

            _ghost = new ReplayGhost();
            _ghostGraphics = new ReplayGhostGraphics(_ghost);
            _player.Room.realizedRoom.AddObject(_ghostGraphics);
        }

        //private void ExitGame(bool asDeath, bool asQuit)
        public static void RainWorldGame_ExitGame_Pre(RainWorldGame __instance, bool asDeath, bool asQuit) {
            _writer.Close();
            // Todo: last line goes wrong
        }

        public static void RainWorldGame_Update_Post(RainWorldGame __instance) {
            if (_player?.realizedCreature == null) {
                return;
            }

            var worldCoord = _player.realizedCreature.coord.SaveToString();
            var chunkPos = _player.realizedCreature.mainBodyChunk.pos;

            if (!_replay.EndOfStream) {
                string line = _replay.ReadLine();
                _ghost.Pos = ReadPosition(line);
            }

            _writer.WriteLine($"{worldCoord}, ({chunkPos.x}, {chunkPos.y})");
        }

        private static Vector2 ReadPosition(string line) {
            var parts = line.Split(new [] {"", "(", ",", ")"}, StringSplitOptions.RemoveEmptyEntries);
            var v = new Vector2(
                float.Parse(parts[parts.Length - 2]),
                float.Parse(parts[parts.Length - 1]));
            return v;
        }



        private static string GetNewReplayFileName() {
            DateTime now = DateTime.Now;
            return $"Replay_{now.Day}-{now.Month}-{now.Year}-{now.Hour}-{now.Minute}-{now.Ticks}.txt";
        }
    }
}
