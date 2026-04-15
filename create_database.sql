l-- Create Database (idempotent)
IF DB_ID(N'TechShopWebsite1') IS NULL
BEGIN
    CREATE DATABASE TechShopWebsite1;
END
GO

USE TechShopWebsite1;
GO

-- Create Categories Table
CREATE TABLE Categories (
    category_id INT PRIMARY KEY IDENTITY(1,1),
    name NVARCHAR(100) NOT NULL UNIQUE
);
GO

-- Create Suppliers Table
CREATE TABLE Suppliers (
    supplier_id INT PRIMARY KEY IDENTITY(1,1),
    name NVARCHAR(255) NOT NULL,
    contact_person NVARCHAR(100),
    phone NVARCHAR(20),
    email NVARCHAR(100),
    address NVARCHAR(255)
);
GO

-- Create Accounts Table
CREATE TABLE Accounts (
    account_id INT PRIMARY KEY IDENTITY(1,1),
    username NVARCHAR(50) NOT NULL UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,
    email NVARCHAR(100),
    full_name NVARCHAR(100),
    phone NVARCHAR(20),
    address NVARCHAR(255),
    is_active BIT NOT NULL DEFAULT 1,
    role NVARCHAR(20) NOT NULL DEFAULT 'Customer'
);
GO

-- Create Employees Table
CREATE TABLE Employees (
    employee_id INT PRIMARY KEY IDENTITY(1,1),
    account_id INT UNIQUE,
    employee_code NVARCHAR(10) NOT NULL UNIQUE,
    position NVARCHAR(50),
    department NVARCHAR(50),
    FOREIGN KEY (account_id) REFERENCES Accounts(account_id)
);
GO

-- Create Products Table
CREATE TABLE Products (
    product_id INT PRIMARY KEY IDENTITY(1,1),
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    image_url NVARCHAR(500),
    price DECIMAL(10, 2) NOT NULL,
    category_id INT,
    supplier_id INT,
    FOREIGN KEY (category_id) REFERENCES Categories(category_id),
    FOREIGN KEY (supplier_id) REFERENCES Suppliers(supplier_id)
);
GO

-- Create Inventory Table
CREATE TABLE Inventory (
    inventory_id INT PRIMARY KEY IDENTITY(1,1),
    product_id INT NOT NULL UNIQUE,
    quantity_in_stock INT NOT NULL DEFAULT 0,
    last_updated_date DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (product_id) REFERENCES Products(product_id)
);
GO

-- Create Orders Table
CREATE TABLE Orders (
    order_id INT PRIMARY KEY IDENTITY(1,1),
    account_id INT NOT NULL,
    order_date DATETIME NOT NULL DEFAULT GETDATE(),
    total_amount DECIMAL(10, 2) NOT NULL,
    status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    customer_name NVARCHAR(100),
    customer_phone NVARCHAR(20),
    customer_address NVARCHAR(255),
    notes NVARCHAR(500),
    FOREIGN KEY (account_id) REFERENCES Accounts(account_id)
);
GO

-- Create OrderDetails Table (EF: OrderItem)
CREATE TABLE OrderDetails (
    OrderID INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL,
    Price DECIMAL(18, 2) NOT NULL,
    PRIMARY KEY (OrderID, ProductID),
    FOREIGN KEY (OrderID) REFERENCES Orders(order_id),
    FOREIGN KEY (ProductID) REFERENCES Products(product_id)
);
GO

-- Create Cart_Items Table
CREATE TABLE Cart_Items (
    cart_item_id INT PRIMARY KEY IDENTITY(1,1),
    account_id INT NOT NULL,
    product_id INT NOT NULL,
    quantity INT NOT NULL,
    added_date DATETIME NOT NULL DEFAULT GETDATE(),
    UNIQUE (account_id, product_id),
    FOREIGN KEY (account_id) REFERENCES Accounts(account_id),
    FOREIGN KEY (product_id) REFERENCES Products(product_id)
);
GO

-- Create Receipts_Shipments Table
CREATE TABLE Receipts_Shipments (
    movement_id INT PRIMARY KEY IDENTITY(1,1),
    product_id INT NOT NULL,
    movement_type NVARCHAR(10) NOT NULL,
    quantity INT NOT NULL,
    movement_date DATETIME NOT NULL DEFAULT GETDATE(),
    related_order_id INT,
    FOREIGN KEY (product_id) REFERENCES Products(product_id),
    FOREIGN KEY (related_order_id) REFERENCES Orders(order_id)
);
GO

