namespace MyShopClient.Models
{
    // Corresponds to GraphQL root: { data: { login: { ... } } }
    public class LoginRoot
    {
        public ApiResult<LoginUserDto>? Login { get; set; }
    }
}
