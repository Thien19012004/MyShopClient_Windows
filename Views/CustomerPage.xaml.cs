using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShopClient.Models;
using MyShopClient.ViewModels;
using System.ComponentModel;
using System.Linq;

namespace MyShopClient.Views
{
    public sealed partial class CustomerPage : Page
    {
        public CustomerListViewModel ViewModel => (CustomerListViewModel)DataContext;
      private bool _isUpdatingSelection = false;

        public CustomerPage()
        {
          this.InitializeComponent();
     DataContext = App.Services.GetService<CustomerListViewModel>();

  // Subscribe to Customers collection changes
        ViewModel.Customers.CollectionChanged += Customers_CollectionChanged;
        }

     private void Customers_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // When customers are added, subscribe to their PropertyChanged
         if (e.NewItems != null)
            {
  foreach (CustomerListItemDto customer in e.NewItems)
          {
  customer.PropertyChanged += Customer_PropertyChanged;
            }
 }

    // When customers are removed, unsubscribe
  if (e.OldItems != null)
  {
    foreach (CustomerListItemDto customer in e.OldItems)
            {
    customer.PropertyChanged -= Customer_PropertyChanged;
                }
            }
  }

        private void Customer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isUpdatingSelection) return;

            if (e.PropertyName == nameof(CustomerListItemDto.IsSelected) && sender is CustomerListItemDto customer)
            {
           if (customer.IsSelected)
 {
  if (!ViewModel.SelectedItems.Contains(customer))
      {
      ViewModel.SelectedItems.Add(customer);
          }
 }
         else
         {
           ViewModel.SelectedItems.Remove(customer);
      }

      // Update SelectAll checkbox state
       UpdateSelectAllCheckBoxState();
            }
        }

        private void UpdateSelectAllCheckBoxState()
{
          if (_isUpdatingSelection) return;

    if (ViewModel.Customers.Count ==0)
 {
      SelectAllCheckBox.IsChecked = false;
            }
  else if (ViewModel.Customers.All(c => c.IsSelected))
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

        // Update selected items collection
    ViewModel.SelectedItems.Clear();

      foreach (var customer in ViewModel.Customers)
       {
 customer.IsSelected = selectAll;
           if (selectAll)
                {
         ViewModel.SelectedItems.Add(customer);
       }
    }

            _isUpdatingSelection = false;
        }
    }
}
