using MyShopClient.Infrastructure.GraphQL;
using MyShopClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Services.ImageUpload
{
    public class ImageUploadService : IImageUploadService
    {
        private readonly IGraphQLClient _gql;
        
      // Tăng buffer size cho streaming upload (256KB cho tốc độ tốt hơn)
      private const int BufferSize = 262144; // 256KB
        
     // Mapping content-type dựa vào file extension
        private static readonly Dictionary<string, string> ContentTypeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", "image/jpeg" },
     { ".jpeg", "image/jpeg" },
          { ".png", "image/png" },
            { ".gif", "image/gif" },
   { ".webp", "image/webp" },
{ ".bmp", "image/bmp" }
        };

        public ImageUploadService(IGraphQLClient gql)
     {
    _gql = gql;
      }

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
        // Validate input
     if (imageStream == null || !imageStream.CanRead)
        {
      return new ApiResult<ImageUploadResult> 
  { 
                 Success = false, 
                 StatusCode = 400, 
         Message = "Invalid image stream" 
            };
           }

      // GraphQL multipart request needs special format: operations, map, file
                using var content = new MultipartFormDataContent();
        content.Headers.Add("GraphQL-Preflight", "1");

  // Serialize operations once
    var operations = JsonSerializer.Serialize(new
     {
   query = ImageUploadQueries.UploadImageMutation,
             variables = new { file = (string?)null }
  });
                content.Add(new StringContent(operations), "operations");

      // Serialize map once
    var map = JsonSerializer.Serialize(new { file = new[] { "variables.file" } });
   content.Add(new StringContent(map), "map");

   // Detect content type từ file extension
     var contentType = GetContentType(fileName);

       // Tối ưu hóa streaming: dùng larger buffer size
       var streamContent = new StreamContent(imageStream, BufferSize);
    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
           
                // Thêm header để gợi ý server nén (nếu hỗ trợ)
       // streamContent.Headers.ContentEncoding.Add("gzip");
    
         content.Add(streamContent, "file", fileName);

             var data = await _gql.SendMultipartAsync<UploadImagePayload>(content, cancellationToken);
    if (data?.UploadProductAsset == null)
                {
             return new ApiResult<ImageUploadResult> 
             { 
            Success = false, 
    StatusCode = 500, 
     Message = "Invalid response from server" 
       };
     }

       return data.UploadProductAsset;
            }
            catch (OperationCanceledException)
        {
       return new ApiResult<ImageUploadResult> 
        { 
         Success = false, 
         StatusCode = 408, 
        Message = "Upload timeout" 
    };
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

        private class DeleteImagePayload
        {
            public ApiResult<object?> DeleteUploadedAsset { get; set; } = null!;
    }

        public async Task<ApiResult<bool>> DeleteImageAsync(
      string publicId,
       CancellationToken cancellationToken = default)
        {
            try
            {
      var variables = new { publicId };
     var data = await _gql.SendAsync<DeleteImagePayload>(
      ImageUploadQueries.DeleteImageMutation, 
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
    catch (OperationCanceledException)
            {
      return new ApiResult<bool> 
        { 
       Success = false, 
         StatusCode = 408, 
   Message = "Delete timeout" 
     };
        }
         catch (Exception ex)
   {
          return new ApiResult<bool> 
      { 
            Success = false, 
    StatusCode = 500, 
        Message = ex.Message 
         };
            }
     }

   /// <summary>
        /// Xác định content-type dựa trên file extension
     /// </summary>
        private static string GetContentType(string fileName)
  {
        if (string.IsNullOrWhiteSpace(fileName))
     return "application/octet-stream";

    var extension = Path.GetExtension(fileName);
 return ContentTypeMap.TryGetValue(extension, out var contentType) 
      ? contentType 
 : "application/octet-stream";
        }
    }
}
