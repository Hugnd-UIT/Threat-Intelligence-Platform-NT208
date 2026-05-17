using System;

namespace backend.Models
{
    public class Logs
    {
        public string? _key { get; set; }
        public string? _id { get; set; }
        public string? _rev { get; set; }

        public string Username { get; set; } = string.Empty;
        public string ClientIp { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}