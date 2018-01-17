using System.Collections.Generic;
using Harmony;
using UnityEngine;

/* 
 * Changes:
 * - Using Harmony patcher to insert hooks
 * - Encapsulated all slugcat camo state to make this mod compatible with coopmod, which features
 * multiple slugcat instances.
 */

namespace CamoMod {
    public class SlugcatCamoState {
        public RoomCamera.SpriteLeaser SpriteLeaser;
        public RoomCamera RoomCamera;
        public Player Player;
        public List<Color> BodyColors;
        public Color MainColor;
        public float CamoIntens;
        public Color ColorDelta;
        public float ColorDeltaSum;
        public Color BackGroundColor;
        public Color CurrentPlayerColor;
        public float CamoPercent;
        public float CamoPercentBalance;

        public SlugcatCamoState() {
            BodyColors = new List<Color>();
            CamoIntens = 1.48f;
            CamoPercentBalance = 8.5f;
        }
    }
    /// <summary>
    /// Camo Slugat Mod, by LodeRunner
    /// </summary>
    public static class CamoMod {
        private static readonly Dictionary<PlayerGraphics, SlugcatCamoState> CamoStates =
            new Dictionary<PlayerGraphics, SlugcatCamoState>();

        public static void Initialize() {
            PatchHooks();
            Debug.Log("CamoMod initialized");
        }

        // Todo: streamline this a little
        private static void PatchHooks() {
            var harmony = HarmonyInstance.Create("com.loderunner.rainworld.mod.camomod");

            var original = typeof(Player).GetMethod("Update");
            var hook = typeof(CamoMod).GetMethod("Player_Update_Pre");
            harmony.Patch(original, new HarmonyMethod(hook), null);

            original = typeof(PlayerGraphics).GetMethod("Update");
            hook = typeof(CamoMod).GetMethod("PlayerGraphics_Update_Pre");
            harmony.Patch(original, new HarmonyMethod(hook), null);

            original = typeof(PlayerGraphics).GetMethod("DrawSprites");
            hook = typeof(CamoMod).GetMethod("PlayerGraphics_DrawSprites_Post");
            harmony.Patch(original, null, new HarmonyMethod(hook));


            Debug.Log("Patched methods: ");
            var methods = harmony.GetPatchedMethods();
            foreach (var method in methods) {
                Debug.Log("  " + method);
            }
        }

        private static Color CalculateBodyColor(SlugcatCamoState s) {
            s.BodyColors.Clear();
            for (int i = 0; i < (int) s.Player.bodyChunks.Length; i++) {
                s.BodyColors.Add(s.RoomCamera.PixelColorAtCoordinate(s.Player.bodyChunks[i].pos));
            }
            for (int j = 0; j < s.BodyColors.Count; j++) {
                s.MainColor += s.BodyColors[j];
            }
            s.MainColor = s.MainColor / (s.BodyColors.Count * s.CamoIntens);
            return s.MainColor;
        }

        private static void CalculateCamoPercent(SlugcatCamoState s) {
            s.CamoPercent = 100f - s.ColorDeltaSum * 100f / 3f;
        }

        private static void CalculateColorDeltaSum(SlugcatCamoState s) {
            for (int i = 0; i < s.SpriteLeaser.sprites.Length; i++)
                s.CurrentPlayerColor += s.SpriteLeaser.sprites[i].color;
            s.CurrentPlayerColor /= s.SpriteLeaser.sprites.Length;
            s.ColorDeltaSum = 
                Mathf.Abs(s.CurrentPlayerColor.r - s.BackGroundColor.r) +
                Mathf.Abs(s.CurrentPlayerColor.g - s.BackGroundColor.g) +
                Mathf.Abs(s.CurrentPlayerColor.b - s.BackGroundColor.b);
        }

        private static void CalculateVisibilityBonus(SlugcatCamoState s) {
            s.Player.slugcatStats.generalVisibilityBonus = 0f - s.CamoPercent * 10f / 100f + s.CamoPercentBalance;
        }

        private static void ChangeColor(SlugcatCamoState state) {
            for (int i = 0; i < state.SpriteLeaser.sprites.Length; i++) {
                if (i != 9) {
                    state.BackGroundColor = CalculateBodyColor(state);
                    state.SpriteLeaser.sprites[i].color = new Color(
                        Mathf.Lerp(state.SpriteLeaser.sprites[i].color.r, state.BackGroundColor.r, 0.03f),
                        Mathf.Lerp(state.SpriteLeaser.sprites[i].color.g, state.BackGroundColor.g, 0.03f),
                        Mathf.Lerp(state.SpriteLeaser.sprites[i].color.b, state.BackGroundColor.b, 0.03f));
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

        public static void Player_Update_Pre(Player __instance) {
            PlayerGraphics g = (PlayerGraphics)__instance.graphicsModule;
            SlugcatCamoState s = GetOrCreateCamoState(g);

            s.Player = __instance;
        }

        public static void PlayerGraphics_DrawSprites_Post(PlayerGraphics __instance,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {

            SlugcatCamoState s = GetOrCreateCamoState(__instance);

            s.SpriteLeaser = sLeaser;
            s.RoomCamera = rCam;
        }

        public static void PlayerGraphics_Update_Pre(PlayerGraphics __instance) {
            SlugcatCamoState s = GetOrCreateCamoState(__instance);

            ChangeColor(s);
            CalculateColorDeltaSum(s);
            CalculateCamoPercent(s);
            CalculateVisibilityBonus(s);
        }

        private static SlugcatCamoState GetOrCreateCamoState(PlayerGraphics g) {
            if (!CamoStates.ContainsKey(g)) {
                CamoStates.Add(g, new SlugcatCamoState());
            }
            return CamoStates[g];
        }

        #endregion
    }
}

