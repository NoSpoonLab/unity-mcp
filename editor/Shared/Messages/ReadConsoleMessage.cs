using UnityMCP.Bridge.Shared.Messages.Abstractions;
namespace UnityMCP.Bridge.Shared.Messages
{
    public class ReadConsoleMessage : ToolMessage
    {
        public string[] Types { get; set; }
        public int? Count { get; set; }
        public string FilterText { get; set; }
        public string SinceTimestamp { get; set; }
        public string Format { get; set; }
        public bool IncludeStacktrace { get; set; }

        public ReadConsoleMessage() : base("get")
        {
            // Valores por defecto
            Types = new[] { "error", "warning", "log" };
            Format = "detailed";
            IncludeStacktrace = true;
        }

        public ReadConsoleMessage(
            string action = "get",
            string[] types = null,
            int? count = null,
            string filterText = null,
            string sinceTimestamp = null,
            string format = "detailed",
            bool includeStacktrace = true) : base(action)
        {
            Types = types ?? new[] { "error", "warning", "log" };
            Count = count;
            FilterText = filterText;
            SinceTimestamp = sinceTimestamp;
            Format = format;
            IncludeStacktrace = includeStacktrace;
        }
        
        public const string MessageType = "read_console";
    }
} 