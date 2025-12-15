# Debug H??ng D?n - Upload ?nh Không Hi?n Th?

## Tri?u Ch?ng
- Click upload ?nh ? Ch?n file ? Không có gì hi?n th?
- Ho?c hi?n th? nh?ng ?nh tr?ng/không load
- Khi check l?i product thì không có ?nh

## Các B??c Debug

### 1. Ki?m Tra Output Window Logs
**Khi app ?ang ch?y** (F5), sau khi click upload:

1. M? **View ? Output** (Ctrl+Alt+O)
2. Ch?n dropdown "Show output from:" ? **Debug**
3. Tìm các dòng log b?t ??u b?ng `[Upload]`

**Các log c?n ki?m tra:**
```
[Upload] Starting upload for abc.jpg
[Upload] Result - Success: True/False, Message: ...
[Upload] Image uploaded successfully. URL: ..., PublicId: ...
```

ho?c n?u có l?i:
```
[Upload] Upload failed: ...
[Upload] Exception: ...
```

### 2. Các L?i Th??ng G?p

#### A. Upload Mutation Không Thành Công
**Log:**
```
[Upload] Result - Success: False, Message: GraphQL error: ...
```

**Nguyên nhân:**
- Backend không nh?n ???c file ?úng format
- Mutation uploadProductAsset không t?n t?i ho?c sai tên
- Server config sai (BaseUrl không ?úng)

**Gi?i pháp:**
1. Ki?m tra GraphQL endpoint: `http://localhost:5135/graphql`
2. Test mutation tr?c ti?p trên GraphQL playground
3. Ki?m tra server logs

#### B. Multipart Upload Format Sai
**Log:**
```
[Upload] Exception: ...
[Upload] Stack trace: ...
```

**V?n ?? trong ImageUploadService.cs:**
```csharp
// Operations
var operations = new
{
    query = UploadImageMutation,
    variables = new { file = (string?)null }
};
content.Add(new StringContent(JsonSerializer.Serialize(operations)), "operations");

// Map
var map = new { file = new[] { "variables.file" } };
content.Add(new StringContent(JsonSerializer.Serialize(map)), "map");

// File
var streamContent = new StreamContent(imageStream);
streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
content.Add(streamContent, "file", fileName);
```

**Ki?m tra:**
- Tên field trong map ph?i kh?p: `"variables.file"`
- Content-Type ph?i ?úng: `image/jpeg`, `image/png`

#### C. URL Không H?p L?
**Log:**
```
[Upload] Image uploaded successfully. URL: null, PublicId: ...
```

**Nguyên nhân:**
- Backend tr? v? response không ?úng format
- Deserialize JSON failed

**Gi?i pháp:**
Ki?m tra response structure:
```json
{
  "data": {
    "uploadProductAsset": {
      "statusCode": 200,
      "success": true,
      "data": {
        "url": "https://...",
   "publicId": "..."
  }
    }
  }
}
```

### 3. Ki?m Tra UI Binding

#### A. Collection Có D? Li?u Không?
Trong ViewModel, thêm breakpoint t?i:
```csharp
NewProductImages.Add(imageItem); // ?ã thêm vào collection?
imageItem.Url = result.Data.Url; // URL có giá tr??
```

#### B. Converter Ho?t ??ng?
File: `Converters/SafeUriConverter.cs`

Thêm debug log:
```csharp
public object? Convert(object value, Type targetType, object parameter, string language)
{
    Debug.WriteLine($"[SafeUriConverter] Input: {value}");
    
    if (value is string urlString && !string.IsNullOrWhiteSpace(urlString))
    {
   if (urlString.StartsWith("http://") || ...)
        {
     if (Uri.TryCreate(urlString, UriKind.Absolute, out Uri? uri))
     {
      Debug.WriteLine($"[SafeUriConverter] Output: {uri}");
           return uri;
   }
        }
    }
    
    Debug.WriteLine("[SafeUriConverter] Output: Placeholder");
    return new Uri("ms-appx:///Assets/placeholder-image.png");
}
```

### 4. Test Th? Công

#### Step-by-Step:
1. Run app (F5)
2. M? Product ? Click "Add Item"
3. Click "Upload" ? Ch?n ?nh
4. **Quan sát:**
   - Loading spinner xu?t hi?n không?
   - Sau vài giây có hi?n th? gì không?
   - Check Output window có log không?

5. N?u th?y URL nh?ng ?nh tr?ng:
   - Copy URL t? log
   - Paste vào browser
   - N?u ?nh hi?n ? v?n ?? là binding
   - N?u ?nh không hi?n ? v?n ?? là URL/server

### 5. Workaround T?m Th?i

N?u upload không ho?t ??ng, test v?i URL t?nh:
```csharp
// Trong ViewModel
[RelayCommand]
private void TestAddImageUrl()
{
    NewProductImages.Add(new ProductImageItem
    {
        Url = "https://via.placeholder.com/150",
        PublicId = "test"
    });
}
```

N?u ?nh test hi?n th? OK ? V?n ?? ? upload service
N?u ?nh test c?ng tr?ng ? V?n ?? ? UI binding

### 6. Check Backend

Test mutation tr?c ti?p:
```graphql
mutation {
  uploadProductAsset(file: null) {
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

Upload file qua Postman/Thunder Client:
```
POST http://localhost:5135/graphql
Content-Type: multipart/form-data

operations: {"query":"mutation uploadProductAsset($file: Upload!) { ... }","variables":{"file":null}}
map: {"file":["variables.file"]}
file: [Select file]
```

## Checklist Nhanh

- [ ] Output window có log `[Upload]` không?
- [ ] Success = true hay false?
- [ ] URL có giá tr? không?
- [ ] Copy URL paste vào browser ? ?nh hi?n th??
- [ ] Collection NewProductImages có item không? (breakpoint)
- [ ] SafeUriConverter ???c g?i không? (thêm log)
- [ ] Backend mutation ho?t ??ng? (test riêng)

## Liên H?
N?u v?n không ho?t ??ng, cung c?p:
1. Full Output window log
2. Screenshot l?i
3. Response t? server (n?u có)
