using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using Newtonsoft.Json;
using UnityMCP.Bridge.Shared.Data;
using UnityMCP.Bridge.Shared.Messages;
using UnityMCP.Bridge.Shared.Messages.Abstractions;
using UnityMCP.Bridge.Tools;

namespace UnityMCP.Bridge
{
    public partial class UnityMCPWindow
    {
        #region Tool Registry
        // Dictionary that maps message types to their corresponding handler
        private static readonly Dictionary<string, object> _toolHandlers = new ();

        // Initialize tool handlers
        static UnityMCPWindow()
        {
            // Register available tools with static methods
            RegisterStaticTool<ReadConsoleMessage>(ReadConsoleMessage.MessageType,  ReadConsoleTool.HandleMessage);
            RegisterStaticTool<ExecuteMenuItemMessage>(ExecuteMenuItemMessage.MessageType, ExecuteMenuItemTool.HandleMessage);
            RegisterStaticTool<ManageGameObjectMessage>(ManageGameObjectMessage.MessageType, ManageGameObjectTool.HandleMessage);
            RegisterStaticTool<ManageSceneMessage>(ManageSceneMessage.MessageType, ManageSceneTool.HandleMessage);
            RegisterStaticTool<ManageScriptMessage>(ManageScriptMessage.MessageType, ManageScriptTool.HandleMessage);
            RegisterStaticTool<ManageAssetMessage>(ManageAssetMessage.MessageType, ManageAssetTool.HandleMessage);
            RegisterStaticTool<ManageEditorMessage>(ManageEditorMessage.MessageType, ManageEditorTool.HandleMessage);
            
            // More tools can be registered as they are implemented
            // RegisterTool<OtherToolMessage>("other_message_type", "other_response_type", OtherTool.HandleMessage);
        }

        /// <summary>
        /// Registers a tool for a specific message type
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="messageType">Message type to handle</param>
        /// <param name="responseType">Response type to send</param>
        /// <param name="handler">Function that handles the message</param>
        private static void RegisterTool<T>(string messageType, string responseType, Func<T, object> handler) 
            where T : ToolMessage, new()
        {
            // Store both the response type and the handler
            _toolHandlers[messageType] = new ToolHandlerInfo<T>
            {
                ResponseType = responseType,
                Handler = handler,
                IsStatic = false
            };
        }
        
        /// <summary>
        /// Registers a static tool for a specific message type
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="messageType">Message type to handle</param>
        /// <param name="responseType">Response type to send</param>
        /// <param name="handler">Static function that handles the message</param>
        private static void RegisterStaticTool<T>(string messageType, ToolContracts.MessageHandler<T> handler) 
            where T : ToolMessage, new()
        {
            // Store both the response type and the handler
            _toolHandlers[messageType] = new StaticToolHandlerInfo<T>
            {
                ResponseType = messageType,
                Handler = handler,
                IsStatic = true
            };
        }

        /// <summary>
        /// Clase para almacenar información del manejador de herramientas
        /// </summary>
        private class ToolHandlerInfo<T> where T : ToolMessage
        {
            public string ResponseType { get; set; }
            public Func<T, object> Handler { get; set; }
            public bool IsStatic { get; set; }
        }
        
        /// <summary>
        /// Clase para almacenar información del manejador de herramientas estáticas
        /// </summary>
        private class StaticToolHandlerInfo<T> where T : ToolMessage
        {
            public string ResponseType { get; set; }
            public ToolContracts.MessageHandler<T> Handler { get; set; }
            public bool IsStatic { get; set; }
        }
        #endregion

        #region TCP Client Variables
        private TcpClient _tcpClient;
        private NetworkStream _tcpStream;
        private bool _isConnected;
        private Task _messageListenerTask;
        private CancellationTokenSource _cancellationTokenSource;
        private Queue<string> _messageQueue = new Queue<string>();
        private object _queueLock = new object();
        #endregion

