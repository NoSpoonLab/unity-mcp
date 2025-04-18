using ModelContextProtocol.Server;
using System.ComponentModel;
using UnityMCP.Bridge.Shared.Data;
using UnityMCP.Bridge.Shared.Messages;
using System.Text;

namespace UnityMCP.Server.Tools
{
    [McpServerToolType]
    public sealed class ManageScripts
    {
        [McpServerTool, Description("Manages C# scripts in Unity (create, read, update, delete). Make reference variables public for easier access in the Unity Editor.")]
        public static async Task<Dictionary<string, object>> ManageScriptCommand(
            TcpServer tcpServer,
            [Description("Operation ('create', 'read', 'update', 'delete').")] string action,
            [Description("Script name (no .cs extension). ")] string name = null,
            [Description("Asset path (default: 'Assets/').")] string path = null,
            [Description("C# code for 'create'/'update'.")] string contents = null,
            [Description("Type hint (e.g., 'MonoBehaviour').")] string scriptType = null,
            [Description("Script namespace.")] string @namespace = null
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
                    { "namespace", @namespace },
                    { "scriptType", scriptType }
                };

                // Codifica el contenido en base64 si corresponde
                if (contents != null)
                {
                    if (action == "create" || action == "update")
                    {
                        var encodedContents = Convert.ToBase64String(Encoding.UTF8.GetBytes(contents));
                        paramsDict["encodedContents"] = encodedContents;
                        paramsDict["contentsEncoded"] = true;
                    }
                    else
                    {
                        paramsDict["contents"] = contents;
                    }
                }

                // Elimina valores nulos para no enviar nulls innecesarios
                var cleanParams = paramsDict.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);

                // Crea el mensaje para Unity
                var message = new MessagePacket
                {
                    Type = "manage_script", // Debe coincidir con el handler de Unity
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
                else if (response.Type == "manage_script")
                {
                    object resultData = null;
                    if (response.Data.TryGetValue("data", out var dataObj) && dataObj is Dictionary<string, object> dataDict)
                    {
                        // Si el contenido viene codificado, lo decodificamos
                        if (dataDict.TryGetValue("contentsEncoded", out var encodedFlag) && encodedFlag is bool flag && flag)
                        {
                            if (dataDict.TryGetValue("encodedContents", out var encodedContentObj) && encodedContentObj is string encodedContent)
                            {
                                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encodedContent));
                                dataDict["contents"] = decoded;
                                dataDict.Remove("encodedContents");
                                dataDict.Remove("contentsEncoded");
                            }
                        }
                        resultData = dataDict;
                    }

                    return new Dictionary<string, object>
                    {
                        { "success", true },
                        { "message", response.Data.TryGetValue("message", out var successMsg) ? 
                            successMsg?.ToString() : "Operation successful." },
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
                    { "message", $"Error managing script: {ex.Message}" },
                    { "data", null }
                };
            }
        }
    }
} 