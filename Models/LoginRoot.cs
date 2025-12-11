using MyShopClient.Models.Common;

namespace MyShopClient.Models
{
    /// <summary>
    /// Root response object for login mutation
    /// Corresponds to GraphQL response data wrapper
    /// </summary>
    public class LoginRoot
    {
        public ApiResult<LoginUserDto>? Login { get; set; }
    }
}
