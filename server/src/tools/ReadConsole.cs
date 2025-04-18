using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using UnityMCP.Bridge.Shared.Data;
using UnityMCP.Bridge.Shared.Messages;

namespace UnityMCP.Server.Tools
{
    [McpServerToolType]
    public sealed class ReadConsole
    {
        [McpServerTool, Description("Gets messages from or clears the Unity Editor console.")]
        public static async Task<Dictionary<string, object>> ReadConsoleMessages(
            TcpServer tcpServer,
            [Description("Operation ('get' or 'clear')")] string action = "get",
            [Description("Message types to get ('error', 'warning', 'log', 'all'), eg: '[\"error\", \"warning\", \"log\", \"all\"]'")] string[] types = null,
            [Description("Max messages to return")] int? count = null,
            [Description("Text filter for messages")] string filterText = null,
            [Description("Get messages after this timestamp (ISO 8601)")] string sinceTimestamp = null,
            [Description("Output format ('plain', 'detailed', 'json')")] string format = "detailed",
            [Description("Include stack traces in output")] bool includeStacktrace = true)
        {
            try
            {
                // Create the Read Console message
                var readMessage = new ReadConsoleMessage(
                    action,
                    types,
                    count,
                    filterText,
                    sinceTimestamp,
                    format,
                    includeStacktrace
                );

                // Create message for Unity client
                var message = new MessagePacket
                {
                    Type = ReadConsoleMessage.MessageType,
                    Data = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(readMessage))
                };

                // Send message to Unity client and wait for response
                var response = await tcpServer.SendAndWaitForResponseAsync(message, TimeSpan.FromSeconds(10));

                // Process response
                if (response.Type == Constants.MessageTypeError)
                {
                    return new Dictionary<string, object>
                    {
                        { "success", false },
                        { "message", response.Data.TryGetValue("message", out var errorMsg) ? errorMsg?.ToString() : "Unknown error" },
                        { "data", null }
                    };
                }
                else if (response.Type == ReadConsoleMessage.MessageType)
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
                            successMsg?.ToString() : "Console messages retrieved successfully" },
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
                    { "message", $"Error reading console messages: {ex.Message}" },
                    { "data", null }
                };
            }
        }
    }
}