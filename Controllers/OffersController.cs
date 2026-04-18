using Graduation_Project_Backend.DTOs.Common;
using Graduation_Project_Backend.DTOs.Offers;
using Graduation_Project_Backend.Extensions;
using Graduation_Project_Backend.Filters;
using Graduation_Project_Backend.Service;
using Graduation_Project_Backend.Service.Common;
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
        public async Task<IActionResult> GetOffers(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received request to fetch visible offers.");

            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var offers = await _offersService.GetVisibleOffersAsync(session.UserId, cancellationToken);
                _logger.LogInformation("Fetched {OfferCount} visible offers successfully.", offers.Count);
                return Ok(offers);
            }
            catch (ApiException ex)
            {
                _logger.LogWarning(ex, "Offers request failed.");
                return ToErrorResult(ex);
            }
        }

        [HttpGet("manage")]
        public async Task<IActionResult> GetManagedOffers(CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var offers = await _offersService.GetManagedOffersAsync(session.UserId, cancellationToken);
                return Ok(offers);
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateOffer([FromBody] CreateOfferRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var offer = await _offersService.CreateOfferAsync(session.UserId, request, cancellationToken);
                return CreatedAtAction(nameof(GetOffers), new { id = offer.Id }, offer);
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateOffer(long id, [FromBody] UpdateOfferRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var offer = await _offersService.UpdateOfferAsync(session.UserId, id, request, cancellationToken);
                return Ok(offer);
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteOffer(long id, CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                await _offersService.DeleteOfferAsync(session.UserId, id, cancellationToken);
                return NoContent();
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpPatch("{id:long}/status")]
        public async Task<IActionResult> SetOfferStatus(long id, [FromBody] UpdateStatusRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var offer = await _offersService.SetOfferStatusAsync(session.UserId, id, request.IsActive, cancellationToken);
                return Ok(offer);
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        private IActionResult ToErrorResult(ApiException exception)
            => StatusCode(exception.StatusCode, new
            {
                success = false,
                error = new
                {
                    code = exception.Code,
                    message = exception.Message
                }
            });
    }
}
