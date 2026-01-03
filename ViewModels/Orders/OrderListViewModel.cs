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
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq.Expressions;
using MyShopClient.Services.Customer;
using System.Threading;

namespace MyShopClient.ViewModels
{
    public class OrderItemInputVM : ObservableObject
    {
        private int _productId;
        public int ProductId { get => _productId; set => SetProperty(ref _productId, value); }

        private string? _productName;
        public string? ProductName { get => _productName; set => SetProperty(ref _productName, value); }

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
        private readonly ICustomerService _customerService;
        private readonly Services.Product.IProductService _productService;

        private Orders.OrderDeleteViewModel? _deleter;
        private int _searchVersion;

        public ObservableCollection<OrderListItemDto> Orders { get; } = new();

        public ObservableCollection<string> StatusOptions { get; } =
            new(new[] { "All", "Created", "Paid", "Cancelled" });

        [ObservableProperty] private string selectedStatus = "All";
        [ObservableProperty] private string? customerNameText;
        [ObservableProperty] private string? saleNameText;
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

        public OrderListViewModel(IOrderService orderService, IPdfExportService pdfExportService, IAppSettingsService appSettings, IPromotionService promotionService, ICustomerService customerService, Services.Product.IProductService productService)
            : base(appSettings, s => s.OrdersPageSize)
        {
            _orderService = orderService;
            _pdfExportService = pdfExportService;
            _app_settings = appSettings;
            _promotionService = promotionService;
            _customerService = customerService;
            _productService = productService;
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
        
        // Normalize text for accent-insensitive search
        private static string NormalizeForSearch(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var formD = text.Normalize(NormalizationForm.FormD);
            var filtered = formD.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
            return new string(filtered.ToArray()).ToLowerInvariant();
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

            DateTime? fromDate = DateTime.TryParse(FromDateText, out var fromDt) ? fromDt.Date : null;
            DateTime? toDate = DateTime.TryParse(ToDateText, out var toDt) ? toDt.Date : null;
            // Shift selected date range to UTC when sending to API to avoid timezone day-shift
            var offset = TimeZoneInfo.Local.BaseUtcOffset;
            DateTime? apiFromDate = fromDate?.Add(-offset);
            DateTime? apiToDateExclusive = toDate?.AddDays(1).Add(-offset);

            OrderStatus? status = null;
            if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "All")
            {
                if (Enum.TryParse<OrderStatus>(SelectedStatus, true, out var parsed)) status = parsed;
            }

            bool hasNameFilter = !string.IsNullOrWhiteSpace(CustomerNameText) || !string.IsNullOrWhiteSpace(SaleNameText);
            int requestPage = hasNameFilter ? 1 : page;
            int requestPageSize = hasNameFilter ? Math.Max(pageSize, 1000) : pageSize;

            var opt = new OrderQueryOptions
            {
                Page = requestPage,
                PageSize = requestPageSize,
                CustomerId = null,
                SaleId = null,
                Status = status,
                FromDate = apiFromDate,
                ToDate = apiToDateExclusive
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

                // Local filter by customer/sale name (accent- and case-insensitive)
                var items = pageData.Items.AsEnumerable();

                // Client-side date filter in local time to avoid UTC/day-shift issues
                if (fromDate != null || toDate != null)
                {
                    items = items.Where(o =>
                    {
                        var createdDate = (o.CreatedAt.Kind == DateTimeKind.Utc
                            ? o.CreatedAt.ToLocalTime()
                            : DateTime.SpecifyKind(o.CreatedAt, DateTimeKind.Utc).ToLocalTime()).Date;
                         if (fromDate != null && createdDate < fromDate.Value) return false;
                         if (toDate != null && createdDate > toDate.Value) return false;
                         return true;
                    });
                }

                if (!string.IsNullOrWhiteSpace(CustomerNameText))
                {
                    var needleRaw = CustomerNameText.Trim();
                    var needle = NormalizeForSearch(needleRaw);
                    items = items.Where(o => !string.IsNullOrWhiteSpace(o.CustomerName) &&
                        (NormalizeForSearch(o.CustomerName).Contains(needle) || o.CustomerName.Contains(needleRaw, StringComparison.OrdinalIgnoreCase)));
                }

                if (!string.IsNullOrWhiteSpace(SaleNameText))
                {
                    var needleRaw = SaleNameText.Trim();
                    var needle = NormalizeForSearch(needleRaw);
                    items = items.Where(o => !string.IsNullOrWhiteSpace(o.SaleName) &&
                        (NormalizeForSearch(o.SaleName).Contains(needle) || o.SaleName.Contains(needleRaw, StringComparison.OrdinalIgnoreCase)));
                }

                var filtered = items.ToList();

                // Apply local paging when using name filter
                int effectivePage = hasNameFilter ? page : pageData.Page;
                int effectivePageSize = pageSize;
                var pagedFiltered = hasNameFilter
                    ? filtered.Skip((effectivePage - 1) * effectivePageSize).Take(effectivePageSize).ToList()
                    : filtered;

                Orders.Clear();
                SelectedItems.Clear();
                foreach (var o in pagedFiltered) Orders.Add(o);

                if (hasNameFilter)
                {
                    var filteredTotalPages = Math.Max(1, (int)Math.Ceiling((double)filtered.Count / effectivePageSize));
                    if (effectivePage > filteredTotalPages) effectivePage = filteredTotalPages;
                    SetPageResult(effectivePage, effectivePageSize, filtered.Count, filteredTotalPages);
                }
                else
                {
                    SetPageResult(pageData.Page, pageData.PageSize, pageData.TotalItems, Math.Max(1, pageData.TotalPages));
                }

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

        private async Task DebounceSearchAsync()
        {
            var version = Interlocked.Increment(ref _searchVersion);
            try
            {
                await Task.Delay(300);
                if (version != _searchVersion) return;
                await LoadPageAsync(1);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                OnPropertyChanged(nameof(HasError));
            }
        }

        partial void OnCustomerNameTextChanged(string value) => _ = DebounceSearchAsync();
        partial void OnSaleNameTextChanged(string value) => _ = DebounceSearchAsync();
        partial void OnFromDateTextChanged(string? value) => _ = DebounceSearchAsync();
        partial void OnToDateTextChanged(string? value) => _ = DebounceSearchAsync();
    }
}
