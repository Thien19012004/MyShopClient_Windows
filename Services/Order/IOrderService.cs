using MyShopClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyShopClient.Services.Order
{
    public interface IOrderService
    {
        Task<ApiResult<PagedOrderResult>> GetOrdersAsync(OrderQueryOptions options);
        Task<ApiResult<OrderDetailDto>> GetOrderByIdAsync(int orderId);
        Task<ApiResult<OrderDetailDto>> CreateOrderAsync(OrderCreateInput input);
        Task<ApiResult<OrderDetailDto>> UpdateOrderAsync(int orderId, OrderUpdateInput input);
        Task<ApiResult<bool>> DeleteOrderAsync(int orderId);
    }
}
