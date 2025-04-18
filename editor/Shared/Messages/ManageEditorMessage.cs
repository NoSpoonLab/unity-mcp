using UnityMCP.Bridge.Shared.Messages.Abstractions;

namespace UnityMCP.Bridge.Shared.Messages
{
    public class ManageEditorMessage : ToolMessage
    {
        public string Action { get; set; }
        public bool? WaitForCompletion { get; set; }
        public string ToolName { get; set; }
        public string TagName { get; set; }
        public string LayerName { get; set; }

        public ManageEditorMessage() : base("manage_editor")
        {
        }

        public ManageEditorMessage(
            string action,
            bool? waitForCompletion = null,
            string toolName = null,
            string tagName = null,
            string layerName = null
        ) : base("manage_editor")
        {
            Action = action;
            WaitForCompletion = waitForCompletion;
            ToolName = toolName;
            TagName = tagName;
            LayerName = layerName;
        }

        public const string MessageType = "manage_editor";
    }
} 