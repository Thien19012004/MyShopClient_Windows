using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MyShopClient.Models.Categories;
using MyShopClient.Models.Common;
using MyShopClient.Services.Helpers;

namespace MyShopClient.Services
{
    public interface ICategoryService
    {
   Task<ApiResult<CategoryPageDto>> GetCategoriesAsync(string? search, int page, int pageSize);
   Task<ApiResult<CategoryItemDto>> CreateCategoryAsync(CategoryCreateInput input);
  Task<ApiResult<CategoryItemDto>> UpdateCategoryAsync(int categoryId, CategoryUpdateInput input);
        Task<ApiResult<CategoryItemDto>> DeleteCategoryAsync(int categoryId);
    }

    /// <summary>
    /// Service for Category GraphQL operations
    /// </summary>
    public class CategoryService : GraphQlClientBase, ICategoryService
    {
        // --- wrapper cho field data ---
        private class CategoriesPayload
        {
  public ApiResult<CategoryPageDto>? Categories { get; set; }
        }

  private class CreateCategoryPayload
      {
          public ApiResult<CategoryItemDto>? CreateCategory { get; set; }
        }

        private class UpdateCategoryPayload
        {
         public ApiResult<CategoryItemDto>? UpdateCategory { get; set; }
        }

      private class DeleteCategoryPayload
        {
        public ApiResult<CategoryItemDto>? DeleteCategory { get; set; }
        }

    public CategoryService(HttpClient httpClient, IServerConfigService config)
            : base(httpClient, config)
        {
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
        var payload = await PostGraphQlAsync<CategoriesPayload>(query, variables);
      return payload?.Categories ?? new ApiResult<CategoryPageDto>
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
var payload = await PostGraphQlAsync<CreateCategoryPayload>(query, variables);
     return payload?.CreateCategory ?? new ApiResult<CategoryItemDto>
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
    var payload = await PostGraphQlAsync<UpdateCategoryPayload>(query, variables);
        return payload?.UpdateCategory ?? new ApiResult<CategoryItemDto>
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
         var payload = await PostGraphQlAsync<DeleteCategoryPayload>(query, variables);
                return payload?.DeleteCategory ?? new ApiResult<CategoryItemDto>
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
