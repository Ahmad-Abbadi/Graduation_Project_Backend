using Graduation_Project_Backend.Models.Entities;

namespace Graduation_Project_Backend.Service
{
    public interface IOffersService
    {
        Task<List<Offer>> GetOffersAsync();
    }
}
