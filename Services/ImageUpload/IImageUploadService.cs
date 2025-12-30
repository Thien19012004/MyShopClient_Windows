using MyShopClient.Models;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Services.ImageUpload
{
    public interface IImageUploadService
    {
        /// <summary>
        /// Upload m?t file ?nh lên server
        /// </summary>
   Task<ApiResult<ImageUploadResult>> UploadImageAsync(
    Stream imageStream,
     string fileName,
     CancellationToken cancellationToken = default);

      /// <summary>
        /// Xóa ?nh ?ã upload d?a vào publicId
        /// </summary>
        Task<ApiResult<bool>> DeleteImageAsync(
          string publicId,
  CancellationToken cancellationToken = default);
    }

    public class ImageUploadResult
    {
   public string Url { get; set; } = string.Empty;
      public string PublicId { get; set; } = string.Empty;
    }
}
