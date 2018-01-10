using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;
using System.Reflection;

/// Rain World mod loader concept
/// 
/// Based on: http://www.codersblock.org/blog//2014/06/integrating-monocecil-with-unity.html
/// 
/// Created by Martijn Zandvliet, 10/01/2017

/* Todo:
 * - Generalized mod loading routine: scan file system for mod dlls, load them in order
 * - Instrument vanilla game code with calls into mods, so that mods can specify where/when
 * to update (e.g. player jump, level load, etc.)
 */

public static class Injector {
    public const string RootFolder = "D:\\Games\\SteamLibrary\\steamapps\\common\\Rain World";
    public const string AssemblyFolder = "D:\\Games\\SteamLibrary\\steamapps\\common\\Rain World\\RainWorld_Data\\Managed";

    public static void Inject() {
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
            return;
        }

        // Write the patched assembly to the game directory
        if (File.Exists(patchedAssemblyPath)) {
            File.Delete(patchedAssemblyPath);
        }
        Console.WriteLine("Writing to " + patchedAssemblyPath + "...");
        assembly.Write(patchedAssemblyPath, writerParameters);
    }

    private static void ProcessAssembly(AssemblyDefinition assembly) {
        foreach (ModuleDefinition module in assembly.Modules) {
            Console.WriteLine("Module: " + module.FullyQualifiedName);

            // Here we go hunting for classes, methods, and other bits of IL that we want to inject into

            foreach (TypeDefinition type in module.Types) {
                if (type.Name.Equals("RainWorld")) {
                    Console.WriteLine("Found RainWorld class: " + module.FullyQualifiedName);

                    foreach (MethodDefinition method in type.Methods) {
                        try {
                            if (method.Name == "Start") {
                                Console.WriteLine("Found method: " + method.Name);

                                ILProcessor ilProcessor = method.Body.GetILProcessor();

                                // Create the hook to Assembly.Load
                                MethodReference assemblyLoadFunction = module.Import(
                                    typeof(System.Reflection.Assembly).GetMethod(
                                        "LoadFrom",
                                        new [] { typeof(string) }));

                                // Insert the call to load our mod assembly
                                Instruction first = method.Body.Instructions[0];
                                ilProcessor.InsertBefore(first, Instruction.Create(
                                    OpCodes.Ldstr,
                                    Path.Combine(AssemblyFolder, "MyMod.dll")));
                                ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Call, assemblyLoadFunction));

                                // Now RainWorld.Start loads our MyMod.dll

                                // Next we need to insert a call to our mod code...

                                // Create the hook our mod method
                                MethodReference myModFunction = module.Import(
                                    typeof(MyMod).GetMethod(
                                        "RegisterLogCallback",
                                        BindingFlags.Public | BindingFlags.Static));

                                // Insert the call to our mod method
                                first = method.Body.Instructions[0]; // This should point to Assembly.LoadFrom
                                ilProcessor.InsertAfter(first, Instruction.Create(OpCodes.Call, myModFunction));

                                // Now we should have our logging feature injected into the game! :D
                            }
                        }
                        catch (Exception e) {
                            Console.WriteLine("!! Failed on: " + type.Name + "." + method.Name + ": " + e.Message);
                        }
                    }
                }
            }
        }
    }
}

/// <summary>
/// Used to mark modules in assemblies as already patched.
/// </summary>
[AttributeUsage(AttributeTargets.Module)]
public class RainworldAssemblyAlreadyPatchedAttribute : Attribute {
}