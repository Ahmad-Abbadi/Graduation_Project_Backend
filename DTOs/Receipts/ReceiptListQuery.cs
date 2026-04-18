using System.ComponentModel.DataAnnotations;

namespace Graduation_Project_Backend.DTOs.Receipts
{
    public sealed class ReceiptListQuery
    {
        public Guid? StoreId { get; set; }
        public string? Status { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
    }
}
