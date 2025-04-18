using System.Collections.Generic;
using UnityMCP.Bridge.Shared.Messages.Abstractions;

namespace UnityMCP.Bridge.Shared.Messages
{
    public class ManageGameObjectMessage : ToolMessage
    {
        public string Action { get; set; }
        public string Target { get; set; }
        public string SearchMethod { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public string Parent { get; set; }
        public List<float> Position { get; set; }
        public List<float> Rotation { get; set; }
        public List<float> Scale { get; set; }
        public List<string> ComponentsToAdd { get; set; }
        public string PrimitiveType { get; set; }
        public bool SaveAsPrefab { get; set; }
        public string PrefabPath { get; set; }
        public bool? SetActive { get; set; }
        public string Layer { get; set; }
        public List<string> ComponentsToRemove { get; set; }
        public Dictionary<string, Dictionary<string, object>> ComponentProperties { get; set; }
        public string SearchTerm { get; set; }
        public bool FindAll { get; set; }
        public bool SearchInChildren { get; set; }
        public bool SearchInactive { get; set; }
        public string ComponentName { get; set; }

        public ManageGameObjectMessage() : base("manage_gameobject")
        {
        }

        public ManageGameObjectMessage(
            string action,
            string target = null,
            string searchMethod = null,
            string name = null,
            string tag = null,
            string parent = null,
            List<float> position = null,
            List<float> rotation = null,
            List<float> scale = null,
            List<string> componentsToAdd = null,
            string primitiveType = null,
            bool saveAsPrefab = false,
            string prefabPath = null,
            bool? setActive = null,
            string layer = null,
            List<string> componentsToRemove = null,
            Dictionary<string, Dictionary<string, object>> componentProperties = null,
            string searchTerm = null,
            bool findAll = false,
            bool searchInChildren = false,
            bool searchInactive = false,
            string componentName = null
        ) : base("manage_gameobject")
        {
            Action = action;
            Target = target;
            SearchMethod = searchMethod;
            Name = name;
            Tag = tag;
            Parent = parent;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            ComponentsToAdd = componentsToAdd;
            PrimitiveType = primitiveType;
            SaveAsPrefab = saveAsPrefab;
            PrefabPath = prefabPath;
            SetActive = setActive;
            Layer = layer;
            ComponentsToRemove = componentsToRemove;
            ComponentProperties = componentProperties;
            SearchTerm = searchTerm;
            FindAll = findAll;
            SearchInChildren = searchInChildren;
            SearchInactive = searchInactive;
            ComponentName = componentName;
        }

        public const string MessageType = "manage_gameobject";
    }
} 