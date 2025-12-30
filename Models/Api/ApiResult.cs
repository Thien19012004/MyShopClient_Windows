namespace MyShopClient.Models
{
 // Dùng chung cho m?i API tr? v?: statusCode, success, message, data
 public class ApiResult<T>
 {
 public int StatusCode { get; set; }
 public bool Success { get; set; }
 public string? Message { get; set; }
 public T? Data { get; set; }

 public static ApiResult<T> CreateSuccess(T data, string? message = null)
 {
 return new ApiResult<T>
 {
 StatusCode =200,
 Success = true,
 Message = message,
 Data = data
 };
 }

 public static ApiResult<T> CreateFailure(string message, int statusCode =400)
 {
 return new ApiResult<T>
 {
 StatusCode = statusCode,
 Success = false,
 Message = message,
 Data = default
 };
 }
 }
}
