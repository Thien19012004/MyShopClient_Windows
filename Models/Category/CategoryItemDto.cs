using CommunityToolkit.Mvvm.ComponentModel;

namespace MyShopClient.Models
{
    public partial class CategoryItemDto : ObservableObject
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductCount { get; set; }

        // UI selection for lists
        [ObservableProperty]
        private bool isSelected;
    }
}
