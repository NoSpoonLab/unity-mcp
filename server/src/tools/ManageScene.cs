using ModelContextProtocol.Server;
using System.ComponentModel;
using UnityMCP.Bridge.Shared.Data;
using UnityMCP.Bridge.Shared.Messages;

namespace UnityMCP.Server.Tools
{
    [McpServerToolType]
    public sealed class ManageScene
    {
        [McpServerTool, Description("Manages Unity scenes (load, save, create, get hierarchy, etc.).")]
        public static async Task<Dictionary<string, object>> ManageSceneCommand(
            TcpServer tcpServer,
            [Description("Operation (e.g., 'load', 'save', 'create', 'get_hierarchy').")] string action,
            [Description("Scene name (no extension) for create/load/save.")] string name = null,
            [Description("Asset path for scene operations (default: 'Assets/').")] string path = null,
            [Description("Build index for load/build settings actions.")] int? buildIndex = null
        )
        {
            try
            {
                // Prepara el diccionario de parámetros
                var paramsDict = new Dictionary<string, object>
                {
                    { "action", action },
                    { "name", name },
                    { "path", path },
                    { "buildIndex", buildIndex }
                };

                // Elimina valores nulos para no enviar nulls innecesarios
                var cleanParams = paramsDict.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);

                // Crea el mensaje para Unity
                var message = new MessagePacket
                {
                    Type = "manage_scene", // Debe coincidir con el handler de Unity
                    Data = cleanParams
                };

                // Envía el mensaje y espera la respuesta
                var response = await tcpServer.SendAndWaitForResponseAsync(message, TimeSpan.FromSeconds(15));

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
                else if (response.Type == "manage_scene")
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
                            successMsg?.ToString() : "Scene operation successful." },
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
                    { "message", $"Error managing scene: {ex.Message}" },
                    { "data", null }
                };
            }
        }
    }
} 