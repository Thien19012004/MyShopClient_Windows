namespace MyShopClient.Models
{
    public class GraphQlResponse<T>
    {
        public T? Data { get; set; }
        public GraphQlError[]? Errors { get; set; }
    }

    public class GraphQlError
    {
        public string Message { get; set; } = string.Empty;
    }
}
