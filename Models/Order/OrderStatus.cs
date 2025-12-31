using System.Text.Json.Serialization;
using MyShopClient.Converters;

namespace MyShopClient.Models
{
 // Match server enum exactly for JSON serialization
 [JsonConverter(typeof(OrderStatusJsonConverter))]
 public enum OrderStatus
 {
 Created =0,
 Paid =1,
 Cancelled =2
 }
}
