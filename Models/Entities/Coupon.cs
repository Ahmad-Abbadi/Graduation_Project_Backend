using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project_Backend.Models.Entities
{
    public class Coupon
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid ManagerId { get; set; }
        public string Type { get; set; } = "";
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public string Discription { get; set; } = "";
        public bool IsActive { get; set; }
        public decimal? CostPoint { get; set; }
        [Column("mall_id")]
        public Guid MallID { get; set; }
    }
}
