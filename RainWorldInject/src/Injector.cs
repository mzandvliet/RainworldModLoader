using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

/* Todo:
 * 
 * - Get rid of hardcoded paths (perhaps use a file browser to let user find Assembly-CSharp, or a config file)
 * - Ship injector with 0Harmony.dll, optionally include and install patched mono.dll
 * 
 * - Try injecting the modloader into RainWorld.ctor (That way, Rainworld.Start is open for instrumentation/transpiling)
 */

namespace RainWorldInject {
    /// Rain World assembly code injector
    /// Injects hooks to a mod loader
    /// 
    /// Based on: http://www.codersblock.org/blog//2014/06/integrating-monocecil-with-unity.html
    /// 
    /// Created by Martijn Zandvliet, 10/01/2017
    public class Injector {
        public string AssemblyFolder = @"RainWorld_Data\Managed";

        public bool Inject() {
            Console.WriteLine("Rain World Mod Loader: Code Injection...\n");

            string assemblyPath = Path.Combine(AssemblyFolder, "Assembly-CSharp.dll");
            string assemblyBackupPath = Path.Combine(AssemblyFolder, "Assembly-CSharp-Backup.dll");

            // First, back up the original file before we do anything to it

            if (!File.Exists(assemblyPath)) {
                Console.WriteLine("!! Can't locate the file Assembly-CSharp.dll !!");
                return false;
            }

            if (Backup(assemblyPath, assemblyBackupPath)) {
                Console.WriteLine("Backed up original game file as Assembly-CSharp-Backup.dll\n");
            }
            else {
                return false;
            }

            // Read assembly

            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyBackupPath);

            try {
                Console.WriteLine("Processing " + Path.GetFileName(assemblyBackupPath) + "...");
                ProcessAssembly(assembly);
            }
            catch (Exception e) {
                Console.WriteLine(
                    "!! Exception while processing assembly: " + assembly.Name +
                    ", Reason: " + e.Message);
                return false;
            }

            // Write the patched assembly to the game directory

            if (File.Exists(assemblyPath)) {
                try {
                    File.Delete(assemblyPath);
                }
                catch (Exception e) {
                    Console.WriteLine(
                        "!! Can't overwrite Assembly-CSharp.dll because it is in use (is the" +
                        " game running?) Reason: " + e.Message);
                    return false;
                }
                
            }

            Console.WriteLine("Writing to " + assemblyPath + "...");
            assembly.Write(assemblyPath);

            return true;
        }

        private bool Backup(string assemblyPath, string backupPath) {
            // Todo: first verify that assembly is original, with md5 hash

            // Handle old filenames, if present
            string assemblyOriginalPath = Path.Combine(AssemblyFolder, "Assembly-CSharp-Original.dll");
            if (File.Exists(assemblyOriginalPath) && !File.Exists(backupPath)) {
                Console.WriteLine("Detected old ModLoader setup, updating filenames...");
                File.Move(assemblyOriginalPath, backupPath);
            }

            // Backup vanilla assembly
            if (!File.Exists(backupPath)) {
                try {
                    File.Copy(assemblyPath, backupPath);
                }
                catch (Exception e) {
                    Console.WriteLine("!! Couldn't back up original game files !!");
                    return false;
                }
            }

            return true;
        }

        private void ProcessAssembly(AssemblyDefinition assembly) {
            foreach (ModuleDefinition module in assembly.Modules) {
                Console.WriteLine("Module: " + module.FullyQualifiedName);

                /*
                 * Here we go hunting for classes, methods, and other bits of IL that
                 * we want to inject into
                 * 
                 * In this case we want to inject code that loads our MyMod assembly
                 * as soon as the game runs RainWorld.Start(), and then calls a method
                 * from that mod.
                 */

                foreach (TypeDefinition type in module.Types) {
                    if (type.Name.Equals("RainWorld")) {
                        Console.WriteLine("Found RainWorld class: " + module.FullyQualifiedName);

                        foreach (MethodDefinition method in type.Methods) {
                            try {
                                if (method.Name == "Start") {
                                    InstrumentRainworldStartMethod(method, module);
                                }
                            }
                            catch (Exception e) {
                                Console.WriteLine(
                                    "!! Failed on: " + type.Name + "."
                                    + method.Name + ": " + e.Message);
                            }
                        }
                    }
                }
            }
        }

        private void InstrumentRainworldStartMethod(MethodDefinition method, ModuleDefinition module) {
            Console.WriteLine("Patching target method: " + method.Name);

            ILProcessor il = method.Body.GetILProcessor();

            /* 
            * Create the instruction for Assembly.Load
            */

            MethodReference assemblyLoadFunc = module.Import(
                typeof(Assembly).GetMethod(
                    "LoadFrom",
                    new[] {typeof(string)}));

            /*
            * Insert the call to load our ModLoader assembly
            */

            string modLoaderAssemblyPath = Path.Combine(AssemblyFolder, "ModLoader.dll");

            Instruction firstInstr = method.Body.Instructions[0];
            Instruction loadStringInstr = Instruction.Create(OpCodes.Ldstr, modLoaderAssemblyPath);
            il.InsertBefore(firstInstr, loadStringInstr);

            Instruction loadAssemblyInstr = Instruction.Create(OpCodes.Call, assemblyLoadFunc);
            il.InsertAfter(loadStringInstr, loadAssemblyInstr);

            /* 
            * Now RainWorld.Start() should load our ModLoader.dll assembly before
            * any other code runs! Great!
            * 
            * Next we need to call its initialize method and pass it a ref to the
            * RainWorld instance's this reference.
            */


            // Push rainworld this reference onto eval stack so we can pass it along
            Instruction pushRainworldRefInstr = Instruction.Create(OpCodes.Ldarg_0);
            il.InsertAfter(loadAssemblyInstr, pushRainworldRefInstr);

            MethodReference initializeFunc = module.Import(typeof(Modding.ModLoader).GetMethod("Initialize"));

            Instruction callModInstr = Instruction.Create(OpCodes.Call, initializeFunc);
            il.InsertAfter(pushRainworldRefInstr, callModInstr);

            /* 
            * That's it!
            * 
            * The game now loads our ModLoader, which in turn will
            * load any number of mods found in the Mods directory.
            */
        }
    }

    /// <summary>
    /// Used to mark modules in assemblies as already patched.
    /// </summary>
    [AttributeUsage(AttributeTargets.Module)]
    public class RainworldAssemblyAlreadyPatchedAttribute : Attribute {
    }
}
