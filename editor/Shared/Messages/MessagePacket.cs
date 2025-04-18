using System;
using System.Collections.Generic;

namespace UnityMCP.Bridge.Shared.Messages
{
    [Serializable]
    public class MessagePacket
    {
        public string Type { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public string Id { get; set; }
        
        // Constructor por defecto requerido para la serialización
        public MessagePacket()
        {
            Id = Guid.NewGuid().ToString();
        }
        
        // Métodos de utilidad para crear mensajes
        public static MessagePacket CreateResponse(bool success, string message)
        {
            return new MessagePacket
            {
                Type = "response",
                Data = new Dictionary<string, object>
                {
                    { "success", success },
                    { "message", message }
                }
            };
        }
        
        public static MessagePacket CreateError(string errorMessage)
        {
            return new MessagePacket
            {
                Type = "error",
                Data = new Dictionary<string, object>
                {
                    { "success", false },
                    { "message", errorMessage }
                }
            };
        }
        
        // Agrega el ID de correlación de la solicitud a la respuesta
        public MessagePacket WithCorrelationId(string correlationId)
        {
            this.Id = correlationId;
            return this;
        }
    }

    /// <summary>
    /// Normalized response object for consistent communication patterns.
    /// </summary>
    public static class Response 
    {
        /// <summary>
        /// Creates a successful response with data payload.
        /// </summary>
        /// <param name="message">Success message</param>
        /// <param name="data">Optional data payload</param>
        /// <returns>A response object indicating success with optional data</returns>
        public static object Success(string message, object data = null)
        {
            return new 
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// Creates an error response with optional data.
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="data">Optional data payload</param>
        /// <returns>A response object indicating failure with error details</returns>
        public static object Error(string message, object data = null)
        {
            return new 
            {
                Success = false,
                Message = message,
                Data = data
            };
        }
    }
} 