using CommunityToolkit.Mvvm.ComponentModel;
using MyShopClient.Models.Categories;
using MyShopClient.ViewModels.Common;

namespace MyShopClient.ViewModels
{
    /// <summary>
    /// Dialog state container for Category Management dialog
    /// </summary>
  public partial class CategoryDialogState : ObservableObject
    {
    [ObservableProperty] private string? dialogError;

  public bool HasError => !string.IsNullOrWhiteSpace(DialogError);

      [ObservableProperty] private string? searchText;
  [ObservableProperty] private string? nameText;
  [ObservableProperty] private string? descriptionText;
      [ObservableProperty] private CategoryItemDto? selectedItem;

      /// <summary>
      /// Reset all fields to empty state
    /// </summary>
 public void Reset()
      {
     DialogError = string.Empty;
SearchText = string.Empty;
      NameText = string.Empty;
      DescriptionText = string.Empty;
    SelectedItem = null;
     }

    /// <summary>
        /// Clear only editing fields (but keep search)
  /// </summary>
   public void ClearEditingFields()
    {
   NameText = string.Empty;
      DescriptionText = string.Empty;
  SelectedItem = null;
   DialogError = string.Empty;
      }

     /// <summary>
       /// Load from CategoryItemDto
     /// </summary>
     public void LoadFromCategory(CategoryItemDto category)
    {
    SelectedItem = category;
  NameText = category.Name;
     DescriptionText = category.Description;
   DialogError = string.Empty;
    }

    /// <summary>
   /// Validate category name field
  /// </summary>
   public bool Validate(out string? error)
   {
   return ValidationHelper.IsRequired(NameText, out error);
      }

/// <summary>
      /// Convert to CategoryCreateInput
  /// </summary>
public CategoryCreateInput ToCreateInput()
  {
     return new CategoryCreateInput
   {
   Name = NameText!,
         Description = DescriptionText
      };
   }

    /// <summary>
 /// Convert to CategoryUpdateInput
 /// </summary>
      public CategoryUpdateInput ToUpdateInput()
  {
    return new CategoryUpdateInput
 {
  Name = NameText,
  Description = DescriptionText
 };
   }
   }
}
