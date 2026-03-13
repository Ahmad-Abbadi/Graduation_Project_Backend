using Graduation_Project_Backend.DTOs;
using Graduation_Project_Backend.Service;
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
        public async Task<IActionResult> GetTransactionById(long id)
        {
            var transaction = await _rewardsService.GetTransactionDetailsAsync(id);
            if (transaction == null)
                return NotFound("Transaction not found.");

            return Ok(transaction);
        }
    }
}
