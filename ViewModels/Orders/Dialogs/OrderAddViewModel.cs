using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.Customer;
using MyShopClient.Services.Order;
using MyShopClient.Services.Promotion;
using MyShopClient.Services.Product;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels.Orders
{
    public partial class OrderAddViewModel : ObservableObject
    {
        private readonly IOrderService _orderService;
        private readonly IPromotionService _promotionService;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        private readonly Func<Task> _reloadCallback;

        public OrderAddViewModel(IOrderService orderService, IPromotionService promotionService, ICustomerService customerService, IProductService productService, Func<Task> reloadCallback)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _reloadCallback = reloadCallback ?? throw new ArgumentNullException(nameof(reloadCallback));

            NewOrderItems = new ObservableCollection<OrderItemInputVM>();
            OrderPromotionOptions = new ObservableCollection<PromotionItemDto>();
            CustomerOptions = new ObservableCollection<CustomerListItemDto>();
            SaleOptions = new ObservableCollection<CustomerListItemDto>();
            ProductSuggestions = new ObservableCollection<ProductItemDto>();
        }

        // Dialog state + fields
        private bool _isOpen;
        public bool IsOpen { get => _isOpen; set => SetProperty(ref _isOpen, value); }

        public ObservableCollection<OrderItemInputVM> NewOrderItems { get; }

        public ObservableCollection<PromotionItemDto> OrderPromotionOptions { get; }
        public ObservableCollection<ProductItemDto> ProductSuggestions { get; }

        private PromotionItemDto? _selectedOrderPromotion;
        public PromotionItemDto? SelectedOrderPromotion { get => _selectedOrderPromotion; set => SetProperty(ref _selectedOrderPromotion, value); }

        private int _newSubtotal;
        public int NewSubtotal { get => _newSubtotal; set => SetProperty(ref _newSubtotal, value); }
        private int _newOrderDiscountAmount;
        public int NewOrderDiscountAmount { get => _newOrderDiscountAmount; set => SetProperty(ref _newOrderDiscountAmount, value); }
        private int _newTotalPrice;
        public int NewTotalPrice { get => _newTotalPrice; set => SetProperty(ref _newTotalPrice, value); }

        private string? _error;
        public string? Error { get => _error; set => SetProperty(ref _error, value); }
        public bool HasError => !string.IsNullOrWhiteSpace(Error);

        public ObservableCollection<CustomerListItemDto> CustomerOptions { get; }
        public ObservableCollection<CustomerListItemDto> SaleOptions { get; }

        private CustomerListItemDto? _selectedCustomer;
        public CustomerListItemDto? SelectedCustomer { get => _selectedCustomer; set => SetProperty(ref _selectedCustomer, value); }

        private CustomerListItemDto? _selectedSale;
        public CustomerListItemDto? SelectedSale { get => _selectedSale; set => SetProperty(ref _selectedSale, value); }

        private string? _customerIdText;
        public string? CustomerIdText { get => _customerIdText; set => SetProperty(ref _customerIdText, value); }

        private string? _saleIdText;
        public string? SaleIdText { get => _saleIdText; set => SetProperty(ref _saleIdText, value); }

        public void DoOpen()
        {
            Error = string.Empty;
            SelectedCustomer = null;
            SelectedSale = null;
            NewOrderItems.Clear();
            NewOrderItems.Add(new OrderItemInputVM { ProductId = 0, ProductName = string.Empty, Quantity = 1 });
            _ = LoadPromotionsAsync();
            _ = LoadCustomersAsync();
            _ = LoadSalesListAsync();
            ProductSuggestions.Clear();
            SelectedOrderPromotion = OrderPromotionOptions.FirstOrDefault();
            UpdatePreview();
            IsOpen = true;
        }

        public void DoCancel()
        {
            IsOpen = false;
            Error = string.Empty;
        }

        private void UpdatePreview()
        {
            var subtotal = 0;
            NewSubtotal = subtotal;
            var percent = SelectedOrderPromotion?.DiscountPercent ?? 0;
            NewOrderDiscountAmount = (int)Math.Round(subtotal * (double)(percent / 100m));
            NewTotalPrice = Math.Max(0, subtotal - NewOrderDiscountAmount);
        }

        private async Task LoadPromotionsAsync()
        {
            try
            {
                var res = await _promotionService.GetPromotionsAsync(new PromotionQueryOptions { Page = 1, PageSize = 200, OnlyActive = true, At = DateTime.UtcNow, Scope = PromotionScope.Order });
                OrderPromotionOptions.Clear();
                OrderPromotionOptions.Add(new PromotionItemDto { PromotionId = 0, Name = "(No promotion)", DiscountPercent = 0, StartDate = DateTime.UtcNow.AddYears(-1), EndDate = DateTime.UtcNow.AddYears(1), Scope = PromotionScope.Order });
                if (res.Success && res.Data != null)
                {
                    foreach (var p in res.Data.Items.Where(p => p.Scope == PromotionScope.Order)) OrderPromotionOptions.Add(p);
                }
            }
            catch { }
        }

        private async Task LoadCustomersAsync()
        {
            try
            {
                var res = await _customerService.GetCustomersAsync(new CustomerQueryOptions { Page = 1, PageSize = 200 });
                CustomerOptions.Clear();

                if (res.Success && res.Data != null)
                {
                    foreach (var c in res.Data.Items)
                    {
                        CustomerOptions.Add(c);
                    }
                }
            }
            catch { }
        }

        private async Task LoadSalesListAsync()
        {
            try
            {
                SaleOptions.Clear();
                // Resolve auth and kpi services from service provider
                var auth = App.Services.GetService(typeof(Services.Auth.IAuthService)) as Services.Auth.IAuthService;
                var kpi = App.Services.GetService(typeof(Services.Kpi.IKpiService)) as Services.Kpi.IKpiService;
                var current = auth?.CurrentUser;

                bool isAdmin = current != null && current.Roles.Any(r =>
                !string.IsNullOrEmpty(r) && (
                r.IndexOf("admin", StringComparison.OrdinalIgnoreCase) >=0 ||
                r.IndexOf("mod", StringComparison.OrdinalIgnoreCase) >=0
                ));

                bool isSale = current != null && current.Roles.Any(r => r.Equals("Sale", StringComparison.OrdinalIgnoreCase));

                // If user is a Sale (non-admin), only show their own entry
                if (isSale && !isAdmin)
                {
                    SaleOptions.Add(new CustomerListItemDto { CustomerId = current!.UserId, Name = current.FullName ?? current.Username });
                    SelectedSale = SaleOptions.FirstOrDefault();
                    return;
                }

                // Admin/Moderator: load sales via KPI service
                if (kpi != null)
                {
                    var res = await kpi.GetKpiCommissionsAsync(null, null, null, 1, 200);
                    if (res.Success && res.Data != null)
                    {
                        var unique = res.Data.Items
                        .GroupBy(i => i.SaleId)
                        .Select(g => g.First())
                        .ToList();

                        foreach (var s in unique)
                        {
                            SaleOptions.Add(new CustomerListItemDto { CustomerId = s.SaleId, Name = s.SaleName });
                        }
                    }

                    var targets = await kpi.GetSaleKpiTargetsAsync(null, null, null, 1, 200);
                    if (targets.Success && targets.Data != null)
                    {
                        var existing = SaleOptions.Select(x => x.CustomerId).ToHashSet();
                        var add = targets.Data.Items.Where(t => !existing.Contains(t.SaleId)).GroupBy(t => t.SaleId).Select(g => g.First());
                        foreach (var s in add)
                            SaleOptions.Add(new CustomerListItemDto { CustomerId = s.SaleId, Name = s.SaleName });
                    }
                }

                // Additionally, if admin/moderator, fetch all users so moderators (e.g. StoreModerator) appear
                if (isAdmin)
                {
                    try
                    {
                        var gql = App.Services.GetService(typeof(MyShopClient.Infrastructure.GraphQL.IGraphQLClient)) as MyShopClient.Infrastructure.GraphQL.IGraphQLClient;
                        if (gql != null)
                        {
                            const string usersQuery = @"
query($pagination: PaginationInput, $filter: UserFilterInput) {
 users(pagination: $pagination, filter: $filter) {
 statusCode
 success
 message
 data {
 page
 pageSize
 totalItems
 totalPages
 items {
 userId
 username
 fullName
 isActive
 roles
 }
 }
 }
}";
                           
                            var variables = new { pagination = new { page =1, pageSize =500 }, filter = (object?)null };
                           
                            var usersRes = await gql.SendAsync<UsersRoot>(usersQuery, variables);
                            var usersPage = usersRes?.Users?.Data;
                            if (usersPage?.Items != null)
                            {
                                var existing = SaleOptions.Select(s => s.CustomerId).ToHashSet();
                                foreach (var u in usersPage.Items)
                                {
                                    if (u == null) continue;
                                    if (existing.Contains(u.UserId)) continue;
                                    // include moderators and sales and admins
                                    var roles = u.Roles ?? new List<string>();
                                    bool isSaleUser = roles.Any(r => r != null && r.Equals("Sale", StringComparison.OrdinalIgnoreCase));
                                    bool isModOrAdmin = roles.Any(r => !string.IsNullOrEmpty(r) && (r.IndexOf("mod", StringComparison.OrdinalIgnoreCase) >=0 || r.IndexOf("admin", StringComparison.OrdinalIgnoreCase) >=0));
                                    if (isSaleUser || isModOrAdmin)
                                    {
                                        var name = u.FullName ?? u.Username ?? string.Format("User {0}", u.UserId);
                                        SaleOptions.Add(new CustomerListItemDto { CustomerId = u.UserId, Name = name });
                                        existing.Add(u.UserId);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[OrderAdd] LoadSalesListAsync users fetch error: {ex.Message}");
                    }
                }

                // Ensure current user appears (even if admin)
                if (current != null && !SaleOptions.Any(s => s.CustomerId == current.UserId))
                {
                    SaleOptions.Insert(0, new CustomerListItemDto { CustomerId = current.UserId, Name = current.FullName ?? current.Username });
                }

                SelectedSale = SaleOptions.FirstOrDefault(s => s.CustomerId == current?.UserId) ?? SaleOptions.FirstOrDefault();
            }
            catch (Exception ex)
            {
                // swallow, but log
                System.Diagnostics.Debug.WriteLine($"[OrderAdd] LoadSalesListAsync error: {ex.Message}");
            }
        }

        public async Task<bool> DoConfirmAsync()
        {
            Error = string.Empty;

            if (SelectedCustomer == null)
            {
                Error = "Please select customer.";
                return false;
            }

            int saleId;
            if (SelectedSale != null)
            {
                saleId = SelectedSale.CustomerId;
            }
            else if (!int.TryParse(SaleIdText, out saleId) || saleId <= 0)
            {
                Error = "Please enter a valid sale id.";
                return false;
            }

            var items = NewOrderItems.Where(i => i.ProductId > 0 && i.Quantity > 0).Select(i => new OrderItemInput { ProductId = i.ProductId, Quantity = i.Quantity }).ToList();
            if (items.Count == 0)
            {
                Error = "Please add at least one item.";
                return false;
            }

            if (SelectedOrderPromotion != null && SelectedOrderPromotion.PromotionId > 0 && SelectedOrderPromotion.Scope != PromotionScope.Order)
            {
                Error = "Only ORDER-scope promotions can be applied when creating an order.";
                return false;
            }

            var promoId = SelectedOrderPromotion?.PromotionId;
            var promoIds = (promoId.HasValue && promoId.Value > 0 && SelectedOrderPromotion?.Scope == PromotionScope.Order)
            ? new List<int> { promoId.Value }
            : new List<int>();

            var input = new OrderCreateInput
            {
                CustomerId = SelectedCustomer.CustomerId,
                SaleId = saleId,
                PromotionIds = promoIds,
                Items = items
            };

            try
            {
                var result = await _orderService.CreateOrderAsync(input);
                if (!result.Success)
                {
                    Error = result.Message ?? "Create order failed.";
                    return false;
                }

                IsOpen = false;
                await _reloadCallback();
                return true;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                return false;
            }
        }

        [RelayCommand]
        private void Open() => DoOpen();
        [RelayCommand]
        private void Cancel() => DoCancel();
        [RelayCommand]
        private async Task Confirm() => await DoConfirmAsync();
        [RelayCommand]
        private void AddOrderItemRow()
        {
            NewOrderItems.Add(new OrderItemInputVM { ProductId = 0, ProductName = string.Empty, Quantity = 1 });
            UpdatePreview();
        }

        [RelayCommand]
        private void RemoveOrderItemRow(OrderItemInputVM? row)
        {
            if (row == null) return;
            if (NewOrderItems.Count <= 1) return;
            NewOrderItems.Remove(row);
            UpdatePreview();
        }

        public async Task SearchProductsAsync(string? text)
        {
            ProductSuggestions.Clear();
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                var res = await _productService.GetProductsAsync(new ProductQueryOptions
                {
                    Page = 1,
                    PageSize = 10,
                    Search = text.Trim(),
                    SortField = ProductSortField.Name,
                    SortAscending = true
                });

                if (res.Success && res.Data != null)
                {
                    foreach (var p in res.Data.Items)
                    {
                        ProductSuggestions.Add(p);
                    }
                }
            }
            catch
            {
                // swallow to avoid breaking typing
            }
        }

        // DTOs used when fetching users via GraphQL
        private class UserItem { public int UserId { get; set; } public string? Username { get; set; } public string? FullName { get; set; } public bool IsActive { get; set; } public List<string>? Roles { get; set; } }
        private class UserPage { public List<UserItem>? Items { get; set; } }
        private class UsersRoot { public ApiResult<UserPage>? Users { get; set; } }
    }
}
