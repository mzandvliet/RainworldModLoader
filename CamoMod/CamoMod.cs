using System.Collections.Generic;
using Harmony;
using UnityEngine;

/* Todo:
 * - Incompatible with Co-op mod because hooks assume 1 slugcat
 * - Change hooks to receive a list of spawned slugcats, hook them once
 */

namespace CamoMod {
    public class CamoSlugcatState {
        public RoomCamera.SpriteLeaser _hookedLeaser;
        public RoomCamera _hookedRoomCamera;
        public Player _player;
        public List<Color> _bodyColors;
        public Color _mainColor;
        public float _camoIntens;
        public Color _colorDelta;
        public float _colorDeltaSum;
        public Color _backGroundColor;
        public Color _currentPlayerColor;
        public float _camoPercent;
        public float _camoPercentBalance;

        public CamoSlugcatState() {
            _bodyColors = new List<Color>();
            _camoIntens = 1.48f;
            _camoPercentBalance = 8.5f;
        }
    }
    /// <summary>
    /// Camo Slugat Mod, by LodeRunner
    /// </summary>
    public static class CamoMod {
        private static Dictionary<PlayerGraphics, CamoSlugcatState> _slugcats = new Dictionary<PlayerGraphics, CamoSlugcatState>();

        public static void Initialize() {
           

            PatchHooks();

            Debug.Log("CamoMod initialized");
        }

        // Todo: streamline this a little
        private static void PatchHooks() {
            var harmony = HarmonyInstance.Create("com.loderunner.rainworld.mod.camomod");

            var original = typeof(Player).GetMethod("Update");
            var hook = typeof(CamoMod).GetMethod("Player_UpdatePre");
            harmony.Patch(original, new HarmonyMethod(hook), null);

            original = typeof(PlayerGraphics).GetMethod("Update");
            hook = typeof(CamoMod).GetMethod("PlayerGraphics_UpdatePre");
            harmony.Patch(original, new HarmonyMethod(hook), null);

            original = typeof(PlayerGraphics).GetMethod("DrawSprites");
            hook = typeof(CamoMod).GetMethod("PlayerGraphics_DrawSpritesPost");
            harmony.Patch(original, null, new HarmonyMethod(hook));


            Debug.Log("Patched methods: ");
            var methods = harmony.GetPatchedMethods();
            foreach (var method in methods) {
                Debug.Log("  " + method);
            }
        }

        private static Color CalculateBodyColor(CamoSlugcatState state) {
            state._bodyColors.Clear();
            for (int i = 0; i < (int)state._player.bodyChunks.Length; i++)
                state._bodyColors.Add(state._hookedRoomCamera.PixelColorAtCoordinate(state._player.bodyChunks[i].pos));
            for (int j = 0; j < state._bodyColors.Count; j++)
                state._mainColor += state._bodyColors[j];
            state._mainColor = state._mainColor / (state._bodyColors.Count * state._camoIntens);
            return state._mainColor;
        }

        private static void CalculateCamoPercent(CamoSlugcatState state) {
            state._camoPercent = 100f - state._colorDeltaSum * 100f / 3f;
        }

        private static void CalculateColorDeltaSum(CamoSlugcatState state) {
            for (int i = 0; i < (int)state._hookedLeaser.sprites.Length; i++)
                state._currentPlayerColor += state._hookedLeaser.sprites[i].color;
            state._currentPlayerColor /= (float)((int)state._hookedLeaser.sprites.Length);
            state._colorDeltaSum = Mathf.Abs(state._currentPlayerColor.r - state._backGroundColor.r) + Mathf.Abs(state._currentPlayerColor.g - state._backGroundColor.g) + Mathf.Abs(state._currentPlayerColor.b - state._backGroundColor.b);
        }

        private static void CalculateVisibilityBonus(CamoSlugcatState state) {
            state._player.slugcatStats.generalVisibilityBonus = 0f - state._camoPercent * 10f / 100f + state._camoPercentBalance;
        }

        private static void ChangeColor(CamoSlugcatState state) {
            for (int i = 0; i < (int)state._hookedLeaser.sprites.Length; i++) {
                if (i != 9) {
                    state._backGroundColor = CalculateBodyColor(state);
                    state._hookedLeaser.sprites[i].color = new Color(Mathf.Lerp(state._hookedLeaser.sprites[i].color.r, state._backGroundColor.r, 0.03f), Mathf.Lerp(state._hookedLeaser.sprites[i].color.g, state._backGroundColor.g, 0.03f), Mathf.Lerp(state._hookedLeaser.sprites[i].color.b, state._backGroundColor.b, 0.03f));
                }
            }
        }

        #region DevTools

//        private static void CamoIntensController() {
//            if (Input.GetKeyDown((KeyCode)117))
//                _camoIntens += 0.1f;
//            if (Input.GetKeyDown((KeyCode)105))
//                _camoIntens -= 0.1f;
//        }
//
//        private static void CamoPercentBalance() {
//            if (Input.GetKeyDown((KeyCode)104))
//                _camoPercentBalance += 0.1f;
//            if (Input.GetKeyDown((KeyCode)106))
//                _camoPercentBalance -= 0.1f;
//        }

        #endregion

        #region Hooks

        public static void Player_UpdatePre(Player __instance) {
            PlayerGraphics g = (PlayerGraphics)__instance.graphicsModule;
            if (g == null) {
                Debug.LogError("Couldn't get PlayerGraphics from Player");
                return;
            }
            if (!_slugcats.ContainsKey(g)) {
                _slugcats.Add(g, new CamoSlugcatState());
            }

            _slugcats[g]._player = __instance;
        }

        public static void PlayerGraphics_DrawSpritesPost(PlayerGraphics __instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
            if (!_slugcats.ContainsKey(__instance)) {
                _slugcats.Add(__instance, new CamoSlugcatState());
            }
            CamoSlugcatState s = _slugcats[__instance];

            s._hookedLeaser = sLeaser;
            s._hookedRoomCamera = rCam;
        }

        public static void PlayerGraphics_UpdatePre(PlayerGraphics __instance) {
            if (!_slugcats.ContainsKey(__instance)) {
                _slugcats.Add(__instance, new CamoSlugcatState());
            }
            CamoSlugcatState s = _slugcats[__instance];

            ChangeColor(s);
            CalculateColorDeltaSum(s);
            CalculateCamoPercent(s);
            CalculateVisibilityBonus(s);
        }

        #endregion
    }
}

