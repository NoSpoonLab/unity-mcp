using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityMCP.Bridge.Shared.Messages;
using UnityMCP.Bridge;

namespace UnityMCP.Bridge.Tools
{
    /// <summary>
    /// Tool to manage asset operations within the Unity project.
    /// </summary>
    public static class ManageAssetTool
    {
        // List of valid actions
        private static readonly List<string> ValidActions = new List<string>
        {
            "import",
            "create",
            "modify",
            "delete",
            "duplicate",
            "move",
            "rename",
            "search",
            "get_info",
            "create_folder",
            "get_components",
        };

        /// <summary>
        /// Main entry point for processing asset management messages.
        /// </summary>
        /// <param name="message">Message to process (must contain a JObject Params)</param>
        /// <returns>Processing result</returns>
        public static object HandleMessage(ManageAssetMessage message)
        {
            try
            {
                if (message == null)
                {
                    UnityMCPEditorLogger.LogError("[ManageAsset] Message is null");
                    return Response.Error("Message is null");
                }
                return HandleCommand(message);
            }
            catch (Exception ex)
            {
                var errorMessage = $"[ManageAsset] Error processing message: {ex.Message}";
                UnityMCPEditorLogger.LogError(errorMessage);
                return Response.Error(errorMessage);
            }
        }

        /// <summary>
        /// Main implementation of asset management logic.
        /// </summary>
        public static object HandleCommand(ManageAssetMessage message)
        {
            string action = message.Action?.ToLower();
            if (string.IsNullOrEmpty(action))
            {
                return Response.Error("The 'action' parameter is required.");
            }

            if (!ValidActions.Contains(action))
            {
                string validActionsList = string.Join(", ", ValidActions);
                return Response.Error($"Unknown action: '{action}'. Valid actions: {validActionsList}");
            }

            string path = message.Path;

            try
            {
                switch (action)
                {
                    case "import":
                        return ReimportAsset(path, message.Properties);
                    case "create":
                        return CreateAsset(message);
                    case "modify":
                        return ModifyAsset(path, message.Properties);
                    case "delete":
                        return DeleteAsset(path);
                    case "duplicate":
                        return DuplicateAsset(path, message.Destination);
                    case "move":
                    case "rename":
                        return MoveOrRenameAsset(path, message.Destination);
                    case "search":
                        return SearchAssets(message);
                    case "get_info":
                        return GetAssetInfo(path, message.GeneratePreview);
                    case "create_folder":
                        return CreateFolder(path);
                    case "get_components":
                        return GetComponentsFromAsset(path);
                    default:
                        string validActionsListDefault = string.Join(", ", ValidActions);
                        return Response.Error($"Unknown action: '{action}'. Valid actions: {validActionsListDefault}");
                }
            }
            catch (Exception e)
            {
                UnityMCPEditorLogger.LogError($"[ManageAsset] Action '{action}' failed for path '{path}': {e}");
                return Response.Error($"Internal error processing action '{action}' on '{path}': {e.Message}");
            }
        }

        private static object ReimportAsset(string path, Dictionary<string, object> properties)
        {
            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for reimport.");
            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Asset not found at path: {fullPath}");

            try
            {
                if (properties != null && properties.Count > 0)
                {
                    Debug.LogWarning(
                        "[ManageAsset.Reimport] Modifying importer properties before reimport is not fully implementado aún."
                    );
                }

                AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);
                return Response.Success($"Asset '{fullPath}' reimported.", GetAssetData(fullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to reimport asset '{fullPath}': {e.Message}");
            }
        }

        private static object CreateAsset(ManageAssetMessage message)
        {
            string path = message.Path;
            string assetType = message.AssetType;
            var properties = message.Properties;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for create.");
            if (string.IsNullOrEmpty(assetType))
                return Response.Error("'assetType' is required for create.");

