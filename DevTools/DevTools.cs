using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Harmony;
using UnityEngine;

/* Todo:
 * - Incorporate this into the mod loads
 * - Reimplement setup values as a dictionary that is mod-friendly
 * - And at the same time improves serialization
 */

namespace DevToolsMod
{
    /// <summary>
    /// Enabled the built-in develop tools functionality detailed here:
    /// http://rain-world-modding.wikia.com/wiki/Dev_Tools
    /// </summary>
    public static class DevToolsMod {
        public static void Initialize() {
            PatchHooks();
        }

        private static void PatchHooks() {
            var harmony = HarmonyInstance.Create("com.andrewfm.rainworld.mod.devtools");

            var start = typeof(RainWorld).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            var startTrans = typeof(DevToolsMod).GetMethod("RainWorld_Start_Trans");
            harmony.Patch(start, null, null, new HarmonyMethod(startTrans));

            Debug.Log("DevToolsMod patched in all its hooks and is good to go!");
        }
        
        /// <summary>
        /// Transpiles RainWorld.Start()
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> RainWorld_Start_Trans(IEnumerable<CodeInstruction> instructions) {
            var codeInstructions = new List<CodeInstruction>(instructions);
            
            // this.buildType = RainWorld.BuildType.Testing;
            codeInstructions[1].operand = RainWorld.BuildType.Testing; 

            // this.setup = DevTools.RainWorld_LoadSetupValues
            var loadSetupValuesMethod = AccessTools.Method(typeof(DevToolsMod), "RainWorld_LoadSetupValues", new[] { typeof(bool) });
            codeInstructions[43].operand = loadSetupValuesMethod;

            return codeInstructions.AsEnumerable();
        }

        /// <summary>
        /// A function that completely replaces RainWorld.LoadSetupValues()
        /// </summary>
        public static RainWorldGame.SetupValues RainWorld_LoadSetupValues(bool distributionBuild) {
            // Todo: make this mod friendly, use dictionary key/value etc.
            RainWorldGame.SetupValues values;

            if (distributionBuild || !System.IO.File.Exists(RWCustom.Custom.RootFolderDirectory() + "setup.txt")) {
                values = DefaultSetupValues;
            }
            else {
                var loadedValues = LoadSetupValues();
                values = CreateValues(loadedValues);
            }
            
            // Here's the only real change for now
            values.devToolsActive = true;

            return values;
        }

        private static int[] LoadSetupValues() {
            string[] lines = System.IO.File.ReadAllLines(RWCustom.Custom.RootFolderDirectory() + "setup.txt");
            int[] keys = new int[82];
            for (int i = 1; i < lines.Length; i++) {
                var keyId = GetKeyId(lines, i);
                if (keyId != -1) {
                    keys[keyId] = (int) short.Parse(Regex.Split(lines[i], ": ")[1]);
                }
                else {
                    Debug.Log("Couldn't find option: " + Regex.Split(lines[i], ": ")[0]);
                }
            }
            return keys;
        }