-- Create Indexes
CREATE INDEX IX_Products_CategoryId ON Products(category_id);
CREATE INDEX IX_Products_SupplierId ON Products(supplier_id);
CREATE INDEX IX_Orders_AccountId ON Orders(account_id);
CREATE INDEX IX_ReceiptShipments_ProductId ON Receipts_Shipments(product_id);
CREATE INDEX IX_ReceiptShipments_RelatedOrderId ON Receipts_Shipments(related_order_id);
GO

-- =====================================================
-- SAMPLE DATA
-- =====================================================

-- Categories (6 categories)
INSERT INTO Categories (name) VALUES (N'Điện thoại');
INSERT INTO Categories (name) VALUES (N'Laptop');
INSERT INTO Categories (name) VALUES (N'Tablet');
INSERT INTO Categories (name) VALUES (N'Phụ kiện');
INSERT INTO Categories (name) VALUES (N'Đồng hồ thông minh');
INSERT INTO Categories (name) VALUES (N'Gaming');
GO

-- Suppliers
INSERT INTO Suppliers (name, contact_person, phone, email, address)
VALUES (N'Samsung Việt Nam', N'Nguyễn Văn A', '0912345678', 'contact@samsung.vn', N'TP. Hồ Chí Minh');
INSERT INTO Suppliers (name, contact_person, phone, email, address)
VALUES (N'Apple Vietnam', N'Trần Thị B', '0987654321', 'contact@apple.vn', N'Hà Nội');
INSERT INTO Suppliers (name, contact_person, phone, email, address)
VALUES (N'LG Electronics', N'Phạm Văn C', '0933333333', 'contact@lg.vn', N'TP. Hồ Chí Minh');
GO

-- Accounts
INSERT INTO Accounts (username, password_hash, email, full_name, phone, address, role)
VALUES (N'admin', 'admin123', 'admin@techshop.vn', N'Quản trị viên', '0917111111', N'TP. Hồ Chí Minh', 'Admin');
INSERT INTO Accounts (username, password_hash, email, full_name, phone, address, role)
VALUES (N'employee1', 'employee123', 'employee@techshop.vn', N'Nhân viên 1', '0918222222', N'Hà Nội', 'Employee');
INSERT INTO Accounts (username, password_hash, email, full_name, phone, address, role)
VALUES (N'customer1', 'customer123', 'customer@email.com', N'Khách hàng 1', '0919333333', N'TP. Hồ Chí Minh', 'Customer');
GO

-- Employees
INSERT INTO Employees (account_id, employee_code, position, department)
VALUES (1, 'ADM001', N'Quản trị viên hệ thống', N'Quản trị');
INSERT INTO Employees (account_id, employee_code, position, department)
VALUES (2, 'EMP001', N'Nhân viên bán hàng', N'Bán hàng');
GO

-- Products (24 items total: 4 original + 20 new samples)
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'iPhone 15', N'iPhone 15 128GB - Màn hình Super Retina XDR 6.1 inch, chip A16 Bionic, camera 48MP, pin trọn ngày.', CAST(29990000 AS DECIMAL(10, 2)), 1, 2);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'Samsung Galaxy S24', N'Galaxy S24 256GB - Màn hình Dynamic AMOLED 2X 6.2 inch, chip Snapdragon 8 Gen 3, camera 50MP.', CAST(24990000 AS DECIMAL(10, 2)), 1, 1);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'MacBook Pro 16', N'MacBook Pro 16 inch M3 Pro - Chip M3 Pro, RAM 18GB, SSD 512GB, màn hình Liquid Retina XDR.', CAST(59990000 AS DECIMAL(10, 2)), 2, 2);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'OLED Tablet Pro', N'Máy tính bảng OLED Tablet Pro 11 inch - Màn hình OLED 120Hz, RAM 8GB, bút cảm ứng, pin 8000mAh.', CAST(12990000 AS DECIMAL(10, 2)), 3, 1);
GO

