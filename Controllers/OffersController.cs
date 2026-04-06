using Graduation_Project_Backend.Filters;
using Graduation_Project_Backend.Service;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SessionRequired]
    public sealed class OffersController : ControllerBase
    {
        private readonly IOffersService _offersService;
        private readonly ILogger<OffersController> _logger;

        public OffersController(IOffersService offersService, ILogger<OffersController> logger)
        {
            _offersService = offersService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetOffers()
        {
            _logger.LogInformation("Received request to fetch offers.");

            try
            {
                var offers = await _offersService.GetOffersAsync();
                _logger.LogInformation("Fetched {OfferCount} offers successfully.", offers.Count);
                return Ok(offers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch offers.");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    error = new
                    {
                        code = "OFFERS_FETCH_FAILED",
                        message = "Failed to fetch offers."
                    }
                });
            }
        }
    }
}
