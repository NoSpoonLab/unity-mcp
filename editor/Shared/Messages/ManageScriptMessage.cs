using UnityMCP.Bridge.Shared.Messages.Abstractions;

namespace UnityMCP.Bridge.Shared.Messages
{
    public class ManageScriptMessage : ToolMessage
    {
        public string Action { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Contents { get; set; }
        public string ScriptType { get; set; }
        public string Namespace { get; set; }
        public string EncodedContents { get; set; }
        public bool? ContentsEncoded { get; set; }

        public ManageScriptMessage() : base("manage_script")
        {
        }

        public ManageScriptMessage(
            string action,
            string name = null,
            string path = null,
            string contents = null,
            string scriptType = null,
            string @namespace = null,
            string encodedContents = null,
            bool? contentsEncoded = null
        ) : base("manage_script")
        {
            Action = action;
            Name = name;
            Path = path;
            Contents = contents;
            ScriptType = scriptType;
            Namespace = @namespace;
            EncodedContents = encodedContents;
            ContentsEncoded = contentsEncoded;
        }

        public const string MessageType = "manage_script";
    }
} 