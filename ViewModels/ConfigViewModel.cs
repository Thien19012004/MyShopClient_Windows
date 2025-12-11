using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels
{
    public partial class ConfigViewModel : ObservableObject
    {
        private readonly IServerConfigService _configService;
        private readonly INavigationService _navigation;
        private readonly HttpClient _http;

        [ObservableProperty]
        private string baseUrl = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool isSuccess;

        public ConfigViewModel(
            IServerConfigService configService,
            INavigationService navigation,
            HttpClient http)
        {
            _configService = configService;
            _navigation = navigation;
            _http = http;

            // load cấu hình hiện tại
            BaseUrl = _configService.Current.BaseUrl;
        }

        partial void OnStatusMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasStatus));
        }

        public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);

        [RelayCommand]
        private async Task TestAsync()
        {
            StatusMessage = string.Empty;
            IsSuccess = false;

            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                StatusMessage = "Base URL không được để trống.";
                return;
            }

            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var baseUri))
            {
                StatusMessage = "Base URL không hợp lệ.";
                return;
            }

            IsBusy = true;

            try
            {
                // Bạn có thể đổi endpoint health này cho khớp server của bạn
                var pingUri = new Uri(baseUri, "/api/health");
                var resp = await _http.GetAsync(pingUri);

                if (resp.IsSuccessStatusCode)
                {
                    StatusMessage = "Kết nối server thành công.";
                    IsSuccess = true;
                }
                else
                {
                    StatusMessage = $"Server trả về mã lỗi {(int)resp.StatusCode}.";
                    IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Không kết nối được server: {ex.Message}";
                IsSuccess = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void Save()
        {
            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                StatusMessage = "Base URL không được để trống.";
                IsSuccess = false;
                return;
            }

            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            {
                StatusMessage = "Base URL không hợp lệ.";
                IsSuccess = false;
                return;
            }

            _configService.Save(new ServerConfig
            {
                BaseUrl = BaseUrl.Trim()
            });

            StatusMessage = "Đã lưu cấu hình. Bạn nên khởi động lại ứng dụng nếu đang dùng server khác.";
            IsSuccess = true;
        }

        [RelayCommand]
        private void Back()
        {
            _navigation.NavigateToLogin();
        }
    }
}
