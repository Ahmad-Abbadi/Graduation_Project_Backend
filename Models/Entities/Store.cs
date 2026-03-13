namespace Graduation_Project_Backend.Models.Entities
{
    public sealed class Store
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid MallID { get; set; }
    }
}
