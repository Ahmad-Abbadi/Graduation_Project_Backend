using Graduation_Project_Backend.DTOs.Dashboard;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Graduation_Project_Backend.Models.ViewModels
{
    public sealed class ManagerDashboardViewModel
    {
        public bool CanManageMallOperations { get; set; }
        public string MallName { get; set; } = string.Empty;
        public int StoresCount { get; set; }
        public int ManagersCount { get; set; }
        public int ActiveOffersCount { get; set; }
        public int ActiveCouponsCount { get; set; }
        public int ActiveAnnouncementsCount { get; set; }
        public DashboardSummaryResponse Summary { get; set; } = new();
        public DashboardSalesResponse Sales { get; set; } = new();
        public DashboardPointsResponse Points { get; set; } = new();
        public DashboardCouponsResponse Coupons { get; set; } = new();
        public DashboardActivityResponse Activity { get; set; } = new();
    }

    public sealed class ManagerStoresPageViewModel
    {
        public bool CanManageMallOperations { get; set; }
        public AdminStoreForm Form { get; set; } = new();
        public IReadOnlyList<AdminStoreListItem> Stores { get; set; } = [];
    }

    public sealed class ManagerStoreManagersPageViewModel
    {
        public bool CanManageMallOperations { get; set; }
        public AdminManagerForm Form { get; set; } = new();
        public bool IsEditing => Form.Id.HasValue;
        public IReadOnlyList<AdminManagerListItem> Managers { get; set; } = [];
        public IReadOnlyList<AdminStoreListItem> Stores { get; set; } = [];
    }

    public sealed class ManagerOffersPageViewModel
    {
        public bool CanManageMallOperations { get; set; }
        public ManagerOfferForm Form { get; set; } = new();
        public bool IsEditing => Form.Id.HasValue;
        public IReadOnlyList<DTOs.Offers.ManageOfferListItemResponse> Offers { get; set; } = [];
        public IReadOnlyList<SelectListItem> StoreOptions { get; set; } = [];
    }

    public sealed class ManagerCouponsPageViewModel
    {
        public bool CanManageMallOperations { get; set; }
        public ManagerCouponForm Form { get; set; } = new();
        public bool IsEditing => Form.Id.HasValue;
        public IReadOnlyList<Models.Entities.Coupon> Coupons { get; set; } = [];
    }

    public sealed class ManagerAnnouncementsPageViewModel
    {
        public bool CanManageMallOperations { get; set; }
        public ManagerAnnouncementForm Form { get; set; } = new();
        public bool IsEditing => Form.Id.HasValue;
        public IReadOnlyList<DTOs.Announcements.ManageAnnouncementListItemResponse> Announcements { get; set; } = [];
        public IReadOnlyList<SelectListItem> StoreOptions { get; set; } = [];
    }

    public sealed class ManagerOfferForm
    {
        public long? Id { get; set; }
        public Guid StoreId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset StartAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset EndAt { get; set; } = DateTimeOffset.UtcNow.AddDays(7);
        public bool IsActive { get; set; } = true;
    }

    public sealed class ManagerCouponForm
    {
        public Guid? Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset StartAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset EndAt { get; set; } = DateTimeOffset.UtcNow.AddDays(7);
        public bool IsActive { get; set; } = true;
        public decimal? CostPoint { get; set; }
    }

    public sealed class ManagerAnnouncementForm
    {
        public Guid? Id { get; set; }
        public Guid? StoreId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AnnouncementType { get; set; } = "general";
        public string Priority { get; set; } = "normal";
        public bool IsActive { get; set; } = true;
        public bool IsPinned { get; set; }
        public string? ImageUrl { get; set; }
        public DateTimeOffset StartDate { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset EndDate { get; set; } = DateTimeOffset.UtcNow.AddDays(7);
    }
}
