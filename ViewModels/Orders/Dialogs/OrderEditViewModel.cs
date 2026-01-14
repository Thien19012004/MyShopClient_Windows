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
    public partial class OrderEditViewModel : ObservableObject
    {
        private readonly IOrderService _orderService;
        private readonly IPromotionService _promotionService;
        private readonly Func<Task> _reloadCallback;

        public OrderEditViewModel(IOrderService orderService, IPromotionService promotionService, Func<Task> reloadCallback)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
            _reloadCallback = reloadCallback ?? throw new ArgumentNullException(nameof(reloadCallback));
            AllPromotionOptions = new ObservableCollection<PromotionItemDto>();
            OrderPromotionOptions = new ObservableCollection<PromotionItemDto>();
            EditOrderItems = new ObservableCollection<OrderItemDto>();
        }

        public ObservableCollection<OrderItemDto> EditOrderItems { get; }
        public ObservableCollection<PromotionItemDto> AllPromotionOptions { get; }
        public ObservableCollection<PromotionItemDto> OrderPromotionOptions { get; }
        public ObservableCollection<string> OrderPromotionLines { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> ProductPromotionLines { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> CategoryPromotionLines { get; } = new ObservableCollection<string>();

        // Status options cho edit dialog (không có "All")
        public ObservableCollection<string> StatusOptions { get; } =
   new(new[] { "Created", "Paid", "Cancelled" });

        [ObservableProperty]
        private bool _isOpen;

        [ObservableProperty]
        private string? _error;

        [ObservableProperty]
        private int _editOrderId;

        [ObservableProperty]
        private string _editCustomerName = string.Empty;

        [ObservableProperty]
        private string _editStatusText = "Created";

        [ObservableProperty]
        private int _editSubtotal;

        [ObservableProperty]
        private int _editOrderDiscountAmount;

        [ObservableProperty]
        private int _editTotalPrice;

        [ObservableProperty]
        private string _editPromotionText = string.Empty;

        [ObservableProperty]
        private PromotionItemDto? _editSelectedOrderPromotion;

        [ObservableProperty]
        private bool _editIsPaid;

        [ObservableProperty]
        private bool _editOriginallyPaid;

        public async Task DoOpenAsync(OrderListItemDto item, OrderDetailDto detail)
        {
            Error = string.Empty;
            EditOrderId = detail.OrderId;
            EditCustomerName = detail.CustomerName;
            EditStatusText = detail.Status.ToString();
            EditSubtotal = detail.Subtotal;
            EditOrderDiscountAmount = detail.OrderDiscountAmount;
            EditTotalPrice = detail.TotalPrice;

            EditOrderItems.Clear();
            if (detail.Items != null)
            {
                foreach (var it in detail.Items) EditOrderItems.Add(it);
            }

            AllPromotionOptions.Clear();
            OrderPromotionOptions.Clear();
            OrderPromotionOptions.Add(new PromotionItemDto { PromotionId = 0, Name = "(No promotion)", DiscountPercent = 0, StartDate = DateTime.UtcNow.AddYears(-1), EndDate = DateTime.UtcNow.AddYears(1), Scope = PromotionScope.Order });

            // Load promotions if not loaded
            var all = await _promotionService.GetPromotionsAsync(new PromotionQueryOptions { Page = 1, PageSize = 200, OnlyActive = true, At = DateTime.UtcNow });
            if (all.Success && all.Data != null)
            {
                foreach (var p in all.Data.Items)
                {
                    AllPromotionOptions.Add(p);
                    if (p.Scope == PromotionScope.Order) OrderPromotionOptions.Add(p);
                }
            }

            OrderPromotionLines.Clear();
            ProductPromotionLines.Clear();
            CategoryPromotionLines.Clear();
            // Build promotion display text
            var promotionDetails = new List<string>();
            var orderPromos = new List<PromotionItemDto>();
            var productPromos = new List<PromotionItemDto>();
            var categoryPromos = new List<PromotionItemDto>();

            if (detail.PromotionIds != null && detail.PromotionIds.Count > 0)
            {
                foreach (var promoId in detail.PromotionIds)
                {
                    var promo = AllPromotionOptions.FirstOrDefault(p => p.PromotionId == promoId);
                    if (promo != null && promo.PromotionId > 0)
                    {
                        switch (promo.Scope)
                        {
                            case PromotionScope.Order:
                                orderPromos.Add(promo);
                                break;
                            case PromotionScope.Product:
                                productPromos.Add(promo);
                                break;
                            case PromotionScope.Category:
                                categoryPromos.Add(promo);
                                break;
                        }
                    }
                }

                // Populate line collections
                if (orderPromos.Any())
                {
                    foreach (var p in orderPromos) OrderPromotionLines.Add($"• {p.Name} (-{p.DiscountPercent}%)");
                }
                if (productPromos.Any())
                {
                    foreach (var p in productPromos) ProductPromotionLines.Add($"• {p.Name} (-{p.DiscountPercent}%)");
                }
                if (categoryPromos.Any())
                {
                    foreach (var p in categoryPromos) CategoryPromotionLines.Add($"• {p.Name} (-{p.DiscountPercent}%)");
                }

                // Keep EditPromotionText for backward compatibility
                var boxEmoji = "\U0001F4E6"; // 📦
                var labelEmoji = "\U0001F3F7\uFE0F"; // 🏷️ (label + VS16)
                var folderEmoji = "\U0001F4C1"; // 📁

                if (orderPromos.Any())
                {
                    promotionDetails.Add(boxEmoji + " Order-level:");
                    foreach (var p in orderPromos) promotionDetails.Add($" • {p.Name} (-{p.DiscountPercent}%)");
                }
                if (productPromos.Any())
                {
                    promotionDetails.Add(orderPromos.Any() ? "\n" + labelEmoji + " Product-level:" : labelEmoji + " Product-level:");
                    foreach (var p in productPromos) promotionDetails.Add($" • {p.Name} (-{p.DiscountPercent}%)");
                }
                if (categoryPromos.Any())
                {
                    promotionDetails.Add((orderPromos.Any() || productPromos.Any()) ? "\n" + folderEmoji + " Category-level:" : folderEmoji + " Category-level:");
                    foreach (var p in categoryPromos) promotionDetails.Add($" • {p.Name} (-{p.DiscountPercent}%)");
                }

                EditPromotionText = promotionDetails.Any() ? string.Join("\n", promotionDetails) : "(none)";
            }
            else
            {
                EditPromotionText = "(none)";
            }

            // map selected order-level promotion
            var existingOrderPromoId = detail.PromotionIds?.FirstOrDefault(id => AllPromotionOptions.Any(p => p.PromotionId == id && p.Scope == PromotionScope.Order)) ?? 0;
            EditSelectedOrderPromotion = OrderPromotionOptions.FirstOrDefault(p => p.PromotionId == existingOrderPromoId) ?? OrderPromotionOptions.FirstOrDefault();

            EditIsPaid = detail.Status == OrderStatus.Paid;
            EditOriginallyPaid = EditIsPaid;
            IsOpen = true;
        }

        [RelayCommand]
        private void Cancel() => IsOpen = false;

        [RelayCommand]
        private async Task Confirm()
        {
            Error = string.Empty;
            if (EditOriginallyPaid) { Error = "This order is PAID and cannot be edited."; return; }
            if (!Enum.TryParse<OrderStatus>(EditStatusText, true, out var newStatus)) { Error = "Invalid status"; return; }
            var promoId = EditSelectedOrderPromotion?.PromotionId;
            var promoIds = (promoId.HasValue && promoId.Value > 0 && EditSelectedOrderPromotion?.Scope == PromotionScope.Order)
            ? new List<int> { promoId.Value }
            : new List<int>();
            var input = new OrderUpdateInput { Status = newStatus, PromotionIds = promoIds, Items = null };
            var res = await _orderService.UpdateOrderAsync(EditOrderId, input);
            if (!res.Success) { Error = res.Message ?? "Update order failed"; return; }
            IsOpen = false;
            await _reloadCallback();
        }
    }
}
