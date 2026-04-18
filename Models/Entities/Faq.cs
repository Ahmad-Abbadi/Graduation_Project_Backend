namespace Graduation_Project_Backend.Models.Entities
{
    public sealed class Faq
    {
        public Guid Id { get; set; }
        public Guid MallID { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string[]? Keywords { get; set; }
        public string? Language { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public int UsageCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
