using System.ComponentModel.DataAnnotations;

namespace Graduation_Project_Backend.DTOs.Offers
{
    public sealed class CreateOfferRequest
    {
        [Required]
        public Guid StoreId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
