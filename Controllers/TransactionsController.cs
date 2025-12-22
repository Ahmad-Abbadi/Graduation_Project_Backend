using Microsoft.AspNetCore.Mvc;
using Cahser_API.Models;
using Cahser_API.Models.Entities;
using System.Collections.Concurrent;
using Cahser_API.Models;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    // Thread-safe dictionary
    private static readonly ConcurrentDictionary<Guid, Transaction> all_transactions
        = new ConcurrentDictionary<Guid, Transaction>();

    [HttpPost]
    public IActionResult AddTransaction([FromBody] AddTransactionDto addTransactionDto)
    {
        // 1️⃣ Null check
        if (addTransactionDto == null)
            return BadRequest("Request body is null.");

        // 2 Model validation
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // 3️⃣ Business validation
        if (addTransactionDto.Price < 0)
            return BadRequest("Price cannot be negative.");

        if (addTransactionDto.Points < 0)
            return BadRequest("Points cannot be negative.");

        // 4️⃣ Generate ID if missing
        var transactionId = addTransactionDto.Id == Guid.Empty
            ? Guid.NewGuid()
            : addTransactionDto.Id;

        // 5️⃣ Create entity
        var transaction = new Transaction
        {
            Id = transactionId,
            ReceiptId = addTransactionDto.ReceiptId,
            UserId = addTransactionDto.UserId,
            StoreId = addTransactionDto.StoreId,
            Price = addTransactionDto.Price,
            Points = addTransactionDto.Points,
            ValidatedBy = addTransactionDto.ValidatedBy,
            ValidatedAt = addTransactionDto.ValidatedAt,
            CreatedAt = addTransactionDto.CreatedAt
        };

        // 6️⃣ Prevent duplicate insert
        if (!all_transactions.TryAdd(transaction.Id, transaction))
        {
            return Conflict($"Transaction with ID {transaction.Id} already exists.");
        }

        // 7️⃣ Return proper response
        return CreatedAtAction(
            nameof(GetTransactionById),
            new { id = transaction.Id },
            transaction
        );
    }

    // 🔎 Helper endpoint
    [HttpGet("{id:guid}")]
    public IActionResult GetTransactionById(Guid id)
    {
        if (!all_transactions.TryGetValue(id, out var transaction))
            return NotFound("Transaction not found.");

        return Ok(transaction);
    }
}
