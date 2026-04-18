using Graduation_Project_Backend.DTOs.Stores;
using Graduation_Project_Backend.Models.Entities;

namespace Graduation_Project_Backend.Service
{
    public interface IStoresService
    {
        Task<List<Store>> GetStoresAsync();
        Task<Store?> GetStoreByIdAsync(Guid storeId);
        Task<IReadOnlyList<StoreResponse>> GetVisibleStoresAsync(Guid currentUserId, CancellationToken cancellationToken = default);
        Task<StoreResponse?> GetVisibleStoreByIdAsync(Guid currentUserId, Guid storeId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ManageStoreListItemResponse>> GetManagedStoresAsync(Guid currentUserId, CancellationToken cancellationToken = default);
        Task<StoreResponse> CreateStoreAsync(Guid currentUserId, CreateStoreRequest request, CancellationToken cancellationToken = default);
        Task<StoreResponse> UpdateStoreAsync(Guid currentUserId, Guid storeId, UpdateStoreRequest request, CancellationToken cancellationToken = default);
    }
}
