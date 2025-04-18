using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UnityMCP.Bridge.Shared.Messages;
using UnityMCP.Bridge.Shared.Messages.Abstractions;

namespace UnityMCP.Server
{
    public partial class TcpServer
    {
        #region Tool Registry

        // Tools registry
        private static readonly Dictionary<string, object> _toolHandlers = new();

        private void RegisterTools()
        {
            // Register available tools
            RegisterTool<ReadConsoleMessage>(ReadConsoleMessage.MessageType, HandleConsoleMessage);
            RegisterTool<ExecuteMenuItemMessage>(ExecuteMenuItemMessage.MessageType, HandleExecuteMenuItemMessage);
            RegisterTool<ManageGameObjectMessage>(ManageGameObjectMessage.MessageType, HandleManageGameObjectMessage);
            RegisterTool<ManageSceneMessage>(ManageSceneMessage.MessageType, HandleManageSceneMessage);
            RegisterTool<ManageScriptMessage>(ManageScriptMessage.MessageType, HandleManageScriptMessage);
            RegisterTool<ManageAssetMessage>(ManageAssetMessage.MessageType, HandleManageAssetMessage);
            RegisterTool<ManageEditorMessage>(ManageEditorMessage.MessageType, HandleManageEditorMessage);
        }

        private void RegisterTool<T>(string messageType, Func<T, Task<MessagePacket>> handler) where T : ToolMessage
        {
            _toolHandlers[messageType] = new ToolHandlerInfo<T>
            {
                Handler = handler
            };
            _logger.LogInformation($"Tool registered for message type: {messageType}");
        }

        private class ToolHandlerInfo<T> where T : ToolMessage
        {
            public Func<T, Task<MessagePacket>> Handler { get; set; }
        }

        private async Task<MessagePacket> HandleRegisteredToolMessage(MessagePacket message)
        {
            if (!_toolHandlers.ContainsKey(message.Type))
            {
                throw new InvalidOperationException($"No handler found for message type: {message.Type}");
            }

            try
            {
                var handlerInfo = _toolHandlers[message.Type];
                var handlerInfoType = handlerInfo.GetType();
                var messageType = handlerInfoType.GetGenericArguments()[0];
                
                // Deserialize to the specific type
                var toolMessage = JsonSerializer.Deserialize(
                    JsonSerializer.Serialize(message.Data), 
                    messageType
                ) as ToolMessage;

                if (toolMessage == null)
                {
                    throw new Exception($"Could not deserialize to type {messageType.Name}");
                }

                // Invoke the handler
                var handler = handlerInfoType.GetProperty("Handler").GetValue(handlerInfo);
                var result = await (Task<MessagePacket>)handler.GetType().GetMethod("Invoke").Invoke(
                    handler, 
                    new object[] { toolMessage }
                );

                return result;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error processing message {message.Type}: {ex.Message}";
                _logger.LogError(errorMessage);
                return MessagePacket.CreateError(errorMessage);
            }
        }

        #endregion

        #region Tool Handlers

        private async Task<MessagePacket> HandleConsoleMessage(ReadConsoleMessage message)
        {
            _logger.LogInformation($"Console message received: {JsonSerializer.Serialize(message)}");
            return MessagePacket.CreateResponse(true, "Console message processed successfully");
        }

        private async Task<MessagePacket> HandleExecuteMenuItemMessage(ExecuteMenuItemMessage message)
        {
            _logger.LogInformation($"ExecuteMenuItem message received: {JsonSerializer.Serialize(message)}");
            return MessagePacket.CreateResponse(true, "ExecuteMenuItem message processed correctamente");
        }

        private async Task<MessagePacket> HandleManageGameObjectMessage(ManageGameObjectMessage message)
        {
            _logger.LogInformation($"ManageGameObject message received: {JsonSerializer.Serialize(message)}");
            return MessagePacket.CreateResponse(true, "ManageGameObject message processed successfully");
        }

        private async Task<MessagePacket> HandleManageSceneMessage(ManageSceneMessage message)
        {
            _logger.LogInformation($"ManageScene message received: {JsonSerializer.Serialize(message)}");
            return MessagePacket.CreateResponse(true, "ManageScene message processed successfully");
        }

        private async Task<MessagePacket> HandleManageScriptMessage(ManageScriptMessage message)
        {
            _logger.LogInformation($"ManageScript message received: {JsonSerializer.Serialize(message)}");
            return MessagePacket.CreateResponse(true, "ManageScript message processed successfully");
        }

        private async Task<MessagePacket> HandleManageAssetMessage(ManageAssetMessage message)
        {
            _logger.LogInformation($"ManageAsset message received: {JsonSerializer.Serialize(message)}");
            return MessagePacket.CreateResponse(true, "ManageAsset message processed successfully");
        }

        private async Task<MessagePacket> HandleManageEditorMessage(ManageEditorMessage message)
        {
            _logger.LogInformation($"ManageEditor message received: {JsonSerializer.Serialize(message)}");
            return MessagePacket.CreateResponse(true, "ManageEditor message processed successfully");
        }
        
        
        


        #endregion
    }
} 