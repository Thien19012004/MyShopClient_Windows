using MyShopClient.Infrastructure.GraphQL;
using MyShopClient.Models;
using System;
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
                // GraphQL multipart request needs special format: operations, map, file
                using var content = new MultipartFormDataContent();
                content.Headers.Add("GraphQL-Preflight", "1");

                var operations = new
                {
                    query = ImageUploadQueries.UploadImageMutation,
                    variables = new { file = (string?)null }
                };
                content.Add(new StringContent(JsonSerializer.Serialize(operations)), "operations");

                var map = new { file = new[] { "variables.file" } };
                content.Add(new StringContent(JsonSerializer.Serialize(map)), "map");

                var streamContent = new StreamContent(imageStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(streamContent, "file", fileName);

                var data = await _gql.SendMultipartAsync<UploadImagePayload>(content, cancellationToken);
                if (data?.UploadProductAsset == null)
                {
                    return new ApiResult<ImageUploadResult> { Success = false, StatusCode =500, Message = "Invalid response from server" };
                }

                return data.UploadProductAsset;
            }
            catch (Exception ex)
            {
                return new ApiResult<ImageUploadResult> { Success = false, StatusCode =500, Message = ex.Message };
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
            var variables = new { publicId };
            var data = await _gql.SendAsync<DeleteImagePayload>(ImageUploadQueries.DeleteImageMutation, variables, cancellationToken);
            var inner = data?.DeleteUploadedAsset;
            return new ApiResult<bool> { StatusCode = inner?.StatusCode ??500, Success = inner?.Success ?? false, Message = inner?.Message, Data = inner?.Success ?? false };
        }
    }
}
