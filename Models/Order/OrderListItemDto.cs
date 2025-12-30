using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyShopClient.Models
{
 public class OrderListItemDto : INotifyPropertyChanged
 {
 public int OrderId { get; set; }
 public string CustomerName { get; set; } = string.Empty;
 public string SaleName { get; set; } = string.Empty;
 public OrderStatus Status { get; set; }

 // Server only returns these fields in list
 public int TotalPrice { get; set; }
 public int OrderDiscountAmount { get; set; }
 public int ItemsCount { get; set; }
 public DateTime CreatedAt { get; set; }

 private bool _isSelected;
 public bool IsSelected
 {
 get => _isSelected;
 set
 {
 if (_isSelected != value)
 {
 _isSelected = value;
 OnPropertyChanged();
 }
 }
 }

 public event PropertyChangedEventHandler? PropertyChanged;

 protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
 {
 PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
 }
 }
}
