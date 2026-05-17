using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace backend.DTOs
{
    public class GraphDataResponse
    {
        [JsonPropertyName("nodes")]
        public List<GraphNode> Nodes 
        { 
            get; 
            set; 
        } = new List<GraphNode>();

        [JsonPropertyName("links")]
        public List<GraphLink> Links 
        { 
            get; 
            set; 
        } = new List<GraphLink>();
    }

    public class GraphNode
    {
        [JsonPropertyName("id")]
        public string Id 
        { 
            get; 
            set; 
        } = string.Empty;

        [JsonPropertyName("name")]
        public string Name 
        { 
            get; 
            set; 
        } = string.Empty;

        [JsonPropertyName("type")]
        public string Type 
        { 
            get; 
            set; 
        } = string.Empty;

        [JsonPropertyName("val")]
        public double Val 
        { 
            get; 
            set; 
        }

        [JsonPropertyName("color")]
        public string Color 
        { 
            get; 
            set; 
        } = string.Empty;

        [JsonPropertyName("actualRiskScore")]
        public double? ActualRiskScore 
        { 
            get; 
            set; 
        }

        [JsonPropertyName("isExpandable")]
        public bool IsExpandable 
        { 
            get; 
            set; 
        }
    }

    public class GraphLink
    {
        [JsonPropertyName("source")]
        public string Source 
        { 
            get; 
            set; 
        } = string.Empty;

        [JsonPropertyName("target")]
        public string Target 
        { 
            get; 
            set; 
        } = string.Empty;

        [JsonPropertyName("name")]
        public string Name 
        { 
            get; 
            set; 
        } = string.Empty;
    }
}