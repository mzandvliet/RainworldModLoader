using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;

/* Todo:
 * - Ship injector with 0Harmony.dll, optionally include and install patched mono.dll
 * - Modloader.dll and Harmony.dll should be part of distribution
 */

namespace RainWorldInject {
    /// Rain World assembly code injector
    /// Injects hooks to a mod loader
    /// 
    /// Based on: http://www.codersblock.org/blog//2014/06/integrating-monocecil-with-unity.html
    /// 
    /// Created by Martijn Zandvliet, 10/01/2017
    public static class Injector {
        public const string AssemblyFolder = "..\\RainWorld_Data\\Managed";

        public static bool Inject() {
            Console.WriteLine("Rain World Mod Loader: Code Injection...\n");

            string assemblyPath = Path.Combine(AssemblyFolder, "Assembly-CSharp.dll");
            string assemblyBackupPath = Path.Combine(AssemblyFolder, "Assembly-CSharp-Backup.dll");

            var dependencies = LoadDependencies();

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

            var readerParameters = CreateReaderParameters(assemblyBackupPath);
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyBackupPath, readerParameters);

            // Patch it

            try {
                Console.WriteLine("Processing " + Path.GetFileName(assemblyBackupPath) + "...");
                PatchAssembly(assembly, dependencies);
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

        private static IDictionary<Dependencies, Assembly> LoadDependencies() {
            return new Dictionary<Dependencies, Assembly>() {
                {Dependencies.UnityEngine, Assembly.LoadFrom(Path.Combine(AssemblyFolder, "UnityEngine.dll"))},
                {Dependencies.AssemblyCSharp, Assembly.LoadFrom(Path.Combine(AssemblyFolder, "Assembly-CSharp-Backup.dll"))},
                {Dependencies.ModLoader, Assembly.LoadFrom(Path.Combine(AssemblyFolder, "ModLoader.dll"))}
            };

        }

        private static bool Backup(string assemblyPath, string backupPath) {
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

        private static ReaderParameters CreateReaderParameters(string assemblyBackupPath) {
            // Create resolver
            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver();
            assemblyResolver.AddSearchDirectory(AssemblyFolder);

            // Create reader parameters with resolver
            ReaderParameters readerParameters = new ReaderParameters {
                AssemblyResolver = assemblyResolver
            };

            // Create writer parameters
            WriterParameters writerParameters = new WriterParameters();

            // mdbs have the naming convention myDll.dll.mdb whereas pdbs have myDll.pdb
            String mdbPath = assemblyBackupPath + ".mdb";
            String pdbPath = assemblyBackupPath.Substring(0, assemblyBackupPath.Length - 3) + "pdb";

            // Figure out if there's an pdb/mdb to go with it
            if (File.Exists(pdbPath)) {
                readerParameters.ReadSymbols = true;
                readerParameters.SymbolReaderProvider = new PdbReaderProvider();
                writerParameters.WriteSymbols = true;
                writerParameters.SymbolWriterProvider = new MdbWriterProvider();
                // pdb written out as mdb, as mono can't work with pdbs
            }
            else if (File.Exists(mdbPath)) {
                readerParameters.ReadSymbols = true;
                readerParameters.SymbolReaderProvider = new MdbReaderProvider();
                writerParameters.WriteSymbols = true;
                writerParameters.SymbolWriterProvider = new MdbWriterProvider();
            }
            else {
                readerParameters.ReadSymbols = false;
                readerParameters.SymbolReaderProvider = null;
                writerParameters.WriteSymbols = false;
                writerParameters.SymbolWriterProvider = null;
            }
            return readerParameters;
        }

        private static void PatchAssembly(AssemblyDefinition assembly, IDictionary<Dependencies, Assembly> dependencies) {
            foreach (ModuleDefinition module in assembly.Modules) {
                Console.WriteLine("Module: " + module.FullyQualifiedName);

                foreach (TypeDefinition type in module.Types) {
                    if (type.Name.Equals("RainWorld")) {
                        Console.WriteLine("Patching class: " + module.FullyQualifiedName);
                        try {
                            InjectRainWorldHooks(module, type, dependencies);
                        }
                        catch (Exception e) {
                            Console.WriteLine("!! Failed on: " + type.Name + "." + type.Methods[0].Name + ": " + e.Message);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Inserts the modloader code by adding an Awake() method to RainWorld, such that
        /// the mod loader can apply changes RainWorld.Start() before it runs.
        /// </summary>
        private static void InjectRainWorldHooks(ModuleDefinition module, TypeDefinition type, IDictionary<Dependencies, Assembly> dependencies) {
            MethodDefinition method = new MethodDefinition(
                "Awake",
                Mono.Cecil.MethodAttributes.Private,
                module.TypeSystem.Void);

            ILProcessor worker = method.Body.GetILProcessor();
            InsertModLoaderInstructions(module, worker, dependencies);
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            type.Methods.Add(method);
        }

        /// <summary>
        /// Generates the actual IL for the mod loader hook
        /// </summary>
        private static void InsertModLoaderInstructions(ModuleDefinition module, ILProcessor il, IDictionary<Dependencies, Assembly> dependencies) {
            /* Create the instruction for Assembly.Load */

            MethodReference assemblyLoadFunc = module.Import(
                typeof(Assembly).GetMethod("LoadFrom", new[] { typeof(string) }));

            /* Insert the call to load our ModLoader assembly (and Harmony) */

            string harmonyAssemblyPath = Path.Combine("RainWorld_Data\\Managed\\", "0Harmony.dll");
            string modLoaderAssemblyPath = Path.Combine("RainWorld_Data\\Managed\\", "ModLoader.dll");
            var loadAssemblyInstr = InsertLoadAssemblyInstructions(il, assemblyLoadFunc, harmonyAssemblyPath);
                loadAssemblyInstr = InsertLoadAssemblyInstructions(il, assemblyLoadFunc, modLoaderAssemblyPath);

            /* Now RainWorld should load our ModLoader.dll assembly before
             * any other code runs! Great!
             * 
             * Next we need to call its initialize method and pass it a ref to the
            * RainWorld instance's this reference. */


            // Push rainworld this reference onto eval stack so we can pass it along
            Instruction pushThis = il.Create(OpCodes.Ldarg_0);
            il.InsertAfter(loadAssemblyInstr, pushThis);

            var rainWorldType = dependencies[Dependencies.AssemblyCSharp].GetType("RainWorld");
            var initMethod = dependencies[Dependencies.ModLoader].GetType("Modding.ModLoader").GetMethod("Initialize", new []{ rainWorldType });
            MethodReference initializeMethod = module.Import(initMethod);

            Instruction callModInstr = il.Create(OpCodes.Call, initializeMethod);
            il.InsertAfter(pushThis, callModInstr);

            /* That's it!
             * 
             * The game now loads our ModLoader, which in turn will
             * load any number of mods found in the Mods directory.
             */
        }

        private static Instruction InsertLoadAssemblyInstructions(ILProcessor il, MethodReference assemblyLoadFunc, string assemblyPath) {
            Instruction loadStringInstr = il.Create(OpCodes.Ldstr, assemblyPath);
            il.Body.Instructions.Add(loadStringInstr);

            Instruction loadAssemblyInstr = il.Create(OpCodes.Call, assemblyLoadFunc);
            il.InsertAfter(loadStringInstr, loadAssemblyInstr);

            il.InsertAfter(loadAssemblyInstr, il.Create(OpCodes.Pop));
            return loadAssemblyInstr;
        }
    }

    public enum Dependencies {
        UnityEngine,
        AssemblyCSharp,
        ModLoader
    }

    /// <summary>
    /// Used to mark modules in assemblies as already patched.
    /// </summary>
    [AttributeUsage(AttributeTargets.Module)]
    public class RainworldAssemblyAlreadyPatchedAttribute : Attribute {
    }
}
