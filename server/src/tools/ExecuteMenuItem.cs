using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using UnityMCP.Bridge.Shared.Data;
using UnityMCP.Bridge.Shared.Messages;

namespace UnityMCP.Server.Tools
{
    [McpServerToolType]
    public sealed class ExecuteMenuItem
    {
        [McpServerTool, Description("Executes a Unity Editor menu item via its path(e.g., \"File/Save Project\").")]
        public static async Task<Dictionary<string, object>> ExecuteMenuItemCommand(
            TcpServer tcpServer,
            [Description(MenuPaths)] string menuPath,
            [Description("The operation to perform (default: 'execute')")] string action = "execute",
            [Description("Parameters for the menu item (rarely used), but if parameters is not passed, then always pass null")] Dictionary<string, object> parameters = null)
        {
            try
            {
                // Normaliza el valor de action
                action = string.IsNullOrEmpty(action) ? "execute" : action.ToLower();

                // Prepara el diccionario de parámetros
                var paramsDict = new Dictionary<string, object>
                {
                    { "action", action },
                    { "menuPath", menuPath },
                    { "parameters", parameters ?? new Dictionary<string, object>() }
                };

                // Crea el mensaje para Unity
                var message = new MessagePacket
                {
                    Type = "execute_menu_item", // Debe coincidir con el handler de Unity
                    Data = paramsDict
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
                else if (response.Type == "execute_menu_item")
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
                            successMsg?.ToString() : "Menu item executed successfully" },
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
                    { "message", $"Error executing menu item: {ex.Message}" },
                    { "data", null }
                };
            }
        }

        // The full path of the menu item to execute, available:
        public const string MenuPaths =
@"The full path of the menu item to execute,(e.g., ""File/Save""), available:
- File/New Scene
- File/Open Scene
- File/Save
- File/Save As...
- File/Save As Scene Template...
- File/New Project...
- File/Open Project...
- File/Save Project
- File/Build Profiles
- File/Build And Run
- File/Exit

- Edit/Undo Paste Global Light 2D
- Edit/Redo
- Edit/Undo History
- Edit/Select All
- Edit/Deselect All
- Edit/Select Children
- Edit/Select Prefab Root
- Edit/Invert Selection
- Edit/Cut
- Edit/Copy
- Edit/Paste
- Edit/Duplicate
- Edit/Rename
- Edit/Delete
- Edit/Frame Selected in Scene
- Edit/Frame Selected in Window under Cursor
- Edit/Lock View to Selected
- Edit/Play
- Edit/Pause
- Edit/Step
- Edit/Project Settings...
- Edit/Preferences...
- Edit/Shortcuts...
- Edit/Clear All PlayerPrefs

- GameObject/Create Empty
- GameObject/Create Empty Child
- GameObject/Create Empty Parent
- GameObject/Center On Children
- GameObject/Make Parent
- GameObject/Clear Parent
- GameObject/Set as first sibling
- GameObject/Set as last sibling
- GameObject/Move To View
- GameObject/Align With View
- GameObject/Align View to Selected
- GameObject/Toggle Active State";
    }
}
