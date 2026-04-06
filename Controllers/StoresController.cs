using Graduation_Project_Backend.Filters;
using Graduation_Project_Backend.Service;
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
        public async Task<IActionResult> GetStores()
        {
            var stores = await _storesService.GetStoresAsync();
            return Ok(stores);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetStoreById(Guid id)
        {
            var store = await _storesService.GetStoreByIdAsync(id);
            if (store == null)
                return NotFound("Store not found.");

            return Ok(store);
        }
    }
}
