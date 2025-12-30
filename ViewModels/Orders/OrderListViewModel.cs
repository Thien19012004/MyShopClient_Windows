using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.AppSettings;
using MyShopClient.Services.Order;
using MyShopClient.Services.PdfExport;
using MyShopClient.Services.Promotion;
using MyShopClient.ViewModels.Orders;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MyShopClient.ViewModels
{
    public class OrderItemInputVM : ObservableObject
    {
        private int _productId;
        public int ProductId { get => _productId; set => SetProperty(ref _productId, value); }

        private int _quantity;
        public int Quantity { get => _quantity; set => SetProperty(ref _quantity, value); }
    }

    // Refactored to use SelectableListViewModel for selection & bulk delete
    public partial class OrderListViewModel : SelectableListViewModel<OrderListItemDto>
    {
        private readonly IOrderService _orderService;
        private readonly IPdfExportService _pdfExportService;
        private readonly IAppSettingsService _app_settings;
        private readonly IPromotionService _promotionService;

        private Orders.OrderDeleteViewModel? _deleter;

        public ObservableCollection<OrderListItemDto> Orders { get; } = new();

        public ObservableCollection<string> StatusOptions { get; } =
            new(new[] { "All", "Created", "Paid", "Cancelled" });

        [ObservableProperty] private string selectedStatus = "All";
        [ObservableProperty] private string? customerIdText;
        [ObservableProperty] private string? saleIdText;
        [ObservableProperty] private string? fromDateText;
        [ObservableProperty] private string? toDateText;

        // Use base paging properties (CurrentPage/PageSize/TotalItems/TotalPages)

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        // summary đơn giản
        [ObservableProperty] private int totalOrdersOnPage;
        [ObservableProperty] private int totalPaidOnPage;
        [ObservableProperty] private int totalAmountOnPage;

        // ===== Selection summary (legacy names used by XAML) =====
        [ObservableProperty] private bool hasSelectedOrders;
        [ObservableProperty] private int selectedOrdersCount;

        // Single delete / print dialog state (restore properties expected by XAML)
        [ObservableProperty] private bool isDeleteConfirmOpen;
        [ObservableProperty] private OrderListItemDto? orderToDelete;
        [ObservableProperty] private string deleteConfirmMessage = string.Empty;

        [ObservableProperty] private bool isPrintConfirmOpen;
        [ObservableProperty] private string printConfirmMessage = string.Empty;

        // Bulk delete message used by UI
        [ObservableProperty] private string bulkDeleteConfirmMessage = string.Empty;

        public OrderListViewModel(IOrderService orderService, IPdfExportService pdfExportService, IAppSettingsService appSettings, IPromotionService promotionService)
            : base(appSettings, s => s.OrdersPageSize)
        {
            _orderService = orderService;
            _pdfExportService = pdfExportService;
            _app_settings = appSettings;
            _promotionService = promotionService;
            _deleter = new Orders.OrderDeleteViewModel(_orderService);

            PageSize = _app_settings.OrdersPageSize;

            // attach selection tracker for Orders collection
            AttachSelectionTracker(Orders);
            // keep legacy selection summary in sync
            SelectedItems.CollectionChanged += (s, e) => UpdateSelectionState();

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadPageAsync();
        }

        private void UpdateSelectionState()
        {
            SelectedOrdersCount = SelectedItems.Count;
            HasSelectedOrders = SelectedItems.Count >0;
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

        // ========== paging: implement core load used by base PagedListViewModel ==========";

        protected override async Task LoadPageCoreAsync(int page, int pageSize)
        {
            ErrorMessage = string.Empty;

            int? customerId = int.TryParse(CustomerIdText, out var c) ? c : null;
            int? saleId = int.TryParse(SaleIdText, out var s) ? s : null;

            DateTime? from = DateTime.TryParse(FromDateText, out var f) ? f : null;
            DateTime? to = DateTime.TryParse(ToDateText, out var t) ? t : null;

            OrderStatus? status = null;
            if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "All")
            {
                if (Enum.TryParse<OrderStatus>(SelectedStatus, true, out var parsed)) status = parsed;
            }

            var opt = new OrderQueryOptions
            {
                Page = page,
                PageSize = pageSize,
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
                    SetPageResult(1, pageSize,0,1);
                    TotalOrdersOnPage = TotalPaidOnPage = TotalAmountOnPage =0;
                    return;
                }

                var pageData = result.Data;

                Orders.Clear();
                SelectedItems.Clear();
                foreach (var o in pageData.Items) Orders.Add(o);

                SetPageResult(pageData.Page, pageData.PageSize, pageData.TotalItems, Math.Max(1, pageData.TotalPages));

                TotalOrdersOnPage = Orders.Count;
                TotalPaidOnPage = Orders.Count(o => o.Status == OrderStatus.Paid);
                TotalAmountOnPage = Orders.Sum(o => o.TotalPrice);

                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(PageSize));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(TotalPages));
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Orders.Clear();
                SetPageResult(1, pageSize,0,1);
                TotalOrdersOnPage = TotalPaidOnPage = TotalAmountOnPage =0;
            }
            finally
            {
                OnPropertyChanged(nameof(HasError));
            }
        }

        // ========= Commands =========

        [RelayCommand]
        private Task ApplyFilterAsync() => LoadPageAsync(1);

        // navigation commands are provided by base PagedListViewModel

        // ------ Delete Single Order (with confirmation) ------
        [RelayCommand]
        private void OpenDeleteConfirm(OrderListItemDto? order)
        {
            if (order == null) return;
            OrderToDelete = order;
            DeleteConfirmMessage = $"Are you sure you want to delete Order #{order.OrderId}?";
            IsDeleteConfirmOpen = true;
        }

        [RelayCommand]
        private void CancelDeleteConfirm()
        {
            IsDeleteConfirmOpen = false;
            OrderToDelete = null;
        }

        [RelayCommand]
        private async Task ConfirmDeleteOrderAsync()
        {
            if (OrderToDelete == null) return;

            IsDeleteConfirmOpen = false;

            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _deleter.DeleteAsync(OrderToDelete.OrderId);
                if (!result.Success)
                {
                    ErrorMessage = result.Message ?? "Delete order failed.";
                }
                else
                {
                    IsBusy = false;
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
                OrderToDelete = null;
            }
        }

        // ------ Bulk Delete (with confirmation) ------
        // Use base select/bulk flow; provide a helper command to set message then open base dialog
        [RelayCommand]
        private void ShowBulkDeleteDialog()
        {
            if (SelectedItems.Count ==0) return;
            var orderIds = string.Join(", ", SelectedItems.Select(o => $"#{o.OrderId}"));
            BulkDeleteConfirmMessage = $"Are you sure you want to delete {SelectedOrdersCount} order(s)?\n\nOrders: {orderIds}";
            // Open base dialog
            IsBulkDeleteConfirmOpen = true;
        }

        // Derived class hook for SelectableListViewModel bulk delete
        protected override async Task<bool> DeleteItemsAsync(OrderListItemDto[] items)
        {
            if (items == null || items.Length ==0) return false;

            try
            {
                // Use the existing bulk delete implementation on the OrderDeleteViewModel
                var ids = items.Select(i => i.OrderId).ToArray();
                var (success, failedIds) = await _deleter.BulkDeleteAsync(ids);

                if (success >0)
                {
                    // short delay to allow backend to settle and then reload current page
                    await Task.Delay(200);
                    await LoadPageAsync(CurrentPage);
                }

                if (failedIds != null && failedIds.Count >0)
                {
                    var attempted = items.Length;
                    var failed = failedIds.Count;
                    ErrorMessage = failed == attempted
                        ? $"Failed to delete any of the selected {attempted} order(s)."
                        : $"Deleted {success} order(s). Failed to delete {failed} order(s). Failed IDs: {string.Join(",", failedIds)}";
                }

                OnPropertyChanged(nameof(HasError));
                return success >0;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                OnPropertyChanged(nameof(HasError));
                return false;
            }
        }

        // ------ Print Orders (with confirmation) ------
        [RelayCommand]
        private void OpenPrintConfirm()
        {
            if (SelectedItems.Count ==0) return;

            var orderIds = string.Join(", ", SelectedItems.Select(o => $"#{o.OrderId}"));
            PrintConfirmMessage = $"Export {SelectedOrdersCount} order(s) to PDF?\n\nOrders: {orderIds}";
            IsPrintConfirmOpen = true;
        }

        [RelayCommand]
        private void CancelPrintConfirm()
        {
            IsPrintConfirmOpen = false;
        }

        [RelayCommand]
        private async Task ConfirmPrintOrdersAsync()
        {
            IsPrintConfirmOpen = false;

            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var orderDetails = new System.Collections.Generic.List<OrderDetailDto>();

                foreach (var order in SelectedItems)
                {
                    var result = await _orderService.GetOrderByIdAsync(order.OrderId);
                    if (result.Success && result.Data != null)
                    {
                        orderDetails.Add(result.Data);
                    }
                }

                if (orderDetails.Count == 0)
                {
                    ErrorMessage = "No orders to export.";
                    return;
                }

                var fileName = $"Orders_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = await _pdfExportService.ExportOrdersToPdfAsync(orderDetails, fileName);

                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Export failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(HasError));
            }
        }

        // Public wrappers so code-behind can call dialog handlers
        public async Task OpenEditDialogAsyncPublic(OrderListItemDto? order)
        {
            await OpenEditDialogAsync(order);
        }

        public void OpenDeleteConfirmPublic(OrderListItemDto? order)
        {
            OpenDeleteConfirm(order);
        }
    }
}
