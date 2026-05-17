namespace backend.Models
{
    public class User
    {
        public string? _key { get; set; }
        public string? _id { get; set; }
        public string? _rev { get; set; }
        public string username { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public string? role { get; set; }
        public bool? isLocked { get; set; }
    }
}