            string fullPath = SanitizeAssetPath(path);
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), directory)))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), directory));
                AssetDatabase.Refresh();
            }

            if (AssetExists(fullPath))
                return Response.Error($"Asset already exists at path: {fullPath}");

            try
            {
                UnityEngine.Object newAsset = null;
                string lowerAssetType = assetType.ToLowerInvariant();

                if (lowerAssetType == "folder")
                {
                    return CreateFolder(path);
                }
                else if (lowerAssetType == "material")
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    if (properties != null)
                        ApplyMaterialProperties(mat, properties);
                    AssetDatabase.CreateAsset(mat, fullPath);
                    newAsset = mat;
                }
                else if (lowerAssetType == "scriptableobject")
                {
                    string scriptClassName = properties != null && properties.ContainsKey("scriptClass") ? properties["scriptClass"]?.ToString() : null;
                    if (string.IsNullOrEmpty(scriptClassName))
                        return Response.Error(
                            "'scriptClass' property required when creating ScriptableObject asset."
                        );

                    Type scriptType = FindType(scriptClassName);
                    if (
                        scriptType == null
                        || !typeof(ScriptableObject).IsAssignableFrom(scriptType)
                    )
                    {
                        return Response.Error(
                            $"Script class '{scriptClassName}' not found or does not inherit from ScriptableObject."
                        );
                    }

                    ScriptableObject so = ScriptableObject.CreateInstance(scriptType);
                    // TODO: Aplicar propiedades al ScriptableObject si es necesario
                    AssetDatabase.CreateAsset(so, fullPath);
                    newAsset = so;
                }
                else if (lowerAssetType == "prefab")
                {
                    return Response.Error(
                        "Creating prefabs programmatically usually requires a source GameObject. Use manage_gameobject to create/configure, then save as prefab via a separate mechanism or future enhancement."
                    );
                }
                else
                {
                    return Response.Error(
                        $"Creation for asset type '{assetType}' is not explicitly supported yet. Supported: Folder, Material, ScriptableObject."
                    );
                }

                if (
                    newAsset == null
                    && !Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), fullPath))
                )
                {
                    return Response.Error(
                        $"Failed to create asset '{assetType}' at '{fullPath}'. See logs for details."
                    );
                }

                AssetDatabase.SaveAssets();
                return Response.Success(
                    $"Asset '{fullPath}' created successfully.",
                    GetAssetData(fullPath)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create asset at '{fullPath}': {e.Message}");
            }
        }

        private static object CreateFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for create_folder.");
            string fullPath = SanitizeAssetPath(path);
            string parentDir = Path.GetDirectoryName(fullPath);
            string folderName = Path.GetFileName(fullPath);

            if (AssetExists(fullPath))
            {
                // Check if it's actually a folder already
                if (AssetDatabase.IsValidFolder(fullPath))
                {
                    return Response.Success(
                        $"Folder already exists at path: {fullPath}",
                        GetAssetData(fullPath)
                    );
                }
                else
                {
                    return Response.Error(
                        $"An asset (not a folder) already exists at path: {fullPath}"
                    );
                }
            }

            try
            {
                // Ensure parent exists
                if (!string.IsNullOrEmpty(parentDir) && !AssetDatabase.IsValidFolder(parentDir))
                {
                    // Recursively create parent folders if needed (AssetDatabase handles this internally)
                    // Or we can do it manually: Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), parentDir)); AssetDatabase.Refresh();
                }

                string guid = AssetDatabase.CreateFolder(parentDir, folderName);
                if (string.IsNullOrEmpty(guid))
                {
                    return Response.Error(
                        $"Failed to create folder '{fullPath}'. Check logs and permissions."
                    );
                }

                // AssetDatabase.Refresh(); // CreateFolder usually handles refresh
                return Response.Success(
                    $"Folder '{fullPath}' created successfully.",
                    GetAssetData(fullPath)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create folder '{fullPath}': {e.Message}");
            }
        }

        private static object ModifyAsset(string path, Dictionary<string, object> properties)
        {
            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for modify.");
            if (properties == null || properties.Count == 0)
                return Response.Error("'properties' are required for modify.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Asset not found at path: {fullPath}");

            try
            {
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    fullPath
                );
                if (asset == null)
                    return Response.Error($"Failed to load asset at path: {fullPath}");

                bool modified = false;

                if (asset is GameObject gameObject)
                {
                    foreach (var prop in properties)
                    {
                        string componentName = prop.Key;
                        if (prop.Value is Dictionary<string, object> componentProperties && componentProperties.Count > 0)
                        {
                            Component targetComponent = gameObject.GetComponent(componentName);
                            if (targetComponent != null)
                            {
                                modified |= ApplyObjectProperties(targetComponent, componentProperties);
                            }
                            else
                            {
                                Debug.LogWarning($"[ManageAsset.ModifyAsset] Component '{componentName}' not found on GameObject '{gameObject.name}' in asset '{fullPath}'. Skipping modification for this component.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[ManageAsset.ModifyAsset] Property '{prop.Key}' for GameObject modification should have a dictionary value containing component properties. Skipping.");
                        }
                    }
                }
                else if (asset is Material material)
                {
                    modified |= ApplyMaterialProperties(material, properties);
                }
                else if (asset is ScriptableObject so)
                {
                    modified |= ApplyObjectProperties(so, properties);
                }
                else if (asset is Texture)
                {
                    AssetImporter importer = AssetImporter.GetAtPath(fullPath);
                    if (importer != null)
                    {
                        bool importerModified = ApplyObjectProperties(importer, properties);
                        if (importerModified)
                        {
                            AssetDatabase.WriteImportSettingsIfDirty(fullPath);
                            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);
                            modified = true;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Could not get AssetImporter for {fullPath}.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ManageAsset.ModifyAsset] Asset type '{asset.GetType().Name}' at '{fullPath}' is not explicitly handled for component modification. Attempting generic property setting on the asset itself.");
                    modified |= ApplyObjectProperties(asset, properties);
                }

                if (modified)
                {
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssets();
                    return Response.Success(
                        $"Asset '{fullPath}' modified successfully.",
                        GetAssetData(fullPath)
                    );
                }
                else
                {
                    return Response.Success(
                        $"No applicable or modifiable properties found for asset '{fullPath}'. Check component names, property names, and values.",
                        GetAssetData(fullPath)
                    );
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ManageAsset] Action 'modify' failed for path '{path}': {e}");
                return Response.Error($"Failed to modify asset '{fullPath}': {e.Message}");
            }
        }

        private static object DeleteAsset(string path)
        {
            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for delete.");
            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Asset not found at path: {fullPath}");

            try
            {
                bool success = AssetDatabase.DeleteAsset(fullPath);
                if (success)
                {
                    // AssetDatabase.Refresh(); // DeleteAsset usually handles refresh
                    return Response.Success($"Asset '{fullPath}' deleted successfully.");
                }
                else
                {
                    // This might happen if the file couldn't be deleted (e.g., locked)
                    return Response.Error(
                        $"Failed to delete asset '{fullPath}'. Check logs or if the file is locked."
                    );
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error deleting asset '{fullPath}': {e.Message}");
            }
        }

        private static object DuplicateAsset(string path, string destinationPath)
        {
            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for duplicate.");

            string sourcePath = SanitizeAssetPath(path);
            if (!AssetExists(sourcePath))
                return Response.Error($"Source asset not found at path: {sourcePath}");

            string destPath;
            if (string.IsNullOrEmpty(destinationPath))
            {
                // Generate a unique path if destination is not provided
                destPath = AssetDatabase.GenerateUniqueAssetPath(sourcePath);
            }
            else
            {
                destPath = SanitizeAssetPath(destinationPath);
                if (AssetExists(destPath))
                    return Response.Error($"Asset already exists at destination path: {destPath}");
                // Ensure destination directory exists
                EnsureDirectoryExists(Path.GetDirectoryName(destPath));
            }

            try
            {
                bool success = AssetDatabase.CopyAsset(sourcePath, destPath);
                if (success)
                {
                    // AssetDatabase.Refresh();
                    return Response.Success(
                        $"Asset '{sourcePath}' duplicated to '{destPath}'.",
                        GetAssetData(destPath)
                    );
                }
                else
                {
                    return Response.Error(
                        $"Failed to duplicate asset from '{sourcePath}' to '{destPath}'."
                    );
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error duplicating asset '{sourcePath}': {e.Message}");
            }
        }

        private static object MoveOrRenameAsset(string path, string destinationPath)
        {
            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for move/rename.");
            if (string.IsNullOrEmpty(destinationPath))
                return Response.Error("'destination' path is required for move/rename.");

            string sourcePath = SanitizeAssetPath(path);
            string destPath = SanitizeAssetPath(destinationPath);

            if (!AssetExists(sourcePath))
                return Response.Error($"Source asset not found at path: {sourcePath}");
            if (AssetExists(destPath))
                return Response.Error(
                    $"An asset already exists at the destination path: {destPath}"
                );

            // Ensure destination directory exists
            EnsureDirectoryExists(Path.GetDirectoryName(destPath));

            try
            {
                // Validate will return an error string if failed, null if successful
                string error = AssetDatabase.ValidateMoveAsset(sourcePath, destPath);
                if (!string.IsNullOrEmpty(error))
                {
                    return Response.Error(
                        $"Failed to move/rename asset from '{sourcePath}' to '{destPath}': {error}"
                    );
                }

                string guid = AssetDatabase.MoveAsset(sourcePath, destPath);
                if (!string.IsNullOrEmpty(guid)) // MoveAsset returns the new GUID on success
                {
                    // AssetDatabase.Refresh(); // MoveAsset usually handles refresh
                    return Response.Success(
                        $"Asset moved/renamed from '{sourcePath}' to '{destPath}'.",
                        GetAssetData(destPath)
                    );
                }
                else
                {
                    // This case might not be reachable if ValidateMoveAsset passes, but good to have
                    return Response.Error(
                        $"MoveAsset call failed unexpectedly for '{sourcePath}' to '{destPath}'."
                    );
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error moving/renaming asset '{sourcePath}': {e.Message}");
            }
        }

        private static object SearchAssets(ManageAssetMessage message)
        {
            string searchPattern = message.SearchPattern;
            string filterType = message.FilterType;
            string pathScope = message.Path; // Use path as folder scope
            string filterDateAfterStr = message.FilterDateAfter;
            int pageSize = message.PageSize ?? 50; // Default page size
            int pageNumber = message.PageNumber ?? 1; // Default page number (1-based)
            bool generatePreview = message.GeneratePreview;

            List<string> searchFilters = new List<string>();
            if (!string.IsNullOrEmpty(searchPattern))
                searchFilters.Add(searchPattern);
            if (!string.IsNullOrEmpty(filterType))
                searchFilters.Add($"t:{filterType}");

            string[] folderScope = null;
            if (!string.IsNullOrEmpty(pathScope))
            {
                folderScope = new string[] { SanitizeAssetPath(pathScope) };
                if (!AssetDatabase.IsValidFolder(folderScope[0]))
                {
                    // Maybe the user provided a file path instead of a folder?
                    // We could search in the containing folder, or return an error.
                    Debug.LogWarning(
                        $"Search path '{folderScope[0]}' is not a valid folder. Searching entire project."
                    );
                    folderScope = null; // Search everywhere if path isn't a folder
                }
            }

            DateTime? filterDateAfter = null;
            if (!string.IsNullOrEmpty(filterDateAfterStr))
            {
                if (
                    DateTime.TryParse(
                        filterDateAfterStr,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out DateTime parsedDate
                    )
                )
                {
                    filterDateAfter = parsedDate;
                }
                else
                {
                    Debug.LogWarning(
                        $"Could not parse filterDateAfter: '{filterDateAfterStr}'. Expected ISO 8601 format."
                    );
                }
            }

            try
            {
                string[] guids = AssetDatabase.FindAssets(
                    string.Join(" ", searchFilters),
                    folderScope
                );
                List<object> results = new List<object>();
                int totalFound = 0;

                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(assetPath))
                        continue;

                    // Apply date filter if present
                    if (filterDateAfter.HasValue)
                    {
                        DateTime lastWriteTime = File.GetLastWriteTimeUtc(
                            Path.Combine(Directory.GetCurrentDirectory(), assetPath)
                        );
                        if (lastWriteTime <= filterDateAfter.Value)
                        {
                            continue; // Skip assets older than or equal to the filter date
                        }
                    }

                    totalFound++; // Count matching assets before pagination
                    results.Add(GetAssetData(assetPath, generatePreview));
                }

                // Apply pagination
                int startIndex = (pageNumber - 1) * pageSize;
                var pagedResults = results.Skip(startIndex).Take(pageSize).ToList();

                return Response.Success(
                    $"Found {totalFound} asset(s). Returning page {pageNumber} ({pagedResults.Count} assets).",
                    new
                    {
                        totalAssets = totalFound,
                        pageSize = pageSize,
                        pageNumber = pageNumber,
                        assets = pagedResults,
                    }
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Error searching assets: {e.Message}");
            }
        }

        private static object GetAssetInfo(string path, bool generatePreview)
        {
            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for get_info.");
            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Asset not found at path: {fullPath}");

            try
            {
                return Response.Success(
                    "Asset info retrieved.",
                    GetAssetData(fullPath, generatePreview)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Error getting info for asset '{fullPath}': {e.Message}");
            }
        }

        /// <summary>
        /// Retrieves components attached to a GameObject asset (like a Prefab).
        /// </summary>
        /// <param name="path">The asset path of the GameObject or Prefab.</param>
        /// <returns>A response object containing a list of component type names or an error.</returns>
        private static object GetComponentsFromAsset(string path)
        {
            // 1. Validate input path
            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for get_components.");

            // 2. Sanitize and check existence
            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Asset not found at path: {fullPath}");

            try
            {
                // 3. Load the asset
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    fullPath
                );
                if (asset == null)
                    return Response.Error($"Failed to load asset at path: {fullPath}");

                // 4. Check if it's a GameObject (Prefabs load as GameObjects)
                GameObject gameObject = asset as GameObject;
                if (gameObject == null)
                {
                    // Also check if it's *directly* a Component type (less common for primary assets)
                    Component componentAsset = asset as Component;
                    if (componentAsset != null)
                    {
                        // If the asset itself *is* a component, maybe return just its info?
                        // This is an edge case. Let's stick to GameObjects for now.
                        return Response.Error(
                            $"Asset at '{fullPath}' is a Component ({asset.GetType().FullName}), not a GameObject. Components are typically retrieved *from* a GameObject."
                        );
                    }
                    return Response.Error(
                        $"Asset at '{fullPath}' is not a GameObject (Type: {asset.GetType().FullName}). Cannot get components from this asset type."
                    );
                }

                // 5. Get components
                Component[] components = gameObject.GetComponents<Component>();

                // 6. Format component data
                List<object> componentList = components
                    .Select(comp => new
                    {
                        typeName = comp.GetType().FullName,
                        instanceID = comp.GetInstanceID(),
                        // TODO: Add more component-specific details here if needed in the future?
                        //       Requires reflection or specific handling per component type.
                    })
                    .ToList<object>(); // Explicit cast for clarity if needed

                // 7. Return success response
                return Response.Success(
                    $"Found {componentList.Count} component(s) on asset '{fullPath}'.",
                    componentList
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[ManageAsset.GetComponentsFromAsset] Error getting components for '{fullPath}': {e}"
                );
                return Response.Error(
                    $"Error getting components for asset '{fullPath}': {e.Message}"
                );
            }
        }

        // --- Internal Helpers ---

        /// <summary>
        /// Ensures the asset path starts with "Assets/".
        /// </summary>
        private static string SanitizeAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            path = path.Replace('\\', '/'); // Normalize separators
            if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return "Assets/" + path.TrimStart('/');
            }
            return path;
        }

        /// <summary>
        /// Checks if an asset exists at the given path (file or folder).
        /// </summary>
        private static bool AssetExists(string sanitizedPath)
        {
            // AssetDatabase APIs are generally preferred over raw File/Directory checks for assets.
            // Check if it's a known asset GUID.
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(sanitizedPath)))
            {
                return true;
            }
            // AssetPathToGUID might not work for newly created folders not yet refreshed.
            // Check directory explicitly for folders.
            if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), sanitizedPath)))
            {
                // Check if it's considered a *valid* folder by Unity
                return AssetDatabase.IsValidFolder(sanitizedPath);
            }
            // Check file existence for non-folder assets.
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), sanitizedPath)))
            {
                return true; // Assume if file exists, it's an asset or will be imported
            }

            return false;
            // Alternative: return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(sanitizedPath));
        }

        /// <summary>
        /// Ensures the directory for a given asset path exists, creating it if necessary.
        /// </summary>
        private static void EnsureDirectoryExists(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return;
            string fullDirPath = Path.Combine(Directory.GetCurrentDirectory(), directoryPath);
            if (!Directory.Exists(fullDirPath))
            {
                Directory.CreateDirectory(fullDirPath);
                AssetDatabase.Refresh(); // Let Unity know about the new folder
            }
        }

        /// <summary>
        /// Aplica propiedades a un Material usando un Dictionary<string, object>.
        /// </summary>
        private static bool ApplyMaterialProperties(Material mat, Dictionary<string, object> properties)
        {
            if (mat == null || properties == null)
                return false;
            bool modified = false;

            if (properties.TryGetValue("shader", out var shaderObj) && shaderObj is string shaderName)
            {
                Shader newShader = Shader.Find(shaderName);
                if (newShader != null && mat.shader != newShader)
                {
                    mat.shader = newShader;
                    modified = true;
                }
            }
            if (properties.TryGetValue("color", out var colorObj) && colorObj is Dictionary<string, object> colorProps)
            {
                string propName = colorProps.ContainsKey("name") ? colorProps["name"]?.ToString() : "_Color";
                if (colorProps.TryGetValue("value", out var colArrObj) && colArrObj is IEnumerable<object> colArrEnum)
                {
                    var colArr = colArrEnum.Cast<object>().ToList();
                    if (colArr.Count >= 3)
                    {
                        try
                        {
                            Color newColor = new Color(
                                Convert.ToSingle(colArr[0]),
                                Convert.ToSingle(colArr[1]),
                                Convert.ToSingle(colArr[2]),
                                colArr.Count > 3 ? Convert.ToSingle(colArr[3]) : 1.0f
                            );
                            if (mat.HasProperty(propName) && mat.GetColor(propName) != newColor)
                            {
                                mat.SetColor(propName, newColor);
                                modified = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Error parsing color property '{propName}': {ex.Message}");
                        }
                    }
                }
            }
            if (properties.TryGetValue("float", out var floatObj) && floatObj is Dictionary<string, object> floatProps)
            {
                string propName = floatProps.ContainsKey("name") ? floatProps["name"]?.ToString() : null;
                if (!string.IsNullOrEmpty(propName) && floatProps.TryGetValue("value", out var valObj) && (valObj is float || valObj is double || valObj is int))
                {
                    try
                    {
                        float newVal = Convert.ToSingle(valObj);
                        if (mat.HasProperty(propName) && mat.GetFloat(propName) != newVal)
                        {
                            mat.SetFloat(propName, newVal);
                            modified = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Error parsing float property '{propName}': {ex.Message}");
                    }
                }
            }
            if (properties.TryGetValue("texture", out var texObj) && texObj is Dictionary<string, object> texProps)
            {
                string propName = texProps.ContainsKey("name") ? texProps["name"]?.ToString() : "_MainTex";
                string texPath = texProps.ContainsKey("path") ? texProps["path"]?.ToString() : null;
                if (!string.IsNullOrEmpty(texPath))
                {
                    Texture newTex = AssetDatabase.LoadAssetAtPath<Texture>(SanitizeAssetPath(texPath));
                    if (newTex != null && mat.HasProperty(propName) && mat.GetTexture(propName) != newTex)
                    {
                        mat.SetTexture(propName, newTex);
                        modified = true;
                    }
                    else if (newTex == null)
                    {
                        Debug.LogWarning($"Texture not found at path: {texPath}");
                    }
                }
            }
            return modified;
        }

        /// <summary>
        /// Helper genérico para establecer propiedades en cualquier UnityEngine.Object usando reflexión.
        /// </summary>
        private static bool ApplyObjectProperties(UnityEngine.Object target, Dictionary<string, object> properties)
        {
            if (target == null || properties == null)
                return false;
            bool modified = false;
            Type type = target.GetType();

            foreach (var prop in properties)
            {
                string propName = prop.Key;
                object propValue = prop.Value;
                if (SetPropertyOrField(target, propName, propValue, type))
                {
                    modified = true;
                }
            }
            return modified;
        }

        /// <summary>
        /// Helper para establecer una propiedad o campo vía reflexión, manejando tipos básicos y objetos Unity.
        /// </summary>
        private static bool SetPropertyOrField(
            object target,
            string memberName,
            object value,
            Type type = null
        )
        {
            type = type ?? target.GetType();
            System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.IgnoreCase;

            try
            {
                System.Reflection.PropertyInfo propInfo = type.GetProperty(memberName, flags);
                if (propInfo != null && propInfo.CanWrite)
                {
                    object convertedValue = ConvertToType(value, propInfo.PropertyType);
                    if (
                        convertedValue != null
                        && !object.Equals(propInfo.GetValue(target), convertedValue)
                    )
                    {
                        propInfo.SetValue(target, convertedValue);
                        return true;
                    }
                }
                else
                {
                    System.Reflection.FieldInfo fieldInfo = type.GetField(memberName, flags);
                    if (fieldInfo != null)
                    {
                        object convertedValue = ConvertToType(value, fieldInfo.FieldType);
                        if (
                            convertedValue != null
                            && !object.Equals(fieldInfo.GetValue(target), convertedValue)
                        )
                        {
                            fieldInfo.SetValue(target, convertedValue);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[SetPropertyOrField] Failed to set '{memberName}' on {type.Name}: {ex.Message}"
                );
            }
            return false;
        }

        /// <summary>
        /// Conversión simple de object a tipo destino para tipos Unity y primitivos comunes.
        /// </summary>
        private static object ConvertToType(object value, Type targetType)
        {
            try
            {
                if (value == null)
                    return null;

                if (targetType == typeof(string))
                    return value.ToString();
                if (targetType == typeof(int))
                    return Convert.ToInt32(value);
                if (targetType == typeof(float))
                    return Convert.ToSingle(value);
                if (targetType == typeof(bool))
                    return Convert.ToBoolean(value);
                if (targetType == typeof(Vector2) && value is IEnumerable<object> arrV2 && arrV2.Count() == 2)
                    return new Vector2(
                        Convert.ToSingle(arrV2.ElementAt(0)),
                        Convert.ToSingle(arrV2.ElementAt(1))
                    );
                if (targetType == typeof(Vector3) && value is IEnumerable<object> arrV3 && arrV3.Count() == 3)
                    return new Vector3(
                        Convert.ToSingle(arrV3.ElementAt(0)),
                        Convert.ToSingle(arrV3.ElementAt(1)),
                        Convert.ToSingle(arrV3.ElementAt(2))
                    );
                if (targetType == typeof(Vector4) && value is IEnumerable<object> arrV4 && arrV4.Count() == 4)
                    return new Vector4(
                        Convert.ToSingle(arrV4.ElementAt(0)),
                        Convert.ToSingle(arrV4.ElementAt(1)),
                        Convert.ToSingle(arrV4.ElementAt(2)),
                        Convert.ToSingle(arrV4.ElementAt(3))
                    );
                if (targetType == typeof(Quaternion) && value is IEnumerable<object> arrQ && arrQ.Count() == 4)
                    return new Quaternion(
                        Convert.ToSingle(arrQ.ElementAt(0)),
                        Convert.ToSingle(arrQ.ElementAt(1)),
                        Convert.ToSingle(arrQ.ElementAt(2)),
                        Convert.ToSingle(arrQ.ElementAt(3))
                    );
                if (targetType == typeof(Color) && value is IEnumerable<object> arrC && arrC.Count() >= 3)
                    return new Color(
                        Convert.ToSingle(arrC.ElementAt(0)),
                        Convert.ToSingle(arrC.ElementAt(1)),
                        Convert.ToSingle(arrC.ElementAt(2)),
                        arrC.Count() > 3 ? Convert.ToSingle(arrC.ElementAt(3)) : 1.0f
                    );
                if (targetType.IsEnum)
                    return Enum.Parse(targetType, value.ToString(), true);

                // Cargar objetos Unity por path si es string
                if (
                    typeof(UnityEngine.Object).IsAssignableFrom(targetType)
                    && value is string assetPathStr
                )
                {
                    string assetPath = SanitizeAssetPath(assetPathStr);
                    UnityEngine.Object loadedAsset = AssetDatabase.LoadAssetAtPath(assetPath, targetType);
                    if (loadedAsset == null)
                    {
                        Debug.LogWarning($"[ConvertToType] No se pudo cargar el asset de tipo {targetType.Name} desde la ruta: {assetPath}");
                    }
                    return loadedAsset;
                }

                // Fallback: conversión directa
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ConvertToType] No se pudo convertir '{value}' a tipo '{targetType.Name}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Helper to find a Type by name, searching relevant assemblies.
        /// Needed for creating ScriptableObjects or finding component types by name.
        /// </summary>
        private static Type FindType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            // Try direct lookup first (common Unity types often don't need assembly qualified name)
            var type =
                Type.GetType(typeName)
                ?? Type.GetType($"UnityEngine.{typeName}, UnityEngine.CoreModule")
                ?? Type.GetType($"UnityEngine.UI.{typeName}, UnityEngine.UI")
                ?? Type.GetType($"UnityEditor.{typeName}, UnityEditor.CoreModule");

            if (type != null)
                return type;

            // If not found, search loaded assemblies (slower but more robust for user scripts)
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Look for non-namespaced first
                type = assembly.GetType(typeName, false, true); // throwOnError=false, ignoreCase=true
                if (type != null)
                    return type;

                // Check common namespaces if simple name given
                type = assembly.GetType("UnityEngine." + typeName, false, true);
                if (type != null)
                    return type;
                type = assembly.GetType("UnityEditor." + typeName, false, true);
                if (type != null)
                    return type;
                // Add other likely namespaces if needed (e.g., specific plugins)
            }

            Debug.LogWarning($"[FindType] Type '{typeName}' not found in any loaded assembly.");
            return null; // Not found
        }

        // --- Data Serialization ---

        /// <summary>
        /// Creates a serializable representation of an asset.
        /// </summary>
        private static object GetAssetData(string path, bool generatePreview = false)
        {
            if (string.IsNullOrEmpty(path) || !AssetExists(path))
                return null;

            string guid = AssetDatabase.AssetPathToGUID(path);
            Type assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            string previewBase64 = null;
            int previewWidth = 0;
            int previewHeight = 0;

            if (generatePreview && asset != null)
            {
                Texture2D preview = AssetPreview.GetAssetPreview(asset);

                if (preview != null)
                {
                    try
                    {
                        // Ensure texture is readable for EncodeToPNG
                        // Creating a temporary readable copy is safer
                        RenderTexture rt = RenderTexture.GetTemporary(
                            preview.width,
                            preview.height
                        );
                        Graphics.Blit(preview, rt);
                        RenderTexture previous = RenderTexture.active;
                        RenderTexture.active = rt;
                        Texture2D readablePreview = new Texture2D(preview.width, preview.height);
                        readablePreview.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                        readablePreview.Apply();
                        RenderTexture.active = previous;
                        RenderTexture.ReleaseTemporary(rt);

                        byte[] pngData = readablePreview.EncodeToPNG();
                        previewBase64 = Convert.ToBase64String(pngData);
                        previewWidth = readablePreview.width;
                        previewHeight = readablePreview.height;
                        UnityEngine.Object.DestroyImmediate(readablePreview); // Clean up temp texture
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            $"Failed to generate readable preview for '{path}': {ex.Message}. Preview might not be readable."
                        );
                        // Fallback: Try getting static preview if available?
                        // Texture2D staticPreview = AssetPreview.GetMiniThumbnail(asset);
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"Could not get asset preview for {path} (Type: {assetType?.Name}). Is it supported?"
                    );
                }
            }

            return new
            {
                path = path,
                guid = guid,
                assetType = assetType?.FullName ?? "Unknown",
                name = Path.GetFileNameWithoutExtension(path),
                fileName = Path.GetFileName(path),
                isFolder = AssetDatabase.IsValidFolder(path),
                instanceID = asset?.GetInstanceID() ?? 0,
                lastWriteTimeUtc = File.GetLastWriteTimeUtc(
                        Path.Combine(Directory.GetCurrentDirectory(), path)
                    )
                    .ToString("o"), // ISO 8601
                // --- Preview Data ---
                previewBase64 = previewBase64, // PNG data as Base64 string
                previewWidth = previewWidth,
                previewHeight = previewHeight,
                // TODO: Add more metadata? Importer settings? Dependencies?
            };
        }
    }
} 