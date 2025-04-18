using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityMCP.Bridge.Shared.Messages;
using UnityMCP.Bridge;

namespace UnityMCP.Bridge.Tools
{
    /// <summary>
    /// Tool for CRUD operations of C# scripts within the Unity project.
    /// </summary>
    public static class ManageScriptTool
    {
        /// <summary>
        /// Main entry point for processing script management messages.
        /// </summary>
        /// <param name="message">Message to process (must contain a JObject Params)</param>
        /// <returns>Processing result</returns>
        public static object HandleMessage(ManageScriptMessage message)
        {
            try
            {
                if (message == null)
                {
                    UnityMCPEditorLogger.LogError("[ManageScript] Message is null");
                    return Response.Error("Message is null");
                }
                return HandleCommand(message);
            }
            catch (Exception ex)
            {
                var errorMessage = $"[ManageScript] Error processing message: {ex.Message}";
                UnityMCPEditorLogger.LogError(errorMessage);
                return Response.Error(errorMessage);
            }
        }

        /// <summary>
        /// Main implementation of script management logic.
        /// </summary>
        public static object HandleCommand(ManageScriptMessage message)
        {
            string action = message.Action?.ToLower();
            string name = message.Name;
            string path = message.Path;
            string contents = null;

            bool contentsEncoded = message.ContentsEncoded ?? false;
            if (contentsEncoded && !string.IsNullOrEmpty(message.EncodedContents))
            {
                try
                {
                    contents = DecodeBase64(message.EncodedContents);
                }
                catch (Exception e)
                {
                    return Response.Error($"Failed to decode script contents: {e.Message}");
                }
            }
            else
            {
                contents = message.Contents;
            }

            string scriptType = message.ScriptType;
            string namespaceName = message.Namespace;

            if (string.IsNullOrEmpty(action))
            {
                return Response.Error("The 'action' parameter is required.");
            }
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("The 'name' parameter is required.");
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                return Response.Error($"Invalid script name: '{name}'. Use only letters, numbers, underscores, and do not start with a number.");
            }

            string relativeDir = path ?? "Scripts";
            if (!string.IsNullOrEmpty(relativeDir))
            {
                relativeDir = relativeDir.Replace('\\', '/').Trim('/');
                if (relativeDir.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    relativeDir = relativeDir.Substring("Assets/".Length).TrimStart('/');
                }
            }
            if (string.IsNullOrEmpty(relativeDir))
            {
                relativeDir = "Scripts";
            }

            string scriptFileName = $"{name}.cs";
            string fullPathDir = System.IO.Path.Combine(UnityEngine.Application.dataPath, relativeDir);
            string fullPath = System.IO.Path.Combine(fullPathDir, scriptFileName);
            string relativePath = System.IO.Path.Combine("Assets", relativeDir, scriptFileName).Replace('\\', '/');

            if (action == "create" || action == "update")
            {
                try
                {
                    System.IO.Directory.CreateDirectory(fullPathDir);
                }
                catch (Exception e)
                {
                    return Response.Error($"Could not create directory '{fullPathDir}': {e.Message}");
                }
            }

            switch (action)
            {
                case "create":
                    return CreateScript(fullPath, relativePath, name, contents, scriptType, namespaceName);
                case "read":
                    return ReadScript(fullPath, relativePath);
                case "update":
                    return UpdateScript(fullPath, relativePath, name, contents);
                case "delete":
                    return DeleteScript(fullPath, relativePath);
                default:
                    return Response.Error($"Unknown action: '{action}'. Valid actions: create, read, update, delete.");
            }
        }

        private static string DecodeBase64(string encoded)
        {
            byte[] data = Convert.FromBase64String(encoded);
            return System.Text.Encoding.UTF8.GetString(data);
        }

