using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyShopClient.Models
{
 // Customer item in list (kept INotifyPropertyChanged and IsSelected to avoid breaking UI logic)
 public class CustomerListItemDto : INotifyPropertyChanged
 {
 public int CustomerId { get; set; }
 public string Name { get; set; } = string.Empty;
 public string Phone { get; set; } = string.Empty;
 public string Email { get; set; } = string.Empty;
 public string Address { get; set; } = string.Empty;
 public int OrderCount { get; set; }

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
