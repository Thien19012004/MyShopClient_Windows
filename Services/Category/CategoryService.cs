using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MyShopClient.Models;
using MyShopClient.Infrastructure.GraphQL;

namespace MyShopClient.Services.Category
{
  

    public class CategoryService : ICategoryService
    {
        private readonly HttpClient _httpClient;
        private readonly IServerConfigService _config;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IGraphQLClient _gql;

        public CategoryService(HttpClient httpClient, IServerConfigService config, IGraphQLClient gql)
        {
            _httpClient = httpClient;
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _gql = gql;
        }

        // --------- wrapper cho field data ----------
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
            var query = CategoryQueries.GetCategoriesQuery;
            var variables = new { page, pageSize, search };

            try
            {
                var data = await _gql.SendAsync<CategoriesData>(query, variables);
                return data?.Categories ?? new ApiResult<CategoryPageDto>
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
            var query = CategoryQueries.CreateCategoryMutation;
            var variables = new
            {
                name = input.Name,
                description = input.Description
            };
            try
            {
                var data = await _gql.SendAsync<CreateCategoryData>(query, variables);
                return data?.CreateCategory ?? new ApiResult<CategoryItemDto>
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
            var query = CategoryQueries.UpdateCategoryMutation;
            var variables = new
            {
                id = categoryId,
                name = input.Name,
                description = input.Description
            };

            try
            {
                var data = await _gql.SendAsync<UpdateCategoryData>(query, variables);
                return data?.UpdateCategory ?? new ApiResult<CategoryItemDto>
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
            var query = CategoryQueries.DeleteCategoryMutation;
            var variables = new { id = categoryId };

            try
            {
                var data = await _gql.SendAsync<DeleteCategoryData>(query, variables);
                return data?.DeleteCategory ?? new ApiResult<CategoryItemDto>
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
