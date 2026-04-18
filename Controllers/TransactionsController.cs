using Graduation_Project_Backend.DTOs;
using Graduation_Project_Backend.DTOs.Receipts;
using Graduation_Project_Backend.Extensions;
using Graduation_Project_Backend.Filters;
using Graduation_Project_Backend.Service;
using Graduation_Project_Backend.Service.Common;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class TransactionsController : ControllerBase
    {
        private readonly IRewardsService _rewardsService;

        public TransactionsController(IRewardsService rewardsService)
        {
            _rewardsService = rewardsService;
        }

        [HttpPost]
        public async Task<IActionResult> AddTransaction([FromBody] AddTransactionDto? dto)
        {
            if (dto == null)
                return BadRequest("Request body is null.");

            if (dto.Price < 0)
                return BadRequest("Price cannot be negative.");

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
                return BadRequest("Phone number is required.");

            if (string.IsNullOrWhiteSpace(dto.ReceiptId))
                return BadRequest("Receipt ID is required.");

            if (dto.StoreId == Guid.Empty)
                return BadRequest("Store ID is required.");

            try
            {
                var result = await _rewardsService.ProcessTransactionAsync(
                    dto.PhoneNumber,
                    dto.StoreId,
                    dto.ReceiptId,
                    dto.ReceiptDescription,
                    dto.Price
                );

                return CreatedAtAction(nameof(GetTransactionById),
                    new { id = result.TransactionId },
                    result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id:long}")]
        [SessionRequired]
        public async Task<IActionResult> GetTransactionById(long id, CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var transaction = await _rewardsService.GetReceiptDetailsForUserAsync(session.UserId, id, cancellationToken);
                if (transaction == null)
                    return NotFound("Transaction not found.");

                return Ok(transaction);
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpGet("my-receipts")]
        [SessionRequired]
        public async Task<IActionResult> GetMyReceipts([FromQuery] ReceiptListQuery query, CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var result = await _rewardsService.GetMyReceiptsAsync(session.UserId, query, cancellationToken);
                return Ok(result);
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
