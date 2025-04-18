using ModelContextProtocol.Server;
using System.ComponentModel;
using UnityMCP.Bridge.Shared.Data;
using UnityMCP.Bridge.Shared.Messages;

namespace UnityMCP.Server.Tools
{
    [McpServerToolType]
    public sealed class ManageGameObject
    {
        [McpServerTool, Description("Manages GameObjects: create, modify, delete, find, and component operations.")]
        public static async Task<Dictionary<string, object>> ManageGameObjectCommand(
            TcpServer tcpServer,
            [Description("Operation (e.g., 'create', 'modify', 'find', 'add_component', 'remove_component', 'set_component_property').")] string action,
            [Description("GameObject identifier (name or path string) for modify/delete/component actions.")] string target = null,
            [Description("How to find objects ('by_name', 'by_id', 'by_path', etc.).")] string searchMethod = null,
            [Description("GameObject name - used for both 'create' (initial name) and 'modify' (rename).")] string name = null,
            [Description("Tag name - used for both 'create' (initial tag) and 'modify' (change tag).")] string tag = null,
            [Description("Parent GameObject reference - used for both 'create' (initial parent) and 'modify' (change parent).")] string parent = null,
            [Description("Position as [x, y, z].")] List<float> position = null,
            [Description("Rotation as [x, y, z].")] List<float> rotation = null,
            [Description("Scale as [x, y, z].")] List<float> scale = null,
            [Description("List of component names to add.")] List<string> componentsToAdd = null,
            [Description("Primitive type for creation.")] string primitiveType = null,
            [Description("Whether to save as prefab.")] bool saveAsPrefab = false,
            [Description("Prefab path.")] string prefabPath = null,
            [Description("Prefab folder (default: 'Assets/Prefabs').")] string prefabFolder = "Assets/Prefabs",
            [Description("Set active state.")] bool? setActive = null,
            [Description("Layer name.")] string layer = null,
            [Description("List of component names to remove.")] List<string> componentsToRemove = null,
            [Description("Dict mapping Component names to their properties to set.")] Dictionary<string, Dictionary<string, object>> componentProperties = null,
            [Description("Search term for 'find'.")] string searchTerm = null,
            [Description("If true, find all matching objects.")] bool findAll = false,
            [Description("If true, search in children.")] bool searchInChildren = false,
            [Description("If true, search inactive objects.")] bool searchInactive = false,
            [Description("Component name for component actions.")] string componentName = null
        )
        {
            try
            {
                // Prepara el diccionario de parámetros
                var paramsDict = new Dictionary<string, object>
                {
                    { "action", action },
                    { "target", target },
                    { "searchMethod", searchMethod },
                    { "name", name },
                    { "tag", tag },
                    { "parent", parent },
                    { "position", position },
                    { "rotation", rotation },
                    { "scale", scale },
                    { "componentsToAdd", componentsToAdd },
                    { "primitiveType", primitiveType },
                    { "saveAsPrefab", saveAsPrefab },
                    { "prefabPath", prefabPath },
                    { "prefabFolder", prefabFolder },
                    { "setActive", setActive },
                    { "layer", layer },
                    { "componentsToRemove", componentsToRemove },
                    { "componentProperties", componentProperties },
                    { "searchTerm", searchTerm },
                    { "findAll", findAll },
                    { "searchInChildren", searchInChildren },
                    { "searchInactive", searchInactive },
                    { "componentName", componentName }
                };

                // Lógica especial para prefabPath
                if (action == "create" && saveAsPrefab)
                {
                    if (string.IsNullOrEmpty(prefabPath))
                    {
                        if (string.IsNullOrEmpty(name))
                        {
                            return new Dictionary<string, object>
                            {
                                { "success", false },
                                { "message", "Cannot create default prefab path: 'name' parameter is missing." }
                            };
                        }
                        // Usa prefabFolder y name para construir el path
                        var constructedPath = $"{prefabFolder}/{name}.prefab".Replace("\\", "/");
                        paramsDict["prefabPath"] = constructedPath;
                    }
                    else if (!prefabPath.ToLower().EndsWith(".prefab"))
                    {
                        return new Dictionary<string, object>
                        {
                            { "success", false },
                            { "message", $"Invalid prefabPath: '{prefabPath}' must end with .prefab" }
                        };
                    }
                }
                // Elimina prefabFolder antes de enviar
                paramsDict.Remove("prefabFolder");

                // Elimina valores nulos para no enviar nulls innecesarios
                var cleanParams = paramsDict.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);

                // Crea el mensaje para Unity
                var message = new MessagePacket
                {
                    Type = "manage_gameobject", // Debe coincidir con el handler de Unity
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
                else if (response.Type == "manage_gameobject")
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
                            successMsg?.ToString() : "GameObject operation successful." },
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
                    { "message", $"Error managing GameObject: {ex.Message}" },
                    { "data", null }
                };
            }
        }
    }
} 