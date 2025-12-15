# Fix - Product Không Hi?n Th? ?nh

## ?? V?n ??
- Upload ?nh thành công ?
- Save product thành công ?
- Nh?ng khi Edit/View product ? 0 ?nh ?

## ?? Nguyên Nhân
**GetProducts query thi?u field `imagePaths`**

Query c?:
```graphql
items {
  productId
  sku
  name
  salePrice
  importPrice
  stockQuantity
  categoryName
  # ? THI?U imagePaths
}
```

## ? Gi?i Pháp
Thêm `imagePaths` vào query trong `ProductService.cs`:

```csharp
private string BuildGetProductsQuery(ProductQueryOptions opt)
{
    // ...
    return $@"
query GetProducts {{
  products(...) {{
    statusCode
    success
    message
    data {{
      page
   pageSize
      totalItems
      totalPages
      items {{
        productId
    sku
        name
    salePrice
        importPrice
        stockQuantity
        categoryName
        imagePaths    ? THÊM DÒNG NÀY
}}
    }}
  }}
}}";
}
```

## ?? Cách Test

### Step 1: Rebuild
```
Shift+F5 (Stop debug)
Ctrl+Shift+B (Rebuild)
F5 (Run)
```

### Step 2: Upload và Save
1. Add Product
2. Upload ?nh ? Ch? thành công
3. ?i?n thông tin product
4. Click "Add"
5. **Check Output window:**
```
[ConfirmAdd] NewProductImages count: 1
[ConfirmAdd] Filtered imagePaths count: 1
[ConfirmAdd] ImagePath: https://res.cloudinary.com/...
[ConfirmAdd] Calling CreateProductAsync with 1 images
[ConfirmAdd] Result - Success: True
```

### Step 3: Verify trong Edit
1. Tìm product v?a t?o trong list
2. Click nút Edit (?)
3. **Check Output window:**
```
[EditProduct] Product has 1 images
[EditProduct] Adding image: https://res.cloudinary.com/...
[EditProduct] EditProductImages count: 1
```
4. Dialog s? hi?n th? ?nh ?

### Step 4: Verify trong List
1. Product list s? hi?n th? thumbnail ?nh trong c?t Image
2. N?u không hi?n th? ? Check FirstImageConverter

## ?? Checklist Debug

N?u v?n không hi?n th?, check theo th? t?:

### 1. ImagePaths có ???c g?i lên server?
**Output window:**
```
[ConfirmAdd] Filtered imagePaths count: X
[ConfirmAdd] ImagePath: https://...
```
- N?u count = 0 ? Upload ch?a xong ho?c URL không h?p l?
- N?u count > 0 ? OK, ti?p

### 2. Server có l?u ImagePaths?
**Test tr?c ti?p GraphQL playground:**
```graphql
query {
  productById(productId: 123) {
    statusCode
    data {
      productId
      name
      imagePaths  # Check xem có data không
    }
  }
}
```
- N?u imagePaths = [] ? Server không l?u
- N?u imagePaths = [...] ? Server OK, ti?p

### 3. GetProducts có l?y imagePaths?
**Check query trong ProductService.cs:**
```csharp
items {
  ...
  imagePaths  ? Ph?i có dòng này
}
```

### 4. Frontend binding có ?úng?
**Check ProductPage.xaml:**
```xaml
<!-- Product List -->
<BitmapImage UriSource="{Binding ImagePaths, Converter={StaticResource FirstImageConverter}}" />

<!-- Edit Dialog -->
<ItemsControl ItemsSource="{Binding EditProductImages}">
  <BitmapImage UriSource="{Binding Url, Converter={StaticResource SafeUriConverter}}" />
</ItemsControl>
```

## ?? Expected Behavior

### After Fix:
1. **Add Product:**
 - Upload ?nh ? Hi?n th? trong dialog ?
   - Save ? ImagePaths ???c g?i lên server ?

2. **Product List:**
   - C?t Image hi?n th? thumbnail ?
   - Dùng FirstImageConverter ?? l?y ?nh ??u tiên ?

3. **Edit Product:**
   - Dialog hi?n th? t?t c? ?nh hi?n t?i ?
   - Upload thêm ?nh m?i ? OK ?
   - Xóa ?nh ? OK ?
   - Save ? ImagePaths updated ?

## ?? Common Mistakes

### ? Mistake 1: Query thi?u imagePaths
```graphql
items {
  productId
  name
  # Thi?u imagePaths
}
```
**Fix:** Thêm `imagePaths` vào query

### ? Mistake 2: Backend không return imagePaths
Server có th? c?n config resolver cho field này.

### ? Mistake 3: Binding sai
```xaml
<!-- SAI -->
<BitmapImage UriSource="{x:Bind Url, Converter=...}" />

<!-- ?ÚNG -->
<BitmapImage UriSource="{Binding Url, Converter=...}" />
```

## ? Summary

**Root cause:** GetProducts query thi?u field `imagePaths`

**Fix:** Thêm 1 dòng vào query
```csharp
imagePaths  // <- Thêm dòng này
```

**Result:** Product hi?n th? ?nh ??y ?? trong List và Edit dialog ??

---

**Stop debug ? Rebuild ? Run ? Test!**
