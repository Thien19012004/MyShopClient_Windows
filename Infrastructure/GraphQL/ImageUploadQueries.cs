namespace MyShopClient.Infrastructure.GraphQL
{
 public static class ImageUploadQueries
 {
 public const string UploadImageMutation = @"
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

 public const string DeleteImageMutation = @"
mutation DeleteAsset($publicId: String!) {
 deleteUploadedAsset(publicId: $publicId) {
 statusCode
 success
 message
 data
 }
}";
 }
}
