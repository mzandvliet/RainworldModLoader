using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Harmony;
using UnityEngine;
using Debug = UnityEngine.Debug;

/* Todo:
 * - On new game, there is desynch because start time behaves differently from shelter load (cinematic or fade-in, probably)
 * - Everything is ok into region gates, but after that and possibly a shelter it stops working.
 * - Game exit from menu creates new replay file?
 * - Actually, taking shelter into a new day creates a new replay file!
 * - Multiple runs within a single launch of the game don't work, it just records the first.
 * - let user select run (separate recording / playback folders)
 * 
 * - Need to select replays that start from the same place we just loaded
 * This system is currently unaware of starting location
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
 */

namespace ReplayGhostMod {
    /// <summary>
    /// Replay Ghost Mod, by mzandvliet
    /// </summary>
    public static class ReplayGhostMod {
        private const string RecordingFolder = "Mods\\ReplayGhostMod\\Replays";

        private static AbstractCreature _player;
        private static ReplayGhost _ghost;
        private static ReplayGhostGraphics _ghostGraphics;

        private static TextWriter _writer;
        private static StreamReader _replay;

        private static int _ghostRoom;
        private static int _lastPlayerRoom;

        private static float _sessionStartTime;
        private static bool _playerWasInShortcut;

        private static float _readerTime;

        private static GhostState _lastGhostSnapshot;
        private static GhostState _currentGhostSnapshot;

        private static bool _ghostInPipe;

        public static void Initialize() {
            PatchHooks();
        }

        private static void PatchHooks() {
            Debug.Log("PATCHHOOKS YO");

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
            Debug.Log("RainWorldGame_Ctor_Post");
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
            Debug.Log("RainWorldGame_ExitGame_Pre");
            _ghostGraphics.Destroy();

            _writer.WriteLine($"f={GetSessionTime()}");
            _writer.Close();
            _writer.Dispose();
            _writer = null;
        }

        public static void RainWorldGame_Update_Post(RainWorldGame __instance) {
            Debug.Log("RainWorldGame_Update_Post");
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

                bool done = false;
                while (!_replay.EndOfStream && !done && _readerTime < GetSessionTime()) {
                    string line = _replay.ReadLine();
                    done = Parse(line);
                }

                if (_player.pos.room != _lastPlayerRoom) {
                    // Todo: I guess not need if already in the right room
                    MoveGhostSpriteToRoom(_ghostRoom);
                }
            }

            // Interpolate on-screen ghost state based on last two read values
            float lerp = (GetSessionTime() - _lastGhostSnapshot.Time) / (_currentGhostSnapshot.Time - _lastGhostSnapshot.Time);
            GhostState renderState = GhostState.Lerp(_lastGhostSnapshot, _currentGhostSnapshot, lerp);
            _ghost.Pos = renderState.Pos;
            _ghost.Rot = renderState.Rot;
        }

        private static bool Parse(string line) {
            var parts = line.Split(new[] { ":", "|", "," }, StringSplitOptions.RemoveEmptyEntries);

            _readerTime = float.Parse(parts[1]);

            string type = parts[0];
            if (type == "i") {
                OnGhostEnterShortcut();
                return false;
            }
            if (type == "o") {
                var state = new GhostState() {
                    Time = _readerTime,
                    Pos = ReadVector2(parts[2], parts[3]),
                    Rot = ReadVector2(parts[4], parts[5])
                };
                OnGhostExitShortcut(state);
                return false;
            }
            if (type == "r") {
                int room = int.Parse(parts[2]);
                OnGhostRoomChange(room);
                return false;
            }
            if (type == "t") {
                var state = new GhostState() {
                    Time = _readerTime,
                    Pos = ReadVector2(parts[2], parts[3]),
                    Rot = ReadVector2(parts[4], parts[5])
                };
                OnGhostTransformUpdate(state);
                return true;
            }
            return true;
        }

