# Fix L?i Upload ?nh - System.ArgumentException

## V?n ??
Khi upload ?nh, ?ng d?ng báo l?i:
```
System.ArgumentException: The value cannot be converted to type Uri
The parameter is incorrect.
```

## Nguyên Nhân
- Khi ?nh ?ang upload, `Url` property có giá tr? `"Uploading..."` (string text)
- WinUI c? g?ng convert string này thành Uri và th?t b?i

## Gi?i Pháp ?ã Áp D?ng

### 1. T?o SafeUriConverter
File: `Converters/SafeUriConverter.cs`

Converter này:
- Ki?m tra string có ph?i là URL h?p l? không (http://, https://, ms-appx://)
- N?u không h?p l? (nh? "Uploading..."), tr? v? placeholder image
- An toàn không throw exception

### 2. ??ng Ký Converter
File: `App.xaml`
```xaml
<conv:SafeUriConverter x:Key="SafeUriConverter" />
```

### 3. S?a XAML Binding
Thay ??i t?:
```xaml
<BitmapImage UriSource="{x:Bind Url, Mode=OneWay}" />
```

Thành:
```xaml
<BitmapImage UriSource="{Binding Url, Mode=OneWay, Converter={StaticResource SafeUriConverter}}" />
```

**L?u ý**: Ph?i dùng `{Binding}` thay vì `{x:Bind}` vì `{x:Bind}` không h? tr? Converter trong context này.

### 4. Thêm Placeholder Image (Tùy ch?n)
T?o file `Assets/placeholder-image.png` (50x50px, màu xám nh?t) ?? hi?n th? khi:
- URL không h?p l?
- ?ang upload
- L?i load ?nh

## Cách Test
1. Stop debug hi?n t?i
2. Rebuild solution (Ctrl+Shift+B)
3. Run l?i app (F5)
4. Th? upload ?nh - không còn crash

## Files ?ã Thay ??i
- ? `Converters/SafeUriConverter.cs` - Converter m?i
- ? `App.xaml` - ??ng ký converter
- ? `Views/ProductPage.xaml` - S?a binding trong Add/Edit dialog
- ?? `Assets/placeholder-image.png` - C?n t?o (optional)

## L?u Ý
- Converter s? t? ??ng handle m?i tr??ng h?p URL không h?p l?
- Không c?n s?a ViewModel hay code-behind
- Binding an toàn, không còn crash khi upload
