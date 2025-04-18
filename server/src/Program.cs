
namespace UnityMCP.Server{
    class Program {
        public static async Task Main(string[] args){
            McpServer mcpServer = new McpServer();
            await mcpServer.Initialize(args);
        }
    }
}