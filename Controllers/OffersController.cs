using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.DOTs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Graduation_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OffersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public OffersController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OfferDto>>> GetOffers(
            [FromQuery] Guid? storeId,
            [FromQuery] bool activeOnly = true)
        {
            var now = DateTimeOffset.UtcNow;

            var query = _db.Offers
                .AsNoTracking()
                .Include(o => o.Store)
                .AsQueryable();

            if (storeId.HasValue)
                query = query.Where(o => o.StoreId == storeId.Value);

            if (activeOnly)
            {
                query = query.Where(o =>
                    o.IsActive &&
                    o.StartAt <= now &&
                    o.EndAt >= now);
            }

            var offers = await query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OfferDto
                {
                    Id = o.Id,
                    StoreId = o.StoreId,
                    StoreName = o.Store != null ? o.Store.Name : "",
                    Title = o.Title,
                    Description = o.Description,
                    OfferType = o.OfferType,
                    DiscountPercent = o.DiscountPercent,
                    BonusPoints = o.BonusPoints,
                    PointsMultiplier = o.PointsMultiplier,
                    StartAt = o.StartAt,
                    EndAt = o.EndAt,
                    IsActive = o.IsActive,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return Ok(offers);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<OfferDto>> GetOffer(long id)
        {
            var offer = await _db.Offers
                .AsNoTracking()
                .Include(o => o.Store)
                .Where(o => o.Id == id)
                .Select(o => new OfferDto
                {
                    Id = o.Id,
                    StoreId = o.StoreId,
                    StoreName = o.Store != null ? o.Store.Name : "",
                    Title = o.Title,
                    Description = o.Description,
                    OfferType = o.OfferType,
                    DiscountPercent = o.DiscountPercent,
                    BonusPoints = o.BonusPoints,
                    PointsMultiplier = o.PointsMultiplier,
                    StartAt = o.StartAt,
                    EndAt = o.EndAt,
                    IsActive = o.IsActive,
                    CreatedAt = o.CreatedAt
                })
                .SingleOrDefaultAsync();

            if (offer == null)
                return NotFound("Offer not found.");

            return Ok(offer);
        }

        [HttpPost("{id:long}/redeem")]
        public async Task<ActionResult<RedeemOfferResponseDto>> RedeemOffer(
            long id,
            [FromBody] RedeemOfferRequestDto request)
        {
            if (request == null || request.UserId == Guid.Empty)
                return BadRequest("UserId is required.");

            var now = DateTimeOffset.UtcNow;

            var offer = await _db.Offers
                .SingleOrDefaultAsync(o => o.Id == id);
            if (offer == null)
                return NotFound("Offer not found.");

            if (!offer.IsActive || offer.StartAt > now || offer.EndAt < now)
                return BadRequest("Offer is not active.");

            if (!offer.BonusPoints.HasValue || offer.BonusPoints.Value <= 0)
                return BadRequest("Offer does not define points cost.");

            var user = await _db.UserProfiles
                .SingleOrDefaultAsync(u => u.Id == request.UserId);
            if (user == null)
                return NotFound("User not found.");

            var pointsCost = offer.BonusPoints.Value;
            if (user.TotalPoints < pointsCost)
                return BadRequest("Not enough points.");

            const decimal amount = 1m;
            var discountPercent = offer.DiscountPercent.GetValueOrDefault();
            var discountFactor = discountPercent > 0
                ? Math.Max(0m, 1m - (discountPercent / 100m))
                : 1m;
            var finalPrice = amount * discountFactor;
            var multiplier = offer.PointsMultiplier.GetValueOrDefault(1m);
            if (multiplier <= 0)
                multiplier = 1m;
            var earnedPoints = (int)Math.Round(finalPrice * multiplier, MidpointRounding.AwayFromZero);

            user.TotalPoints = user.TotalPoints - pointsCost + earnedPoints;
            await _db.SaveChangesAsync();

            return Ok(new RedeemOfferResponseDto
            {
                OfferId = offer.Id,
                UserId = user.Id,
                Amount = amount,
                FinalPrice = finalPrice,
                EarnedPoints = earnedPoints,
                DiscountPercent = offer.DiscountPercent,
                PointsMultiplier = offer.PointsMultiplier,
                PointsSpent = pointsCost,
                RemainingPoints = user.TotalPoints,
                Message = "Offer redeemed."
            });
        }

        [HttpPost("dev-create")]
        public async Task<ActionResult<OfferDto>> DevCreateOffer([FromBody] DevCreateOfferRequestDto request)
        {
            if (!_env.IsDevelopment())
                return Forbid("Dev-only endpoint.");

            if (request == null)
                return BadRequest("Invalid body.");

            if (request.StoreId == Guid.Empty)
                return BadRequest("StoreId is required.");

            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest("Title is required.");

            if (string.IsNullOrWhiteSpace(request.OfferType))
                return BadRequest("OfferType is required.");

            var offerType = request.OfferType.Trim();

            var now = DateTimeOffset.UtcNow;
            var startAt = request.StartAt ?? now;
            var endAt = request.EndAt ?? now.AddDays(30);

            if (endAt < startAt)
                return BadRequest("EndAt must be after StartAt.");

            var store = await _db.Stores.SingleOrDefaultAsync(s => s.Id == request.StoreId);
            if (store == null)
            {
                var storeName = string.IsNullOrWhiteSpace(request.StoreName)
                    ? "Dev Store"
                    : request.StoreName.Trim();

                store = new Models.Entities.Store
                {
                    Id = request.StoreId,
                    Name = storeName
                };

                _db.Stores.Add(store);
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx &&
                                                  pgEx.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
                                                  pgEx.ConstraintName == "store_id_fkey")
                {
                    return BadRequest("StoreId must exist in auth.users before it can be added to store.");
                }
            }

            var offer = new Models.Entities.Offer
            {
                StoreId = request.StoreId,
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                OfferType = offerType,
                DiscountPercent = request.DiscountPercent,
                BonusPoints = request.BonusPoints,
                PointsMultiplier = request.PointsMultiplier,
                StartAt = startAt,
                EndAt = endAt,
                IsActive = request.IsActive ?? true,
                CreatedAt = now
            };

            _db.Offers.Add(offer);
            await _db.SaveChangesAsync();

            var response = new OfferDto
            {
                Id = offer.Id,
                StoreId = offer.StoreId,
                StoreName = store.Name,
                Title = offer.Title,
                Description = offer.Description,
                OfferType = offer.OfferType,
                DiscountPercent = offer.DiscountPercent,
                BonusPoints = offer.BonusPoints,
                PointsMultiplier = offer.PointsMultiplier,
                StartAt = offer.StartAt,
                EndAt = offer.EndAt,
                IsActive = offer.IsActive,
                CreatedAt = offer.CreatedAt
            };

            return Ok(response);
        }
    }
}