        private static int GetKeyId(string[] lines, int i) {
            int key = -1;
            string text = Regex.Split(lines[i], ": ")[0];
            switch (text) {
                case "player 2 active":
                    key = 0;
                    break;
                case "pink":
                    key = 1;
                    break;
                case "green":
                    key = 2;
                    break;
                case "blue":
                    key = 3;
                    break;
                case "white":
                    key = 4;
                    break;
                case "spears":
                    key = 5;
                    break;
                case "flies":
                    key = 6;
                    break;
                case "leeches":
                    key = 7;
                    break;
                case "snails":
                    key = 8;
                    break;
                case "vultures":
                    key = 9;
                    break;
                case "lantern mice":
                    key = 10;
                    break;
                case "cicadas":
                    key = 11;
                    break;
                case "palette":
                    key = 12;
                    break;
                case "lizard laser eyes":
                    key = 13;
                    break;
                case "player invincibility":
                    key = 14;
                    break;
                case "cycle time min in seconds":
                    key = 15;
                    break;
                case "cycle time max in seconds":
                    key = 59;
                    break;
                case "flies to win":
                    key = 16;
                    break;
                case "world creatures spawn":
                    key = 17;
                    break;
                case "don't bake":
                    key = 18;
                    break;
                case "widescreen":
                    key = 19;
                    break;
                case "start screen":
                    key = 20;
                    break;
                case "cycle startup":
                    key = 21;
                    break;
                case "full screen":
                    key = 22;
                    break;
                case "yellow":
                    key = 23;
                    break;
                case "red":
                    key = 24;
                    break;
                case "spiders":
                    key = 25;
                    break;
                case "player glowing":
                    key = 26;
                    break;
                case "garbage worms":
                    key = 27;
                    break;
                case "jet fish":
                    key = 28;
                    break;
                case "black":
                    key = 29;
                    break;
                case "sea leeches":
                    key = 30;
                    break;
                case "salamanders":
                    key = 31;
                    break;
                case "big eels":
                    key = 32;
                    break;
                case "default settings screen":
                    key = 33;
                    break;
                case "player 1 active":
                    key = 34;
                    break;
                case "deer":
                    key = 35;
                    break;
                case "dev tools active":
                    key = 36;
                    break;
                case "daddy long legs":
                    key = 37;
                    break;
                case "tube worms":
                    key = 38;
                    break;
                case "bro long legs":
                    key = 39;
                    break;
                case "tentacle plants":
                    key = 40;
                    break;
                case "pole mimics":
                    key = 41;
                    break;
                case "miros birds":
                    key = 42;
                    break;
                case "load game":
                    key = 43;
                    break;
                case "multi use gates":
                    key = 44;
                    break;
                case "temple guards":
                    key = 45;
                    break;
                case "centipedes":
                    key = 46;
                    break;
                case "world":
                    key = 47;
                    break;
                case "gravity flicker cycle min":
                    key = 48;
                    break;
                case "gravity flicker cycle max":
                    key = 49;
                    break;
                case "reveal map":
                    key = 50;
                    break;
                case "scavengers":
                    key = 51;
                    break;
                case "scavengers shy":
                    key = 52;
                    break;
                case "scavenger like player":
                    key = 53;
                    break;
                case "centiwings":
                    key = 54;
                    break;
                case "small centipedes":
                    key = 55;
                    break;
                case "load progression":
                    key = 56;
                    break;
                case "lungs":
                    key = 57;
                    break;
                case "play music":
                    key = 58;
                    break;
                case "cheat karma":
                    key = 60;
                    break;
                case "load all ambient sounds":
                    key = 61;
                    break;
                case "overseers":
                    key = 62;
                    break;
                case "ghosts":
                    key = 63;
                    break;
                case "fire spears":
                    key = 64;
                    break;
                case "scavenger lanterns":
                    key = 65;
                    break;
                case "always travel":
                    key = 66;
                    break;
                case "scavenger bombs":
                    key = 67;
                    break;
                case "the mark":
                    key = 68;
                    break;
                case "custom":
                    key = 69;
                    break;
                case "big spiders":
                    key = 70;
                    break;
                case "egg bugs":
                    key = 71;
                    break;
                case "single player character":
                    key = 72;
                    break;
                case "needle worms":
                    key = 73;
                    break;
                case "small needle worms":
                    key = 74;
                    break;
                case "spitter spiders":
                    key = 75;
                    break;
                case "dropwigs":
                    key = 76;
                    break;
                case "cyan":
                    key = 77;
                    break;
                case "king vultures":
                    key = 78;
                    break;
                case "log spawned creatures":
                    key = 79;
                    break;
                case "red centipedes":
                    key = 80;
                    break;
                case "proceed lineages":
                    key = 81;
                    break;
            }
            return key;
        }

