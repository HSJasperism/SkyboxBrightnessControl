using System.Text;
using UnityEngine;

namespace SkyboxBrightnessControl
{
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

        internal static void printActiveCameras()
        {
            var output = new StringBuilder();
            output.AppendLine($"{LogPrefix} List of active cameras:");
            foreach (var camera in Camera.allCameras) output.AppendLine($"{camera.name}, aspect: {camera.aspect}, fov: {camera.fieldOfView}, is ortho? {camera.orthographic}");
            Debug.Log(output.ToString());
        }
    }
}