using Graduation_Project_Backend.Extensions;
using Graduation_Project_Backend.Filters;
using Graduation_Project_Backend.Models.ViewModels;
using Graduation_Project_Backend.Service;
using Graduation_Project_Backend.Service.Common;
using Graduation_Project_Backend.Service.Portal;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Controllers
{
    [Route("manager")]
    [PortalAuthorize(PortalConstants.ManagerRole)]
    public sealed class ManagerController : Controller
    {
        private readonly IManagerPortalService _managerPortalService;

        public ManagerController(IManagerPortalService managerPortalService)
        {
            _managerPortalService = managerPortalService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            Guid userId = HttpContext.GetCurrentUserSession().UserId;
            return View(await _managerPortalService.GetDashboardAsync(userId, cancellationToken));
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard([FromQuery] string? period, CancellationToken cancellationToken)
        {
            Guid userId = HttpContext.GetCurrentUserSession().UserId;
            return View(await _managerPortalService.GetDashboardReportAsync(userId, period, cancellationToken));
        }

        [HttpGet("stores")]
        public async Task<IActionResult> Stores(CancellationToken cancellationToken)
        {
            try
            {
                Guid userId = HttpContext.GetCurrentUserSession().UserId;
                return View(await _managerPortalService.GetStoresPageAsync(userId, cancellationToken));
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Offers));
            }
        }

        [HttpPost("stores")]
        public async Task<IActionResult> CreateStore(AdminStoreForm form, CancellationToken cancellationToken)
            => await ExecuteRedirectAsync(
                async userId =>
                {
                    Guid storeId = await _managerPortalService.CreateStoreAsync(userId, form, cancellationToken);
                    TempData["SuccessMessage"] = $"Store created successfully. ID: {storeId}";
                },
                nameof(Stores));

        [HttpGet("store-managers")]
        public async Task<IActionResult> StoreManagers([FromQuery] Guid? editManagerId, CancellationToken cancellationToken)
        {
            try
            {
                Guid userId = HttpContext.GetCurrentUserSession().UserId;
                return View(await _managerPortalService.GetStoreManagersPageAsync(userId, editManagerId, cancellationToken));
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Offers));
            }
        }

        [HttpPost("store-managers")]
        public async Task<IActionResult> CreateStoreManager(AdminManagerForm form, CancellationToken cancellationToken)
            => await ExecuteRedirectAsync(
                async userId =>
                {
                    Guid managerId = await _managerPortalService.CreateStoreManagerAsync(userId, form, cancellationToken);
                    TempData["SuccessMessage"] = $"Store manager created successfully. ID: {managerId}";
                },
                nameof(StoreManagers));

        [HttpPost("store-managers/update")]
        public async Task<IActionResult> UpdateStoreManager(AdminManagerForm form, CancellationToken cancellationToken)
            => await ExecuteRedirectAsync(
                async userId =>
                {
                    await _managerPortalService.UpdateStoreManagerAsync(userId, form, cancellationToken);
                    TempData["SuccessMessage"] = "Store manager updated successfully.";
                },
                nameof(StoreManagers));

        [HttpPost("store-managers/delete")]
        public async Task<IActionResult> DeleteStoreManager([FromForm] Guid managerId, CancellationToken cancellationToken)
            => await ExecuteRedirectAsync(
                async userId =>
                {
                    await _managerPortalService.DeleteStoreManagerAsync(userId, managerId, cancellationToken);
                    TempData["SuccessMessage"] = "Store manager deleted successfully.";
                },
                nameof(StoreManagers));

        [HttpGet("offers")]
        public async Task<IActionResult> Offers([FromQuery] long? editOfferId, CancellationToken cancellationToken)
        {
            Guid userId = HttpContext.GetCurrentUserSession().UserId;
            return View(await _managerPortalService.GetOffersPageAsync(userId, editOfferId, cancellationToken));
        }

        [HttpPost("offers")]
        public async Task<IActionResult> CreateOffer(ManagerOfferForm form, CancellationToken cancellationToken)
            => await ExecuteRedirectAsync(
                async userId =>
                {
                    await _managerPortalService.CreateOfferAsync(userId, form, cancellationToken);
                    TempData["SuccessMessage"] = "Offer created successfully.";
                },
                nameof(Offers));

        [HttpPost("offers/update")]
        public async Task<IActionResult> UpdateOffer(ManagerOfferForm form, CancellationToken cancellationToken)
            => await ExecuteRedirectAsync(
                async userId =>
                {
                    await _managerPortalService.UpdateOfferAsync(userId, form, cancellationToken);
                    TempData["SuccessMessage"] = "Offer updated successfully.";
                },
                nameof(Offers));

        [HttpPost("offers/delete")]
        public async Task<IActionResult> DeleteOffer([FromForm] long offerId, CancellationToken cancellationToken)
            => await ExecuteRedirectAsync(
                async userId =>
                {
                    await _managerPortalService.DeleteOfferAsync(userId, offerId, cancellationToken);
                    TempData["SuccessMessage"] = "Offer deleted successfully.";
                },
                nameof(Offers));

        [HttpGet("coupons")]
        public async Task<IActionResult> Coupons([FromQuery] Guid? editCouponId, CancellationToken cancellationToken)
        {
            try
            {
                Guid userId = HttpContext.GetCurrentUserSession().UserId;
                return View(await _managerPortalService.GetCouponsPageAsync(userId, editCouponId, cancellationToken));
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Offers));
            }
        }

        [HttpPost("coupons")]
        public async Task<IActionResult> CreateCoupon(ManagerCouponForm form, CancellationToken cancellationToken)
            => await ExecuteRedirectAsync(
                async userId =>
                {
                    await _managerPortalService.CreateCouponAsync(userId, form, cancellationToken);
                    TempData["SuccessMessage"] = "Coupon created successfully.";
                },
                nameof(Coupons));

        [HttpPost("coupons/update")]
        public async Task<IActionResult> UpdateCoupon(ManagerCouponForm form, CancellationToken cancellationToken)
            => await ExecuteRedirectAsync(
                async userId =>
                {
                    await _managerPortalService.UpdateCouponAsync(userId, form, cancellationToken);
                    TempData["SuccessMessage"] = "Coupon updated successfully.";
                },
                nameof(Coupons));

        [HttpPost("coupons/delete")]
        public async Task<IActionResult> DeleteCoupon([FromForm] Guid couponId, CancellationToken cancellationToken)
            => await ExecuteRedirectAsync(
                async userId =>
                {
                    await _managerPortalService.DeleteCouponAsync(userId, couponId, cancellationToken);
                    TempData["SuccessMessage"] = "Coupon deleted successfully.";
                },
                nameof(Coupons));

        [HttpGet("announcements")]
        public async Task<IActionResult> Announcements([FromQuery] Guid? editAnnouncementId, CancellationToken cancellationToken)
        {
            try
            {
                Guid userId = HttpContext.GetCurrentUserSession().UserId;
                return View(await _managerPortalService.GetAnnouncementsPageAsync(userId, editAnnouncementId, cancellationToken));
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Offers));
            }
        }

        [HttpPost("announcements")]
        public async Task<IActionResult> CreateAnnouncement(ManagerAnnouncementForm form, CancellationToken cancellationToken)
            => await ExecuteRedirectAsync(
                async userId =>
                {
                    await _managerPortalService.CreateAnnouncementAsync(userId, form, cancellationToken);
                    TempData["SuccessMessage"] = "Announcement created successfully.";
                },
                nameof(Announcements));

        [HttpPost("announcements/update")]
        public async Task<IActionResult> UpdateAnnouncement(ManagerAnnouncementForm form, CancellationToken cancellationToken)
            => await ExecuteRedirectAsync(
                async userId =>
                {
                    await _managerPortalService.UpdateAnnouncementAsync(userId, form, cancellationToken);
                    TempData["SuccessMessage"] = "Announcement updated successfully.";
                },
                nameof(Announcements));

        [HttpPost("announcements/delete")]
        public async Task<IActionResult> DeleteAnnouncement([FromForm] Guid announcementId, CancellationToken cancellationToken)
            => await ExecuteRedirectAsync(
                async userId =>
                {
                    await _managerPortalService.DeleteAnnouncementAsync(userId, announcementId, cancellationToken);
                    TempData["SuccessMessage"] = "Announcement deleted successfully.";
                },
                nameof(Announcements));

        private async Task<IActionResult> ExecuteRedirectAsync(Func<Guid, Task> action, string actionName)
        {
            try
            {
                Guid userId = HttpContext.GetCurrentUserSession().UserId;
                await action(userId);
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = $"Database update failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return RedirectToAction(actionName);
        }
    }
}
