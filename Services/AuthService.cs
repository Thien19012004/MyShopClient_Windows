using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MyShopClient.Models;
using MyShopClient.Services.Helpers;

namespace MyShopClient.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _http;
        private readonly ISecureStorageService _secureStorage;
        private readonly IServerConfigService _configService;

        private const string CredentialKey = "login_credentials";

        public bool IsAuthenticated { get; private set; }
        public string? AccessToken { get; private set; }
        public LoginUserDto? CurrentUser { get; private set; }

        public AuthService(
            HttpClient http,
            ISecureStorageService secureStorage,
            IServerConfigService configService)
        {
            _http = http;
            _secureStorage = secureStorage;
            _configService = configService;
        }

        public async Task<LoginResult> LoginAsync(string username, string password, bool remember)
        {
            try
            {
                var baseUrl = _configService.Current.BaseUrl?.TrimEnd('/');
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    return new LoginResult(false, "Server URL is not configured.");
                }

                // Giả sử endpoint GraphQL là /graphql
                var graphqlUrl = $"{baseUrl}/graphql";

                var requestBody = new
                {
                    query = @"
mutation($username: String!, $password: String!) {
  login(input: { username: $username, password: $password }) {
    statusCode
    success
    message
    data {
      userId
      username
      fullName
      roles
      token
    }
  }
}",
                    variables = new
                    {
                        username,
                        password
                    }
                };

                var response = await _http.PostAsJsonAsync(graphqlUrl, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    return new LoginResult(false, $"HTTP error: {(int)response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var gql = GraphQlHelper.ExtractData<LoginRoot>(content);

                var loginPayload = gql?.Login;

                if (loginPayload == null)
                {
                    return new LoginResult(false, "Invalid GraphQL response.");
                }

                if (!loginPayload.Success)
                {
                    return new LoginResult(false, loginPayload.Message ?? "Login failed.");
                }

                // Lưu thông tin user + token
                CurrentUser = loginPayload.Data;
                AccessToken = loginPayload.Data?.Token;
                IsAuthenticated = true;

                if (remember)
                {
                    var plain = $"{username}::{password}";
                    await _secureStorage.SaveEncryptedAsync(CredentialKey, plain);
                }
                else
                {
                    await _secureStorage.DeleteAsync(CredentialKey);
                }

                return new LoginResult(true, null);
            }
            catch (Exception ex)
            {
                return new LoginResult(false, $"Cannot connect to server: {ex.Message}");
            }
        }

        public async Task<bool> TryAutoLoginAsync()
        {
            var plain = await _secureStorage.LoadDecryptedAsync(CredentialKey);
            if (string.IsNullOrEmpty(plain)) return false;

            var parts = plain.Split("::");
            if (parts.Length != 2) return false;

            var username = parts[0];
            var password = parts[1];

            var result = await LoginAsync(username, password, remember: true);
            return result.IsSuccess;
        }

        public async Task LogoutAsync()
        {
            IsAuthenticated = false;
            AccessToken = null;
            CurrentUser = null;
            await _secureStorage.DeleteAsync(CredentialKey);
            await Task.CompletedTask;
        }
    }
}
