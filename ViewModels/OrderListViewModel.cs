using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services;
using System;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels
{
    public class OrderItemInputVM : ObservableObject
    {
        private int _productId;
        public int ProductId { get => _productId; set => SetProperty(ref _productId, value); }

        private int _quantity;
        public int Quantity { get => _quantity; set => SetProperty(ref _quantity, value); }
    }

    public partial class OrderListViewModel : ObservableObject
    {
        private readonly IOrderService _orderService;

        public ObservableCollection<OrderListItemDto> Orders { get; } = new();
        public ObservableCollection<string> StatusOptions { get; } =
            new(new[] { "All", "CREATED", "PAID", "CANCELLED" });

        [ObservableProperty] private string selectedStatus = "All";
        [ObservableProperty] private string? customerIdText;
        [ObservableProperty] private string? saleIdText;
        [ObservableProperty] private string? fromDateText;
        [ObservableProperty] private string? toDateText;

        [ObservableProperty] private int currentPage = 1;
        [ObservableProperty] private int pageSize = 10;
        [ObservableProperty] private int totalPages = 1;
        [ObservableProperty] private int totalItems = 0;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        // summary đơn giản
        [ObservableProperty] private int totalOrdersOnPage;
        [ObservableProperty] private int totalPaidOnPage;
        [ObservableProperty] private int totalAmountOnPage;

        // ===== Overlay Add =====
        [ObservableProperty] private bool isAddDialogOpen;
        [ObservableProperty] private string? newCustomerIdText;
        [ObservableProperty] private string? newSaleIdText;
        public ObservableCollection<OrderItemInputVM> NewOrderItems { get; } = new();
        [ObservableProperty] private string? addDialogError;
        public bool HasAddDialogError => !string.IsNullOrWhiteSpace(AddDialogError);

        // ===== Overlay Edit (chỉ đổi status) =====
        [ObservableProperty] private bool isEditDialogOpen;
        [ObservableProperty] private int editOrderId;
        [ObservableProperty] private string editCustomerName = string.Empty;
        [ObservableProperty] private string editStatus = "CREATED";
        [ObservableProperty] private int editTotalPrice;
        [ObservableProperty] private string? editDialogError;
        public bool HasEditDialogError => !string.IsNullOrWhiteSpace(EditDialogError);

        public OrderListViewModel(IOrderService orderService)
        {
            _orderService = orderService;
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadPageAsync();
        }

        private static DateTime? ParseFilterDate(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            text = text.Trim();

            // Đúng format placeholder: yyyy-MM-dd
            if (DateTime.TryParseExact(
                    text,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var dt))
            {
                return dt.Date;   // chỉ lấy phần ngày
            }

            // nếu sai format thì coi như không có filter
            return null;
        }


        // ========= CORE LOAD ORDERS ========= 

        private async Task LoadPageAsync(int? page = null)
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = string.Empty;

            if (page.HasValue)
                CurrentPage = page.Value;

            int? customerId = int.TryParse(CustomerIdText, out var c) ? c : null;
            int? saleId = int.TryParse(SaleIdText, out var s) ? s : null;

            DateTime? from = DateTime.TryParse(FromDateText, out var f) ? f : null;
            DateTime? to = DateTime.TryParse(ToDateText, out var t) ? t : null;

            string? status = SelectedStatus == "All" ? null : SelectedStatus;

            var opt = new OrderQueryOptions
            {
                Page = CurrentPage,
                PageSize = PageSize,
                CustomerId = customerId,
                SaleId = saleId,
                Status = status,
                FromDate = ParseFilterDate(FromDateText),
                ToDate = ParseFilterDate(ToDateText)
            };

            try
            {
                var result = await _orderService.GetOrdersAsync(opt);
                if (!result.Success || result.Data == null)
                {
                    ErrorMessage = result.Message ?? "Cannot load orders.";
                    Orders.Clear();
                    TotalItems = 0;
                    TotalPages = 1;
                    TotalOrdersOnPage = TotalPaidOnPage = TotalAmountOnPage = 0;
                    return;
                }

                var pageData = result.Data;

                Orders.Clear();
                foreach (var o in pageData.Items)
                {
                    // nếu server không có ItemsCount thì có thể set bằng 0 hoặc tính từ items trong query
                    o.ItemsCount = o.ItemsCount == 0 && o is { } ? 0 : o.ItemsCount;
                    Orders.Add(o);
                }

                CurrentPage = pageData.Page;
                PageSize = pageData.PageSize;
                TotalItems = pageData.TotalItems;
                TotalPages = Math.Max(1, pageData.TotalPages);

                // summary
                TotalOrdersOnPage = Orders.Count;
                TotalPaidOnPage = Orders.Count(o => o.Status == "PAID");
                TotalAmountOnPage = Orders.Sum(o => o.TotalPrice);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Orders.Clear();
                TotalItems = 0;
                TotalPages = 1;
                TotalOrdersOnPage = TotalPaidOnPage = TotalAmountOnPage = 0;
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(HasError));
            }
        }

        // ========= Commands =========

        [RelayCommand]
        private Task ApplyFilterAsync() => LoadPageAsync(1);

        [RelayCommand]
        private Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
                return LoadPageAsync(CurrentPage + 1);
            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
                return LoadPageAsync(CurrentPage - 1);
            return Task.CompletedTask;
        }

        // ------ Add Order Overlay ------
        [RelayCommand]
        private void OpenAddDialog()
        {
            NewCustomerIdText = string.Empty;
            NewSaleIdText = string.Empty;
            NewOrderItems.Clear();
            NewOrderItems.Add(new OrderItemInputVM { ProductId = 1, Quantity = 1 });
            AddDialogError = string.Empty;
            IsAddDialogOpen = true;
            OnPropertyChanged(nameof(HasAddDialogError));
        }

        [RelayCommand]
        private void CancelAddDialog()
        {
            IsAddDialogOpen = false;
        }

        [RelayCommand]
        private void AddOrderItemRow()
        {
            NewOrderItems.Add(new OrderItemInputVM { ProductId = 0, Quantity = 1 });
        }

        [RelayCommand]
        private void RemoveOrderItemRow(OrderItemInputVM? row)
        {
            if (row == null) return;
            if (NewOrderItems.Count <= 1) return;
            NewOrderItems.Remove(row);
        }

        [RelayCommand]
        private async Task ConfirmAddOrderAsync()
        {
            AddDialogError = string.Empty;
            OnPropertyChanged(nameof(HasAddDialogError));

            if (!int.TryParse(NewCustomerIdText, out var customerId))
            {
                AddDialogError = "Invalid customer id.";
                OnPropertyChanged(nameof(HasAddDialogError));
                return;
            }

            if (!int.TryParse(NewSaleIdText, out var saleId))
            {
                AddDialogError = "Invalid sale id.";
                OnPropertyChanged(nameof(HasAddDialogError));
                return;
            }

            var items = NewOrderItems
                .Where(i => i.ProductId > 0 && i.Quantity > 0)
                .Select(i => new OrderItemInput { ProductId = i.ProductId, Quantity = i.Quantity })
                .ToList();

            if (items.Count == 0)
            {
                AddDialogError = "Please add at least one item.";
                OnPropertyChanged(nameof(HasAddDialogError));
                return;
            }

            var input = new OrderCreateInput
            {
                CustomerId = customerId,
                SaleId = saleId,
                Items = items
            };

            try
            {
                var result = await _orderService.CreateOrderAsync(input);
                if (!result.Success)
                {
                    AddDialogError = result.Message ?? "Create order failed.";
                    OnPropertyChanged(nameof(HasAddDialogError));
                    return;
                }

                IsAddDialogOpen = false;
                await LoadPageAsync(1); // reload
            }
            catch (Exception ex)
            {
                AddDialogError = ex.Message;
                OnPropertyChanged(nameof(HasAddDialogError));
            }
        }

        // ------ Edit Order Overlay (đổi status) ------
        [RelayCommand]
        private async Task OpenEditDialogAsync(OrderListItemDto? order)
        {
            if (order == null) return;
            EditDialogError = string.Empty;
            OnPropertyChanged(nameof(HasEditDialogError));

            var detailRes = await _orderService.GetOrderByIdAsync(order.OrderId);
            if (!detailRes.Success || detailRes.Data == null)
            {
                EditDialogError = detailRes.Message ?? "Cannot load order detail.";
                OnPropertyChanged(nameof(HasEditDialogError));
                return;
            }

            var d = detailRes.Data;
            EditOrderId = d.OrderId;
            EditCustomerName = d.CustomerName;
            EditStatus = d.Status;
            EditTotalPrice = d.TotalPrice;

            IsEditDialogOpen = true;
        }

        [RelayCommand]
        private void CancelEditDialog()
        {
            IsEditDialogOpen = false;
        }

        [RelayCommand]
        private async Task ConfirmEditOrderAsync()
        {
            EditDialogError = string.Empty;
            OnPropertyChanged(nameof(HasEditDialogError));

            var input = new OrderUpdateInput
            {
                Status = EditStatus,
                Items = null   // chỉ đổi status
            };

            try
            {
                var result = await _orderService.UpdateOrderAsync(EditOrderId, input);
                if (!result.Success)
                {
                    EditDialogError = result.Message ?? "Update order failed.";
                    OnPropertyChanged(nameof(HasEditDialogError));
                    return;
                }

                IsEditDialogOpen = false;
                await LoadPageAsync(CurrentPage);
            }
            catch (Exception ex)
            {
                EditDialogError = ex.Message;
                OnPropertyChanged(nameof(HasEditDialogError));
            }
        }

        // ------ Delete ------
        [RelayCommand]
        private async Task DeleteOrderAsync(OrderListItemDto? order)
        {
            if (order == null) return;
            if (IsBusy) return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _orderService.DeleteOrderAsync(order.OrderId);
                if (!result.Success)
                {
                    ErrorMessage = result.Message ?? "Delete order failed.";
                }
                else
                {
                    await LoadPageAsync(CurrentPage);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(HasError));
            }

            await LoadPageAsync(CurrentPage); // reload

        }
    }
}
