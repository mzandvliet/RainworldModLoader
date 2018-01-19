using System;
using System.IO;

/// Rain World mod loader concept
///
/// Patches vanilla RainWorld DLL with a mod loader
/// Mods are provided as separate assemblies, loaded by the game at startup
/// 
/// Created by Martijn Zandvliet, 10/01/2017

/* Todo:
 * 
 * On boot, compare existing Assembly.dll with checksum to verify it is the original
 * from the 1.5 version of the game. Then back that up as Assembly-Original.dll,
 * load that as readonly, apply patches, and save to Assembly.dll
 * 
 * Optionally add a custom attribute to patched module to easily tell that it's already
 * modded.
 */

namespace RainWorldInject {
    class Program {
        static void Main(string[] args) {

            Injector injector = new Injector();
            ConfigManager config = new ConfigManager("RainWorldInject.conf");

            string path = config.GetValue("GamePath", @""); 

            while (!CheckGameFolderValid(path)) {
                Console.WriteLine("Please enter the game path where RainWorld.exe located:");
                path = Console.ReadLine();
            }
            injector.AssemblyFolder = Path.Combine(path, @"RainWorld_Data\Managed");

            bool success = false;
            try {
                success = injector.Inject();
            } catch (Exception e) { // this might happen, eg. missing dll
                Console.WriteLine("!! Error when trying to inject, {0} !!", e.Message);
            }

            Console.WriteLine();
            if (success) {
                config.SetValue("GamePath", path); // save config here so the saved value is valid.
                Console.WriteLine("All done, enjoy!\n");
            } else {
                Console.WriteLine("Something went wrong, quitting...\n");
            }
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }

        public static bool CheckGameFolderValid(string path) {
            if (!File.Exists(Path.Combine(path, @"RainWorld.exe"))) return false;
            if (!File.Exists(Path.Combine(path, @"RainWorld_Data\Managed\Assembly-CSharp.dll"))) return false;
            return true;
        }
    }
}
