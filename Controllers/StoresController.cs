using Graduation_Project_Backend.DTOs.Stores;
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
    public sealed class StoresController : ControllerBase
    {
        private readonly IStoresService _storesService;

        public StoresController(IStoresService storesService)
        {
            _storesService = storesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetStores(CancellationToken cancellationToken)
        {
            var session = HttpContext.GetCurrentUserSession();
            var stores = await _storesService.GetVisibleStoresAsync(session.UserId, cancellationToken);
            return Ok(stores);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetStoreById(Guid id, CancellationToken cancellationToken)
        {
            var session = HttpContext.GetCurrentUserSession();
            var store = await _storesService.GetVisibleStoreByIdAsync(session.UserId, id, cancellationToken);
            if (store == null)
                return NotFound("Store not found.");

            return Ok(store);
        }

        [HttpGet("manage")]
        public async Task<IActionResult> GetManagedStores(CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var stores = await _storesService.GetManagedStoresAsync(session.UserId, cancellationToken);
                return Ok(stores);
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateStore([FromBody] CreateStoreRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var store = await _storesService.CreateStoreAsync(session.UserId, request, cancellationToken);
                return CreatedAtAction(nameof(GetStoreById), new { id = store.Id }, store);
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateStore(Guid id, [FromBody] UpdateStoreRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var store = await _storesService.UpdateStoreAsync(session.UserId, id, request, cancellationToken);
                return Ok(store);
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
