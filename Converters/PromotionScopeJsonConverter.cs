using MyShopClient.Models;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyShopClient.Converters
{

    /// Custom JSON converter for PromotionScope enum to handle GraphQL uppercase format

    public class PromotionScopeJsonConverter : JsonConverter<PromotionScope>
    {
        public override PromotionScope Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return value?.ToUpperInvariant() switch
            {
                "PRODUCT" => PromotionScope.Product,
                "CATEGORY" => PromotionScope.Category,
                "ORDER" => PromotionScope.Order,
         
                "Product" => PromotionScope.Product,
                "Category" => PromotionScope.Category,
                "Order" => PromotionScope.Order,
                _ => throw new JsonException($"Unknown PromotionScope value: {value}")
            };
        }

        public override void Write(Utf8JsonWriter writer, PromotionScope value, JsonSerializerOptions options)
        {
            
            var stringValue = value switch
            {
                PromotionScope.Product => "PRODUCT",
                PromotionScope.Category => "CATEGORY",
                PromotionScope.Order => "ORDER",
                _ => throw new JsonException($"Unknown PromotionScope value: {value}")
            };
            writer.WriteStringValue(stringValue);
        }
    }


    /// Custom JSON converter for nullable PromotionScope enum

    public class NullablePromotionScopeJsonConverter : JsonConverter<PromotionScope?>
    {
        public override PromotionScope? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            var value = reader.GetString();
            return value?.ToUpperInvariant() switch
            {
                "PRODUCT" => PromotionScope.Product,
                "CATEGORY" => PromotionScope.Category,
                "ORDER" => PromotionScope.Order,
         
                "Product" => PromotionScope.Product,
                "Category" => PromotionScope.Category,
                "Order" => PromotionScope.Order,
                null => null,
                _ => throw new JsonException($"Unknown PromotionScope value: {value}")
            };
        }

        public override void Write(Utf8JsonWriter writer, PromotionScope? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

           
            var stringValue = value.Value switch
            {
                PromotionScope.Product => "PRODUCT",
                PromotionScope.Category => "CATEGORY",
                PromotionScope.Order => "ORDER",
                _ => throw new JsonException($"Unknown PromotionScope value: {value}")
            };
            writer.WriteStringValue(stringValue);
        }
    }
}
