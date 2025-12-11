using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigation;

        [ObservableProperty]
        private string username = string.Empty;

        // Password không đưa vào XAML binding trực tiếp
        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private bool rememberMe = true;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public string AppVersionText => $"Version {App.CurrentVersion}";

        public LoginViewModel(IAuthService authService, INavigationService navigation)
        {
            _authService = authService;
            _navigation = navigation;

            // Thử auto login
           
        }

        partial void OnErrorMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasError));
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter username and password.";
                return;
            }

            IsBusy = true;

            var result = await _authService.LoginAsync(Username, Password, RememberMe);

            IsBusy = false;

            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage ?? "Login failed.";
                return;
            }
            var user = _authService.CurrentUser;
            Debug.WriteLine(
                $"[LOGIN SUCCESS] userId={user?.UserId}, username={user?.Username}, fullName={user?.FullName}, roles={string.Join(",", user?.Roles ?? new())}");

            _navigation.NavigateToMainShell();
        }

        [RelayCommand]
        private void OpenConfig()
        {
            _navigation.NavigateToConfig();
        }
    }
}
