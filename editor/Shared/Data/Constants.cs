namespace UnityMCP.Bridge.Shared.Data
{
    public static class Constants
    {
        // Configuración de red
        public const int DefaultTcpPort = 6500;
        public const string DefaultTcpPortStr = "6500";
        public const int BufferSize = 524288; // 512KB (512 * 1024 bytes)

        // Timeouts
        public const int DefaultResponseTimeoutSeconds = 10;
        
        // Tipos de mensajes
        public const string MessageTypeError = "error";
    }
}