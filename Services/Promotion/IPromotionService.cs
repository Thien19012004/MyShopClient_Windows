using MyShopClient.Models;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Services.Promotion
{
  public interface IPromotionService
    {
     /// <summary>
        /// Get paginated list of promotions
        /// </summary>
    Task<ApiResult<PromotionPageResult>> GetPromotionsAsync(
            PromotionQueryOptions options,
CancellationToken cancellationToken = default);

  /// <summary>
   /// Get promotion detail by ID
        /// </summary>
        Task<ApiResult<PromotionDetailDto>> GetPromotionByIdAsync(
      int promotionId,
       CancellationToken cancellationToken = default);

        /// <summary>
        /// Create new promotion
        /// </summary>
        Task<ApiResult<PromotionDetailDto>> CreatePromotionAsync(
    CreatePromotionInput input,
          CancellationToken cancellationToken = default);

     /// <summary>
        /// Update existing promotion
      /// </summary>
        Task<ApiResult<PromotionDetailDto>> UpdatePromotionAsync(
          int promotionId,
            UpdatePromotionInput input,
        CancellationToken cancellationToken = default);

   /// <summary>
        /// Delete promotion
/// </summary>
        Task<ApiResult<bool>> DeletePromotionAsync(
            int promotionId,
            CancellationToken cancellationToken = default);
 }
}
