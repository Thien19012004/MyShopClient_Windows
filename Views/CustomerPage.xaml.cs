using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using MyShopClient.Controls;
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
        private Grid _headerWideGrid;
        private Grid _headerNarrowGrid;
        private BlueCheckBox _selectAllCheckBoxWide;
        private BlueCheckBox _selectAllCheckBoxNarrow;

        public CustomerPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetService<CustomerListViewModel>();

            CacheControls();

            // Subscribe to Customers collection changes
            ViewModel.Customers.CollectionChanged += Customers_CollectionChanged;

            Loaded += CustomerPage_Loaded;
            SizeChanged += CustomerPage_SizeChanged;
        }

        private void CustomerPage_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLayoutState(ActualWidth);
        }

        private void CustomerPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateLayoutState(e.NewSize.Width);
        }

        private void UpdateLayoutState(double width)
        {
            bool isNarrow = width < 1100;

            _headerWideGrid.Visibility = isNarrow ? Visibility.Collapsed : Visibility.Visible;
            _headerNarrowGrid.Visibility = isNarrow ? Visibility.Visible : Visibility.Collapsed;

            CustomerListView.ItemTemplate = (DataTemplate)Resources[isNarrow ? "CustomerNarrowTemplate" : "CustomerWideTemplate"];

            // Keep select-all checkboxes in sync
            if (_selectAllCheckBoxWide != null && _selectAllCheckBoxNarrow != null)
            {
                var active = isNarrow ? _selectAllCheckBoxNarrow : _selectAllCheckBoxWide;
                var inactive = isNarrow ? _selectAllCheckBoxWide : _selectAllCheckBoxNarrow;
                inactive.IsChecked = active.IsChecked;
            }
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

            bool allSelected = ViewModel.Customers.Count > 0 && ViewModel.Customers.All(c => c.IsSelected);

            _selectAllCheckBoxWide.IsChecked = allSelected;
            _selectAllCheckBoxNarrow.IsChecked = allSelected;
        }

        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            _isUpdatingSelection = true;

            bool selectAll = (sender as ToggleButton)?.IsChecked ?? false;

            // Sync both checkboxes
            _selectAllCheckBoxWide.IsChecked = selectAll;
            _selectAllCheckBoxNarrow.IsChecked = selectAll;
            
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

        private void CacheControls()
        {
            _headerWideGrid = (Grid)FindName("HeaderWideGrid");
            _headerNarrowGrid = (Grid)FindName("HeaderNarrowGrid");
            _selectAllCheckBoxWide = (BlueCheckBox)FindName("SelectAllCheckBoxWide");
            _selectAllCheckBoxNarrow = (BlueCheckBox)FindName("SelectAllCheckBoxNarrow");
        }
    }
}
