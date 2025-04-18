using System.Collections.Generic;
using UnityMCP.Bridge.Shared.Messages.Abstractions;

namespace UnityMCP.Bridge.Shared.Messages
{
    public class ExecuteMenuItemMessage : ToolMessage
    {
        public string MenuPath { get; set; }
        public Dictionary<string, object> Parameters { get; set; }

        public ExecuteMenuItemMessage() : base("execute")
        {
            Parameters = new Dictionary<string, object>();
        }

        public ExecuteMenuItemMessage(
            string action = "execute",
            string menuPath = null,
            Dictionary<string, object> parameters = null) : base(action)
        {
            MenuPath = menuPath;
            Parameters = parameters ?? new Dictionary<string, object>();
        }

        public const string MessageType = "execute_menu_item";
    }
} 