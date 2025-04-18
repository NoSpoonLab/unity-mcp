using ModelContextProtocol.Server;
using System.ComponentModel;
using UnityMCP.Bridge.Shared.Data;
using UnityMCP.Bridge.Shared.Messages;

namespace UnityMCP.Server.Tools
{
    [McpServerToolType]
    public sealed class ManageEditor
    {
        [McpServerTool, Description("Controls and queries the Unity editor's state and settings.")]
        public static async Task<Dictionary<string, object>> ManageEditorCommand(
            TcpServer tcpServer,
            [Description("Operation (e.g., 'play', 'pause', 'get_state', 'set_active_tool', 'add_tag').")] string action,
            [Description("If true, waits for certain actions to complete.")] bool? waitForCompletion = null,
            [Description("Name of the tool to activate (for 'set_active_tool').")] string toolName = null,
            [Description("Name of the tag to add (for 'add_tag').")] string tagName = null,
            [Description("Name of the layer to add (for 'add_layer').")] string layerName = null
        )
        {
            try
            {
                // Prepara el diccionario de parámetros
                var paramsDict = new Dictionary<string, object>
                {
                    { "action", action },
                    { "waitForCompletion", waitForCompletion },
                    { "toolName", toolName },
                    { "tagName", tagName },
                    { "layerName", layerName }
                };

                // Elimina valores nulos para no enviar nulls innecesarios
                var cleanParams = paramsDict.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);

                // Crea el mensaje para Unity
                var message = new MessagePacket
                {
                    Type = "manage_editor", // Debe coincidir con el handler de Unity
                    Data = cleanParams
                };

                // Envía el mensaje y espera la respuesta
                var response = await tcpServer.SendAndWaitForResponseAsync(message, TimeSpan.FromSeconds(10));

                // Procesa la respuesta
                if (response.Type == Constants.MessageTypeError)
                {
                    return new Dictionary<string, object>
                    {
                        { "success", false },
                        { "message", response.Data.TryGetValue("message", out var errorMsg) ? errorMsg?.ToString() : "Unknown error" },
                        { "data", null }
                    };
                }
                else if (response.Type == "manage_editor")
                {
                    object resultData = null;
                    if (response.Data.TryGetValue("data", out var data))
                    {
                        resultData = data;
                    }

                    return new Dictionary<string, object>
                    {
                        { "success", true },
                        { "message", response.Data.TryGetValue("message", out var successMsg) ? 
                            successMsg?.ToString() : "Editor operation successful." },
                        { "data", resultData }
                    };
                }
                else
                {
                    return new Dictionary<string, object>
                    {
                        { "success", false },
                        { "message", "Unexpected response type from Unity client" },
                        { "data", null }
                    };
                }
            }
            catch (TimeoutException)
            {
                return new Dictionary<string, object>
                {
                    { "success", false },
                    { "message", "Timeout waiting for Unity client response" },
                    { "data", null }
                };
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>
                {
                    { "success", false },
                    { "message", $"Error managing editor: {ex.Message}" },
                    { "data", null }
                };
            }
        }
    }
} 