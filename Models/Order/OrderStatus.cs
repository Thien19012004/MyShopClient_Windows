using System.Text.Json.Serialization;
using MyShopClient.Converters;

namespace MyShopClient.Models
{
   
    [JsonConverter(typeof(OrderStatusJsonConverter))]
    public enum OrderStatus
    {
        Created = 0,
        Paid = 1,
        Cancelled = 2
    }
}
