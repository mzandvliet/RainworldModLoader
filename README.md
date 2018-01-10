# RainworldModLoader
Proof of concept Rainworld Mod Loader (meaning not usable, but with extra work it could be)

If you are a mod developer, please do get in touch and provide feedback!

# Why?

The existing approach to modding Rainworld is to load the game's dotnet assembly using a tool called dnSpy, which lets you see decompiled source. You then have to edit the low level IL assembly code to do anything, deal with all sorts of cryptic errors, and after that your code is stuffed into the game DLL. This workflow is not great for writing big modifications.

I still had a program around for doing code injection in Unity from when I wanted to instrument unity's built code with profiler hooks, and thought I'd try to use it to get a better workflow going.

# How?

An injector program which takes the vanilla game dll, injects a small mod loader routine. That mod loader can then load custom DLLs with mod-only code, and get the game to call into it. This only has to be done once, and the resulting assembly could be redistributed to players to enable modding for them.

As a trivial example mod I took this snippet: http://rain-world-modding.wikia.com/wiki/Adding_an_Exception_Handler.
It tells the game to output debug messages to a text log. I put this code into a separate assembly project (MyMod), and the game now loads that DLL at startup, if it finds it in the game folder.

If this is polished up a bit, it could patch the game with generic hooks for mods to register themselves to, so your mod code could register to be called on update, on level start, or whatever else. 

Two big wins:

- The game can easily load multiple mods at once. (providing they don't conflict)
- You can write your code in visual studio, in C#, organize it into separate DLLs, have dependencies, use source control, and so on.
