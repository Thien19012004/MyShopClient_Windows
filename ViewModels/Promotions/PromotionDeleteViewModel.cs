using MyShopClient.Models;
using MyShopClient.Services.Promotion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels.Promotions
{
    // Helper that encapsulates delete logic and supports CancellationToken
    public class PromotionDeleteViewModel
    {
        private readonly IPromotionService _promotionService;

        public PromotionDeleteViewModel(IPromotionService promotionService)
        {
            _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
        }

        public async Task<ApiResult<bool>> DeleteAsync(int promotionId, CancellationToken cancellationToken = default)
        {
            // Forward to service and allow caller to cancel
            return await _promotionService.DeletePromotionAsync(promotionId, cancellationToken);
        }

        public async Task<(int Success, List<int> FailedIds)> BulkDeleteAsync(IEnumerable<int> promotionIds, CancellationToken cancellationToken = default)
        {
            var ids = promotionIds?.ToArray() ?? Array.Empty<int>();
            var success = 0;
            var failed = new List<int>();

            foreach (var id in ids)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    var res = await _promotionService.DeletePromotionAsync(id, cancellationToken);
                    if (res != null && res.Success && (res.Data == null || res.Data == true))
                    {
                        success++;
                    }
                    else
                    {
                        failed.Add(id);
                    }
                }
                catch
                {
                    failed.Add(id);
                }
            }

            return (success, failed);
        }
    }
}
