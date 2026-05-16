using Graduation_Project_Backend.Models.ViewModels;
using Graduation_Project_Backend.Filters;
using Graduation_Project_Backend.Service;
using Graduation_Project_Backend.Service.Common;
using Graduation_Project_Backend.Service.Portal;
using Graduation_Project_Backend.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Controllers
{
    [Route("admin")]
    [PortalAuthorize(PortalConstants.AdminRole)]
    public sealed class AdminController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            Guid userId = HttpContext.GetCurrentUserSession().UserId;
            AdminDashboardViewModel model = await _adminService.GetDashboardAsync(userId, cancellationToken);
            return View(model);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard([FromQuery] string? period, CancellationToken cancellationToken)
        {
            Guid userId = HttpContext.GetCurrentUserSession().UserId;
            return View(await _adminService.GetDashboardReportAsync(userId, period, cancellationToken));
        }

        [HttpGet("malls")]
        public async Task<IActionResult> Malls(CancellationToken cancellationToken)
        {
            AdminMallsPageViewModel model = await _adminService.GetMallsPageAsync(cancellationToken);
            return View(model);
        }

        [HttpPost("malls")]
        public async Task<IActionResult> CreateMall(AdminMallForm form, CancellationToken cancellationToken)
        {
            try
            {
                Guid mallId = await _adminService.CreateMallAsync(form, cancellationToken);
                TempData["SuccessMessage"] = $"Mall created successfully. ID: {mallId}";
                return RedirectToAction(nameof(Malls));
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Malls));
            }
        }

        [HttpGet("stores")]
        public async Task<IActionResult> Stores([FromQuery] Guid? editStoreId, CancellationToken cancellationToken)
        {
            try
            {
                AdminStoresPageViewModel model = await _adminService.GetStoresPageAsync(editStoreId, cancellationToken);
                return View(model);
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Stores));
            }
        }

        [HttpPost("stores")]
        public async Task<IActionResult> CreateStore(AdminStoreForm form, CancellationToken cancellationToken)
        {
            try
            {
                Guid storeId = await _adminService.CreateStoreAsync(form, cancellationToken);
                TempData["SuccessMessage"] = $"Store created successfully. ID: {storeId}";
                return RedirectToAction(nameof(Stores));
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Stores));
            }
        }

        [HttpPost("stores/update")]
        public async Task<IActionResult> UpdateStore(AdminStoreForm form, CancellationToken cancellationToken)
        {
            try
            {
                await _adminService.UpdateStoreAsync(form, cancellationToken);
                TempData["SuccessMessage"] = "Store updated successfully.";
                return RedirectToAction(nameof(Stores));
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Stores), new { editStoreId = form.Id });
            }
        }

        [HttpGet("managers")]
        public async Task<IActionResult> Managers([FromQuery] Guid? editManagerId, CancellationToken cancellationToken)
        {
            try
            {
                AdminManagersPageViewModel model = await _adminService.GetManagersPageAsync(editManagerId, cancellationToken);
                return View(model);
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Managers));
            }
        }

        [HttpPost("managers")]
        public async Task<IActionResult> CreateManager(AdminManagerForm form, CancellationToken cancellationToken)
        {
            try
            {
                Guid managerId = await _adminService.CreateManagerAsync(form, cancellationToken);
                TempData["SuccessMessage"] = $"Manager created successfully. ID: {managerId}";
                return RedirectToAction(nameof(Managers));
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Managers));
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = $"Database update failed: {ex.InnerException?.Message ?? ex.Message}";
                return RedirectToAction(nameof(Managers));
            }
        }

        [HttpPost("managers/update")]
        public async Task<IActionResult> UpdateManager(AdminManagerForm form, CancellationToken cancellationToken)
        {
            try
            {
                await _adminService.UpdateManagerAsync(form, cancellationToken);
                TempData["SuccessMessage"] = "Manager updated successfully.";
                return RedirectToAction(nameof(Managers));
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Managers), new { editManagerId = form.Id });
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = $"Database update failed: {ex.InnerException?.Message ?? ex.Message}";
                return RedirectToAction(nameof(Managers), new { editManagerId = form.Id });
            }
        }

        [HttpPost("managers/delete")]
        public async Task<IActionResult> DeleteManager([FromForm] Guid managerId, CancellationToken cancellationToken)
        {
            try
            {
                await _adminService.DeleteManagerAsync(managerId, cancellationToken);
                TempData["SuccessMessage"] = "Manager deleted successfully.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = $"Database update failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return RedirectToAction(nameof(Managers));
        }
    }
}
