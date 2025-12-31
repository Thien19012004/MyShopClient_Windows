using MyShopClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyShopClient.Services.Category
{
    public interface ICategoryService
    {
        Task<ApiResult<CategoryPageDto>> GetCategoriesAsync(string? search, int page, int pageSize);
        Task<ApiResult<CategoryItemDto>> CreateCategoryAsync(CategoryCreateInput input);
        Task<ApiResult<CategoryItemDto>> UpdateCategoryAsync(int categoryId, CategoryUpdateInput input);
        Task<ApiResult<CategoryItemDto>> DeleteCategoryAsync(int categoryId);
    }
}
