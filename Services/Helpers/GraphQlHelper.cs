using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyShopClient.Services.Helpers
{
    /// <summary>
    /// Represents a GraphQL error
    /// </summary>
    public class GraphQlError
    {
      public string? Message { get; set; }
    }

    /// <summary>
    /// Represents a GraphQL response wrapper
    /// </summary>
    public class GraphQlResponse<T>
    {
        public T? Data { get; set; }
     public GraphQlError[]? Errors { get; set; }
    }

    /// <summary>
    /// Helper class for GraphQL operations
    /// </summary>
    public static class GraphQlHelper
    {
  public static readonly JsonSerializerOptions DefaultOptions = new()
  {
          PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Parse GraphQL response and extract data or throw exception with error details
        /// </summary>
        public static T ExtractData<T>(string jsonContent)
  {
        var response = JsonSerializer.Deserialize<GraphQlResponse<T>>(jsonContent, DefaultOptions);

     if (response == null)
          {
  throw new InvalidOperationException("Empty GraphQL response.");
      }

 if (response.Errors != null && response.Errors.Length > 0)
            {
       var msg = string.Join("; ", response.Errors.Select(e => e.Message ?? "Unknown error"));
   throw new InvalidOperationException($"GraphQL error: {msg}");
            }

   if (response.Data == null)
      {
     throw new InvalidOperationException("GraphQL response contains no data.");
       }

     return response.Data;
        }

        /// <summary>
        /// Convert string value to GraphQL string literal
     /// </summary>
        public static string ToStringLiteral(string? value)
        {
      if (string.IsNullOrWhiteSpace(value))
        return "null";

 var escaped = value
      .Replace("\\", "\\\\")
          .Replace("\"", "\\\"")
         .Replace("\n", "\\n")
           .Replace("\r", "\\r");

            return $"\"{escaped}\"";
        }

        /// <summary>
        /// Convert nullable int to GraphQL int literal
        /// </summary>
        public static string ToNullableIntLiteral(int? value)
       => value.HasValue ? value.Value.ToString() : "null";

  /// <summary>
      /// Convert nullable bool to GraphQL bool literal
        /// </summary>
   public static string ToBoolLiteral(bool value)
            => value ? "true" : "false";
    }
}