        private static string EncodeBase64(string text)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(data);
        }

        private static object CreateScript(string fullPath, string relativePath, string name, string contents, string scriptType, string namespaceName)
        {
            if (File.Exists(fullPath))
            {
                return Response.Error($"Script already exists in '{relativePath}'. Use the 'update' action to modify it.");
            }

            if (string.IsNullOrEmpty(contents))
            {
                contents = GenerateDefaultScriptContent(name, scriptType, namespaceName);
            }

            if (!ValidateScriptSyntax(contents))
            {
                Debug.LogWarning($"Possible syntax error in the script being created: {name}");
            }

            try
            {
                File.WriteAllText(fullPath, contents);
                AssetDatabase.ImportAsset(relativePath);
                AssetDatabase.Refresh();
                return Response.Success($"Script '{name}.cs' created successfully in '{relativePath}'.", new { path = relativePath });
            }
            catch (Exception e)
            {
                return Response.Error($"Could not create script '{relativePath}': {e.Message}");
            }
        }

        private static object ReadScript(string fullPath, string relativePath)
        {
            if (!File.Exists(fullPath))
            {
                return Response.Error($"Script not found in '{relativePath}'.");
            }

            try
            {
                string contents = File.ReadAllText(fullPath);
                bool isLarge = contents.Length > 10000;
                var responseData = new
                {
                    path = relativePath,
                    contents = contents,
                    encodedContents = isLarge ? EncodeBase64(contents) : null,
                    contentsEncoded = isLarge,
                };

                return Response.Success($"Script '{Path.GetFileName(relativePath)}' read successfully.", responseData);
            }
            catch (Exception e)
            {
                return Response.Error($"Could not read script '{relativePath}': {e.Message}");
            }
        }

        private static object UpdateScript(string fullPath, string relativePath, string name, string contents)
        {
            if (!File.Exists(fullPath))
            {
                return Response.Error($"Script not found in '{relativePath}'. Use the 'create' action to add a new script.");
            }
            if (string.IsNullOrEmpty(contents))
            {
                return Response.Error("Contents are required for the 'update' action.");
            }

            if (!ValidateScriptSyntax(contents))
            {
                Debug.LogWarning($"Possible syntax error in the script being updated: {name}");
            }

            try
            {
                File.WriteAllText(fullPath, contents);
                AssetDatabase.ImportAsset(relativePath);
                AssetDatabase.Refresh();
                return Response.Success($"Script '{name}.cs' updated successfully in '{relativePath}'.", new { path = relativePath });
            }
            catch (Exception e)
            {
                return Response.Error($"Could not update script '{relativePath}': {e.Message}");
            }
        }

        private static object DeleteScript(string fullPath, string relativePath)
        {
            if (!File.Exists(fullPath))
            {
                return Response.Error($"Script not found in '{relativePath}'. Could not delete.");
            }

            try
            {
                bool deleted = AssetDatabase.MoveAssetToTrash(relativePath);
                if (deleted)
                {
                    AssetDatabase.Refresh();
                    return Response.Success($"Script '{Path.GetFileName(relativePath)}' moved to trash correctly.");
                }
                else
                {
                    return Response.Error($"Could not move script '{relativePath}' to trash. It may be locked or in use.");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error deleting script '{relativePath}': {e.Message}");
            }
        }

        private static string GenerateDefaultScriptContent(string name, string scriptType, string namespaceName)
        {
            string usingStatements = "using UnityEngine;\nusing System.Collections;\n";
            string classDeclaration;
            string body =
                "\n    // Use this for initialization\n    void Start() {\n\n    }\n\n    // Update is called once per frame\n    void Update() {\n\n    }\n";

            string baseClass = "";
            if (!string.IsNullOrEmpty(scriptType))
            {
                if (scriptType.Equals("MonoBehaviour", StringComparison.OrdinalIgnoreCase))
                    baseClass = " : MonoBehaviour";
                else if (scriptType.Equals("ScriptableObject", StringComparison.OrdinalIgnoreCase))
                {
                    baseClass = " : ScriptableObject";
                    body = "";
                }
                else if (
                    scriptType.Equals("Editor", StringComparison.OrdinalIgnoreCase)
                    || scriptType.Equals("EditorWindow", StringComparison.OrdinalIgnoreCase)
                )
                {
                    usingStatements += "using UnityEditor;\n";
                    if (scriptType.Equals("Editor", StringComparison.OrdinalIgnoreCase))
                        baseClass = " : Editor";
                    else
                        baseClass = " : EditorWindow";
                    body = "";
                }
            }

            classDeclaration = $"public class {name}{baseClass}";

            string fullContent = $"{usingStatements}\n";
            bool useNamespace = !string.IsNullOrEmpty(namespaceName);

            if (useNamespace)
            {
                fullContent += $"namespace {namespaceName}\n{{\n";
                classDeclaration = "    " + classDeclaration;
                body = string.Join("\n", body.Split('\n').Select(line => "    " + line));
            }

            fullContent += $"{classDeclaration}\n{{\n{body}\n}}";

            if (useNamespace)
            {
                fullContent += "\n}";
            }

            return fullContent.Trim() + "\n";
        }

        private static bool ValidateScriptSyntax(string contents)
        {
            if (string.IsNullOrEmpty(contents))
                return true;

            int braceBalance = 0;
            foreach (char c in contents)
            {
                if (c == '{')
                    braceBalance++;
                else if (c == '}')
                    braceBalance--;
            }

            return braceBalance == 0;
        }
    }
} 