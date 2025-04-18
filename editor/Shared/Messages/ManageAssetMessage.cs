using System.Collections.Generic;
using UnityMCP.Bridge.Shared.Messages.Abstractions;

namespace UnityMCP.Bridge.Shared.Messages
{
    public class ManageAssetMessage : ToolMessage
    {
        public string Action { get; set; }
        public string Path { get; set; }
        public string AssetType { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public string Destination { get; set; }
        public bool GeneratePreview { get; set; }
        public string SearchPattern { get; set; }
        public string FilterType { get; set; }
        public string FilterDateAfter { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }

        public ManageAssetMessage() : base("manage_asset")
        {
            Properties = new Dictionary<string, object>();
        }

        public ManageAssetMessage(
            string action,
            string path,
            string assetType = null,
            Dictionary<string, object> properties = null,
            string destination = null,
            bool generatePreview = false,
            string searchPattern = null,
            string filterType = null,
            string filterDateAfter = null,
            int? pageSize = null,
            int? pageNumber = null
        ) : base("manage_asset")
        {
            Action = action;
            Path = path;
            AssetType = assetType;
            Properties = properties ?? new Dictionary<string, object>();
            Destination = destination;
            GeneratePreview = generatePreview;
            SearchPattern = searchPattern;
            FilterType = filterType;
            FilterDateAfter = filterDateAfter;
            PageSize = pageSize;
            PageNumber = pageNumber;
        }

        public const string MessageType = "manage_asset";
    }
} 