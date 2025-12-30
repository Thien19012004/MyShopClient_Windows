using CommunityToolkit.Mvvm.ComponentModel;

namespace MyShopClient.Models
{
    /// <summary>
    /// Model ??i di?n cho m?t ?nh trong product (dùng cho UI)
    /// </summary>
    public partial class ProductImageItem : ObservableObject
    {
        private string _url = string.Empty;
        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        private string _publicId = string.Empty;
        public string PublicId
        {
            get => _publicId;
            set => SetProperty(ref _publicId, value);
        }

        private bool _isUploading;
        public bool IsUploading
        {
            get => _isUploading;
            set => SetProperty(ref _isUploading, value);
        }

        private bool _isDeleting;
        public bool IsDeleting
        {
            get => _isDeleting;
            set => SetProperty(ref _isDeleting, value);
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    }
}
