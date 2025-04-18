using UnityEngine;

namespace UnityMCP.Bridge
{
    public static class UnityMCPEditorLogger
    {
        public static bool IsDebugEnabled = false;

        public static void Log(string message)
        {
            if (IsDebugEnabled)
                Debug.Log(message);
        }

        public static void LogWarning(string message)
        {
            if (IsDebugEnabled)
                Debug.LogWarning(message);
        }

        public static void LogError(string message)
        {
            if (IsDebugEnabled)
                Debug.LogError(message);
        }

        public static void SetDebugMode(bool enabled)
        {
            IsDebugEnabled = enabled;
        }
    }
} 