        private static RainWorldGame.SetupValues CreateValues(int[] loadedValues) {
            return new RainWorldGame.SetupValues(
                player2: loadedValues[0] > 0,
                pink: loadedValues[1],
                green: loadedValues[2],
                blue: loadedValues[3],
                white: loadedValues[4],
                spears: loadedValues[5],
                flies: loadedValues[6],
                leeches: loadedValues[7],
                snails: loadedValues[8],
                vultures: loadedValues[9],
                lanternMice: loadedValues[10],
                cicadas: loadedValues[11],
                palette: loadedValues[12],
                lizardLaserEyes: loadedValues[13] != 0,
                invincibility: loadedValues[14] != 0,
                fliesToWin: loadedValues[16],
                worldCreaturesSpawn: loadedValues[17] == 1,
                dontBake: loadedValues[18] == 1,
                OBSwidescreen: loadedValues[19] == 1,
                startScreen: loadedValues[20] == 1,
                cycleStartUp: loadedValues[21] == 1,
                OBSfullscreen: loadedValues[22] == 1,
                yellows: loadedValues[23],
                reds: loadedValues[24],
                spiders: loadedValues[25],
                playerGlowing: loadedValues[26] == 1,
                garbageWorms: loadedValues[27],
                jetFish: loadedValues[28],
                black: loadedValues[29],
                seaLeeches: loadedValues[30],
                salamanders: loadedValues[31],
                bigEels: loadedValues[32],
                player1: loadedValues[34] == 1,
                defaultSettingsScreen: loadedValues[33],
                deers: loadedValues[35],
                devToolsActive: loadedValues[36] == 1,
                daddyLongLegs: loadedValues[37],
                tubeWorms: loadedValues[38],
                broLongLegs: loadedValues[39],
                tentaclePlants: loadedValues[40],
                poleMimics: loadedValues[41],
                mirosBirds: loadedValues[42],
                loadGame: loadedValues[43] == 1,
                multiUseGates: loadedValues[44] == 1,
                templeGuards: loadedValues[45],
                centipedes: loadedValues[46],
                world: loadedValues[47] == 1,
                gravityFlickerCycleMin: loadedValues[48],
                gravityFlickerCycleMax: loadedValues[49],
                revealMap: loadedValues[50] == 1,
                scavengers: loadedValues[51],
                scavengersShy: loadedValues[52],
                scavengersLikePlayer: loadedValues[53],
                centiWings: loadedValues[54],
                smallCentipedes: loadedValues[55],
                loadProg: loadedValues[56] == 1,
                lungs: loadedValues[57],
                playMusic: loadedValues[58] == 1,
                cycleTimeMin: loadedValues[15],
                cycleTimeMax: loadedValues[59],
                cheatKarma: loadedValues[60],
                loadAllAmbientSounds: loadedValues[61] == 1,
                overseers: loadedValues[62],
                ghosts: loadedValues[63],
                fireSpears: loadedValues[64],
                scavLanterns: loadedValues[65],
                alwaysTravel: loadedValues[66] == 1,
                scavBombs: loadedValues[67],
                theMark: loadedValues[68] == 1,
                custom: loadedValues[69],
                bigSpiders: loadedValues[70],
                eggBugs: loadedValues[71],
                singlePlayerChar: loadedValues[72],
                needleWorms: loadedValues[73],
                smallNeedleWorms: loadedValues[74],
                spitterSpiders: loadedValues[75],
                dropbugs: loadedValues[76],
                cyanLizards: loadedValues[77],
                kingVultures: loadedValues[78],
                logSpawned: loadedValues[79] == 1,
                redCentis: loadedValues[80],
                proceedLineages: loadedValues[81]);
        }

        private static readonly RainWorldGame.SetupValues DefaultSetupValues = new RainWorldGame.SetupValues(
            false, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, false, 4, true,
            true, false, true, true, false, 0, 0, 0, false, 0, 0, 0, 0, 0, 0, true, 0, 0, false, 0, 0, 0, 0, 0,
            0, true, false, 0, 0, true, 8, 18, false, 0, 0, 0, 0, 0, true, 128, true, 400, 800, 0, false, 0, 0,
            0, 0, false, 0, false, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, false, 0, 0);
    }
}
