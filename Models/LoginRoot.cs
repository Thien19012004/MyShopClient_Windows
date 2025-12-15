namespace MyShopClient.Models
{
    // Tương ứng với root "data"
    public class LoginRoot
    {
        public ApiResult<LoginUserDto>? Login { get; set; }
    }

    // Tương ứng với root GraphQL: { data: {...} }
   
}
