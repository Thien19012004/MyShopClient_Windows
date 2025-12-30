using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MyShopClient.Models;
using System.Net.Http.Headers;
using MyShopClient.Infrastructure.GraphQL;
using MyShopClient.Services.SecureStorage;

namespace MyShopClient.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _http;
        private readonly ISecureStorageService _secureStorage;
        private readonly IServerConfigService _configService;
        private readonly IGraphQLClient _gql;

        private const string CredentialKey = "login_credentials";

        public bool IsAuthenticated { get; private set; }
        public string? AccessToken { get; private set; }
        public LoginUserDto? CurrentUser { get; private set; }

        public AuthService(
            HttpClient http,
            ISecureStorageService secureStorage,
            IServerConfigService configService,
            IGraphQLClient gql)
        {
            _http = http;
            _secureStorage = secureStorage;
            _configService = configService;
            _gql = gql;
        }

        public async Task<LoginResult> LoginAsync(string username, string password, bool remember)
        {
            try
            {
                var query = AuthQueries.LoginMutation;
                var variables = new { username, password };

                // Request the payload type (LoginRoot) — GraphQLClient returns the inner `data` object (LoginRoot)
                var data = await _gql.SendAsync<LoginRoot>(query, variables);

                var loginPayload = data?.Login;

                if (loginPayload == null)
                {
                    return new LoginResult(false, "Invalid GraphQL response.");
                }

                if (!loginPayload.Success)
                {
                    return new LoginResult(false, loginPayload.Message ?? "Login failed.");
                }

                CurrentUser = loginPayload.Data;
                AccessToken = loginPayload.Data?.Token;
                IsAuthenticated = true;

                // Keep old behavior: set default header on underlying HttpClient for backward compatibility
                _http.DefaultRequestHeaders.Authorization = null;
                if (!string.IsNullOrEmpty(AccessToken))
                {
                    _http.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", AccessToken);
                }

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

            // Bỏ Authorization header khi logout
            _http.DefaultRequestHeaders.Authorization = null;

            await _secureStorage.DeleteAsync(CredentialKey);
            await Task.CompletedTask;
        }

        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                var plain = await _secureStorage.LoadDecryptedAsync(CredentialKey);
                if (string.IsNullOrEmpty(plain)) return false;

                var parts = plain.Split("::");
                if (parts.Length != 2) return false;

                var username = parts[0];
                var password = parts[1];

                var res = await LoginAsync(username, password, remember: true);
                return res.IsSuccess;
            }
            catch
            {
                return false;
            }
        }
    }
}
