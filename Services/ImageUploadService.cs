using MyShopClient.Models;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Services
{
    public class ImageUploadService : IImageUploadService
    {
        private readonly HttpClient _httpClient;
   private readonly IServerConfigService _serverConfig;

        public ImageUploadService(HttpClient httpClient, IServerConfigService serverConfig)
      {
 _httpClient = httpClient;
            _serverConfig = serverConfig;
 }

        private async Task<T?> PostGraphQlAsync<T>(string query, object? variables, CancellationToken ct)
        {
  var url = _serverConfig.GraphQlEndpoint;

 var requestBody = new
         {
            query,
      variables
   };

   using var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);
      var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
    {
       throw new Exception(
  $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {content}");
    }

            var options = new JsonSerializerOptions
            {
        PropertyNameCaseInsensitive = true
            };

            var gql = JsonSerializer.Deserialize<GraphQlResponse<T>>(content, options);
            if (gql == null)
         {
              throw new Exception("Empty GraphQL response.");
     }

     if (gql.Errors != null && gql.Errors.Length > 0)
            {
   var msg = string.Join("; ", gql.Errors.Select(e => e.Message));
                throw new Exception("GraphQL error: " + msg);
            }

 return gql.Data;
        }

  private const string UploadImageMutation = @"
mutation UploadProductAsset($file: Upload!) {
  uploadProductAsset(file: $file) {
    statusCode
    success
    message
    data {
    url
      publicId
    }
  }
}";

    private class UploadImagePayload
        {
      public ApiResult<ImageUploadResult> UploadProductAsset { get; set; } = null!;
     }

        public async Task<ApiResult<ImageUploadResult>> UploadImageAsync(
            Stream imageStream,
      string fileName,
            CancellationToken cancellationToken = default)
     {
     try
     {
     // GraphQL multipart request c?n format ??c bi?t
    using var content = new MultipartFormDataContent();
  
        // ===== IMPORTANT: Thêm GraphQL preflight header =====
     content.Headers.Add("GraphQL-Preflight", "1");
                
// Operations (GraphQL query)
    var operations = new
       {
           query = UploadImageMutation,
     variables = new
  {
            file = (string?)null
   }
 };
  content.Add(new StringContent(JsonSerializer.Serialize(operations)), "operations");

       // Map (mapping file vào variable)
         var map = new
  {
     file = new[] { "variables.file" }
    };
    content.Add(new StringContent(JsonSerializer.Serialize(map)), "map");

    // File
     var streamContent = new StreamContent(imageStream);
    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
         content.Add(streamContent, "file", fileName);

  var url = _serverConfig.GraphQlEndpoint;
   using var response = await _httpClient.PostAsync(url, content, cancellationToken);
    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

      if (!response.IsSuccessStatusCode)
      {
      return new ApiResult<ImageUploadResult>
        {
    Success = false,
          StatusCode = (int)response.StatusCode,
   Message = $"Upload failed: {responseContent}"
       };
     }

   var options = new JsonSerializerOptions
    {
    PropertyNameCaseInsensitive = true
      };

var gql = JsonSerializer.Deserialize<GraphQlResponse<UploadImagePayload>>(responseContent, options);
    if (gql?.Data?.UploadProductAsset == null)
           {
     return new ApiResult<ImageUploadResult>
   {
     Success = false,
  StatusCode = 500,
       Message = "Invalid response from server"
   };
   }

      return gql.Data.UploadProductAsset;
    }
  catch (Exception ex)
          {
     return new ApiResult<ImageUploadResult>
  {
             Success = false,
             StatusCode = 500,
  Message = ex.Message
          };
          }
 }
    private const string DeleteImageMutation = @"
mutation DeleteAsset($publicId: String!) {
  deleteUploadedAsset(publicId: $publicId) {
    statusCode
    success
    message
    data
  }
}";

        private class DeleteImagePayload
        {
    public ApiResult<object?> DeleteUploadedAsset { get; set; } = null!;
        }

     public async Task<ApiResult<bool>> DeleteImageAsync(
            string publicId,
 CancellationToken cancellationToken = default)
        {
      var variables = new { publicId };

            var data = await PostGraphQlAsync<DeleteImagePayload>(
     DeleteImageMutation,
        variables,
  cancellationToken);

            var inner = data?.DeleteUploadedAsset;

   return new ApiResult<bool>
     {
    StatusCode = inner?.StatusCode ?? 500,
      Success = inner?.Success ?? false,
        Message = inner?.Message,
                Data = inner?.Success ?? false
        };
   }
    }
}
