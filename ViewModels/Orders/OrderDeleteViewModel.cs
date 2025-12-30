using MyShopClient.Models;
using MyShopClient.Services.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels.Orders
{
    public class OrderDeleteViewModel
    {
        private readonly IOrderService _orderService;
        public OrderDeleteViewModel(IOrderService orderService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        }

        public async Task<ApiResult<bool>> DeleteAsync(int orderId, CancellationToken cancellationToken = default)
        {
            // Delegates to service
            return await _orderService.DeleteOrderAsync(orderId);
        }

        public async Task<(int Success, List<int> FailedIds)> BulkDeleteAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            var list = ids?.ToArray() ?? Array.Empty<int>();
            var success = 0;
            var failed = new List<int>();

            foreach (var id in list)
            {
                if (cancellationToken.IsCancellationRequested) break;
                try
                {
                    var res = await _orderService.DeleteOrderAsync(id);
                    if (res != null && res.Success)
                        success++;
                    else
                        failed.Add(id);
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
