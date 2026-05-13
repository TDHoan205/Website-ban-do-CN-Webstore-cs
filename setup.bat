# =========================================================
# SETUP HƯỚNG DẪN CHO TEAM
# =========================================================
# Chạy script này sau khi clone repo hoặc khi có lỗi build
# =========================================================

@echo off
chcp 65001 >nul
echo.
echo  ╔═══════════════════════════════════════════════════════╗
echo  ║          WEBSTORE PROJECT - SETUP SCRIPT               ║
echo  ╚═══════════════════════════════════════════════════════╝
echo.

REM 1. Xoa cache cu
echo [1/5] Xoa cache build...
if exist "obj" rmdir /s /q "obj"
if exist "bin" rmdir /s /q "bin"
echo       Da xoa xong!

REM 2. Restore dependencies
echo.
echo [2/5] Tai dependencies...
dotnet restore
if errorlevel 1 (
    echo       Loi khi restore! Kiem tra ket noi internet.
    pause
    exit /b 1
)
echo       Done!

REM 3. Build
echo.
echo [3/5] Build project...
dotnet build --no-incremental
if errorlevel 1 (
    echo       Loi build! Xem loi phia tren.
    pause
    exit /b 1
)
echo       Build thanh cong!

REM 4. Kiem tra appsettings.json
echo.
echo [4/5] Kiem tra cau hinh...
if not exist "appsettings.json" (
    echo       TAO FILE: appsettings.json
    copy appsettings.json.example appsettings.json
)

REM 5. Huong dan cau hinh
echo.
echo [5/5] Canh bao cau hinh!
echo.
echo  ═══════════════════════════════════════════════════════
echo  TRUOC KHI CHAY, HAY SUA appsettings.json:
echo  ═══════════════════════════════════════════════════════
echo.
echo  1. Mo file appsettings.json
echo.
echo  2. Thay YOUR_SERVER_NAME bang ten server cua ban:
echo     - Cach xem: Mo SQL Server Management Studio ^(^SSMS^)
echo     - Server name thuong la: localhost, . hoac TENMAY^
echo.
echo  3. Vi du:
echo     BEFORE: "Server=YOUR_SERVER_NAME;..."
echo     AFTER:  "Server=localhost;..."
echo.
echo  4. Neu dung SQL Server khac, kiem tra:
echo     - Database name: TechShopWebsite2
echo     - Authentication: Windows Authentication
echo     - Hoac doi thanh SQL Authentication neu can
echo.
echo  ═══════════════════════════════════════════════════════
echo.

echo.
echo  ═══════════════════════════════════════════════════════
echo  DEPLOY DATABASE (CHAY LAN DAU TIEN):
echo  ═══════════════════════════════════════════════════════
echo.
echo  1. Mo SQL Server Management Studio (SSMS)
echo  2. Connect den server cua ban
echo  3. Mo file: SQL/setup_database.sql
echo  4. Execute script (F5)
echo  5. Database TechShopWebsite2 se duoc tao tu dong
echo.
echo  ═══════════════════════════════════════════════════════
echo.

echo.
echo  ╔═══════════════════════════════════════════════════════╗
echo  ║              SETUP HOAN TAT!                           ║
echo  ╚═══════════════════════════════════════════════════════╝
echo.
echo  Tiep theo:
echo    1. Sua appsettings.json (xem huong dan tren)
echo  2. Chay script SQL de tao database
echo  3. Go lenh: dotnet run
echo.
echo  Neu gap loi, xem file README.md hoac hoi nhom.
echo.
pause
