using UnityEngine;

namespace SkyboxBrightnessControl;

internal static class Logging
{
    private const string LogPrefix = "[Skybox Brightness Control]";
    internal static bool Debugging = false;

    internal static void Info(string message)
    {
        Debug.Log($"{LogPrefix} {message}");
    }

    internal static void Verbose(string message)
    {
        if (Debugging) Debug.Log($"{LogPrefix} {message}");
    }

    internal static void Warning(string message)
    {
        Debug.LogWarning($"{LogPrefix} {message}");
    }

    internal static void Error(string message)
    {
        Debug.LogError($"{LogPrefix} {message}");
    }
}