using MyShopClient.Models;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyShopClient.Converters
{
    /// <summary>
    /// Custom JSON converter for OrderStatus enum to handle GraphQL uppercase format
    /// </summary>
    public class OrderStatusJsonConverter : JsonConverter<OrderStatus>
    {
        public override OrderStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
       var value = reader.GetString();
            return value?.ToUpperInvariant() switch
  {
     "CREATED" => OrderStatus.Created,
    "PAID" => OrderStatus.Paid,
     "CANCELLED" => OrderStatus.Cancelled,
    // Also support PascalCase for backwards compatibility
        "Created" => OrderStatus.Created,
"Paid" => OrderStatus.Paid,
        "Cancelled" => OrderStatus.Cancelled,
    _ => throw new JsonException($"Unknown OrderStatus value: {value}")
            };
        }

    public override void Write(Utf8JsonWriter writer, OrderStatus value, JsonSerializerOptions options)
        {
  // CRITICAL: GraphQL expects UPPERCASE enum values
         var stringValue = value switch
  {
        OrderStatus.Created => "CREATED",
   OrderStatus.Paid => "PAID",
         OrderStatus.Cancelled => "CANCELLED",
    _ => throw new JsonException($"Unknown OrderStatus value: {value}")
            };
    writer.WriteStringValue(stringValue);
        }
    }

    /// <summary>
    /// Custom JSON converter for nullable OrderStatus enum
    /// </summary>
    public class NullableOrderStatusJsonConverter : JsonConverter<OrderStatus?>
    {
        public override OrderStatus? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
         if (reader.TokenType == JsonTokenType.Null)
     return null;

 var value = reader.GetString();
            return value?.ToUpperInvariant() switch
            {
            "CREATED" => OrderStatus.Created,
                "PAID" => OrderStatus.Paid,
  "CANCELLED" => OrderStatus.Cancelled,
    // Also support PascalCase for backwards compatibility
  "Created" => OrderStatus.Created,
     "Paid" => OrderStatus.Paid,
                "Cancelled" => OrderStatus.Cancelled,
         null => null,
 _ => throw new JsonException($"Unknown OrderStatus value: {value}")
      };
        }

        public override void Write(Utf8JsonWriter writer, OrderStatus? value, JsonSerializerOptions options)
     {
            if (!value.HasValue)
     {
       writer.WriteNullValue();
      return;
        }

  // CRITICAL: GraphQL expects UPPERCASE enum values
     var stringValue = value.Value switch
  {
             OrderStatus.Created => "CREATED",
          OrderStatus.Paid => "PAID",
         OrderStatus.Cancelled => "CANCELLED",
    _ => throw new JsonException($"Unknown OrderStatus value: {value}")
            };
       writer.WriteStringValue(stringValue);
        }
    }
}
