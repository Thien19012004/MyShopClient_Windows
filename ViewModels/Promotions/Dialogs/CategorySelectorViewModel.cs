using CommunityToolkit.Mvvm.ComponentModel;
using MyShopClient.Models;
using MyShopClient.Services.Category;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels.Promotions
{
    /// <summary>
    /// Reusable Category Selector dialog - shows category list with checkboxes
    /// </summary>
  public partial class CategorySelectorViewModel : ObservableObject
    {
  private readonly ICategoryService _categoryService;

        public ObservableCollection<CategoryItemDto> Categories { get; } = new();

   [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool isBusy;

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public CategorySelectorViewModel(ICategoryService categoryService)
    {
   _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        }

   public async Task LoadCategoriesAsync()
   {
     if (IsBusy) return;

       IsBusy = true;
       ErrorMessage = string.Empty;

           try
        {
     // Load all categories (no pagination needed for category selector)
  var result = await _categoryService.GetCategoriesAsync(null, 1, 1000);

     if (!result.Success || result.Data == null)
 {
 ErrorMessage = result.Message ?? "Cannot load categories.";
     Categories.Clear();
        return;
      }

 Categories.Clear();
      foreach (var c in result.Data.Items)
   {
      Categories.Add(c);
          }
     }
   catch (Exception ex)
    {
        ErrorMessage = ex.Message;
       Categories.Clear();
   }
    finally
           {
          IsBusy = false;
       }
      }
    }
}
