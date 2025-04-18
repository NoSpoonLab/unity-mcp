using UnityEditor;
using UnityEngine;

namespace UnityMCP.Bridge
{
    public partial class UnityMCPWindow
    {
        #region UI Methods
        private void DrawServerStatusSection()
        {
            EditorGUILayout.LabelField("Server Status", EditorStyles.boldLabel);
            DrawStatusDot(serverStatus == ServerStatus.Running);
            EditorGUILayout.LabelField(
                serverStatus == ServerStatus.Running ? "Running" : "Not Running", 
                statusLabelStyle
            );
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBridgeSection()
        {
            EditorGUILayout.LabelField("Unity MCP Bridge", EditorStyles.boldLabel);
            
            DrawStatusDot(bridgeStatus == BridgeStatus.Running);
            EditorGUILayout.LabelField(
                bridgeStatus == BridgeStatus.Running ? "Running" : "Stopped", 
                statusLabelStyle
            );
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button(bridgeStatus == BridgeStatus.Running ? "Stop Bridge" : "Start Bridge"))
            {
                ToggleBridgeStatus();
            }
        }

        private void DrawCursorSection()
        {
            EditorGUILayout.LabelField("Cursor Configuration", EditorStyles.boldLabel);
            
            DrawStatusDot(cursorStatus == ConfigurationStatus.Configured);
            EditorGUILayout.LabelField(
                cursorStatus == ConfigurationStatus.Configured ? "Configured" : "Not Configured", 
                statusLabelStyle
            );
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Auto Configure Cursor"))
            {
                AutoConfigureCursor();
            }
            
            if (GUILayout.Button("Manual Setup"))
            {
                ManualSetup();
            }
        }

        private void DrawServerInfoMessage()
        {
            EditorGUILayout.HelpBox(
                "The MCP server will start automatically when you launch your project. Make sure the ports are configured correctly.", 
                MessageType.Info
            );
        }

        private void DrawStatusDot(bool isActive)
        {
            Color originalColor = GUI.color;
            GUI.color = isActive ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.8f, 0.4f, 0.4f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("●", statusDotStyle);
            GUI.color = originalColor;
        }

        private void DrawHostSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Host Configuration", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("MCP Host:", GUILayout.Width(70));
            tempMcpHost = EditorGUILayout.TextField(tempMcpHost, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "The MCP Host can be changed if your server is running in a Docker container or a different machine. Default is 'localhost' for local development.", 
                MessageType.Info
            );

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply Host", GUILayout.Width(100)))
            {
                ApplyHostSettings();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawPortSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Port Configuration", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("MCP Port:", GUILayout.Width(70));
            tempMcpPort = EditorGUILayout.TextField(tempMcpPort, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            if (showPortError)
            {
                EditorGUILayout.LabelField(portErrorMessage, errorStyle);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply Port Changes", GUILayout.Width(150)))
            {
                ApplyPortSettings();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        #region Style Methods
        private void InitializeStyles()
        {
            statusDotStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                fixedWidth = 20,
                fixedHeight = 20
            };

            statusLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };

            errorStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.red },
                fontSize = 10,
                wordWrap = true
            };
        }
        #endregion
        #endregion
    }
} 