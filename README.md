# 🛒 Webstore - TechShop Management System

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Status: Active](https://img.shields.io/badge/Status-Active-success)](README.md)

Webstore là một ứng dụng web quản lý cửa hàng bán lẻ công nghệ được xây dựng bằng **ASP.NET Core 8.0**. Hệ thống cung cấp đầy đủ các tính năng quản lý sản phẩm, đơn hàng, kho hàng, nhân viên và khách hàng.

## ✨ Tính Năng Chính

- **👥 Quản Lý Tài Khoản & Xác Thực**
  - Đăng ký tài khoản người dùng
  - Đăng nhập / Đăng xuất
  - Quản lý thông tin tài khoản
  - Hỗ trợ role-based access control

- **📦 Quản Lý Sản Phẩm**
  - CRUD hoàn chỉnh cho sản phẩm
  - Phân loại sản phẩm theo danh mục
  - Quản lý ảnh sản phẩm
  - Xem chi tiết sản phẩm

- **📊 Quản Lý Kho Hàng**
  - Theo dõi tồn kho sản phẩm
  - Cập nhật số lượng hàng
  - Quản lý nhà cung cấp
  - Quản lý lô hàng nhập

- **🛍️ Giỏ Hàng & Đơn Hàng**
  - Thêm/Xóa sản phẩm vào giỏ hàng
  - Tạo đơn hàng từ giỏ hàng
  - Theo dõi trạng thái đơn hàng
  - Quản lý chi tiết đơn hàng

- **👔 Quản Lý Nhân Viên**
  - CRUD thông tin nhân viên
  - Quản lý thông tin cá nhân

- **📈 Báo Cáo & Thống Kê**
  - Xem thống kê bán hàng
  - Phân tích dữ liệu bán hàng

## 🛠️ Yêu Cầu Hệ Thống

- **.NET 8.0 SDK** trở lên
- **SQL Server 2019** trở lên (hoặc SQL Server Express)
- **Visual Studio 2022** (khuyến nghị) hoặc **VS Code** với C# extension
- **Windows** (cho SQL Server Express)

## 📥 Cài Đặt

### 1. Clone Repository
```bash
git clone https://github.com/yourusername/webstore.git
cd webstore
```

### 2. Cấu Hình Cơ Sở Dữ Liệu

#### Tạo Database
Mở **SQL Server Management Studio** hoặc **Azure Data Studio** và chạy script:

```sql
-- Tệp: create_database.sql
```

Hoặc sử dụng Package Manager Console:
```powershell
Update-Database
```

#### Cập Nhật Connection String
Chỉnh sửa `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=TechShopWebsite1;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

### 3. Khôi Phục Packages
```bash
dotnet restore
```

### 4. Chạy Ứng Dụng
```bash
dotnet run
```

Ứng dụng sẽ khởi động tại: **https://localhost:5001** (hoặc cổng được chỉ định)

## 📁 Cấu Trúc Thư Mục

```
Webstore/
├── Controllers/              # Xử lý logic nghiệp vụ
│   ├── AuthController.cs
│   ├── ProductsController.cs
│   ├── OrdersController.cs
│   └── ...
├── Models/                   # Entities & ViewModels
│   ├── Product.cs
│   ├── Order.cs
│   ├── Account.cs
│   └── ...
├── Views/                    # Razor templates
│   ├── Auth/
│   ├── Products/
│   ├── Orders/
│   └── ...
├── Data/
│   ├── ApplicationDbContext.cs   # EF Core DbContext
│   └── SeedData.cs              # Dữ liệu mẫu
├── wwwroot/                  # Static files (CSS, JS, Images)
├── Properties/
│   └── launchSettings.json   # Tính năng chạy
├── appsettings.json          # Cấu hình chính
└── Program.cs                # Khởi tạo ứng dụng
```

## 🔑 Công Nghệ Sử Dụng

| Công Nghệ | Phiên Bản | Mục Đích |
|-----------|----------|---------|
| ASP.NET Core | 8.0 | Framework web |
| Entity Framework Core | 8.0 | ORM |
| SQL Server | 2019+ | Cơ sở dữ liệu |
| Razor Pages | 8.0 | Giao diện người dùng |
| Cookie Authentication | 8.0 | Xác thực |

## 🚀 Cách Sử Dụng

### 1. Trang Chủ
- Truy cập `/Home/Index` để xem trang chủ
- Truy cập `/Home/Landing` cho trang giới thiệu

### 2. Tài Khoản
- **Đăng ký**: `/Auth/Register`
- **Đăng nhập**: `/Auth/Login`
- **Quản lý tài khoản**: `/Accounts`

### 3. Mua Sắm
- **Danh sách sản phẩm**: `/Shop`
- **Danh mục**: `/Categories`
- **Giỏ hàng**: `/CartItems`

### 4. Quản Lý (Yêu cầu quyền admin)
- **Sản phẩm**: `/Products`
- **Đơn hàng**: `/Orders`
- **Kho hàng**: `/Inventory`
- **Nhà cung cấp**: `/Suppliers`
- **Nhân viên**: `/Employees`
- **Thống kê**: `/Statistics`

## 🔐 Bảo Mật

- ✅ Xác thực dựa trên Cookie
- ✅ Mã hóa mật khẩu với salt
- ✅ HttpOnly Cookies
- ✅ HTTPS support
- ✅ Session timeout (30 phút)
- ✅ CSRF protection (ASP.NET Core built-in)

## 📝 Xác Thực

Ứng dụng sử dụng **Cookie-based Authentication**:
- Session timeout: **30 phút**
- Cookie expiration: **8 giờ**
- Sliding expiration: **Enabled**

## 💾 Mô Hình Dữ Liệu

Ứng dụng sử dụng các bảng chính:
- **Accounts** - Tài khoản người dùng
- **Categories** - Danh mục sản phẩm
- **Products** - Sản phẩm
- **Suppliers** - Nhà cung cấp
- **Inventory** - Tồn kho
- **Orders** - Đơn hàng
- **OrderItems** - Chi tiết đơn hàng
- **Employees** - Nhân viên
- **CartItems** - Giỏ hàng
- **ReceiptShipments** - Lô hàng nhập

## 🐛 Troubleshooting

### Lỗi kết nối Database
```
❌ Không thể kết nối tới SQL Server
```
**Giải pháp:**
- Kiểm tra SQL Server đang chạy
- Xác nhận connection string trong `appsettings.json`
- Kiểm tra quyền truy cập tài khoản

### Lỗi Port đã sử dụng
```
System.Net.HttpListenerException: Access Denied
```
**Giải pháp:**
- Thay đổi port trong `launchSettings.json`
- Hoặc chạy với: `dotnet run --urls "https://localhost:5002"`

### Database chưa được tạo
```csharp
// Chạy trong Package Manager Console:
Update-Database
```

## 📋 Roadmap

- [ ] API REST documentation
- [ ] Unit tests
- [ ] Payment gateway integration
- [ ] Email notifications
- [ ] Multi-language support
- [ ] Mobile app (Flutter/React Native)
- [ ] Advanced analytics dashboard

## 🤝 Đóng Góp

Chúng tôi rất hoan nghênh những đóng góp từ cộng đồng!

**Quy trình:**
1. Fork repository
2. Tạo branch feature (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Mở Pull Request

## 📄 License

Dự án này được cấp phép dưới **MIT License** - xem file [LICENSE](LICENSE) để chi tiết.

## 📧 Liên Hệ & Hỗ Trợ

- 📧 Email: [your-email@example.com](mailto:your-email@example.com)
- 🐛 Issues: [GitHub Issues](https://github.com/yourusername/webstore/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/yourusername/webstore/discussions)

## 👨‍💻 Tác Giả

- **Your Name** - Initial work - [@yourgithub](https://github.com/yourusername)

## 🙏 Acknowledgments

- ASP.NET Core team
- Entity Framework Core team
- Bootstrap framework
- Inspiration từ các dự án quản lý cửa hàng tiêu biểu

---

**Được phát triển với ❤️ bằng ASP.NET Core 8.0**
# Website-ban-do-CN-Webstore-cs
