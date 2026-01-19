using Microsoft.UI.Xaml;
using MyShopClient.Models;
using MyShopClient.ViewModels.Promotions;
using System.Linq;

namespace MyShopClient.Converters
{

    /// Real-time sync behavior for ADD PRODUCT SELECTOR
    /// When user ticks checkbox, immediately add/remove from AddVm.SelectedProducts
 
    public static class IsSelectedChangedBehavior
    {
        public static PromotionAddViewModel GetParentViewModel(DependencyObject obj)
        {
            return (PromotionAddViewModel)obj.GetValue(ParentViewModelProperty);
        }

        public static void SetParentViewModel(DependencyObject obj, PromotionAddViewModel value)
        {
            obj.SetValue(ParentViewModelProperty, value);
        }

        public static readonly DependencyProperty ParentViewModelProperty =
            DependencyProperty.RegisterAttached(
                "ParentViewModel",
                typeof(PromotionAddViewModel),
                typeof(IsSelectedChangedBehavior),
                new PropertyMetadata(null, OnParentViewModelChanged));

        private static void OnParentViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MyShopClient.Controls.BlueCheckBox checkbox) return;
            if (e.NewValue is not PromotionAddViewModel addVm) return;

   
            checkbox.RegisterPropertyChangedCallback(
                MyShopClient.Controls.BlueCheckBox.IsCheckedProperty,
                (sender, _) =>
                {
                    if (checkbox.DataContext is ProductItemDto product)
                    {
                        if (checkbox.IsChecked)
                        {
                  
                            if (!addVm.SelectedProducts.Any(p => p.ProductId == product.ProductId))
                            {
                    
                                addVm.SelectedProducts.Add(product);
                            }
                        }
                        else
                        {
                 
                            var existing = addVm.SelectedProducts.FirstOrDefault(p => p.ProductId == product.ProductId);
                            if (existing != null)
                            {
                                addVm.SelectedProducts.Remove(existing);
                            }
                        }
                    }
                });
        }
    }



    public static class IsSelectedChangedBehaviorEdit
    {
        public static PromotionEditViewModel GetParentViewModel(DependencyObject obj)
        {
            return (PromotionEditViewModel)obj.GetValue(ParentViewModelProperty);
        }

        public static void SetParentViewModel(DependencyObject obj, PromotionEditViewModel value)
        {
            obj.SetValue(ParentViewModelProperty, value);
        }

        public static readonly DependencyProperty ParentViewModelProperty =
            DependencyProperty.RegisterAttached(
                "ParentViewModel",
                typeof(PromotionEditViewModel),
                typeof(IsSelectedChangedBehaviorEdit),
                new PropertyMetadata(null, OnParentViewModelChanged));

        private static void OnParentViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MyShopClient.Controls.BlueCheckBox checkbox) return;
            if (e.NewValue is not PromotionEditViewModel editVm) return;

          
            checkbox.RegisterPropertyChangedCallback(
                MyShopClient.Controls.BlueCheckBox.IsCheckedProperty,
                (sender, _) =>
                {
                    if (checkbox.DataContext is ProductItemDto product)
                    {
                        if (checkbox.IsChecked)
                        {
                           
                            if (!editVm.SelectedProducts.Any(p => p.ProductId == product.ProductId))
                            {
                               
                                editVm.SelectedProducts.Add(product);
                            }
                        }
                        else
                        {
                            
                            var existing = editVm.SelectedProducts.FirstOrDefault(p => p.ProductId == product.ProductId);
                            if (existing != null)
                            {
                                editVm.SelectedProducts.Remove(existing);
                            }
                        }
                    }
                });
        }
    }
}
