# MyShopClient

Ứng dụng quản lý bán hàng WinUI 3 với .NET 8, kết nối GraphQL API.

## Yêu cầu hệ thống

- Windows 10 version 1809 trở lên
- .NET 8.0 Runtime
- Visual Studio 2022 (để phát triển)

##  Cài đặt & Chạy

```bash
# Clone repository
git clone https://github.com/Thien19012004/MyShopClient_Windows.git

# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

##  Kiến trúc

```
MyShopClient/
├── Views/              # XAML UI Pages
├── ViewModels/         # MVVM ViewModels
├── Models/        # Data Transfer Objects
├── Services/# Business Logic Services
├── Infrastructure/     # GraphQL Client, HTTP Handlers
├── Converters/         # XAML Value Converters
└── Helpers/     # Utility Classes
```

##  Tính năng chính

| Module | Mô tả |
|--------|-------|
| **Dashboard** | Tổng quan doanh thu, đơn hàng, sản phẩm |
| **Products** | CRUD sản phẩm, quản lý danh mục, upload hình ảnh |
| **Orders** | Quản lý đơn hàng, cập nhật trạng thái |
| **Customers** | Quản lý khách hàng |
| **Promotions** | Tạo và quản lý khuyến mãi |
| **Reports** | Báo cáo doanh thu, xuất PDF/Excel |
| **KPI** | Theo dõi chỉ tiêu bán hàng |

##  Công nghệ sử dụng

- **UI Framework:** WinUI 3 (Windows App SDK 1.8)
- **Pattern:** MVVM với CommunityToolkit.Mvvm
- **API:** GraphQL Client tự xây dựng
- **Charts:** LiveCharts2
- **PDF Export:** QuestPDF
- **Excel Export:** EPPlus

##  Cấu hình

Chỉnh sửa server URL trong trang **Settings** hoặc file cấu hình:

```json
{
  "BaseUrl": "http://localhost:5135"
}
```

## Build Release

```bash
# Build với ReadyToRun optimization
dotnet publish -c Release -r win-x64 --self-contained true
```

Output: `bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish\`

##  Bảo mật

- JWT Authentication với secure storage
- Auto-refresh token
- Retry logic cho network errors

