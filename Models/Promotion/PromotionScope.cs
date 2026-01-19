using System.Text.Json.Serialization;
using MyShopClient.Converters;

namespace MyShopClient.Models
{
    
    [JsonConverter(typeof(PromotionScopeJsonConverter))]
    public enum PromotionScope
    {
        Product = 0,
        Category = 1,
        Order = 2
    }
}
