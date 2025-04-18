using System;

namespace UnityMCP.Bridge.Shared.Messages.Abstractions
{
    /// <summary>
    /// Clase base for all tool messages.
    /// Provides a common structure for all messages that are sent to specific tools.
    /// </summary>
    public abstract class ToolMessage
    {
        /// <summary>
        /// Action to perform by the tool. Generally, "get" or "execute" or another specific action.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        protected ToolMessage()
        {
            // Set default values in derived classes
        }

        /// <summary>
        /// Basic constructor with parameters
        /// </summary>
        /// <param name="action">Action to execute</param>
        protected ToolMessage(string action)
        {
            Action = action;
        }
    }
} 