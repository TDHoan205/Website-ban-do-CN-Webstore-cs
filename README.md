# Webstore - Website Bán Đồ Công Nghệ

Website thương mại điện tử bán đồ công nghệ (điện thoại, laptop, tablet, phụ kiện).

## 🚀 Quick Start (Cho thành viên mới)

### Bước 1: Clone repo
```bash
git clone <repo-url>
cd Website-ban-do-CN-Webstore-cs
```

### Bước 2: Setup nhanh
```bash
# Chạy script setup tự động
setup.bat
```

### Bước 3: Cấu hình Database
1. Mở file `appsettings.json`
2. Tìm dòng:
   ```json
   "Server=YOUR_SERVER_NAME;..."
   ```
3. Thay `YOUR_SERVER_NAME` bằng server của bạn:
   - Nếu dùng SQL Server LocalDB: `Server=(localdb)\mssqllocaldb` hoặc `localhost`
   - Xem server name: Mở **SQL Server Management Studio (SSMS)** → Server name

### Bước 4: Tạo Database
1. Mở **SQL Server Management Studio (SSMS)**
2. Connect đến server của bạn
3. Mở file `SQL/setup_database.sql`
4. Nhấn **F5** hoặc **Execute** để chạy script
5. Database `TechShopWebsite2` sẽ được tạo tự động

### Bước 5: Chạy ứng dụng
```bash
dotnet run
```
Mở trình duyệt: http://localhost:5000

### Tài khoản mặc định
- **Admin**: username: `admin`, password: `admin123`
- **Customer**: username: `customer1`, password: `customer123`

---

## 🔧 Yêu cầu hệ thống

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (hoặc SQL Server Express/LocalDB)
- [SQL Server Management Studio](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms) (khuyến nghị)

---

## 📁 Cấu trúc Project

```
Website-ban-do-CN-Webstore-cs/
├── Controllers/           # Controllers (API & MVC)
├── Models/                # Data Models
├── Views/                 # Razor Views
├── Services/              # Business Logic
├── Data/                  # DbContext, Repositories
├── Helpers/               # Utility classes
├── wwwroot/               # Static files (CSS, JS, images)
├── SQL/                   # Database scripts
├── setup.bat              # Script setup nhanh
├── appsettings.json       # Configuration
└── Program.cs             # Entry point
```

---

## ⚙️ Cấu hình (appsettings.json)

### Database Connection
```json
"ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=TechShopWebsite2;..."
}
```

### Email Settings (cho chức năng quên mật khẩu)
```json
"EmailSettings": {
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
}
```
> Lưu ý: Cần tạo App Password từ Google Account → Security → 2-Step Verification → App Passwords

### AI Chat Settings
```json
"Gemini": {
    "ApiKey": "your-gemini-api-key"
}
```

---

## 🐛 Xử lý lỗi thường gặp

### Lỗi "Cannot open database"
```
1. Kiểm tra SQL Server đang chạy (Services → SQL Server)
2. Kiểm tra server name trong appsettings.json
3. Đảm bảo database TechShopWebsite2 đã được tạo
```

### Lỗi "Port already in use"
```bash
# Tìm và kill process đang dùng port 5000
netstat -ano | findstr :5000
taskkill /PID <process_id> /F
```

### Lỗi CSS/JS không load
```bash
# Xóa cache và build lại
rmdir /s /q obj
rmdir /s /q bin
dotnet build
dotnet run
```

### Lỗi "Unable to resolve services"
```bash
dotnet restore
dotnet build --no-incremental
```

---

## 📝 Git Workflow cho Team

### Trước khi bắt đầu làm việc
```bash
git checkout main
git pull origin main
dotnet restore
dotnet build
```

### Khi push code
```bash
# KHÔNG BAO GIỜ push các file này:
# - appsettings.json (chứa secrets)
# - bin/, obj/ (build outputs)
# - *.log, *.sqlite

# LUÔN LUÔN commit thay đổi có ý nghĩa:
git add .
git commit -m "feat: them chuc nang tim kiem san pham"
git push origin <branch-name>
```

### Tạo Pull Request
1. Tạo branch mới cho feature: `git checkout -b feature/ten-feature`
2. Code và test kỹ
3. Push: `git push origin feature/ten-feature`
4. Tạo Pull Request trên GitHub/GitLab
5. Đợi review và merge

---

## 🔒 Bảo mật

### KHÔNG BAO GIỜ commit:
- `appsettings.json` (chứa API keys, passwords)
- Connection strings thật
- Database files (*.mdf, *.ldf)
- Log files

### LUÔN sử dụng:
- Environment variables cho production
- `.gitignore` đúng cách
- API keys giả cho development

---

## 👥 Team Members

| Thành viên | Vai trò |
|------------|---------|
| [Tên 1] | Backend, Database |
| [Tên 2] | Frontend, UI/UX |
| [Tên 3] | Testing, Documentation |

---

## 📜 License

MIT License - Free to use cho mục đích học tập.

---

## 📞 Liên hệ

- Email nhóm: [team-email@example.com]
- Slack/Discord: [link channel]