-- Products 5-24: New Sample Data
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'iPhone 15 Pro Max', N'iPhone 15 Pro Max 256GB - Titanium grade 5, chip A17 Pro, camera 48MP, màn hình 6.7 inch Super Retina XDR Always-On.', CAST(37990000 AS DECIMAL(10, 2)), 1, 2);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'Samsung Galaxy Z Fold 5', N'Galaxy Z Fold 5 512GB - Màn hình gập Dynamic AMOLED 2X 7.6 inch, Snapdragon 8 Gen 2.', CAST(49990000 AS DECIMAL(10, 2)), 1, 1);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'Xiaomi 14 Pro', N'Xiaomi 14 Pro 512GB - Snapdragon 8 Gen 3, Leica camera 50MP, sạc nhanh 120W, màn hình LTPO AMOLED 6.73 inch.', CAST(19990000 AS DECIMAL(10, 2)), 1, 1);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'OPPO Find X7 Ultra', N'OPPO Find X7 Ultra 512GB - Camera periscope 50MP, chip Dimensity 9300, sạc 100W SUPERVOOC.', CAST(24990000 AS DECIMAL(10, 2)), 1, 1);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'MacBook Air 15 M3', N'MacBook Air 15 inch M3 2024 - Chip M3, RAM 16GB, SSD 256GB, màn hình Liquid Retina 15.3 inch, pin 18h.', CAST(38990000 AS DECIMAL(10, 2)), 2, 2);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'Dell XPS 15 9530', N'Dell XPS 15 9530 - Core i9-13900H, RAM 32GB DDR5, SSD 1TB NVMe, RTX 4060 8GB, màn hình 3.5K OLED 15.6 inch.', CAST(59990000 AS DECIMAL(10, 2)), 2, 1);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'ASUS ROG Zephyrus G16', N'ASUS ROG Zephyrus G16 - Core Ultra 9, RTX 4070, RAM 32GB, SSD 1TB, màn hình 240Hz OLED 16 inch.', CAST(54990000 AS DECIMAL(10, 2)), 2, 1);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'MacBook Pro 14 M3 Pro', N'MacBook Pro 14 inch M3 Pro - Chip M3 Pro 12-core, RAM 18GB, SSD 512GB, Liquid Retina XDR 120Hz.', CAST(44990000 AS DECIMAL(10, 2)), 2, 2);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'iPad Pro 13 M4', N'iPad Pro 13 inch M4 - Chip M4, RAM 16GB, SSD 512GB, màn hình Ultra Retina XDR OLED, bút Apple Pencil Pro.', CAST(49990000 AS DECIMAL(10, 2)), 3, 2);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'Samsung Galaxy Tab S9 Ultra', N'Galaxy Tab S9 Ultra - Màn hình Dynamic AMOLED 2X 14.6 inch 120Hz, RAM 12GB, S Pen.', CAST(32990000 AS DECIMAL(10, 2)), 3, 1);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'Sony WH-1000XM5', N'Tai nghe Sony WH-1000XM5 - Chống ồn chủ động cao cấp, driver 30mm, pin 30h, LDAC, multipoint 2 thiết bị.', CAST(9990000 AS DECIMAL(10, 2)), 4, 1);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'Apple AirPods Pro 2', N'AirPods Pro 2 USB-C - Chống ồn chủ động, âm thanh không gian cá nhân hóa, chip H2, pin 6h.', CAST(7990000 AS DECIMAL(10, 2)), 4, 2);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'Samsung Galaxy Watch 6 Classic', N'Galaxy Watch 6 Classic 47mm - AMOLED 1.5 inch, Exynos W930, GPS, chống nước 5ATM.', CAST(9990000 AS DECIMAL(10, 2)), 5, 1);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'Apple Watch Ultra 2', N'Apple Watch Ultra 2 - Titanium, màn hình Sapphire 49mm, chip S9, GPS L1+L5, chống nước 100m.', CAST(27990000 AS DECIMAL(10, 2)), 5, 2);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'Apple AirTag 4 pack', N'Bộ 4 Apple AirTag - Định vị Bluetooth, Precision Finding, chống nước IP67, pin CR2032 1 năm.', CAST(2990000 AS DECIMAL(10, 2)), 4, 2);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'Samsung T7 Shield 2TB', N'Ổ cứng di động Samsung T7 Shield 2TB - USB 3.2 Gen 2, tốc độ đọc 1050MB/s, chống nước IP65.', CAST(4990000 AS DECIMAL(10, 2)), 4, 1);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'Anker PowerCore 26800mAh', N'Sạc dự phòng Anker PowerCore 26800mAh - Sạc nhanh PowerIQ 3.0, USB-C PD 30W, sạc 3 thiết bị cùng lúc.', CAST(2190000 AS DECIMAL(10, 2)), 4, 1);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'Logitech MX Master 3S', N'Chuột không dây Logitech MX Master 3S - Darkfield 8000 DPI, cuộn MagSpeed, kết nối Bluetooth + USB Receiver.', CAST(3490000 AS DECIMAL(10, 2)), 4, 1);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'Belkin BoostCharge 3-in-1', N'Sạc không dây Belkin BoostCharge 3-in-1 - Sạc Apple Watch + iPhone + AirPods cùng lúc, MagSafe 15W.', CAST(2490000 AS DECIMAL(10, 2)), 4, 2);
INSERT INTO Products (name, description, price, category_id, supplier_id)
VALUES (N'Steam Deck OLED 512GB', N'Máy chơi game cầm tay Steam Deck OLED - Màn hình OLED 7.4 inch 90Hz, AMD APU, pin 50Wh, 512GB NVMe.', CAST(29990000 AS DECIMAL(10, 2)), 6, 1);
GO

