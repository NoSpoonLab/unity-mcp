using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityMCP.Bridge.Shared.Messages;
using UnityMCP.Bridge;

namespace UnityMCP.Bridge.Tools
{
    /// <summary>
    /// Tool to execute Unity Editor menu items by path.
    /// </summary>
    public static class ExecuteMenuItemTool
    {
        // Basic blacklist to prevent accidental execution of potentially disruptive menu items.
        // This can be expanded based on needs.
        private static readonly HashSet<string> _menuPathBlacklist = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase
        )
        {
            "File/Quit",
            // Add other potentially dangerous items like "Edit/Preferences...", "File/Build Settings..." if needed
        };

        /// <summary>
        /// Main entry point for processing ExecuteMenuItemMessage.
        /// </summary>
        /// <param name="message">Message to process</param>
        /// <returns>Processing result</returns>
        public static object HandleMessage(ExecuteMenuItemMessage message)
        {
            try
            {
                if (message == null)
                {
                    UnityMCPEditorLogger.LogError("[ExecuteMenuItem] Message is null");
                    return Response.Error("Message is null");
                }
                return HandleCommand(message);
            }
            catch (Exception ex)
            {
                var errorMessage = $"[ExecuteMenuItem] Error processing message: {ex.Message}";
                UnityMCPEditorLogger.LogError(errorMessage);
                return Response.Error(errorMessage);
            }
        }

        /// <summary>
        /// Main implementation for system compatibility.
        /// </summary>
        public static object HandleCommand(ExecuteMenuItemMessage message)
        {
            string action = message.Action?.ToLower() ?? "execute";

            try
            {
                switch (action)
                {
                    case "execute":
                        return ExecuteItem(message);
                    case "get_available_menus":
                        UnityMCPEditorLogger.LogWarning(
                            "[ExecuteMenuItem] 'get_available_menus' is not fully implemented. Listing all menus dynamically is complex."
                        );
                        return Response.Success(
                            "'get_available_menus' is not implemented. Returning empty list.",
                            new List<string>()
                        );
                    default:
                        return Response.Error(
                            $"Unknown action: '{action}'. Valid actions: 'execute', 'get_available_menus'."
                        );
                }
            }
            catch (Exception e)
            {
                UnityMCPEditorLogger.LogError($"[ExecuteMenuItem] Action '{action}' failed: {e}");
                return Response.Error($"Internal error processing action '{action}': {e.Message}");
            }
        }

        /// <summary>
        /// Executes a specific menu item.
        /// </summary>
        private static object ExecuteItem(ExecuteMenuItemMessage message)
        {
            string menuPath = message.MenuPath;
            // var parameters = message.Parameters; // Not supported currently

            if (string.IsNullOrWhiteSpace(menuPath))
            {
                return Response.Error("Parameter 'MenuPath' is required and is empty or null.");
            }

            if (_menuPathBlacklist.Contains(menuPath))
            {
                return Response.Error(
                    $"Execution of menu item '{menuPath}' is blocked for safety reasons."
                );
            }

            try
            {
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        bool executed = EditorApplication.ExecuteMenuItem(menuPath);
                        if (!executed)
                        {
                            UnityMCPEditorLogger.LogError(
                                $"[ExecuteMenuItem] Could not find or execute menu: '{menuPath}'. It may be invalid, disabled, or context-dependent."
                            );
                        }
                    }
                    catch (Exception delayEx)
                    {
                        UnityMCPEditorLogger.LogError(
                            $"[ExecuteMenuItem] Exception during delayed execution of '{menuPath}': {delayEx}"
                        );
                    }
                };

                return Response.Success(
                    $"Execute menu: '{menuPath}'. Check Unity logs just in case of errors."
                );
            }
            catch (Exception e)
            {
                UnityMCPEditorLogger.LogError(
                    $"[ExecuteMenuItem] Error preparing execution of '{menuPath}': {e}"
                );
                return Response.Error(
                    $"Error preparing execution of menu '{menuPath}': {e.Message}"
                );
            }
        }
    }
}

