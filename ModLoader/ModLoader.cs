using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

/* 
 * First we should by default create a logger for mod stuff
 */

namespace Modding {
    public static class ModLoader {
        private static readonly List<IMod> _loadedMods = new List<IMod>();

        public static void Initialize(RainWorld rainworld) {
            ModLogger.EnableLogging();
            Debug.Log("Yay, we're in the mod loader! Rainworld version: " + rainworld.gameVersion);
            
            // Iterate over mods, load them in order

            string modsPath = Path.Combine(GetGameRootPath(), "Mods");
            var modDirs = Directory.GetDirectories(modsPath);

            for (int i = 0; i < modDirs.Length; i++) {
                var assembly = LoadModAssemblyFromDirectory(modDirs[i]);
                if (assembly != null) {
                    var mod = LoadModFromAssembly(assembly);
                    if (mod != null) {
                        Debug.Log("Initializing this mod...");
                        _loadedMods.Add(mod);
                        mod.Init(rainworld);
                    }
                    else {
                        Debug.LogError("Failed to load mod from assembly, skipping...");
                    }
                }
                else {
                    Debug.LogError("Failed to load mod assembly, skipping...");
                }
            }

            Debug.Log("Loaded mods: ");
            for (int i = 0; i < _loadedMods.Count; i++) {
                Debug.Log("" + i + ": " + _loadedMods[i]);
            }
        }

        private static string GetGameRootPath() {
            // GetExecutingAssembly().location gives managed assembly dir, so root is two dirs up
            var directoryInfo = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).Parent;
            string path = directoryInfo?.ToString();

            return path;
        }

        private static Assembly LoadModAssemblyFromDirectory(string path) {
            var files = Directory.GetFiles(path);
            for (int i = 0; i < files.Length; i++) {
                if (files[i].Contains("Mod.dll")) {
                    Debug.Log("Found Mod Assembly: " + files[i]);
                    var assembly = Assembly.LoadFrom(files[i]);
                    return assembly;
                }
            }
            
            return null;
        }

        private static IMod LoadModFromAssembly(Assembly assembly) {
            // For now, this just returns the first IMod it finds

            foreach (Module module in assembly.GetModules()) {
                foreach (Type type in module.GetTypes()) {
                    if (type.GetInterfaces().Contains(typeof(IMod))) {
                        Console.WriteLine("Found Mod Entrypoint! " + type.FullName);

                        try {
                            IMod mod = (IMod) Activator.CreateInstance(type);
                            return mod;
                        }
                        catch (Exception e) {
                            Debug.LogError($"Something went wrong loading {type.FullName}, {e.Message}");
                        }
                    }
                }
            }

            return null;
        }
    }

    public interface IMod {
        string Name { get; }
        string Version { get; }

        void Init(RainWorld rainworld);
    }

    public static class ModLogger {
        public static void EnableLogging() {
            if (File.Exists("exceptionLog.txt"))
                File.Delete("exceptionLog.txt");
            if (File.Exists("consoleLog.txt"))
                File.Delete("consoleLog.txt");
            Application.RegisterLogCallback(new Application.LogCallback(HandleLog));
        }

        public static void HandleLog(string logString, string stackTrace, LogType type) {
            if (type == LogType.Error || type == LogType.Exception) {
                File.AppendAllText("exceptionLog.txt", logString + Environment.NewLine);
                File.AppendAllText("exceptionLog.txt", stackTrace + Environment.NewLine);
                return;
            }
            File.AppendAllText("consoleLog.txt", logString + Environment.NewLine);
        }
    }
}

