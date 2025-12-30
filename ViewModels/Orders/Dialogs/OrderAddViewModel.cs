using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.Order;
using MyShopClient.Services.Promotion;
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
        private readonly Func<Task> _reloadCallback;

        public OrderAddViewModel(IOrderService orderService, IPromotionService promotionService, Func<Task> reloadCallback)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
            _reloadCallback = reloadCallback ?? throw new ArgumentNullException(nameof(reloadCallback));

            NewOrderItems = new ObservableCollection<OrderItemInputVM>();
            OrderPromotionOptions = new ObservableCollection<PromotionItemDto>();
        }

        // Dialog state + fields
        private bool _isOpen;
        public bool IsOpen { get => _isOpen; set => SetProperty(ref _isOpen, value); }

        private string? _customerIdText;
        public string? CustomerIdText { get => _customerIdText; set => SetProperty(ref _customerIdText, value); }

        private string? _saleIdText;
        public string? SaleIdText { get => _saleIdText; set => SetProperty(ref _saleIdText, value); }

        public ObservableCollection<OrderItemInputVM> NewOrderItems { get; }

        public ObservableCollection<PromotionItemDto> OrderPromotionOptions { get; }

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

        public void DoOpen()
        {
            Error = string.Empty;
            CustomerIdText = string.Empty;
            SaleIdText = string.Empty;
            NewOrderItems.Clear();
            NewOrderItems.Add(new OrderItemInputVM { ProductId = 1, Quantity = 1 });
            _ = LoadPromotionsAsync();
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

        public async Task<bool> DoConfirmAsync()
        {
            Error = string.Empty;

            if (!int.TryParse(CustomerIdText, out var customerId))
            {
                Error = "Invalid customer id.";
                return false;
            }

            if (!int.TryParse(SaleIdText, out var saleId))
            {
                Error = "Invalid sale id.";
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
                CustomerId = customerId,
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
            NewOrderItems.Add(new OrderItemInputVM { ProductId = 0, Quantity = 1 });
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
    }
}
