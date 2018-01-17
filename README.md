(Slugcat Camo Mod by LodeRunner + Co-op Mod by OriginalSINe)

![](https://i.imgur.com/KSE4URu.gif)

# RainworldModLoader
Proof of concept Rainworld Mod Loader (meaning not usable, but with extra work it could be)

If you are a mod developer, please do get in touch and provide feedback! I'm on the Rainworld Discord in #modding, https://discordapp.com/invite/SBmHbpW

(This same approach could be used to create mod loaders for any Unity game, really.)

# Why?

The existing approach to modding Rainworld is to load the game's dotnet assembly using a tool called dnSpy, which lets you see decompiled source. You then have to edit the low level IL assembly code to do anything, deal with all sorts of cryptic errors, and after that your code is stuffed into the game DLL. This workflow is not great for writing and maintaining comprehensive modifications.

# How?

An injector program which takes the vanilla game dll, injects a small mod loader routine. That mod loader can then load custom DLLs with mod-only code, and get the game to call into it. This only has to be done once to enable mod loading for an installed version of the game.

The ModLoader now looks for mod assemblies in the Rainworld/Mods folder. Anything called *******Mod.dll* that contains an implementation of the modding interface gets loaded. It's still work in progress.

Harmony Patcher is currently used to inject your mod hooks into the game's code. Check out the provided example mods to see how it's done.

Three big wins:

- The game can load multiple mods at once. (providing they don't conflict, which requires some developer care)
- You can write your code in visual studio, in C#, organize it into separate DLLs, have dependencies, use source control, and so on.
- Mods and modding APIs don't need to redistribute copyrighted code in Assembly-CSharp, but can be locally applied as a patch to your game install with an easy to use patching program. The Hollow Knight modding community discusses this issue here: https://gist.github.com/thejoshwolfe/db369bebf6518227c830fffee12ddbec
