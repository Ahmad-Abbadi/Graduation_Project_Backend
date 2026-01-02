using Microsoft.AspNetCore.Mvc;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.DOTs;
using Graduation_Project_Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TransactionsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> AddTransaction([FromBody] AddTransactionDto dto)
        {
            if (dto == null)
                return BadRequest("Request body is null.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.Price < 0)
                return BadRequest("Price cannot be negative.");

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
                return BadRequest("Phone number is required.");

            if (string.IsNullOrWhiteSpace(dto.ReceiptId))
                return BadRequest("Receipt ID is required.");

            // Normalize phone
            var phone = NormalizePhone(dto.PhoneNumber);

            // Find user
            var user = await _db.UserProfiles
                .SingleOrDefaultAsync(u => u.PhoneNumber == phone);

            if (user == null)
                return NotFound($"User with phone number {dto.PhoneNumber} not found.");

            // Prevent duplicate receipt
            var receiptExists = await _db.Transactions
                .AnyAsync(t => t.ReceiptId == dto.ReceiptId);

            if (receiptExists)
                return Conflict($"Receipt with ID {dto.ReceiptId} already exists.");

            // Calculate points
            var points = CalculatePoints(dto.Price);
            Console.WriteLine(user.Id);
            // Create transaction (NO ID assignment)
            var transaction = new Transaction
            {
                ReceiptId = dto.ReceiptId,
                ReceiptDescription = dto.ReceiptDescription,
                UserId = user.Id,
                StoreId = dto.StoreId,
                Price = dto.Price,
                Points = points,
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Update user points
            user.TotalPoints += points;

            _db.Transactions.Add(transaction);
            await _db.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetTransactionById),
                new { id = transaction.Id },
                new
                {
                    transaction.Id,
                    transaction.ReceiptId,
                    transaction.UserId,
                    user_phone = user.PhoneNumber,
                    user_name = user.Name,
                    transaction.StoreId,
                    transaction.Price,
                    transaction.Points,
                    user_total_points = user.TotalPoints,
                    transaction.CreatedAt,
                    message = "Transaction created successfully and points added to user."
                }
            );
        }

        // 🔎 Get transaction by ID
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetTransactionById(long id)
        {
            var transaction = await _db.Transactions
                .Include(t => t.User)
                .SingleOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
                return NotFound("Transaction not found.");

            return Ok(new
            {
                transaction.Id,
                transaction.ReceiptId,
                transaction.UserId,
                user_phone = transaction.User?.PhoneNumber,
                user_name = transaction.User?.Name,
                transaction.StoreId,
                transaction.Price,
                transaction.Points,
                transaction.CreatedAt
            });
        }

        // 📊 Points calculation
        private static int CalculatePoints(decimal price)
        {
            return (int)(price * 100);
        }

        // 📱 Normalize phone
        private static string NormalizePhone(string phone)
        {
            phone = phone.Trim().Replace(" ", "").Replace("-", "");

            if (phone.StartsWith("07") && phone.Length == 10)
                return "+962" + phone[1..];

            return phone.StartsWith("+") ? phone : phone;
        }
    }

}