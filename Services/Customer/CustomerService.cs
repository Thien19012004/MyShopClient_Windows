using MyShopClient.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MyShopClient.Infrastructure.GraphQL;

namespace MyShopClient.Services.Customer
{
 public class CustomerService : ICustomerService
 {
 private readonly HttpClient _httpClient;
 private readonly IServerConfigService _config;
 private readonly JsonSerializerOptions _jsonOptions;
 private readonly IGraphQLClient _gql;

 public CustomerService(HttpClient httpClient, IServerConfigService config, IGraphQLClient gql)
 {
 _httpClient = httpClient;
 _config = config;
 _jsonOptions = new JsonSerializerOptions
 {
 PropertyNameCaseInsensitive = true
 };
 _gql = gql;
 }

 private class CustomersData
 {
 public ApiResult<CustomerPageDto>? Customers { get; set; }
 }

 private class CustomerByIdData
 {
 public ApiResult<CustomerDetailDto>? CustomerById { get; set; }
 }

 private class CreateCustomerData
 {
 public ApiResult<CustomerDetailDto>? CreateCustomer { get; set; }
 }

 private class UpdateCustomerData
 {
 public ApiResult<CustomerDetailDto>? UpdateCustomer { get; set; }
 }

 private class DeleteCustomerData
 {
 public ApiResult<bool>? DeleteCustomer { get; set; }
 }

 public async Task<ApiResult<CustomerPageDto>> GetCustomersAsync(CustomerQueryOptions options, CancellationToken cancellationToken = default)
 {
 var query = CustomerQueries.GetCustomersQuery;
 var variables = new { page = options.Page, pageSize = options.PageSize, search = options.Search };

 try
 {
 var data = await _gql.SendAsync<CustomersData>(query, variables, cancellationToken);
 return data?.Customers ?? new ApiResult<CustomerPageDto> { Success = false, Message = "No customers field in response." };
 }
 catch (Exception ex)
 {
 return new ApiResult<CustomerPageDto> { Success = false, Message = ex.Message };
 }
 }

 public async Task<ApiResult<CustomerDetailDto>> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default)
 {
 var query = CustomerQueries.GetCustomerByIdQuery;
 var variables = new { customerId };
 try
 {
 var data = await _gql.SendAsync<CustomerByIdData>(query, variables, cancellationToken);
 return data?.CustomerById ?? new ApiResult<CustomerDetailDto> { Success = false, Message = "No customerById field in response." };
 }
 catch (Exception ex)
 {
 return new ApiResult<CustomerDetailDto> { Success = false, Message = ex.Message };
 }
 }

 public async Task<ApiResult<CustomerDetailDto>> CreateCustomerAsync(CustomerCreateInput input, CancellationToken cancellationToken = default)
 {
 var query = CustomerQueries.CreateCustomerMutation;
 var variables = new { name = input.Name, phone = input.Phone, email = input.Email, address = input.Address };
 try
 {
 var data = await _gql.SendAsync<CreateCustomerData>(query, variables, cancellationToken);
 return data?.CreateCustomer ?? new ApiResult<CustomerDetailDto> { Success = false, Message = "No createCustomer field in response." };
 }
 catch (Exception ex)
 {
 return new ApiResult<CustomerDetailDto> { Success = false, Message = ex.Message };
 }
 }

 public async Task<ApiResult<CustomerDetailDto>> UpdateCustomerAsync(int customerId, CustomerUpdateInput input, CancellationToken cancellationToken = default)
 {
 var query = CustomerQueries.UpdateCustomerMutation;
 var variables = new { customerId, name = input.Name, phone = input.Phone, email = input.Email, address = input.Address };
 try
 {
 var data = await _gql.SendAsync<UpdateCustomerData>(query, variables, cancellationToken);
 return data?.UpdateCustomer ?? new ApiResult<CustomerDetailDto> { Success = false, Message = "No updateCustomer field in response." };
 }
 catch (Exception ex)
 {
 return new ApiResult<CustomerDetailDto> { Success = false, Message = ex.Message };
 }
 }

 public async Task<ApiResult<bool>> DeleteCustomerAsync(int customerId, CancellationToken cancellationToken = default)
 {
 var query = CustomerQueries.DeleteCustomerMutation;
 var variables = new { customerId };
 try
 {
 var data = await _gql.SendAsync<DeleteCustomerData>(query, variables, cancellationToken);
 return data?.DeleteCustomer ?? new ApiResult<bool> { Success = false, Message = "No deleteCustomer field in response." };
 }
 catch (Exception ex)
 {
 return new ApiResult<bool> { Success = false, Message = ex.Message };
 }
 }

 public async Task<ApiResult<bool>> BulkDeleteCustomersAsync(int[] customerIds, CancellationToken cancellationToken = default)
 {
 try
 {
 System.Diagnostics.Debug.WriteLine($"[BulkDelete] Starting bulk delete for {customerIds.Length} customers");
 int successCount =0;
 var errors = new System.Collections.Generic.List<string>();
 foreach (var id in customerIds)
 {
 System.Diagnostics.Debug.WriteLine($"[BulkDelete] Deleting customer ID: {id}");
 var result = await DeleteCustomerAsync(id, cancellationToken);
 if (result.Success)
 {
 successCount++;
 System.Diagnostics.Debug.WriteLine($"[BulkDelete] Successfully deleted customer ID: {id}");
 }
 else
 {
 errors.Add($"Customer ID {id}: {result.Message}");
 System.Diagnostics.Debug.WriteLine($"[BulkDelete] Failed to delete customer ID {id}: {result.Message}");
 }
 }
 System.Diagnostics.Debug.WriteLine($"[BulkDelete] Completed: {successCount}/{customerIds.Length} successful");
 if (successCount == customerIds.Length)
 {
 return new ApiResult<bool> { Success = true, StatusCode =200, Message = $"Successfully deleted all {successCount} customer{(successCount !=1 ? "s" : "") }.", Data = true };
 }
 if (successCount >0)
 {
 return new ApiResult<bool> { Success = false, StatusCode =207, Message = $"Deleted {successCount}/{customerIds.Length} customers. Errors: {string.Join("; ", errors)}", Data = false };
 }
 return new ApiResult<bool> { Success = false, StatusCode =400, Message = $"Failed to delete all customers. Errors: {string.Join("; ", errors)}", Data = false };
 }
 catch (Exception ex)
 {
 System.Diagnostics.Debug.WriteLine($"[BulkDelete] Exception: {ex.Message}");
 return new ApiResult<bool> { Success = false, StatusCode =500, Message = $"Exception during bulk delete: {ex.Message}", Data = false };
 }
 }
 }
}