        #region TCP Client Methods
        private void InitializeTcpClient()
        {
            if (_tcpClient != null)
            {
                DisposeTcpClient();
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _tcpClient = new TcpClient();
        }

        private async Task ConnectTcpClientAsync()
        {
            try
            {
                InitializeTcpClient();

                string host = EditorPrefs.GetString(MCP_HOST_KEY, DEFAULT_HOST);
                int port = int.Parse(EditorPrefs.GetString(MCP_PORT_KEY, DEFAULT_MCP_PORT));

                UnityMCPEditorLogger.Log($"[UnityMCP] Connecting to server {host}:{port}...");
                await _tcpClient.ConnectAsync(host, port);
                
                _tcpStream = _tcpClient.GetStream();
                _isConnected = true;
                
                UnityMCPEditorLogger.Log($"[UnityMCP] Connected to server {host}:{port}");
                
                // Start listening for messages
                _messageListenerTask = ListenForMessagesAsync(_cancellationTokenSource.Token);
                
                // Notify the server that the plugin is connected
                await SendMessageAsync(new MessagePacket 
                { 
                    Type = "connection", 
                    Data = new Dictionary<string, object>
                    {
                        { "client", "UnityPlugin" },
                        { "version", "1.0" }
                    }
                });
            }
            catch (Exception ex)
            {
                UnityMCPEditorLogger.LogError($"[UnityMCP] Error connecting to server: {ex.Message}");
                _isConnected = false;
                DisposeTcpClient();
            }
        }

        private void DisconnectTcpClient()
        {
            if (_isConnected)
            {
                // Send disconnect message (do not await)
                try 
                {
                    var disconnectMessage = new MessagePacket 
                    { 
                        Type = "disconnect", 
                        Data = new Dictionary<string, object>
                        {
                            { "client", "UnityPlugin" }
                        }
                    };
                    
                    string json = SerializeMessage(disconnectMessage);
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    _tcpStream?.Write(data, 0, data.Length);
                }
                catch {}
                
                DisposeTcpClient();
            }
        }
        
        private void DisposeTcpClient()
        {
            _isConnected = false;
            _cancellationTokenSource?.Cancel();
            
            if (_tcpStream != null)
            {
                _tcpStream.Close();
                _tcpStream = null;
            }
            
            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient = null;
            }
            
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private async Task SendMessageAsync(MessagePacket message)
        {
            if (!_isConnected || _tcpStream == null)
            {
                UnityMCPEditorLogger.LogWarning("[UnityMCP] Cannot send message: client not connected");
                return;
            }

            try
            {
                // Convert to JSON using manual serializer
                string json = SerializeMessage(message);
                byte[] data = Encoding.UTF8.GetBytes(json);
                
                await _tcpStream.WriteAsync(data, 0, data.Length);
                UnityMCPEditorLogger.Log($"[UnityMCP] Message sent: {json}");
            }
            catch (Exception ex)
            {
                UnityMCPEditorLogger.LogError($"[UnityMCP] Error sending message: {ex.Message}");
                DisposeTcpClient();
            }
        }

        private async Task ListenForMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[Constants.BufferSize];
            
            try
            {
                while (_isConnected && !cancellationToken.IsCancellationRequested)
                {
                    // If no data is available, wait a bit
                    if (_tcpClient.Available == 0)
                    {
                        await Task.Delay(100, cancellationToken);
                        continue;
                    }
                    
                    int bytesRead = await _tcpStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        UnityMCPEditorLogger.Log($"[UnityMCP] Message received: {message}");
                        
                        // Add message to the queue to process on Unity's main thread
                        lock (_queueLock)
                        {
                            _messageQueue.Enqueue(message);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Operation intentionally cancelled
                UnityMCPEditorLogger.Log("[UnityMCP] Message listening cancelled");
            }
            catch (Exception ex)
            {
                UnityMCPEditorLogger.LogError($"[UnityMCP] Error listening for messages: {ex.Message}");
                DisposeTcpClient();
            }
        }

        private void ProcessMessages()
        {
            if (_messageQueue.Count == 0)
                return;

            List<string> currentMessages = new List<string>();
            lock (_queueLock)
            {
                while (_messageQueue.Count > 0)
                {
                    currentMessages.Add(_messageQueue.Dequeue());
                }
            }

            foreach (string json in currentMessages)
            {
                try
                {
                    // Use manual deserializer
                    MessagePacket message = DeserializeMessage(json);
                    HandleMessage(message);
                }
                catch (Exception ex)
                {
                    UnityMCPEditorLogger.LogError($"[UnityMCP] Error processing message: {ex.Message}");
                }
            }
        }

        private void HandleMessage(MessagePacket message)
        {
            switch (message.Type)
            {
                case "response":
                    object successObj;
                    message.Data.TryGetValue("success", out successObj);
                    bool success = successObj != null && Convert.ToBoolean(successObj);
                    
                    object messageObj;
                    message.Data.TryGetValue("message", out messageObj);
                    string responseMessage = messageObj as string;
                    
                    if (success)
                    {
                        UnityMCPEditorLogger.Log($"[UnityMCP] Server response: {responseMessage}");
                    }
                    else
                    {
                        UnityMCPEditorLogger.LogWarning($"[UnityMCP] Server response (error): {responseMessage}");
                    }
                    break;
                
                case Constants.MessageTypeError:
                    object errorObj;
                    message.Data.TryGetValue("message", out errorObj);
                    string errorMessage = errorObj as string;
                    UnityMCPEditorLogger.LogError($"[UnityMCP] Server error: {errorMessage}");
                    break;
                
                default:
                    // Try generic handling via tool registry
                    if (_toolHandlers.ContainsKey(message.Type))
                    {
                        HandleRegisteredToolMessage(message);
                    }
                    else
                    {
                        UnityMCPEditorLogger.Log($"[UnityMCP] Unknown message: {message.Type}");
                    }
                    break;
            }
        }

        /// <summary>
        /// Handles a message using a registered tool in the system
        /// </summary>
        /// <param name="message">The message to process</param>
        private void HandleRegisteredToolMessage(MessagePacket message)
        {
            if (!_toolHandlers.ContainsKey(message.Type))
            {
                UnityMCPEditorLogger.LogWarning($"[UnityMCP] No handler found for message type: {message.Type}");
                return;
            }

            try
            {
                // Get handler info
                var handlerInfo = _toolHandlers[message.Type];
                
                // Determine if it's a static or instance handler
                bool isStatic = (bool)handlerInfo.GetType().GetProperty("IsStatic").GetValue(handlerInfo);
                
                // Get response type and message type
                string responseType = (string)handlerInfo.GetType().GetProperty("ResponseType").GetValue(handlerInfo);
                Type handlerInfoType = handlerInfo.GetType();
                Type messageType = handlerInfoType.GetGenericArguments()[0];
                
                // Deserialize to the specific type
                var toolMessage = JsonConvert.DeserializeObject(
                    JsonConvert.SerializeObject(message.Data), 
                    messageType
                );
                
                if (toolMessage == null)
                {
                    throw new Exception($"Could not deserialize to type {messageType.Name}");
                }

                // Invoke the handler (static or instance)
                object result;
                
                if (isStatic)
                {
                    // Static handler
                    var handler = handlerInfoType.GetProperty("Handler").GetValue(handlerInfo);
                    result = handler.GetType().GetMethod("Invoke").Invoke(
                        handler, 
                        new object[] { toolMessage }
                    );
                }
                else
                {
                    // Instance handler
                    var handler = handlerInfoType.GetProperty("Handler").GetValue(handlerInfo);
                    result = handler.GetType().GetMethod("Invoke").Invoke(
                        handler, 
                        new object[] { toolMessage }
                    );
                }
                
                // Prepare the response
                var responsePacket = new MessagePacket
                {
                    Type = responseType,
                    Id = message.Id // Keep the ID for correlation
                };
                
                // Process the result
                var responseData = FormatToolResponse(result);
                responsePacket.Data = responseData;
                
                // Send the response
                _ = SendMessageAsync(responsePacket);
            }
            catch (Exception ex)
            {
                string errorMessage = $"[UnityMCP] Error processing message {message.Type}: {ex.Message}";
                UnityMCPEditorLogger.LogError(errorMessage);
                _ = SendMessageAsync(MessagePacket.CreateError(errorMessage).WithCorrelationId(message.Id));
            }
        }

        /// <summary>
        /// Formats the result of a tool to send as a response.
        /// </summary>
        /// <param name="result">Result from the tool handler</param>
        /// <returns>Dictionary with formatted data</returns>
        private Dictionary<string, object> FormatToolResponse(object result)
        {
            var responseData = new Dictionary<string, object>();
            
            // Determine if it's a Response object or a generic object
            if (result != null && result.GetType().GetProperty("Success") != null)
            {
                // It's a Response object, extract fields
                bool success = (bool)result.GetType().GetProperty("Success").GetValue(result);
                string responseMessage = (string)result.GetType().GetProperty("Message").GetValue(result);
                object data = result.GetType().GetProperty("Data")?.GetValue(result);
                
                responseData["success"] = success;
                responseData["message"] = responseMessage;
                
                if (data != null)
                {
                    responseData["data"] = data;
                }
            }
            else
            {
                // Treat as generic object
                responseData["success"] = true;
                responseData["message"] = "Operation completed successfully";
                responseData["data"] = result;
            }
            
            return responseData;
        }
        
        // Manual serialization/deserialization methods for compatibility with System.Text.Json on the server
        private string SerializeMessage(MessagePacket message)
        {
            var result = JsonConvert.SerializeObject(message);
            return result;
        }
        
        private MessagePacket DeserializeMessage(string json)
        {
            // This is a basic and limited implementation
            // In a real case, use a full-featured JSON library compatible with Unity

            MessagePacket message;
            try
            {
                message = JsonConvert.DeserializeObject<MessagePacket>(json);
            }
            catch (Exception ex)
            {
                UnityMCPEditorLogger.LogError($"[UnityMCP] Error deserializing message: {json}");
                UnityMCPEditorLogger.LogError($"[UnityMCP] Error deserializing JSON: {ex.Message}");
                throw;
            }
            
            return message;
        }
        
        // To be called from OnGUI or Update
        private void UpdateTcpConnection()
        {
            // Process received messages
            if (_isConnected)
            {
                ProcessMessages();
            }
        }
        #endregion
    }
} 