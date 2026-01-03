using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShopClient.Models;
using MyShopClient.ViewModels;
using System.Linq;
using System.ComponentModel;
using MyShopClient.Services.Product;
using System;

namespace MyShopClient.Views
{
    public sealed partial class OrderPage : Page
    {
        public OrderListViewModel ViewModel => (OrderListViewModel)DataContext;
        private bool _isUpdatingSelection = false;

        public OrderPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetService<OrderListViewModel>();
            
            // Subscribe to Orders collection changes
            ViewModel.Orders.CollectionChanged += Orders_CollectionChanged;
        }

        private void Orders_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // When orders are added, subscribe to their PropertyChanged
            if (e.NewItems != null)
            {
                foreach (OrderListItemDto order in e.NewItems)
                {
                    order.PropertyChanged += Order_PropertyChanged;
                }
            }

            // When orders are removed, unsubscribe
            if (e.OldItems != null)
            {
                foreach (OrderListItemDto order in e.OldItems)
                {
                    order.PropertyChanged -= Order_PropertyChanged;
                }
            }
        }

        private void Order_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isUpdatingSelection) return;

            if (e.PropertyName == nameof(OrderListItemDto.IsSelected) && sender is OrderListItemDto order)
            {
                // Don't manually update SelectedItems here; SelectableListViewModel handles that.
                // Just update the SelectAll checkbox state.
                UpdateSelectAllCheckBoxState();
            }
        }

        private void UpdateSelectAllCheckBoxState()
        {
            if (_isUpdatingSelection) return;

            if (ViewModel.Orders.Count == 0)
            {
                SelectAllCheckBox.IsChecked = false;
            }
            else if (ViewModel.Orders.All(o => o.IsSelected))
            {
                SelectAllCheckBox.IsChecked = true;
            }
            else
            {
                SelectAllCheckBox.IsChecked = false;
            }
        }

        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            _isUpdatingSelection = true;

            bool selectAll = SelectAllCheckBox.IsChecked;

            // Set IsSelected on each item; the base viewmodel will track SelectedItems
            foreach (var order in ViewModel.Orders)
            {
                order.IsSelected = selectAll;
            }

            _isUpdatingSelection = false;
        }

        // Code-behind handlers to bridge DataTemplate to ViewModel commands
        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is OrderListItemDto order)
            {
                await ViewModel.OpenEditDialogAsyncPublic(order);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is OrderListItemDto order)
            {
                ViewModel.OpenDeleteConfirmPublic(order);
            }
        }

        private void RemoveAddItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is OrderItemInputVM item)
            {
                ViewModel.AddVm.RemoveOrderItemRowCommand.Execute(item);
            }
        }

        private async void ProductAutoSuggest_TextChanged(object sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (!args.CheckCurrent()) return;
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            if (sender is not AutoSuggestBox box) return;
            await ViewModel.AddVm.SearchProductsAsync(box.Text);
        }

        private void ProductAutoSuggest_SuggestionChosen(object sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (sender is not AutoSuggestBox box) return;
            if (box.DataContext is not OrderItemInputVM item) return;
            if (args.SelectedItem is ProductItemDto p)
            {
                item.ProductId = p.ProductId;
                item.ProductName = p.Name;
                box.Text = p.Name;
            }
        }
 
        private void FromDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            ViewModel.FromDateText = args.NewDate.HasValue ? args.NewDate.Value.ToString("yyyy-MM-dd") : string.Empty;
        }

        private void ToDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            ViewModel.ToDateText = args.NewDate.HasValue ? args.NewDate.Value.ToString("yyyy-MM-dd") : string.Empty;
        }

        private void ClearDates_Click(object sender, RoutedEventArgs e)
        {
            FromDatePicker.ClearValue(CalendarDatePicker.DateProperty);
            ToDatePicker.ClearValue(CalendarDatePicker.DateProperty);
            ViewModel.FromDateText = string.Empty;
            ViewModel.ToDateText = string.Empty;
        }
    }
}
