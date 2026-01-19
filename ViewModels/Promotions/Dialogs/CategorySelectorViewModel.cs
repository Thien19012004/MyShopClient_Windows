using CommunityToolkit.Mvvm.ComponentModel;
using MyShopClient.Models;
using MyShopClient.Services.Category;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels.Promotions
{

    /// Category Selector dialog - shows category list with checkboxes
    /// Selection state is managed by the caller (AddVm/EditVm via SelectedCategories)

    public partial class CategorySelectorViewModel : ObservableObject
    {
        private readonly ICategoryService _categoryService;

        public ObservableCollection<CategoryItemDto> Categories { get; } = new();

        [ObservableProperty] private string? errorMessage;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private int selectedCount = 0;

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
         
                    c.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CategoryItemDto.IsSelected))
                {
                    RecalculateSelectedCount();
                }
            };
                    Categories.Add(c);
                }

                RecalculateSelectedCount();
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

        public void UpdateSelectedCount(int count)
        {
            SelectedCount = count;
        }

        public void ResetSelectedCount()
        {
            SelectedCount = 0;
        }

        public void RecalculateSelectedCount()
        {
            SelectedCount = Categories.Count(c => c.IsSelected);
        }
    }
}
