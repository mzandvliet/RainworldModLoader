using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;
using TypeAttributes = Mono.Cecil.TypeAttributes;

/* Todo:
 * - Try injecting the modloader into RainWorld.ctor (That way, Rainworld.Start is open for instrumentation/transpiling)
 * 
 * That doesn't work. For some reason the constructor doesn't get called.
 * Next idea: try inserting an Awake method
 * 
 * - Get rid of hardcoded paths (perhaps use a file browser to let user find Assembly-CSharp)
 * - Make a backup before patching
 */

namespace RainWorldInject {
    /// Rain World assembly code injector
    /// Injects hooks to a mod loader
    /// 
    /// Based on: http://www.codersblock.org/blog//2014/06/integrating-monocecil-with-unity.html
    /// 
    /// Created by Martijn Zandvliet, 10/01/2017
    public static class Injector {
        public const string RootFolder = "D:\\Games\\SteamLibrary\\steamapps\\common\\Rain World";
        public const string AssemblyFolder = "D:\\Games\\SteamLibrary\\steamapps\\common\\Rain World\\RainWorld_Data\\Managed";

        public static bool Inject() {
            Console.WriteLine("Injector running...");

            string unpatchedAssemblyPath = Path.Combine(AssemblyFolder, "Assembly-CSharp-Original.dll");
            string patchedAssemblyPath = Path.Combine(AssemblyFolder, "Assembly-CSharp.dll");

            // Create resolver
            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver();
            assemblyResolver.AddSearchDirectory(AssemblyFolder);

            // Create reader parameters with resolver
            ReaderParameters readerParameters = new ReaderParameters();
            readerParameters.AssemblyResolver = assemblyResolver;

            // Create writer parameters
            WriterParameters writerParameters = new WriterParameters();

            // Process the game assembly

            // mdbs have the naming convention myDll.dll.mdb whereas pdbs have myDll.pdb
            String mdbPath = unpatchedAssemblyPath + ".mdb";
            String pdbPath = unpatchedAssemblyPath.Substring(0, unpatchedAssemblyPath.Length - 3) + "pdb";

            // Figure out if there's an pdb/mdb to go with it
            if (File.Exists(pdbPath)) {
                readerParameters.ReadSymbols = true;
                readerParameters.SymbolReaderProvider = new Mono.Cecil.Pdb.PdbReaderProvider();
                writerParameters.WriteSymbols = true;
                writerParameters.SymbolWriterProvider = new Mono.Cecil.Mdb.MdbWriterProvider(); // pdb written out as mdb, as mono can't work with pdbs
            }
            else if (File.Exists(mdbPath)) {
                readerParameters.ReadSymbols = true;
                readerParameters.SymbolReaderProvider = new Mono.Cecil.Mdb.MdbReaderProvider();
                writerParameters.WriteSymbols = true;
                writerParameters.SymbolWriterProvider = new Mono.Cecil.Mdb.MdbWriterProvider();
            }
            else {
                readerParameters.ReadSymbols = false;
                readerParameters.SymbolReaderProvider = null;
                writerParameters.WriteSymbols = false;
                writerParameters.SymbolWriterProvider = null;
            }

            // Read assembly
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(unpatchedAssemblyPath, readerParameters);

            // Process it if it hasn't already
            Console.WriteLine("Processing " + Path.GetFileName(unpatchedAssemblyPath) + "...");

            try {
                ProcessAssembly(assembly);
            }
            catch (Exception e) {
                // Skip writing if any exception occurred
                Console.WriteLine("!! Exception while processing assembly: " + assembly.FullName + ", " + e.Message);
                return false;
            }

            // Write the patched assembly to the game directory
            if (File.Exists(patchedAssemblyPath)) {
                try {
                    File.Delete(patchedAssemblyPath);
                }
                catch (Exception e) {
                    Console.WriteLine("!! Can't overwrite Assembly-CSharp.dll because it is in use (is the game running?)");
                    return false;
                }
                
            }

            Console.WriteLine("Writing to " + patchedAssemblyPath + "...");
            assembly.Write(patchedAssemblyPath, writerParameters);

            return true;
        }

        private static void ProcessAssembly(AssemblyDefinition assembly) {
            foreach (ModuleDefinition module in assembly.Modules) {
                Console.WriteLine("Module: " + module.FullyQualifiedName);

                /*
                 * Here we go hunting for classes, methods, and other bits of IL that we want to inject into
                 * 
                 * In this case we want to inject code that loads our MyMod assembly as soon as
                 * the game runs RainWorld.Start(), and then calls a method from that mod.
                 */

                InsertMonoBehaviour(module);

                for (var i = 0; i < module.Types.Count; i++) {
                    TypeDefinition type = module.Types[i];
                    if (type.Name.Equals("RainWorld")) {
                        Console.WriteLine("Found RainWorld class: " + module.FullyQualifiedName);
                        try {
                            AddAwakeMethod(module, type);
                        }
                        catch (Exception e) {
                            Console.WriteLine("!! Injection failed: " + e.Message);
                        }
                        break;
                    }
                }
            }
        }

