namespace Graduation_Project_Backend.Service
{
    public sealed class UserAccessContext
    {
        public Guid UserId { get; init; }
        public Guid MallID { get; init; }
        public string UserRole { get; init; } = string.Empty;
        public bool IsManager { get; init; }
        public bool IsMallWideManager { get; init; }
        public Guid? ManagerId { get; init; }
        public string? ManagerRole { get; init; }
        public HashSet<Guid> AssignedStoreIds { get; init; } = [];

        public bool CanAccessStore(Guid storeId)
            => IsManager && (IsMallWideManager || AssignedStoreIds.Contains(storeId));
    }
}
