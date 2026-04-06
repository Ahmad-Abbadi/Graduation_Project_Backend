using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service
{
    public sealed class StoresService : IStoresService
    {
        private readonly AppDbContext _db;

        public StoresService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Store>> GetStoresAsync()
        {
            return await _db.Stores
                .AsNoTracking()
                .OrderBy(store => store.Name)
                .ToListAsync();
        }

        public async Task<Store?> GetStoreByIdAsync(Guid storeId)
        {
            return await _db.Stores
                .AsNoTracking()
                .SingleOrDefaultAsync(store => store.Id == storeId);
        }
    }
}
