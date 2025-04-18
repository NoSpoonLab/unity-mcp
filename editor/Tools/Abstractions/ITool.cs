using UnityMCP.Bridge.Shared.Messages.Abstractions;

namespace UnityMCP.Bridge.Tools
{
    /// <summary>
    /// Common contracts for all editor tools.
    /// </summary>
    public static class ToolContracts
    {
        /// <summary>
        /// Definition of a delegate to handle messages, compatible with static methods.
        /// </summary>
        /// <typeparam name="T">Type of the message to process</typeparam>
        /// <param name="message">Message to process</param>
        /// <returns>Result of processing</returns>
        public delegate object MessageHandler<T>(T message) where T : ToolMessage;
    }
} 