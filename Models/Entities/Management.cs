namespace Graduation_Project_Backend.Models.Entities
{
    public sealed class Management
    {
        public Guid ManagerId { get; set; }
        public Guid StoreId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
