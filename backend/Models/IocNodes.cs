using System;
using System.Collections.Generic;

namespace backend.Models
{
    public class IocNode
    {
        public string? _key { get; set; }
        public string? _id { get; set; }
        public string? _rev { get; set; }

        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int RiskScore { get; set; }
        public string? Country { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public string OriginRef { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}