![](https://imgur.com/a/4qdpB.gif)
(Slugcat Camo Mod by LodeRunner + Co-op Mod by OriginalSINe)

# RainworldModLoader
Proof of concept Rainworld Mod Loader (meaning not usable, but with extra work it could be)

If you are a mod developer, please do get in touch and provide feedback! I'm on the Rainworld Discord in #modding, https://discordapp.com/invite/SBmHbpW

(This same approach could be used to create mod loaders for any Unity game, really.)

# Why?

The existing approach to modding Rainworld is to load the game's dotnet assembly using a tool called dnSpy, which lets you see decompiled source. You then have to edit the low level IL assembly code to do anything, deal with all sorts of cryptic errors, and after that your code is stuffed into the game DLL. This workflow is not great for writing and maintaining comprehensive modifications.

# How?

An injector program which takes the vanilla game dll, injects a small mod loader routine. That mod loader can then load custom DLLs with mod-only code, and get the game to call into it. This only has to be done once to enable mod loading for an installed version of the game.

The ModLoader now looks for mod assemblies in the Rainworld/Mods folder. Anything called *******Mod.dll* that contains an implementation of the IMod interface gets loaded. IMod is too barebones for actual modding, but it demonstrates how this will work in the end.

If this is polished up a bit, it could patch the game with generic hooks for mods to register themselves to, so your mod code could register to be called on update, on level start, or whatever else. 

Three big wins:

- The game could load multiple mods at once. (providing they don't conflict, which requires some developer care)
- You can write your code in visual studio, in C#, organize it into separate DLLs, have dependencies, use source control, and so on.
- Mods and modding APIs don't need to redistribute copyrighted code in Assembly-CSharp, but can be locally applied as a patch to your game install with an easy to use patching program. The Hollow Knight modding community discusses this issue here: https://gist.github.com/thejoshwolfe/db369bebf6518227c830fffee12ddbec