-- Inventory (24 products)
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (1, 50);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (2, 75);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (3, 20);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (4, 35);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (5, 30);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (6, 15);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (7, 60);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (8, 25);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (9, 40);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (10, 20);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (11, 30);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (12, 45);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (13, 35);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (14, 20);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (15, 80);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (16, 50);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (17, 100);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (18, 40);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (19, 60);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (20, 90);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (21, 70);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (22, 55);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (23, 85);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (24, 30);
GO

-- Orders (with customer info)
INSERT INTO Orders (account_id, order_date, total_amount, status, customer_name, customer_phone, customer_address)
VALUES (3, DATEADD(DAY, -3, GETDATE()), CAST(49990000 AS DECIMAL(10, 2)), 'Completed', N'Nguyễn Văn Minh', '0901234567', N'123 Đường Lê Lợi, Quận 1, TP.HCM');
INSERT INTO Orders (account_id, order_date, total_amount, status, customer_name, customer_phone, customer_address)
VALUES (3, DATEADD(DAY, -1, GETDATE()), CAST(27990000 AS DECIMAL(10, 2)), 'Shipped', N'Nguyễn Văn Minh', '0901234567', N'123 Đường Lê Lợi, Quận 1, TP.HCM');
INSERT INTO Orders (account_id, order_date, total_amount, status, customer_name, customer_phone, customer_address)
VALUES (3, GETDATE(), CAST(39990000 AS DECIMAL(10, 2)), 'Pending', N'Nguyễn Văn Minh', '0901234567', N'123 Đường Lê Lợi, Quận 1, TP.HCM');
GO

-- OrderDetails (EF: OrderItem - using DB table name OrderDetails)
INSERT INTO OrderDetails (OrderID, ProductID, Quantity, Price) VALUES (1, 6, 1, CAST(49990000 AS DECIMAL(18, 2)));
INSERT INTO OrderDetails (OrderID, ProductID, Quantity, Price) VALUES (2, 14, 1, CAST(27990000 AS DECIMAL(18, 2)));
INSERT INTO OrderDetails (OrderID, ProductID, Quantity, Price) VALUES (3, 5, 1, CAST(37990000 AS DECIMAL(18, 2)));
GO

-- Cart Items
INSERT INTO Cart_Items (account_id, product_id, quantity) VALUES (3, 3, 1);
GO

-- Receipts_Shipments
INSERT INTO Receipts_Shipments (product_id, movement_type, quantity, related_order_id) VALUES (1, 'Out', 1, 1);
INSERT INTO Receipts_Shipments (product_id, movement_type, quantity, related_order_id) VALUES (2, 'Out', 2, 2);
GO

-- Verify
PRINT 'Database created successfully!';
PRINT 'Tables: Categories, Suppliers, Accounts, Employees, Products, Inventory, Orders, OrderDetails, Cart_Items, Receipts_Shipments';
PRINT 'Sample data inserted: 24 products, 3 orders, 3 order items, inventory.';
GO