        private static void AddAwakeMethod(ModuleDefinition module, TypeDefinition type) {
            MethodDefinition method = new MethodDefinition("Awake",
                    Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.HideBySig,
                    module.TypeSystem.Void);

            ILProcessor il = method.Body.GetILProcessor();
            //InsertDebugLog(module, il, method);
            InsertAddComponent(module, il, method);
            //InsertDestroyCall(module, il, method);
            il.Append(Instruction.Create(OpCodes.Ret));

            type.Methods.Add(method);
        }

        private static void InsertDebugLog(ModuleDefinition module, ILProcessor il, MethodDefinition m) {
            Instruction msg = il.Create(OpCodes.Ldstr, "Initializating...\n");
            MethodReference writeline = module.Import(typeof(UnityEngine.Debug).GetMethod("Log", new [] { typeof(string) }));
            il.Append(msg);
            il.Append(Instruction.Create(OpCodes.Call, writeline));
        }

        private static void InsertDestroyCall(ModuleDefinition module, ILProcessor il, MethodDefinition m) {
            Instruction ldThis = Instruction.Create(OpCodes.Ldarg_0);
            il.Append(ldThis);
            var destroyMethods = typeof(UnityEngine.Object).GetMethods();
            MethodReference destroyMethodRef = module.Import(destroyMethods[6]); // Destroy(object)
            il.Append(Instruction.Create(OpCodes.Call, destroyMethodRef));
        }

        private static void InsertMonoBehaviour(ModuleDefinition module) {
            TypeDefinition myType = new TypeDefinition("Modding", "MyLoader", TypeAttributes.Public | TypeAttributes.Class);
            var monoBehaviour = module.Import(typeof(MonoBehaviour));
            myType.BaseType = monoBehaviour;

            MethodDefinition method = new MethodDefinition("Start",
                    Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.HideBySig,
                    module.TypeSystem.Void);

            ILProcessor il = method.Body.GetILProcessor();
            InsertDebugLog(module, il, method);
            il.Append(Instruction.Create(OpCodes.Ret));

            myType.Methods.Add(method);

            module.Types.Add(myType);
        }

        private static void InsertAddComponent(ModuleDefinition module, ILProcessor il, MethodDefinition m) {
            Instruction ldThis = Instruction.Create(OpCodes.Ldarg_0);
            il.Append(ldThis);

            MethodReference getGameObject = module.Import(typeof(Component).GetMethod("get_gameObject"));
            Instruction callGetGameObject = Instruction.Create(OpCodes.Call, getGameObject);
            il.Append(callGetGameObject);

            il.Append(Instruction.Create(OpCodes.Ldstr, "MyLoader"));

            MethodReference addComponent = module.Import(typeof(UnityEngine.GameObject).GetMethod("AddComponent", new[] { typeof(string) }));

            il.Append(Instruction.Create(OpCodes.Callvirt, addComponent));
            il.Append(Instruction.Create(OpCodes.Pop));
        }

        private static void InsertModLoaderInstructions(ModuleDefinition module, ILProcessor il, MethodDefinition method) {
            Console.WriteLine("Patching target method: " + method.Name);

            /* 
            * Create the instruction for Assembly.Load
            */

            MethodReference assemblyLoadFunc = module.Import(
                typeof(System.Reflection.Assembly).GetMethod(
                    "LoadFrom",
                    new[] { typeof(string) }));

            /*
            * Insert the call to load our ModLoader assembly
            */

            string modLoaderAssemblyPath = Path.Combine(AssemblyFolder, "ModLoader.dll"); // Todo: local path

            Instruction loadStringInstr = Instruction.Create(OpCodes.Ldstr, modLoaderAssemblyPath);
            il.Append(loadStringInstr);

            Instruction loadAssemblyInstr = Instruction.Create(OpCodes.Call, assemblyLoadFunc);
            il.Append(loadAssemblyInstr);

            /* 
            * Now RainWorld should load our ModLoader.dll assembly before
            * any other code runs! Great!
            * 
            * Next we need to call its initialize method and pass it a ref to the
            * RainWorld instance's this reference.
            */


            // Push rainworld this reference onto eval stack so we can pass it along
            //            Instruction pushRainworldRefInstr = Instruction.Create(OpCodes.Ldarg_0);
            //            il.Append(loadAssemblyInstr, pushRainworldRefInstr);
            //
            //            MethodReference initializeFunc = module.Import(typeof(Modding.ModLoader).GetMethod("Initialize"));
            //
            //            Instruction callModInstr = Instruction.Create(OpCodes.Call, initializeFunc);
            //            il.Append(pushRainworldRefInstr, callModInstr);

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
