using MyShopClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyShopClient.Services
{
    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(string username, string password, bool remember);
        Task<bool> TryAutoLoginAsync();
        Task LogoutAsync();
        bool IsAuthenticated { get; }
        LoginUserDto? CurrentUser { get; }
    }
}
