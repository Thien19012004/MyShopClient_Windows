using Microsoft.UI.Xaml;
using MyShopClient.Models;
using MyShopClient.ViewModels.Promotions;
using System.Linq;

namespace MyShopClient.Converters
{
    /// <summary>
    /// Real-time sync behavior for ADD PRODUCT SELECTOR
    /// When user ticks checkbox, immediately add/remove from AddVm.SelectedProducts
    /// </summary>
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

            // Listen for IsChecked changes
            checkbox.RegisterPropertyChangedCallback(
                MyShopClient.Controls.BlueCheckBox.IsCheckedProperty,
                (sender, _) =>
                {
                    if (checkbox.DataContext is ProductItemDto product)
                    {
                        if (checkbox.IsChecked)
                        {
                            // Add to SelectedProducts if not there
                            if (!addVm.SelectedProducts.Any(p => p.ProductId == product.ProductId))
                            {
                                // Add the EXACT same object instance
                                addVm.SelectedProducts.Add(product);
                            }
                        }
                        else
                        {
                            // Remove from SelectedProducts
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

    /// <summary>
    /// Real-time sync behavior for EDIT PRODUCT SELECTOR
    /// When user ticks checkbox, immediately add/remove from EditVm.SelectedProducts
    /// </summary>
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

            // Listen for IsChecked changes
            checkbox.RegisterPropertyChangedCallback(
                MyShopClient.Controls.BlueCheckBox.IsCheckedProperty,
                (sender, _) =>
                {
                    if (checkbox.DataContext is ProductItemDto product)
                    {
                        if (checkbox.IsChecked)
                        {
                            // Add to SelectedProducts if not there
                            if (!editVm.SelectedProducts.Any(p => p.ProductId == product.ProductId))
                            {
                                // Add the EXACT same object instance
                                editVm.SelectedProducts.Add(product);
                            }
                        }
                        else
                        {
                            // Remove from SelectedProducts
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
