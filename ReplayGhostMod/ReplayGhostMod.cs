using System;
using System.IO;
using System.Reflection;
using Harmony;
using UnityEngine;

/* Todo:
 * - Instead of storing complete state every frame, store changes
 *      - this way you only log room on room change, and so on :)
 *      - how to reason about interpolating snapshots?
 *          - you can't interpolate a room change! I mean, classically.
 * - let user select run
 * - investigate frame timing. (we should probably timestamp and interpolate, instead of playing
 * back raw frames)
 * 
 * Interpolating looks bad. Not only do WoorldCoords not interpolate, but now shortcuts appear
 * messed up.
 * 
 * - It'd be nice to see the ghost using shortcuts, with correct color
 * _currentGhostRoom.BlinkShortCut();
 * - Could we still use abstractphysicalobject, and set its collision flags and such to false?
 *     - http://rain-world-modding.wikia.com/wiki/Adding_a_Custom_Creature
 *     - means we could make easy use of features like shortcuts
 * - Show the split times
 * - Implement the wait-for-player-to-catch-up mechanic, showing the splits
 * - Record and render more slugcat state
 * 
 * When exiting game from pause menu:
ObjectDisposedException: The object was used after being disposed.
System.IO.StreamWriter.Write (string) <IL 0x00015, 0x0005c>
System.IO.TextWriter.WriteLine (string) <IL 0x00002, 0x0002d>
ReplayGhostMod.ReplayGhostMod.WriteRecording () <IL 0x0017e, 0x0078f>
ReplayGhostMod.ReplayGhostMod.RainWorldGame_Update_Post (RainWorldGame) <IL 0x00022, 0x000da>
(wrapper dynamic-method) RainWorldGame.Update_Patch1 (object) <IL 0x0058d, 0x01354>
MainLoopProcess.RawUpdate (single) <IL 0x00027, 0x00081>
RainWorldGame.RawUpdate (single) <IL 0x00611, 0x01757>
ProcessManager.Update (single) <IL 0x00037, 0x000cc>
RainWorld.Update () <IL 0x0000b, 0x0003f>
 */



namespace ReplayGhostMod {
    /// <summary>
    /// Replay Ghost Mod, by mzandvliet
    /// </summary>
    public static class ReplayGhostMod {
        // Todo: LOCAL PATH
        private const string RecordingFolder =
            "D:\\Games\\SteamLibrary\\steamapps\\common\\Rain World\\Mods\\ReplayGhostMod\\Replays";

        private static AbstractCreature _player;
        private static ReplayGhost _ghost;
        private static ReplayGhostGraphics _ghostGraphics;

        private static TextWriter _writer;
        private static StreamReader _replay;

        private static int _ghostRoom;
        private static int _lastPlayerRoom;

        private static float _sessionStartTime;
        private static bool _playerWasInShortcut;

        private static float _lastReaderTime;

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
            
            LoadRecording();
            StartRecording();

            _sessionStartTime = Time.time;

            _ghost = new ReplayGhost();
            _ghostGraphics = new ReplayGhostGraphics(_ghost);
        }

        

        //private void ExitGame(bool asDeath, bool asQuit)
        public static void RainWorldGame_ExitGame_Pre(RainWorldGame __instance, bool asDeath, bool asQuit) {
            _ghostGraphics.Destroy();

            _writer.WriteLine($"f={GetSessionTime()}");
            _writer.Close();
            _writer.Dispose();
            _writer = null;
        }

        public static void RainWorldGame_Update_Post(RainWorldGame __instance) {
            if (_player?.realizedCreature == null) {
                return;
            }

            ReadRecording();
            WriteRecording();

            _lastPlayerRoom = _player.pos.room;
        }

        private static void LoadRecording() {
            var replays = Directory.GetFiles(RecordingFolder);
            if (replays.Length == 0) {
                Debug.Log("No recorded runs available");
                return;
            }

            var replay = replays[0];
            _replay = File.OpenText(replay);
        }

