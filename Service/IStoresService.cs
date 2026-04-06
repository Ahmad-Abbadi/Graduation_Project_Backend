using Graduation_Project_Backend.Models.Entities;

namespace Graduation_Project_Backend.Service
{
    public interface IStoresService
    {
        Task<List<Store>> GetStoresAsync();
        Task<Store?> GetStoreByIdAsync(Guid storeId);
    }
}
