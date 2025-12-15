using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MyShopClient.Models;

namespace MyShopClient.Services
{
    public interface ICategoryService
    {
        Task<ApiResult<CategoryPageDto>> GetCategoriesAsync(string? search, int page, int pageSize);
        Task<ApiResult<CategoryItemDto>> CreateCategoryAsync(CategoryCreateInput input);
        Task<ApiResult<CategoryItemDto>> UpdateCategoryAsync(int categoryId, CategoryUpdateInput input);
        Task<ApiResult<CategoryItemDto>> DeleteCategoryAsync(int categoryId);
    }

    public class CategoryService : ICategoryService
    {
        private readonly HttpClient _httpClient;
        private readonly IServerConfigService _config;
        private readonly JsonSerializerOptions _jsonOptions;

        public CategoryService(HttpClient httpClient, IServerConfigService config)
        {
            _httpClient = httpClient;
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        // --------- helper gửi GraphQL chung ----------
        private async Task<TData> SendGraphQlAsync<TData>(string query, object variables)
        {
            var payload = new { query, variables };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var endpoint = new Uri(new Uri(_config.Current.BaseUrl), _config.GraphQlEndpoint);
            var response = await _httpClient.PostAsync(endpoint, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            GraphQlResponse<TData>? graphQl;
            try
            {
                graphQl = JsonSerializer.Deserialize<GraphQlResponse<TData>>(responseJson, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot parse GraphQL response. Raw: {responseJson}", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var msg = graphQl?.Errors?.FirstOrDefault()?.Message
                          ?? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                throw new Exception(msg);
            }

            if (graphQl == null)
                throw new Exception("GraphQL response is null.");

            if (graphQl.Errors != null && graphQl.Errors.Length > 0)
                throw new Exception(graphQl.Errors[0].Message);

            if (graphQl.Data == null)
                throw new Exception("GraphQL response has no data.");


            return graphQl.Data;
        }

        // --- wrapper cho field data ---
        private class CategoriesData
        {
            public ApiResult<CategoryPageDto>? Categories { get; set; }
        }

        private class CreateCategoryData
        {
            public ApiResult<CategoryItemDto>? CreateCategory { get; set; }
        }

        private class UpdateCategoryData
        {
            public ApiResult<CategoryItemDto>? UpdateCategory { get; set; }
        }

        private class DeleteCategoryData
        {
            public ApiResult<CategoryItemDto>? DeleteCategory { get; set; }
        }

        // --------- GET CATEGORIES ----------
        public async Task<ApiResult<CategoryPageDto>> GetCategoriesAsync(string? search, int page, int pageSize)
        {
            const string query = @"
query GetCategories($page:Int!, $pageSize:Int!, $search:String) {
  categories(
    pagination: { page: $page, pageSize: $pageSize }
    search: $search
  ) {
    statusCode
    success
    message
    data {
      page
      pageSize
      totalItems
      totalPages
      items {
        categoryId
        name
        description
        productCount
      }
    }
  }
}";

            var variables = new { page, pageSize, search };

            try
            {
                var data = await SendGraphQlAsync<CategoriesData>(query, variables);
                return data.Categories ?? new ApiResult<CategoryPageDto>
                {
                    Success = false,
                    Message = "No categories field in response."
                };
            }
            catch (Exception ex)
            {
                return new ApiResult<CategoryPageDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // --------- CREATE CATEGORY ----------
        public async Task<ApiResult<CategoryItemDto>> CreateCategoryAsync(CategoryCreateInput input)
        {
            // KHÔNG dùng biến $input nữa, chỉ dùng $name, $description
            const string query = @"
mutation CreateCategory($name:String!, $description:String) {
  createCategory(
    input: {
      name: $name,
      description: $description
    }
  ) {
    statusCode
    success
    message
    data {
      categoryId
      name
      description
    }
  }
}";

            var variables = new
            {
                name = input.Name,
                description = input.Description
            };

            try
            {
                var data = await SendGraphQlAsync<CreateCategoryData>(query, variables);
                return data.CreateCategory ?? new ApiResult<CategoryItemDto>
                {
                    Success = false,
                    Message = "No createCategory field in response."
                };
            }
            catch (Exception ex)
            {
                return new ApiResult<CategoryItemDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // --------- UPDATE CATEGORY ----------
        public async Task<ApiResult<CategoryItemDto>> UpdateCategoryAsync(int categoryId, CategoryUpdateInput input)
        {
            // Tương tự: dùng $id, $name, $description
            const string query = @"
mutation UpdateCategory($id:Int!, $name:String!, $description:String) {
  updateCategory(
    categoryId: $id,
    input: {
      name: $name,
      description: $description
    }
  ) {
    statusCode
    success
    message
    data {
      categoryId
      name
      description
    }
  }
}";

            var variables = new
            {
                id = categoryId,
                name = input.Name,
                description = input.Description
            };

            try
            {
                var data = await SendGraphQlAsync<UpdateCategoryData>(query, variables);
                return data.UpdateCategory ?? new ApiResult<CategoryItemDto>
                {
                    Success = false,
                    Message = "No updateCategory field in response."
                };
            }
            catch (Exception ex)
            {
                return new ApiResult<CategoryItemDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // --------- DELETE CATEGORY ----------
        public async Task<ApiResult<CategoryItemDto>> DeleteCategoryAsync(int categoryId)
        {
            const string query = @"
mutation DeleteCategory($id:Int!) {
  deleteCategory(categoryId: $id) {
    statusCode
    success
    message
    data {
      categoryId
      name
    }
  }
}";

            var variables = new { id = categoryId };

            try
            {
                var data = await SendGraphQlAsync<DeleteCategoryData>(query, variables);
                return data.DeleteCategory ?? new ApiResult<CategoryItemDto>
                {
                    Success = false,
                    Message = "No deleteCategory field in response."
                };
            }
            catch (Exception ex)
            {
                return new ApiResult<CategoryItemDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
    }
}