        private static Vector2 ReadVector2(string x, string y) {
            var v = new Vector2(
                float.Parse(x),
                float.Parse(y));
            return v;
        }

        private static void ClearGhostInterpolationState() {
            _lastGhostSnapshot.Pos = _currentGhostSnapshot.Pos;
            _lastGhostSnapshot.Rot = _currentGhostSnapshot.Rot;
            // Note: not for time, would cause div-by-zero in interpolation
        }

        private static void OnGhostEnterShortcut() {
            RemoveGhostSpriteFromRoom(_ghostRoom);
            _ghostInPipe = true;
        }

        private static void OnGhostExitShortcut(GhostState state) {
            AddGhostSpriteToActiveRoom(_ghostRoom);
            _currentGhostSnapshot = state;
            ClearGhostInterpolationState();
            _ghostInPipe = false;
        }

        private static void OnGhostRoomChange(int room) {
            MoveGhostSpriteToRoom(room);
        }

        private static void OnGhostTransformUpdate(GhostState state) {
            _lastGhostSnapshot = _currentGhostSnapshot;
            _currentGhostSnapshot = state;
        }

        private static void MoveGhostSpriteToRoom(int room) {
            if (!_ghostInPipe) {
                RemoveGhostSpriteFromRoom(_ghostRoom);
                AddGhostSpriteToActiveRoom(room);
            }
            
            _ghostRoom = room;
            ClearGhostInterpolationState();
        }

        private static void AddGhostSpriteToActiveRoom(int room) {
            AbstractRoom absRoom = _player.world.GetAbstractRoom(room);
            Room activeRoom = absRoom?.realizedRoom;
            activeRoom?.AddObject(_ghostGraphics);
        }

        private static void RemoveGhostSpriteFromRoom(int room) {
            AbstractRoom absRoom = _player.world.GetAbstractRoom(room);
            Room activeRoom = absRoom?.realizedRoom;
            activeRoom?.RemoveObject(_ghostGraphics);
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

            var time = GetSessionTime();
            var chunkPos = _player.realizedCreature.mainBodyChunk.pos;
            var chunkRot = _player.realizedCreature.mainBodyChunk.Rotation;

            // Handle shortcut entering, exiting
            bool inShortcut = _player.realizedCreature.inShortcut;
            if (inShortcut && !_playerWasInShortcut) {
                _writer.WriteLine($"i:{time}");
            }
            else if (!inShortcut && _playerWasInShortcut) {
                _writer.WriteLine($"o:{time}||{chunkPos.x},{chunkPos.y}|{chunkRot.x},{chunkRot.y}");
            }
            _playerWasInShortcut = inShortcut;


            // Handle woorldcoord changes
            if (_player.pos.room != _lastPlayerRoom) {
                _writer.WriteLine($"r:{time}|{_player.pos.room}");
                _lastPlayerRoom = _player.pos.room;
            }

            // Handle transform updates
            if (Time.frameCount % 4 == 0) {
                _writer.WriteLine($"t:{time}|{chunkPos.x},{chunkPos.y}|{chunkRot.x},{chunkRot.y}");
            }
        }

        private static float GetSessionTime() {
            return Time.time - _sessionStartTime;
        }

        private static string GetNewReplayFileName() {
            DateTime now = DateTime.Now;
            return $"Replay_{now.Day}-{now.Month}-{now.Year}-{now.Hour}-{now.Minute}-{now.Ticks}.txt";
        }

        #endregion

        private struct GhostState {
            public float Time;
            public Vector2 Pos;
            public Vector2 Rot;

            public static GhostState Lerp(GhostState a, GhostState b, float lerp) {
                return new GhostState() {
                    Time = Mathf.Lerp(a.Time, b.Time, lerp),
                    Pos = Vector2.Lerp(a.Pos, b.Pos, lerp),
                    Rot = Vector3.Slerp(a.Rot, b.Rot, lerp)
                };
            }
        }
    }
}
