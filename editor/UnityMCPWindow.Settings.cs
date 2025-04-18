using UnityEditor;
using UnityEngine;

namespace UnityMCP.Bridge
{
    public partial class UnityMCPWindow
    {

        #region Settings Methods
        private void LoadSettings()
        {
            unityPort = EditorPrefs.GetString(UNITY_PORT_KEY, DEFAULT_UNITY_PORT);
            mcpPort = EditorPrefs.GetString(MCP_PORT_KEY, DEFAULT_MCP_PORT);
            mcpHost = EditorPrefs.GetString(MCP_HOST_KEY, DEFAULT_HOST);
            tempUnityPort = unityPort;
            tempMcpPort = mcpPort;
            tempMcpHost = mcpHost;
        }

        private void SaveSettings()
        {
            EditorPrefs.SetString(UNITY_PORT_KEY, unityPort);
            EditorPrefs.SetString(MCP_PORT_KEY, mcpPort);
            EditorPrefs.SetString(MCP_HOST_KEY, mcpHost);
        }

        private bool ValidatePort(string port, out int portNumber)
        {
            if (int.TryParse(port, out portNumber))
            {
                return portNumber >= MIN_PORT && portNumber <= MAX_PORT;
            }
            return false;
        }

        private bool ValidatePorts(out int unityPortNum, out int mcpPortNum)
        {
            bool unityPortValid = ValidatePort(tempUnityPort, out unityPortNum);
            bool mcpPortValid = ValidatePort(tempMcpPort, out mcpPortNum);

            if (!unityPortValid || !mcpPortValid)
            {
                showPortError = true;
                portErrorMessage = $"Ports must be between {MIN_PORT} and {MAX_PORT}";
                return false;
            }

            if (unityPortNum == mcpPortNum)
            {
                showPortError = true;
                portErrorMessage = "Unity and MCP ports must be different";
                return false;
            }

            showPortError = false;
            return true;
        }

        private void ApplyHostSettings()
        {
            mcpHost = tempMcpHost;
            SaveSettings();
        }

        private void ApplyPortSettings()
        {
            int unityPortNum, mcpPortNum;
            if (ValidatePorts(out unityPortNum, out mcpPortNum))
            {
                unityPort = tempUnityPort;
                mcpPort = tempMcpPort;
                SaveSettings();
            }
        }
        #endregion
    }
} 