using System.Collections.Generic;
using Harmony;
using UnityEngine;

/* Todo:
 * - Incompatible with Co-op mod because hooks assume 1 slugcat
 * - Change hooks to receive a list of spawned slugcats, hook them once
 */

namespace CamoMod {
    /// <summary>
    /// Camo Slugat Mod, by LodeRunner
    /// </summary>
    public static class CamoMod {
        private static PlayerGraphics _graphics;
        private static RoomCamera.SpriteLeaser _hookedLeaser;
        private static RoomCamera _hookedRoomCamera;
        private static Player _player;
        private static List<Color> _bodyColors;
        private static Color _mainColor;
        private static float _camoIntens;
        private static Color _colorDelta;
        private static float _colorDeltaSum;
        private static Color _backGroundColor;
        private static Color _currentPlayerColor;
        private static float _camoPercent;
        private static float _camoPercentBalance;

        public static void Initialize() {
            _bodyColors = new List<Color>();
            _camoIntens = 1.48f;
            _camoPercentBalance = 8.5f;

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

        private static Color CalculateBodyColor() {
            _bodyColors.Clear();
            for (int i = 0; i < (int)_player.bodyChunks.Length; i++)
                _bodyColors.Add(_hookedRoomCamera.PixelColorAtCoordinate(_player.bodyChunks[i].pos));
            for (int j = 0; j < _bodyColors.Count; j++)
                _mainColor += _bodyColors[j];
            _mainColor = _mainColor / (_bodyColors.Count * _camoIntens);
            return _mainColor;
        }

        private static void CalculateCamoPercent() {
            _camoPercent = 100f - _colorDeltaSum * 100f / 3f;
        }

        private static void CalculateColorDeltaSum() {
            for (int i = 0; i < (int)_hookedLeaser.sprites.Length; i++)
                _currentPlayerColor += _hookedLeaser.sprites[i].color;
            _currentPlayerColor /= (float)((int)_hookedLeaser.sprites.Length);
            _colorDeltaSum = Mathf.Abs(_currentPlayerColor.r - _backGroundColor.r) + Mathf.Abs(_currentPlayerColor.g - _backGroundColor.g) + Mathf.Abs(_currentPlayerColor.b - _backGroundColor.b);
        }

        private static void CalculateVisibilityBonus() {
            _player.slugcatStats.generalVisibilityBonus = 0f - _camoPercent * 10f / 100f + _camoPercentBalance;
        }

        private static void ChangeColor() {
            for (int i = 0; i < (int)_hookedLeaser.sprites.Length; i++) {
                if (i != 9) {
                    _backGroundColor = CalculateBodyColor();
                    _hookedLeaser.sprites[i].color = new Color(Mathf.Lerp(_hookedLeaser.sprites[i].color.r, _backGroundColor.r, 0.03f), Mathf.Lerp(_hookedLeaser.sprites[i].color.g, _backGroundColor.g, 0.03f), Mathf.Lerp(_hookedLeaser.sprites[i].color.b, _backGroundColor.b, 0.03f));
                }
            }
        }

        #region DevTools

        private static void CamoIntensController() {
            if (Input.GetKeyDown((KeyCode)117))
                _camoIntens += 0.1f;
            if (Input.GetKeyDown((KeyCode)105))
                _camoIntens -= 0.1f;
        }

        private static void CamoPercentBalance() {
            if (Input.GetKeyDown((KeyCode)104))
                _camoPercentBalance += 0.1f;
            if (Input.GetKeyDown((KeyCode)106))
                _camoPercentBalance -= 0.1f;
        }

        #endregion

        #region Hooks

        public static void Player_UpdatePre(Player __instance) {
            _player = __instance;
        }

        public static void PlayerGraphics_UpdatePre(PlayerGraphics __instance) {
            _graphics = __instance;
            ChangeColor();
            CalculateColorDeltaSum();
            CalculateCamoPercent();
            CalculateVisibilityBonus();
        }
        
        public static void PlayerGraphics_DrawSpritesPost(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
            _hookedLeaser = sLeaser;
            _hookedRoomCamera = rCam;
        } 

        #endregion
    }
}

