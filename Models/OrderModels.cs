using System;
using System.Collections.Generic;

namespace MyShopClient.Models
{
    // Item chi tiết trong order
    public class OrderItemDto
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int UnitPrice { get; set; }
        public int TotalPrice { get; set; }
    }

    // Dòng hiển thị trên list
    public class OrderListItemDto
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string SaleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalPrice { get; set; }
        public int ItemsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Chi tiết order đầy đủ
    public class OrderDetailDto
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public int SaleId { get; set; }
        public string SaleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderPageDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<OrderListItemDto> Items { get; set; } = new();
    }

    // Input tạo/sửa
    public class OrderItemInput
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderCreateInput
    {
        public int CustomerId { get; set; }
        public int SaleId { get; set; }
        public List<OrderItemInput> Items { get; set; } = new();
    }

    public class OrderUpdateInput
    {
        public string? Status { get; set; }  // CREATED / PAID / CANCELLED...
        public List<OrderItemInput>? Items { get; set; }  // để null nếu chỉ đổi status
    }

    // Options query list
    public class OrderQueryOptions
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public int? CustomerId { get; set; }
        public int? SaleId { get; set; }
        public string? Status { get; set; }   // CREATED / PAID / ...

        public DateTime? FromDate { get; set; }   // chỉ cần ngày, FE sẽ format "yyyy-MM-dd"
        public DateTime? ToDate { get; set; }
    }

    public class PagedOrderResult
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<OrderListItemDto> Items { get; set; } = new();
    }
}
