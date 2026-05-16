using Graduation_Project_Backend.DTOs.Dashboard;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Graduation_Project_Backend.Models.ViewModels
{
    public sealed class AdminDashboardViewModel
    {
        public int MallsCount { get; set; }
        public int StoresCount { get; set; }
        public int ManagersCount { get; set; }
        public int StoreAssignmentsCount { get; set; }
        public DashboardSummaryResponse Summary { get; set; } = new();
        public DashboardSalesResponse Sales { get; set; } = new();
        public DashboardPointsResponse Points { get; set; } = new();
        public DashboardCouponsResponse Coupons { get; set; } = new();
        public DashboardActivityResponse Activity { get; set; } = new();
    }

    public sealed class PortalDashboardReportViewModel
    {
        public bool CanManageMallOperations { get; set; } = true;
        public string ScopeTitle { get; set; } = string.Empty;
        public string Period { get; set; } = "daily";
        public string PeriodTitle { get; set; } = string.Empty;
        public DashboardSummaryResponse Summary { get; set; } = new();
        public DashboardSalesResponse Sales { get; set; } = new();
        public DashboardPointsResponse Points { get; set; } = new();
        public DashboardCouponsResponse Coupons { get; set; } = new();
        public DashboardActivityResponse Activity { get; set; } = new();
        public IReadOnlyList<DashboardChartPoint> SalesChart { get; set; } = [];
        public IReadOnlyList<DashboardChartPoint> TransactionsChart { get; set; } = [];
        public IReadOnlyList<DashboardChartPoint> PointsIssuedChart { get; set; } = [];
        public IReadOnlyList<DashboardChartPoint> PointsRedeemedChart { get; set; } = [];
    }

    public sealed class DashboardChartPoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal Percent { get; set; }
    }

    public sealed class AdminMallsPageViewModel
    {
        public AdminMallForm Form { get; set; } = new();
        public IReadOnlyList<AdminMallListItem> Malls { get; set; } = [];
    }

    public sealed class AdminStoresPageViewModel
    {
        public AdminStoreForm Form { get; set; } = new();
        public IReadOnlyList<AdminStoreListItem> Stores { get; set; } = [];
        public IReadOnlyList<SelectListItem> MallOptions { get; set; } = [];
    }

    public sealed class AdminManagersPageViewModel
    {
        public AdminManagerForm Form { get; set; } = new();
        public bool IsEditing => Form.Id.HasValue;
        public IReadOnlyList<AdminManagerListItem> Managers { get; set; } = [];
        public IReadOnlyList<SelectListItem> MallOptions { get; set; } = [];
        public IReadOnlyList<AdminStoreListItem> Stores { get; set; } = [];
    }

    public sealed class AdminMallForm
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class AdminStoreForm
    {
        public Guid? Id { get; set; }
        public Guid MallID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? OperatingHours { get; set; }
        public string? SocialMediaLinksJson { get; set; }
        public string? Description { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? FloorNumber { get; set; }
        public string? StoreImageUrl { get; set; }
    }

    public sealed class AdminManagerForm
    {
        public Guid? Id { get; set; }
        public Guid MallID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "manager";
        public List<Guid> StoreIds { get; set; } = [];
    }

    public sealed class AdminMallListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public int StoresCount { get; set; }
        public int ManagersCount { get; set; }
    }

    public sealed class AdminStoreListItem
    {
        public Guid Id { get; set; }
        public Guid MallID { get; set; }
        public string MallName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? FloorNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
    }

    public sealed class AdminManagerListItem
    {
        public Guid Id { get; set; }
        public Guid MallID { get; set; }
        public string MallName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public IReadOnlyList<string> AssignedStoreNames { get; set; } = [];
        public IReadOnlyList<Guid> AssignedStoreIds { get; set; } = [];
        public bool IsMallWideManager => AssignedStoreNames.Count == 0;
    }
}
