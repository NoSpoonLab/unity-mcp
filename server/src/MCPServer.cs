using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UnityMCP.Bridge.Shared.Data;
using UnityMCP.Server.Tools;

namespace UnityMCP.Server{
    class McpServer {
        
        #region Properties
        private HostApplicationBuilder? _builder;
        private IHost? _host;
        private TcpServer? _tcpServer;
        private readonly int _tcpPort = Constants.DefaultTcpPort; // TCP port for communication with Unity
        private ILogger<McpServer>? _logger;
        #endregion

        #region Methods
        public async Task Initialize(string[] args) {
            Console.WriteLine("[UnityMCP] Initializing the server.");
            // Initialize the builder
            _builder = Host.CreateApplicationBuilder(args);

            Console.WriteLine("[UnityMCP] Configuring the builder.");
            // Configure the builder
            _builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithTools<ReadConsole>()
                .WithTools<ExecuteMenuItem>()
                .WithTools<ManageGameObject>()
                .WithTools<ManageScene>()
                .WithTools<ManageScripts>()
                .WithTools<ManageAssets>()
                .WithTools<ManageEditor>();


            // Configure the logging
            _builder.Logging.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });

            Console.WriteLine("[UnityMCP] Adding the tools from the assembly.");
            _builder.Services.AddSingleton(_ =>
            {
                return new HttpClient();
            });

            // Add TcpServer service as singleton
            _builder.Services.AddSingleton<TcpServer>(serviceProvider => 
            {
                var logger = serviceProvider.GetRequiredService<ILogger<TcpServer>>();
                return new TcpServer(_tcpPort, logger);
            });

            Console.WriteLine("[UnityMCP] Building the application.");
            // Build the application
            _host = _builder.Build();

            // Start the TCP server
            _logger = _host.Services.GetRequiredService<ILogger<McpServer>>();
            _tcpServer = _host.Services.GetRequiredService<TcpServer>();
            
            _logger.LogInformation($"Starting TCP server on port {_tcpPort}");
            // Start the TCP server in a separate thread to avoid blocking
            _ = Task.Run(() => _tcpServer.Start());

            Console.WriteLine("[UnityMCP] Running the application.");
            // Run the application
            await _host.RunAsync();
            Console.WriteLine("[UnityMCP] Application stopped.");
            
            // Stop the TCP server when the application stops
            _tcpServer?.Stop();
        }

        #endregion
    }
}
