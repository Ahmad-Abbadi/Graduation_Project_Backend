using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service
{
    public sealed class OffersService : IOffersService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<OffersService> _logger;

        public OffersService(AppDbContext db, ILogger<OffersService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<Offer>> GetOffersAsync()
        {
            _logger.LogInformation("Loading offers from database.");

            var offers = await _db.Offers
                .AsNoTracking()
                .OrderByDescending(offer => offer.MadeAt)
                .ToListAsync();S

            _logger.LogInformation("Loaded {OfferCount} offers from database.", offers.Count);
            return offers;
        }

    }
}
