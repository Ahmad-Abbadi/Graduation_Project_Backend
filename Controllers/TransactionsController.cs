using Graduation_Project_Backend.DOTs;
using Graduation_Project_Backend.Service;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ServiceClass _service;

        public TransactionsController(ServiceClass service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> AddTransaction(AddTransactionDto dto)
        {
            if (dto == null)
                return BadRequest("Request body is null.");

            if (dto.Price < 0)
                return BadRequest("Price cannot be negative.");

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
                return BadRequest("Phone number is required.");

            if (string.IsNullOrWhiteSpace(dto.ReceiptId))
                return BadRequest("Receipt ID is required.");

            if (dto.StoreId==null)
                return BadRequest("Store ID is required.");

            

            //var store = await _service.GetStoreByIdAsync(dto.StoreId);
            //if (store==null)
            //{
            //    store = await _service.CreateStoreAsync("Walmart");

            //}

            var phone = NormalizePhone(dto.PhoneNumber);

            try
            {
                var result = await _service.ProcessTransactionAsync(
                    phone,
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
            var transaction = await _service.GetTransactionDetailsAsync(id);
            if (transaction == null)
                return NotFound("Transaction not found.");

            return Ok(transaction);
        }

        private static string NormalizePhone(string phone)
        {
            phone = phone.Trim().Replace(" ", "").Replace("-", "");

            if (phone.StartsWith("07") && phone.Length == 10)
                return "+962" + phone[1..];

            return phone;
        }
    }
}
