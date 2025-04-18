using UnityEditor;
using UnityEngine;
using UnityMCP.Bridge.Shared.Data;

namespace UnityMCP.Bridge
{
    public partial class UnityMCPWindow
    {
        #region Constants
        private const string UNITY_PORT_KEY = "UnityMCP_UnityPort";
        private const string MCP_PORT_KEY = "UnityMCP_MCPPort";
        private const string MCP_HOST_KEY = "UnityMCP_Host";
        
        private const string DEFAULT_UNITY_PORT = "6400";
        private const string DEFAULT_MCP_PORT = Constants.DefaultTcpPortStr;
        private const string DEFAULT_HOST = "localhost";
        
        private const int MIN_PORT = 1024;
        private const int MAX_PORT = 65535;
        #endregion

        #region State Variables
        private BridgeStatus bridgeStatus = BridgeStatus.Stopped;
        private ServerStatus serverStatus = ServerStatus.NotRunning;
        private ConfigurationStatus cursorStatus = ConfigurationStatus.NotConfigured;
        #endregion

        #region Configuration Variables
        private string unityPort = DEFAULT_UNITY_PORT;
        private string mcpPort = DEFAULT_MCP_PORT;
        private string mcpHost = DEFAULT_HOST;
        
        private string tempUnityPort = DEFAULT_UNITY_PORT;
        private string tempMcpPort = DEFAULT_MCP_PORT;
        private string tempMcpHost = DEFAULT_HOST;
        #endregion

        #region UI Variables
        private bool showPortError = false;
        private string portErrorMessage = "";
        
        private GUIStyle statusDotStyle;
        private GUIStyle statusLabelStyle;
        private GUIStyle errorStyle;
        #endregion

        #region Enums
        private enum BridgeStatus
        {
            Running,
            Stopped
        }

        private enum ServerStatus
        {
            Running,
            NotRunning
        }

        private enum ConfigurationStatus
        {
            Configured,
            NotConfigured
        }
        #endregion
    }
} 