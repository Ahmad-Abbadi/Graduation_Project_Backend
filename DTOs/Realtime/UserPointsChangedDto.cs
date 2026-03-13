namespace Graduation_Project_Backend.DTOs.Realtime
{
    public sealed class UserPointsChangedDto
    {
        public Guid UserId { get; set; }
        public int TotalPoints { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
    }
}
