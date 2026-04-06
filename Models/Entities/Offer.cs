namespace Graduation_Project_Backend.Models.Entities
{
    public sealed class Offer
    {
        public long Id { get; set; }
        public Guid StoreId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset MadeAt { get; set; }
        public Guid? MallID { get; set; }
    }
}
