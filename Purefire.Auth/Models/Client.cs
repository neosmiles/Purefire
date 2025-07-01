namespace Purefire.Auth.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Name { get; set; }
        public string[] AllowedScopes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
    }
}