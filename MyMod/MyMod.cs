using System;
using System.IO;
using UnityEngine;

/*
 * Super trivial mod example from http://rain-world-modding.wikia.com/wiki/Adding_an_Exception_Handler
 * 
 * The idea would be that you write your mod code here, tagging it with an attribute that tells
 * the game where/when it should run. E.g. on level load, on player jump, and so forth.
 */

public static class MyMod {
    public static void RegisterLogCallback() {
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
