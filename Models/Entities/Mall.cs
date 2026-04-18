namespace Graduation_Project_Backend.Models.Entities
{
    public sealed class Mall
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }
}
