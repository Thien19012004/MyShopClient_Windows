using System.Text.Json.Serialization;
using MyShopClient.Converters;

namespace MyShopClient.Models
{
 // IMPORTANT: Must match server enum exactly for JSON serialization
 [JsonConverter(typeof(PromotionScopeJsonConverter))]
 public enum PromotionScope
 {
 Product =0,
 Category =1,
 Order =2
 }
}
