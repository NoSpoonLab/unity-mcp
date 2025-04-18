using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using UnityMCP.Bridge.Shared.Data;
using UnityMCP.Bridge.Shared.Messages;

namespace UnityMCP.Server.Tools
{
    [McpServerToolType]
    public sealed class ManageAssets
    {
        [McpServerTool, Description("Performs asset operations (import, create, modify, delete, etc.) in Unity.")]
        public static async Task<Dictionary<string, object>> ManageAssetCommand(
            TcpServer tcpServer,
            [Description("Operation to perform (e.g., 'import', 'create', 'modify', 'delete', 'duplicate', 'move', 'rename', 'search', 'get_info', 'create_folder', 'get_components').")] string action,
            [Description("Asset path (e.g., 'Materials/MyMaterial.mat') or search scope.")] string path,
            [Description("Asset type (e.g., 'Material', 'Folder') - required for 'create'.")] string assetType = null,
            [Description("Dictionary of properties for 'create'/'modify'.")] Dictionary<string, object> properties = null,
            [Description("Target path for 'duplicate'/'move'.")] string destination = null,
            [Description("Whether to generate a preview for the asset.")] bool generatePreview = false,
            [Description("Search pattern (e.g., '*.prefab').")] string searchPattern = null,
            [Description("Filter by asset type for search.")] string filterType = null,
            [Description("Filter by date (ISO 8601) for search.")] string filterDateAfter = null,
            [Description("Page size for search results.")] int? pageSize = null,
            [Description("Page number for search results.")] int? pageNumber = null
        )
        {
            try
            {
                // Asegura que properties no sea null
                properties ??= new Dictionary<string, object>();

                // Prepara el diccionario de parámetros
                var paramsDict = new Dictionary<string, object>
                {
                    { "action", action.ToLower() },
                    { "path", path },
                    { "assetType", assetType },
                    { "properties", properties },
                    { "destination", destination },
                    { "generatePreview", generatePreview },
                    { "searchPattern", searchPattern },
                    { "filterType", filterType },
                    { "filterDateAfter", filterDateAfter },
                    { "pageSize", pageSize },
                    { "pageNumber", pageNumber }
                };

                // Elimina valores nulos para no enviar nulls innecesarios
                var cleanParams = paramsDict.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);

                // Crea el mensaje para Unity
                var message = new MessagePacket
                {
                    Type = "manage_asset", // Debe coincidir con el handler de Unity
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
                else if (response.Type == "manage_asset")
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
                            successMsg?.ToString() : "Asset operation completed successfully" },
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
                    { "message", $"Error managing asset: {ex.Message}" },
                    { "data", null }
                };
            }
        }
    }
} 