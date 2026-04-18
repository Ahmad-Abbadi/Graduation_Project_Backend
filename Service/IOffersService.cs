using Graduation_Project_Backend.DTOs.Offers;
using Graduation_Project_Backend.Models.Entities;

namespace Graduation_Project_Backend.Service
{
    public interface IOffersService
    {
        Task<List<Offer>> GetOffersAsync();
        Task<IReadOnlyList<OfferResponse>> GetVisibleOffersAsync(Guid currentUserId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ManageOfferListItemResponse>> GetManagedOffersAsync(Guid currentUserId, CancellationToken cancellationToken = default);
        Task<OfferResponse> CreateOfferAsync(Guid currentUserId, CreateOfferRequest request, CancellationToken cancellationToken = default);
        Task<OfferResponse> UpdateOfferAsync(Guid currentUserId, long offerId, UpdateOfferRequest request, CancellationToken cancellationToken = default);
        Task DeleteOfferAsync(Guid currentUserId, long offerId, CancellationToken cancellationToken = default);
        Task<OfferResponse> SetOfferStatusAsync(Guid currentUserId, long offerId, bool isActive, CancellationToken cancellationToken = default);
    }
}
