using UnityMCP.Bridge.Shared.Messages.Abstractions;

namespace UnityMCP.Bridge.Shared.Messages
{
    public class ManageSceneMessage : ToolMessage
    {
        public string Action { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public int? BuildIndex { get; set; }

        public ManageSceneMessage() : base("manage_scene")
        {
        }

        public ManageSceneMessage(
            string action,
            string name = null,
            string path = null,
            int? buildIndex = null
        ) : base("manage_scene")
        {
            Action = action;
            Name = name;
            Path = path;
            BuildIndex = buildIndex;
        }

        public const string MessageType = "manage_scene";
    }
} 