# H??ng D?n S? D?ng Ch?c N?ng Upload ?nh Product

## T?ng Quan

?ã thêm ch?c n?ng upload và qu?n lý ?nh cho product v?i các tính n?ng sau:

### 1. Hi?n Th? ?nh trong Product List
- Thêm c?t "Image" hi?n th? ?nh ??u tiên (avatar) c?a m?i product
- N?u product không có ?nh, s? hi?n th? placeholder

### 2. Upload ?nh trong Add Product Dialog
- **Nh?p URL tr?c ti?p**: Nh?p URL ?nh và b?m "Add URL"
- **Upload t? máy tính**: B?m nút "?? Upload" ?? ch?n file ?nh t? máy tính
- Hi?n th? danh sách các ?nh ?ã thêm v?i preview thumbnail
- M?i ?nh có nút xóa (??)

### 3. Qu?n Lý ?nh trong Edit Product Dialog
- Hi?n th? t?t c? ?nh hi?n t?i c?a product
- Có th? xóa ?nh (n?u ?nh ?ã upload lên server thì s? g?i API xóa)
- Thêm ?nh m?i b?ng URL ho?c upload t? file
- Có th? s?a (xóa ?nh c? và thêm ?nh m?i)

## Lu?ng Ho?t ??ng

### Upload ?nh
1. User ch?n file ?nh t? máy tính (jpg, jpeg, png, gif)
2. FE g?i GraphQL mutation `uploadProductAsset`
3. Server tr? v? `{ url, publicId }`
4. FE l?u c? `url` và `publicId` vào danh sách ?nh

### Xóa ?nh
1. User b?m nút xóa (??) trên m?t ?nh
2. N?u ?nh có `publicId` (?ã upload), FE g?i mutation `deleteUploadedAsset`
3. ?nh b? xóa kh?i danh sách UI

### L?u Product
- **Create Product**: FE g?i `imagePaths: [url1, url2, ...]` trong createProduct mutation
- **Update Product**: FE g?i `imagePaths: [url1, url2, ...]` trong updateProduct mutation
- Backend t? t?o các b?n ghi ProductImage d?a vào danh sách URL

## Files Thay ??i

### Models
- **ProductImageItem.cs**: Model ??i di?n cho m?t ?nh trong UI (url, publicId, isUploading, isDeleting, errorMessage)
- **ProductItemDto.cs**: ?ã có s?n property `ImagePaths` (List<string>)

### Services
- **IImageUploadService.cs**: Interface ??nh ngh?a API upload/delete ?nh
- **ImageUploadService.cs**: Implementation cho upload/delete ?nh qua GraphQL

### ViewModels
- **ProductListViewModel.cs**: 
  - Thêm `NewProductImages` và `EditProductImages` collections
  - Thêm commands: AddImageUrlToNewProduct, RemoveImageFromNewProduct, UploadImageForNewProduct...
  - S?a ConfirmAddProduct và ConfirmEditProduct ?? g?i ImagePaths

### Views
- **ProductPage.xaml**: 
  - Thêm c?t Image trong product list
  - Thêm UI qu?n lý ?nh trong Add/Edit dialogs
- **ProductPage.xaml.cs**: Thêm event handlers cho nút Upload

### Converters
- **FirstImageConverter.cs**: Convert List<string> ImagePaths thành URL ?nh ??u tiên (ho?c placeholder)

### App
- **App.xaml.cs**: ??ng ký `IImageUploadService`
- **App.xaml**: ??ng ký `FirstImageConverter`

## GraphQL Mutations

### Upload Image
```graphql
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
}
```

### Delete Image
```graphql
mutation DeleteAsset($publicId: String!) {
  deleteUploadedAsset(publicId: $publicId) {
    statusCode
    success
    message
    data
  }
}
```

## L?u Ý
- Upload ?nh s? d?ng multipart/form-data format
- Backend c?n h? tr? scalar Upload trong GraphQL
- ?nh không b?t bu?c khi t?o/s?a product
- N?u xóa ?nh fail, UI v?n xóa kh?i list (?? UX m??t mà)
