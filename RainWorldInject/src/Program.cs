using System;

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
            Injector.Inject();

            Console.WriteLine("All done!");
            Console.ReadLine();
        }
    }
}
