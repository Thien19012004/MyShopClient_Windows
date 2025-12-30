using MyShopClient.Models;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Services.Customer
{
    public interface ICustomerService
    {
 Task<ApiResult<CustomerPageDto>> GetCustomersAsync(
   CustomerQueryOptions options,
            CancellationToken cancellationToken = default);

   Task<ApiResult<CustomerDetailDto>> GetCustomerByIdAsync(
    int customerId,
        CancellationToken cancellationToken = default);

        Task<ApiResult<CustomerDetailDto>> CreateCustomerAsync(
      CustomerCreateInput input,
            CancellationToken cancellationToken = default);

      Task<ApiResult<CustomerDetailDto>> UpdateCustomerAsync(
            int customerId,
      CustomerUpdateInput input,
        CancellationToken cancellationToken = default);

        Task<ApiResult<bool>> DeleteCustomerAsync(
            int customerId,
        CancellationToken cancellationToken = default);

        Task<ApiResult<bool>> BulkDeleteCustomersAsync(
            int[] customerIds,
   CancellationToken cancellationToken = default);
    }
}