        private static void ReadRecording() {
            if (_replay != null && !_replay.EndOfStream) {

                while (!_replay.EndOfStream && _lastReaderTime < GetSessionTime()) {
                    string line = _replay.ReadLine();
                    Parse(line);
                }

                if (_player.pos.room != _lastPlayerRoom) {
                    MoveGhostSpriteToRoom(_player.pos.room);
                }
            }
        }

        private static void Parse(string line) {
            var parts = line.Split(new[] { ":", "|", "," }, StringSplitOptions.RemoveEmptyEntries);

            string type = parts[0];
            if (type == "i") {
                OnEnterShortcut();
            }
            else if (type == "o") {
                OnExitShortcut();
            }
            else if (type == "r") {
                int room = int.Parse(parts[1]);
                OnRoomChange(room);
            }
            else if (type == "t") {
                _lastReaderTime = float.Parse(parts[1]);
                var pos = ReadVector2(parts[2], parts[3]);
                var rot = ReadVector2(parts[4], parts[5]);
                OnTransformUpdate(pos, rot);
            }
        }

        private static Vector2 ReadVector2(string x, string y) {
            var v = new Vector2(
                float.Parse(x),
                float.Parse(y));
            return v;
        }

        private static void OnEnterShortcut() {
            RemoveSprite();
        }

        private static void OnExitShortcut() {
            AddSpriteToActiveRoom(_ghostRoom);
        }

        private static void OnRoomChange(int room) {
            MoveGhostSpriteToRoom(room);
        }

        private static void OnTransformUpdate(Vector2 pos, Vector2 rot) {
            _ghost.Pos = pos;
            _ghost.Rot = rot;
        }

        private static void MoveGhostSpriteToRoom(int room) {
            RemoveSprite();
            AddSpriteToActiveRoom(room);
            _ghostRoom = room;
        }

        private static void AddSpriteToActiveRoom(int room) {
            Room activeRoom = _player.world.GetAbstractRoom(room).realizedRoom;
            if (activeRoom != null) {
                activeRoom.AddObject(_ghostGraphics);
            }
        }

        private static void RemoveSprite() {
            if (_ghostGraphics.room != null) {
                _ghostGraphics.room.RemoveObject(_ghostGraphics);
            }
        }

        #region Record

        private static void StartRecording() {
            _writer = new StreamWriter(Path.Combine(RecordingFolder, GetNewReplayFileName()), false);
        }

        private static void WriteRecording() {
            if (_writer == null) {
                Debug.LogWarning("Attempting to write to file after writer is disposed");
                return;
            }

            // Handle shortcut entering, exiting
            bool inShortcut = _player.realizedCreature.inShortcut;
            if (inShortcut && !_playerWasInShortcut) {
                _writer.WriteLine($"i:{_player.realizedCreature.enteringShortCut}");
            }
            else if (!inShortcut && _playerWasInShortcut) {
                _writer.WriteLine($"o:");
            }
            _playerWasInShortcut = inShortcut;


            // Handle woorldcoord changes
            if (_player.pos.room != _lastPlayerRoom) {
                _writer.WriteLine($"r:{_player.pos.room}");
                _lastPlayerRoom = _player.pos.room;
            }

            // Handle transform updates
            var chunkPos = _player.realizedCreature.mainBodyChunk.pos;
            var chunkRot = _player.realizedCreature.mainBodyChunk.Rotation;
            var time = GetSessionTime();
            _writer.WriteLine($"t:{time}|{chunkPos.x},{chunkPos.y}|{chunkRot.x},{chunkRot.y}");
        }

        private static float GetSessionTime() {
            return Time.time - _sessionStartTime;
        }

        private static string GetNewReplayFileName() {
            DateTime now = DateTime.Now;
            return $"Replay_{now.Day}-{now.Month}-{now.Year}-{now.Hour}-{now.Minute}-{now.Ticks}.txt";
        }

        #endregion


    }
}
