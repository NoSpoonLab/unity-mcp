using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UnityMCP.Bridge.Shared.Data;
using UnityMCP.Bridge.Shared.Messages;
using System.Collections.Generic;

namespace UnityMCP.Server
{
    public partial class TcpServer
    {
        #region Variables

        private TcpListener _listener;
        private readonly int _port;
        private bool _isRunning;
        private readonly ILogger<TcpServer> _logger;
        private List<TcpClient> _clients = new List<TcpClient>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        
        // Request/response handling system
        private readonly Dictionary<string, TaskCompletionSource<MessagePacket>> _pendingResponses = new Dictionary<string, TaskCompletionSource<MessagePacket>>();
        private readonly object _pendingResponsesLock = new object();

        #endregion

        #region Constructors
        
        public TcpServer(int port, ILogger<TcpServer> logger)
        {
            _port = port;
            _logger = logger;
            RegisterTools();
        }

        #endregion

        #region Methods

        public async Task Start()
        {
            if (_isRunning)
                return;

            _listener = new TcpListener(IPAddress.Any, _port);
            _isRunning = true;

            try
            {
                _listener.Start();
                _logger.LogInformation($"TCP Server started on port {_port}");

                while (_isRunning)
                {
                    try
                    {
                        var tcpClient = await _listener.AcceptTcpClientAsync();
                        _clients.Add(tcpClient);
                        _logger.LogInformation("New client connected");
                        
                        // Start task to handle this client
                        _ = HandleClientAsync(tcpClient, _cancellationTokenSource.Token);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError($"Error accepting client: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in TCP server: {ex.Message}");
                Stop();
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _cancellationTokenSource.Cancel();
            
            foreach (var client in _clients)
            {
                try
                {
                    client.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error closing client: {ex.Message}");
                }
            }
            
            _clients.Clear();
            _listener?.Stop();
            _logger.LogInformation("TCP Server stopped");
            
            // Cancel all pending requests
            lock (_pendingResponsesLock)
            {
                foreach (var response in _pendingResponses.Values)
                {
                    response.TrySetCanceled();
                }
                _pendingResponses.Clear();
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = new byte[Constants.BufferSize]; // 512KB buffer
                    
                    while (_isRunning && !cancellationToken.IsCancellationRequested)
                    {
                        int bytesRead = 0;
                        
                        try
                        {
                            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        }
                        catch (IOException)
                        {
                            break; // Client disconnected
                        }
                        
                        if (bytesRead == 0)
                            break; // Client disconnected
                            
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        _logger.LogInformation($"Message received: {message}");
                        
                        try
                        {
                            // Process JSON message
                            var jsonMessage = JsonSerializer.Deserialize<MessagePacket>(message);
                            
                            // Check if it's a response to a pending request
                            bool isHandled = HandleResponseIfPending(jsonMessage);
                            
                            // If it's not a response to a pending request, process it normally
                            if (!isHandled)
                            {
                                // Respond to the client
                                await ProcessMessageAsync(jsonMessage, stream, cancellationToken);
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError($"Error deserializing JSON: {ex.Message}");
                            await SendErrorAsync(stream, "Invalid JSON format", cancellationToken);
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError($"Error handling client: {ex.Message}");
                }
                finally
                {
                    _clients.Remove(client);
                    _logger.LogInformation("Client disconnected");
                }
            }
        }

        private bool HandleResponseIfPending(MessagePacket message)
        {
            // Si no tiene ID, no es respuesta a una petición
            if (string.IsNullOrEmpty(message.Id))
            {
                return false;
            }

            // Acepta como respuesta: 'response', 'error' y cualquier tipo registrado en _toolHandlers
            bool isResponseType = message.Type == "response" || message.Type == "error" || _toolHandlers.ContainsKey(message.Type);
            if (!isResponseType)
            {
                return false;
            }
            
            lock (_pendingResponsesLock)
            {
                if (_pendingResponses.TryGetValue(message.Id, out var tcs))
                {
                    tcs.TrySetResult(message);
                    _pendingResponses.Remove(message.Id);
                    return true;
                }
            }
            
            return false;
        }

        private async Task ProcessMessageAsync(MessagePacket message, NetworkStream stream, CancellationToken cancellationToken)
        {
            MessagePacket response;
            
            // Check if it's a response to a pending request
            if (HandleResponseIfPending(message))
            {
                return;
            }

            try
            {
                // First check if it's a basic server message
                switch (message.Type)
                {
                    case "connection":
                        _logger.LogInformation("Unity client connected");
                        response = MessagePacket.CreateResponse(true, "Connection established successfully");
                        break;
                    
                    case "disconnect":
                        _logger.LogInformation("Unity client disconnected");
                        response = MessagePacket.CreateResponse(true, "Disconnection completed successfully");
                        break;
                    
                    case "test":
                        if (message.Data.TryGetValue("message", out var testMessage))
                        {
                            _logger.LogInformation($"Test message received: {testMessage}");
                        }
                        response = MessagePacket.CreateResponse(true, "Test message received successfully");
                        break;
                    
                    default:
                        // If it's not a basic message, try handling it via registered tools
                        if (_toolHandlers.ContainsKey(message.Type))
                        {
                            response = await HandleRegisteredToolMessage(message);
                        }
                        else
                        {
                            _logger.LogWarning($"Unknown message type: {message.Type}");
                            response = MessagePacket.CreateError($"Unknown message type: {message.Type}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing message: {ex.Message}");
                response = MessagePacket.CreateError($"Error processing message: {ex.Message}");
            }
            
            // Keep the same ID to correlate request/response
            response.Id = message.Id;
            
            await SendMessageAsync(stream, response, cancellationToken);
        }
        
        private async Task SendErrorAsync(NetworkStream stream, string errorMessage, CancellationToken cancellationToken)
        {
            MessagePacket errorResponse = MessagePacket.CreateError(errorMessage);
            await SendMessageAsync(stream, errorResponse, cancellationToken);
        }
        
        private async Task SendMessageAsync(NetworkStream stream, MessagePacket message, CancellationToken cancellationToken)
        {
            string jsonResponse = JsonSerializer.Serialize(message);
            byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse);
            
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);
        }
        
        // Public method to send messages to all connected clients
        public async Task BroadcastMessageAsync(MessagePacket message, CancellationToken cancellationToken = default)
        {
            string jsonMessage = JsonSerializer.Serialize(message);
            byte[] messageBytes = Encoding.UTF8.GetBytes(jsonMessage);
            
            List<TcpClient> disconnectedClients = new List<TcpClient>();
            
            foreach (var client in _clients)
            {
                try
                {
                    if (client.Connected)
                    {
                        NetworkStream stream = client.GetStream();
                        await stream.WriteAsync(messageBytes, 0, messageBytes.Length, cancellationToken);
                    }
                    else
                    {
                        disconnectedClients.Add(client);
                    }
                }
                catch
                {
                    disconnectedClients.Add(client);
                }
            }
            
            foreach (var client in disconnectedClients)
            {
                _clients.Remove(client);
            }
        }
        
        // Method to send a message and wait for its response
        public async Task<MessagePacket> SendAndWaitForResponseAsync(MessagePacket message, TimeSpan? timeout = null)
        {
            // If there are no connected clients, return an error
            if (_clients.Count == 0)
            {
                throw new InvalidOperationException("No clients connected");
            }
            
            // Ensure the message has an ID
            if (string.IsNullOrEmpty(message.Id))
            {
                message.Id = Guid.NewGuid().ToString();
            }
            
            // Create a TaskCompletionSource to wait for the response
            var tcs = new TaskCompletionSource<MessagePacket>();
            
            // Register the TaskCompletionSource for the message ID
            lock (_pendingResponsesLock)
            {
                _pendingResponses[message.Id] = tcs;
            }
            
            try
            {
                // Send the message to all clients (in a real implementation, it might be sent to a specific client)
                await BroadcastMessageAsync(message);
                
                // Wait for the response with a timeout
                if (timeout.HasValue)
                {
                    using var cts = new CancellationTokenSource(timeout.Value);
                    using var registration = cts.Token.Register(() => tcs.TrySetCanceled());
                    return await tcs.Task;
                }
                else
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Default timeout
                    using var registration = cts.Token.Register(() => tcs.TrySetCanceled());
                    return await tcs.Task;
                }
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException("Timeout waiting for response");
            }
            finally
            {
                // Remove the TaskCompletionSource if it's still registered
                lock (_pendingResponsesLock)
                {
                    _pendingResponses.Remove(message.Id);
                }
            }
        }

        #endregion
    }
} 