namespace Graduation_Project_Backend.DTOs.Offers
{
    public sealed class ManageOfferListItemResponse
    {
        public long Id { get; init; }
        public Guid StoreId { get; init; }
        public string StoreName { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTimeOffset StartAt { get; init; }
        public DateTimeOffset EndAt { get; init; }
    }
}
