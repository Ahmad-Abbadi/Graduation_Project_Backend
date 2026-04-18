using Graduation_Project_Backend.DTOs;
using Graduation_Project_Backend.DTOs.Common;
using Graduation_Project_Backend.DTOs.Receipts;
using Graduation_Project_Backend.Models.Entities;

namespace Graduation_Project_Backend.Service
{
    public interface IRewardsService
    {
        Task<TransactionResultDto> ProcessTransactionAsync(
            string phoneNumber,
            Guid storeId,
            string receiptId,
            string? receiptDescription,
            decimal price);

        Task<object?> GetTransactionDetailsAsync(long transactionId);
        Task<List<Coupon>> GetCouponsAsync(bool? isActive);
        Task<object?> GetCouponDetailsAsync(Guid couponId);
        Task<UserCoupon> RedeemCouponAsync(Guid userId, Guid couponId);
        Task<UserCoupon> RedeemCouponBySerialAsync(string serialNumber);
        Task<List<object>> GetUserCouponsViewAsync(Guid userId);
        Task<int?> GetUserTotalPointsAsync(Guid userId);
        Task<PagedResult<ReceiptListItemResponse>> GetMyReceiptsAsync(Guid userId, ReceiptListQuery query, CancellationToken cancellationToken = default);
        Task<ReceiptDetailsResponse?> GetReceiptDetailsForUserAsync(Guid currentUserId, long transactionId, CancellationToken cancellationToken = default);
    }
}
