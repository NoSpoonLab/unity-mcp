using UnityEditor;
using UnityEngine;
using System;

namespace UnityMCP.Bridge
{
    public partial class UnityMCPWindow : EditorWindow
    {
        #region Lifecycle Methods
        /// <summary>
        /// Shows the MCP Bridge window in Unity Editor
        /// </summary>
        [MenuItem("AI/Unity MCP")]
        public static void ShowWindow()
        {
            GetWindow<UnityMCPWindow>("MCP Bridge");
        }

        /// <summary>
        /// Called when the window is enabled
        /// </summary>
        private void OnEnable()
        {
            InitializeStyles();
            LoadSettings();
            
            // Registrar para las actualizaciones de editor
            EditorApplication.update += OnEditorUpdate;
        }
        
        /// <summary>
        /// Called when the window is disabled
        /// </summary>
        private void OnDisable()
        {
            // Desconectar del servidor TCP al cerrar la ventana
            DisconnectTcpClient();
            
            // Desregistrar de las actualizaciones de editor
            EditorApplication.update -= OnEditorUpdate;
        }
        
        /// <summary>
        /// Update method called by Unity Editor
        /// </summary>
        private void OnEditorUpdate()
        {
            // Actualizar la comunicación TCP
            UpdateCommunication();
        }

        /// <summary>
        /// Main GUI rendering method
        /// </summary>
        private void OnGUI()
        {
            if (statusDotStyle == null)
                InitializeStyles();

            GUILayout.Space(10);

            // DrawServerStatusSection(); // Desactivado por requerimiento
            DrawHostSettings();
            GUILayout.Space(5);
            DrawPortSettings();
            DrawServerInfoMessage();
            GUILayout.Space(10);
            DrawBridgeSection();
            GUILayout.Space(10);
            DrawCursorSection();
            
            // Botón para probar la comunicación TCP
            GUILayout.Space(15);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("TCP Communication Test", EditorStyles.boldLabel);
            if (GUILayout.Button("Send Test Message") && _isConnected)
            {
                SendTestMessage();
            }
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Sends a test message to the server
        /// </summary>
        private void SendTestMessage()
        {
            var testData = new System.Collections.Generic.Dictionary<string, object>
            {
                { "message", "Hello from Unity!" },
                { "timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
            };
            
            SendCommand("test", testData);
        }
        #endregion
        
    }
}