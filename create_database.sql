-- Create Database (idempotent)
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
    original_price DECIMAL(10, 2),
    stock_quantity INT DEFAULT 50,
    rating DECIMAL(2, 1) DEFAULT 4.5,
    is_new BIT DEFAULT 0,
    is_hot BIT DEFAULT 0,
    discount_percent INT DEFAULT 0,
    specifications NVARCHAR(MAX),
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

-- Products - 50 items
-- Điện thoại (15 sản phẩm)
INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'iPhone 15 Pro Max 256GB', N'iPhone 15 Pro Max với chip A17 Pro, màn hình Super Retina XDR 6.7 inch, hệ thống camera 48MP. Thiết kế titan cao cấp, hỗ trợ USB-C và Action Button.', 32990000, 34990000, 45, 4.9, 1, 0, 6, '{"screen": "6.7 inch Super Retina XDR 2796x1290 120Hz", "cpu": "Apple A17 Pro", "ram": "8GB", "storage": "256GB", "battery": "4422mAh"}', 'https://via.placeholder.com/300x300/333333/ffffff?text=iPhone+15+Pro+Max', 1, 2);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Samsung Galaxy S24 Ultra 256GB', N'Samsung Galaxy S24 Ultra với bút S Pen tích hợp, camera 200MP, màn hình Dynamic AMOLED 2X 6.8 inch. Chip Snapdragon 8 Gen 3 cho hiệu năng vượt trội.', 28990000, 31990000, 38, 4.8, 1, 0, 9, '{"screen": "6.8 inch Dynamic AMOLED 2X 3120x1440 120Hz", "cpu": "Snapdragon 8 Gen 3", "ram": "12GB", "storage": "256GB", "battery": "5000mAh"}', 'https://via.placeholder.com/300x300/1a237e/ffffff?text=Galaxy+S24+Ultra', 1, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'iPhone 15 128GB', N'iPhone 15 với chip A16 Bionic, camera 48MP, Dynamic Island. Thiết kế kính và nhôm, hỗ trợ sạc USB-C và kháng nước IP68.', 19990000, 21990000, 62, 4.7, 1, 0, 9, '{"screen": "6.1 inch Super Retina XDR 2556x1179 60Hz", "cpu": "Apple A16 Bionic", "ram": "6GB", "storage": "128GB", "battery": "3349mAh"}', 'https://via.placeholder.com/300x300/333333/ffffff?text=iPhone+15', 1, 2);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Samsung Galaxy A55 5G 128GB', N'Samsung Galaxy A55 5G với màn hình Super AMOLED 6.6 inch, camera 50MP, pin 5000mAh. Thiết kế khung kim loại, hỗ trợ 5G.', 9490000, 10990000, 75, 4.5, 1, 0, 14, '{"screen": "6.6 inch Super AMOLED 2340x1080 120Hz", "cpu": "Exynos 1480", "ram": "8GB", "storage": "128GB", "battery": "5000mAh"}', 'https://via.placeholder.com/300x300/1a237e/ffffff?text=Galaxy+A55+5G', 1, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Xiaomi Redmi Note 13 Pro 5G', N'Xiaomi Redmi Note 13 Pro với camera 200MP, màn hình AMOLED 6.67 inch 120Hz, sạc nhanh 67W. Chip Snapdragon 7s Gen 2 mạnh mẽ.', 7990000, 9490000, 88, 4.6, 1, 0, 16, '{"screen": "6.67 inch AMOLED 2712x1220 120Hz", "cpu": "Snapdragon 7s Gen 2", "ram": "8GB", "storage": "256GB", "battery": "5100mAh"}', 'https://via.placeholder.com/300x300/ff5722/ffffff?text=Redmi+Note+13+Pro', 1, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'OPPO Reno11 F 5G', N'OPPO Reno11 F 5G với camera 64MP, màn hình AMOLED 6.7 inch, sạc SUPERVOOC 67W. Thiết kế mỏng nhẹ, cảm biến vân tay trong màn hình.', 9990000, 11990000, 54, 4.4, 1, 0, 17, '{"screen": "6.7 inch AMOLED 2412x1080 120Hz", "cpu": "MediaTek Dimensity 7050", "ram": "8GB", "storage": "256GB", "battery": "5000mAh"}', 'https://via.placeholder.com/300x300/4caf50/ffffff?text=Reno11+F+5G', 1, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'vivo V30e 5G', N'vivo V30e 5G với camera 50MP chống rung, màn hình AMOLED 6.78 inch, pin 5500mAh. Thiết kế cong 3D Ergonomic, trọng lượng chỉ 188g.', 7990000, 9990000, 67, 4.5, 1, 0, 20, '{"screen": "6.78 inch AMOLED 2400x1080 120Hz", "cpu": "Snapdragon 6 Gen 1", "ram": "8GB", "storage": "256GB", "battery": "5500mAh"}', 'https://via.placeholder.com/300x300/9c27b0/ffffff?text=V30e+5G', 1, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Realme C67 5G', N'Realme C67 5G với camera 50MP AI, màn hình 6.72 inch 90Hz, pin 5000mAh. Chip MediaTek Dimensity 6100+, hỗ trợ sạc nhanh 33W.', 4990000, 5990000, 120, 4.3, 1, 0, 17, '{"screen": "6.72 inch IPS LCD 2400x1080 90Hz", "cpu": "MediaTek Dimensity 6100+", "ram": "6GB", "storage": "128GB", "battery": "5000mAh"}', 'https://via.placeholder.com/300x300/00bcd4/ffffff?text=Realme+C67', 1, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Nokia G22', N'Nokia G22 với pin 5050mAh, camera 50MP, Android 13 thuần. Thiết kế bền bỉ, hỗ trợ sạc 18W và Quick Fix để tự sửa màn hình.', 3490000, 4290000, 45, 4.2, 0, 0, 19, '{"screen": "6.52 inch IPS LCD 1600x720 90Hz", "cpu": "Unisoc T606", "ram": "4GB", "storage": "128GB", "battery": "5050mAh"}', 'https://via.placeholder.com/300x300/607d8b/ffffff?text=Nokia+G22', 1, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'POCO X6 Pro 5G', N'POCO X6 Pro 5G với chip Dimensity 8300 Ultra, camera 64MP có OIS, màn hình AMOLED 6.67 inch 120Hz. Sạc nhanh 67W, pin 5000mAh.', 10990000, 12990000, 52, 4.7, 1, 0, 15, '{"screen": "6.67 inch AMOLED 2712x1220 120Hz", "cpu": "Dimensity 8300 Ultra", "ram": "12GB", "storage": "512GB", "battery": "5000mAh"}', 'https://via.placeholder.com/300x300/ff9800/ffffff?text=POCO+X6+Pro', 1, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'iPhone 14 128GB', N'iPhone 14 với chip A15 Bionic, camera 12MP, màn hình Super Retina 6.1 inch. Hỗ trợ Crash Detection và SOS qua vệ tinh.', 16990000, 19990000, 35, 4.8, 0, 0, 15, '{"screen": "6.1 inch Super Retina XDR 2532x1170 60Hz", "cpu": "Apple A15 Bionic", "ram": "6GB", "storage": "128GB", "battery": "3279mAh"}', 'https://via.placeholder.com/300x300/333333/ffffff?text=iPhone+14', 1, 2);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Samsung Galaxy A35 5G', N'Samsung Galaxy A35 5G với màn hình Super AMOLED 6.6 inch, camera 50MP có OIS, pin 5000mAh. Chuẩn kháng nước IP67.', 7490000, 8990000, 68, 4.5, 1, 0, 17, '{"screen": "6.6 inch Super AMOLED 2340x1080 120Hz", "cpu": "Exynos 1380", "ram": "8GB", "storage": "128GB", "battery": "5000mAh"}', 'https://via.placeholder.com/300x300/1a237e/ffffff?text=Galaxy+A35+5G', 1, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'iPhone 13 128GB', N'iPhone 13 với chip A15 Bionic, camera kép 12MP, màn hình Super Retina 6.1 inch. Pin trâu hơn thế hệ trước 15%.', 14490000, 16990000, 28, 4.7, 0, 0, 15, '{"screen": "6.1 inch Super Retina XDR 2532x1170 60Hz", "cpu": "Apple A15 Bionic", "ram": "4GB", "storage": "128GB", "battery": "3240mAh"}', 'https://via.placeholder.com/300x300/333333/ffffff?text=iPhone+13', 1, 2);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Xiaomi 13T Pro', N'Xiaomi 13T Pro với camera Leica 50MP, chip Dimensity 9200+, màn hình AMOLED 6.67 inch 144Hz. Sạc nhanh 120W, pin 5000mAh.', 13990000, 15990000, 42, 4.8, 0, 0, 13, '{"screen": "6.67 inch AMOLED 2712x1220 144Hz", "cpu": "Dimensity 9200+", "ram": "12GB", "storage": "512GB", "battery": "5000mAh"}', 'https://via.placeholder.com/300x300/ff5722/ffffff?text=Xiaomi+13T+Pro', 1, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Samsung Galaxy Z Flip5', N'Samsung Galaxy Z Flip5 với màn hình gập linh hoạt, màn hình ngoài 3.4 inch, camera 12MP. Chip Snapdragon 8 Gen 2, pin 3700mAh.', 18990000, 22990000, 22, 4.6, 0, 0, 17, '{"screen": "6.7 inch Dynamic AMOLED 2X (gập) 3.4 inch Super AMOLED ngoài", "cpu": "Snapdragon 8 Gen 2", "ram": "8GB", "storage": "256GB", "battery": "3700mAh"}', 'https://via.placeholder.com/300x300/1a237e/ffffff?text=Z+Flip5', 1, 1);

-- Laptop (15 sản phẩm)
INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'MacBook Air M3 13 inch 256GB', N'MacBook Air M3 với chip Apple M3 8-core, 16-core Neural Engine, màn hình Liquid Retina 13.6 inch. Thời gian sử dụng lên đến 18 giờ.', 27990000, 29990000, 30, 4.9, 1, 0, 7, '{"screen": "13.6 inch Liquid Retina 2560x1664", "cpu": "Apple M3 8-core", "ram": "16GB", "storage": "256GB SSD", "battery": "52.6Wh (18 giờ)"}', 'https://via.placeholder.com/300x300/90a4ae/ffffff?text=MacBook+Air+M3', 2, 2);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'MacBook Pro 14 inch M3 Pro', N'MacBook Pro 14 inch với chip M3 Pro 11-core CPU, 14-core GPU, màn hình Liquid Retina XDR. Hỗ trợ đến 22 giờ sử dụng pin.', 49990000, 54990000, 15, 4.9, 1, 0, 9, '{"screen": "14.2 inch Liquid Retina XDR 3024x1964 ProMotion 120Hz", "cpu": "Apple M3 Pro 11-core", "ram": "18GB", "storage": "512GB SSD", "battery": "70Wh (22 giờ)"}', 'https://via.placeholder.com/300x300/90a4ae/ffffff?text=MacBook+Pro+14+M3+Pro', 2, 2);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Dell XPS 15 9530', N'Dell XPS 15 với Intel Core i7-13700H, RTX 4060 8GB, màn hình OLED 15.6 inch 3.5K. Thiết kế mỏng nhẹ, vỏ nhôm CNC cao cấp.', 45990000, 52990000, 12, 4.7, 1, 0, 13, '{"screen": "15.6 inch OLED 3.5K (3456x2160) 400 nit 100% DCI-P3", "cpu": "Intel Core i7-13700H", "ram": "32GB DDR5", "storage": "1TB SSD NVMe", "battery": "86Wh"}', 'https://via.placeholder.com/300x300/37474f/ffffff?text=Dell+XPS+15', 2, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'ASUS ROG Zephyrus G14', N'ASUS ROG Zephyrus G14 với AMD Ryzen 9 7940HS, RTX 4060 8GB, màn hình 14 inch QHD+ 165Hz. Thiết kế gaming mỏng nhẹ.', 42990000, 48990000, 18, 4.8, 1, 0, 12, '{"screen": "14 inch QHD+ (2560x1600) 165Hz DCI-P3 100%", "cpu": "AMD Ryzen 9 7940HS", "ram": "16GB DDR5", "storage": "1TB SSD NVMe", "battery": "76Wh"}', 'https://via.placeholder.com/300x300/263238/ffffff?text=ROG+Zephyrus+G14', 2, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Lenovo ThinkPad X1 Carbon Gen 11', N'Lenovo ThinkPad X1 Carbon Gen 11 với Intel Core i7-1365U, màn hình 14 inch 2.8K OLED. Trọng lượng chỉ 1.12kg, vỏ carbon siêu bền.', 39990000, 45990000, 20, 4.9, 1, 0, 13, '{"screen": "14 inch 2.8K OLED (2880x1800) 100% DCI-P3", "cpu": "Intel Core i7-1365U vPro", "ram": "32GB LPDDR5", "storage": "512GB SSD NVMe", "battery": "57Wh"}', 'https://via.placeholder.com/300x300/212121/ffffff?text=ThinkPad+X1+Carbon', 2, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'HP Pavilion Plus 14', N'HP Pavilion Plus 14 với Intel Core i5-13500H, OLED 2.8K 14 inch, thiết kế mỏng nhẹ. Camera 5MP với Windows Studio Effects.', 22990000, 26990000, 35, 4.6, 1, 0, 15, '{"screen": "14 inch 2.8K OLED (2880x1800) 500 nit", "cpu": "Intel Core i5-13500H", "ram": "16GB DDR5", "storage": "512GB SSD NVMe", "battery": "68Wh"}', 'https://via.placeholder.com/300x300/0d47a1/ffffff?text=HP+Pavilion+Plus+14', 2, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Acer Swift Go 14', N'Acer Swift Go 14 với Intel Core i5-13500H, màn hình OLED 14 inch 2.8K, trọng lượng 1.3kg. Hỗ trợ AI và các tính năng thông minh.', 19990000, 23990000, 42, 4.5, 1, 0, 17, '{"screen": "14 inch 2.8K OLED (2880x1800) 500 nit", "cpu": "Intel Core i5-13500H", "ram": "16GB LPDDR5", "storage": "512GB SSD NVMe", "battery": "65Wh"}', 'https://via.placeholder.com/300x300/006064/ffffff?text=Acer+Swift+Go+14', 2, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'MSI Modern 15 H', N'MSI Modern 15 H với Intel Core i5-13420H, RTX 2050 4GB, màn hình 15.6 inch FHD. Phù hợp cho học tập và làm việc văn phòng.', 16990000, 19990000, 48, 4.4, 1, 0, 15, '{"screen": "15.6 inch FHD (1920x1080) IPS 60Hz", "cpu": "Intel Core i5-13420H", "ram": "16GB DDR5", "storage": "512GB SSD NVMe", "battery": "52Wh"}', 'https://via.placeholder.com/300x300/880e4f/ffffff?text=MSI+Modern+15+H', 2, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Microsoft Surface Laptop 5', N'Microsoft Surface Laptop 5 với Intel Core i7-1265U, màn hình PixelSense 13.5 inch, vỏ nhôm cao cấp. Hỗ trợ Surface Pen và Surface Dial.', 28990000, 32990000, 25, 4.6, 0, 0, 12, '{"screen": "13.5 inch PixelSense (2256x1504) 201 PPI", "cpu": "Intel Core i7-1265U", "ram": "16GB LPDDR5x", "storage": "512GB SSD", "battery": "47.4Wh (18 giờ)"}', 'https://via.placeholder.com/300x300/00acc1/ffffff?text=Surface+Laptop+5', 2, 2);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'ASUS ZenBook 14 OLED', N'ASUS ZenBook 14 OLED với Intel Core Ultra 5, màn hình 14 inch 2.8K OLED, trọng lượng 1.2kg. Hỗ trợ AI và các tính năng thông minh.', 24990000, 28990000, 33, 4.7, 1, 0, 14, '{"screen": "14 inch 2.8K OLED (2880x1800) 100% DCI-P3", "cpu": "Intel Core Ultra 5 125H", "ram": "16GB LPDDR5", "storage": "512GB SSD NVMe", "battery": "75Wh"}', 'https://via.placeholder.com/300x300/1b5e20/ffffff?text=ZenBook+14+OLED', 2, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Dell Inspiron 15 3530', N'Dell Inspiron 15 3530 với Intel Core i5-1335U, màn hình 15.6 inch FHD IPS, phù hợp cho học tập và làm việc văn phòng.', 15990000, 18990000, 55, 4.4, 0, 0, 16, '{"screen": "15.6 inch FHD (1920x1080) IPS 120Hz", "cpu": "Intel Core i5-1335U", "ram": "8GB DDR4", "storage": "512GB SSD NVMe", "battery": "54Wh"}', 'https://via.placeholder.com/300x300/37474f/ffffff?text=Inspiron+15+3530', 2, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'HP Victus 15', N'HP Victus 15 với AMD Ryzen 5 7535HS, RTX 2050 4GB, màn hình 15.6 inch FHD 144Hz. Thiết kế gaming hiện đại, tản nhiệt hiệu quả.', 21990000, 25990000, 40, 4.5, 0, 0, 15, '{"screen": "15.6 inch FHD (1920x1080) 144Hz", "cpu": "AMD Ryzen 5 7535HS", "ram": "8GB DDR5", "storage": "512GB SSD NVMe", "battery": "70Wh"}', 'https://via.placeholder.com/300x300/0d47a1/ffffff?text=HP+Victus+15', 2, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Lenovo IdeaPad Gaming 3', N'Lenovo IdeaPad Gaming 3 với AMD Ryzen 5 5600H, GTX 1650 4GB, màn hình 15.6 inch FHD 120Hz. Hiệu năng tốt cho game và đồ họa.', 19990000, 23990000, 38, 4.4, 0, 0, 17, '{"screen": "15.6 inch FHD (1920x1080) 120Hz", "cpu": "AMD Ryzen 5 5600H", "ram": "8GB DDR4", "storage": "512GB SSD NVMe", "battery": "45Wh"}', 'https://via.placeholder.com/300x300/212121/ffffff?text=IdeaPad+Gaming+3', 2, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'ASUS VivoBook 15', N'ASUS VivoBook 15 với Intel Core i5-1235U, màn hình 15.6 inch FHD, thiết kế mỏng nhẹ. Bàn phím số riêng, phù hợp văn phòng.', 13990000, 16990000, 62, 4.3, 0, 0, 18, '{"screen": "15.6 inch FHD (1920x1080) IPS", "cpu": "Intel Core i5-1235U", "ram": "8GB DDR4", "storage": "512GB SSD NVMe", "battery": "42Wh"}', 'https://via.placeholder.com/300x300/263238/ffffff?text=VivoBook+15', 2, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Acer Aspire 5 A515', N'Acer Aspire 5 A515 với Intel Core i5-12450H, màn hình 15.6 inch FHD IPS, thiết kế kim loại sang trọng. Tản nhiệt quạt kép.', 14990000, 17990000, 58, 4.4, 0, 0, 17, '{"screen": "15.6 inch FHD (1920x1080) IPS", "cpu": "Intel Core i5-12450H", "ram": "8GB DDR4", "storage": "512GB SSD NVMe", "battery": "50Wh"}', 'https://via.placeholder.com/300x300/006064/ffffff?text=Acer+Aspire+5', 2, 1);

-- Tablet (10 sản phẩm)
INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'iPad Pro 12.9 inch M2 WiFi 256GB', N'iPad Pro 12.9 inch M2 với chip M2 8-core, Liquid Retina XDR, hỗ trợ Apple Pencil 2 và Magic Keyboard. Camera sau 12MP, Face ID.', 29990000, 33990000, 22, 4.9, 1, 0, 12, '{"screen": "12.9 inch Liquid Retina XDR 2732x2048 ProMotion 120Hz", "cpu": "Apple M2 8-core", "ram": "8GB", "storage": "256GB", "battery": "40.88Wh (10 giờ)"}', 'https://via.placeholder.com/300x300/37474f/ffffff?text=iPad+Pro+12.9', 3, 2);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'iPad Air M2 WiFi 256GB', N'iPad Air M2 với chip M2, màn hình Liquid Retina 10.9 inch, hỗ trợ Apple Pencil Pro. Thiết kế mỏng 6.1mm, trọng lượng 462g.', 19990000, 22990000, 35, 4.8, 1, 0, 13, '{"screen": "10.9 inch Liquid Retina 2360x1640 60Hz", "cpu": "Apple M2 8-core", "ram": "8GB", "storage": "256GB", "battery": "28.65Wh (10 giờ)"}', 'https://via.placeholder.com/300x300/37474f/ffffff?text=iPad+Air+M2', 3, 2);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Samsung Galaxy Tab S9 FE', N'Samsung Galaxy Tab S9 FE với màn hình TFT 10.9 inch, S Pen tặng kèm, chuẩn IP68. Pin 8000mAh, hỗ trợ sạc nhanh 45W.', 11990000, 13990000, 45, 4.6, 1, 0, 14, '{"screen": "10.9 inch TFT (2304x1440) 90Hz", "cpu": "Exynos 1380", "ram": "6GB", "storage": "128GB", "battery": "8000mAh"}', 'https://via.placeholder.com/300x300/1565c0/ffffff?text=Tab+S9+FE', 3, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Samsung Galaxy Tab S9 Ultra', N'Samsung Galaxy Tab S9 Ultra với màn hình Dynamic AMOLED 2X 14.6 inch, S Pen tích hợp, chip Snapdragon 8 Gen 2. Hỗ trợ keyboard cover.', 31990000, 35990000, 18, 4.8, 1, 0, 11, '{"screen": "14.6 inch Dynamic AMOLED 2X (2960x1848) 120Hz", "cpu": "Snapdragon 8 Gen 2", "ram": "12GB", "storage": "256GB", "battery": "11200mAh"}', 'https://via.placeholder.com/300x300/1565c0/ffffff?text=Tab+S9+Ultra', 3, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'iPad mini 6 WiFi 256GB', N'iPad mini 6 với chip A15 Bionic, màn hình Liquid Retina 8.3 inch. Thiết kế nhỏ gọn, hỗ trợ Apple Pencil 2 và 5G.', 14990000, 16990000, 28, 4.7, 0, 0, 12, '{"screen": "8.3 inch Liquid Retina 2266x1488 60Hz", "cpu": "Apple A15 Bionic", "ram": "4GB", "storage": "256GB", "battery": "19.3Wh (10 giờ)"}', 'https://via.placeholder.com/300x300/37474f/ffffff?text=iPad+mini+6', 3, 2);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Xiaomi Pad 6', N'Xiaomi Pad 6 với chip Snapdragon 870, màn hình 11 inch 2.8K 144Hz, 4 loa Dolby Atmos. Pin 8840mAh, sạc nhanh 33W.', 9990000, 11990000, 52, 4.6, 1, 0, 17, '{"screen": "11 inch 2.8K IPS (2880x1800) 144Hz", "cpu": "Snapdragon 870", "ram": "8GB", "storage": "256GB", "battery": "8840mAh"}', 'https://via.placeholder.com/300x300/ff5722/ffffff?text=Xiaomi+Pad+6', 3, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'OPPO Pad Air2', N'OPPO Pad Air2 với màn hình LCD 11.35 inch 2.4K, 4 loa Dolby, pin 8000mAh. Hỗ trợ OPPO Pencil, thiết kế mỏng 6.9mm.', 6490000, 7990000, 38, 4.5, 0, 0, 19, '{"screen": "11.35 inch 2.4K LCD (2408x1720) 90Hz", "cpu": "MediaTek Helio G99", "ram": "8GB", "storage": "128GB", "battery": "8000mAh"}', 'https://via.placeholder.com/300x300/4caf50/ffffff?text=OPPO+Pad+Air2', 3, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Lenovo Tab P12', N'Lenovo Tab P12 với màn hình TDDI 12.7 inch 3K, 4 loa JBL Dolby Atmos, pin 10200mAh. Hỗ trợ bàn phím và bút stylus.', 9990000, 11990000, 30, 4.5, 0, 0, 17, '{"screen": "12.7 inch 3K TDDI (2944x1840) 60Hz", "cpu": "MediaTek Dimensity 7050", "ram": "8GB", "storage": "256GB", "battery": "10200mAh"}', 'https://via.placeholder.com/300x300/212121/ffffff?text=Lenovo+Tab+P12', 3, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Realme Pad X', N'Realme Pad X với chip Snapdragon 695, màn hình 10.95 inch 2K, 4 loa Dolby Atmos. Pin 8340mAh, hỗ trợ stylus.', 5990000, 7490000, 42, 4.4, 0, 0, 20, '{"screen": "10.95 inch 2K IPS (2000x1200) 60Hz", "cpu": "Snapdragon 695", "ram": "6GB", "storage": "128GB", "battery": "8340mAh"}', 'https://via.placeholder.com/300x300/00bcd4/ffffff?text=Realme+Pad+X', 3, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Huawei MatePad 11.5', N'Huawei MatePad 11.5 với màn hình FullView 11.5 inch 2K 120Hz, HUAWEI M-Pencil, 4 loa Harman Kardon. Chip Snapdragon 7 Gen 1.', 8990000, 10990000, 35, 4.5, 0, 0, 18, '{"screen": "11.5 inch 2K IPS (2200x1440) 120Hz", "cpu": "Snapdragon 7 Gen 1", "ram": "8GB", "storage": "256GB", "battery": "7700mAh"}', 'https://via.placeholder.com/300x300/f44336/ffffff?text=MatePad+11.5', 3, 1);

-- Phụ kiện (10 sản phẩm)
INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Tai nghe AirPods Pro 2 USB-C', N'AirPods Pro 2 với USB-C, chip H2, chống ồn chủ động, chế độ xuyên âm. Âm thanh không gian cá nhân hóa, pin 6 giờ.', 6990000, 7490000, 85, 4.8, 1, 0, 7, '{"screen": "N/A", "cpu": "Chip H2", "ram": "N/A", "storage": "N/A", "battery": "6 giờ (30 giờ với hộp sạc)"}', 'https://via.placeholder.com/300x300/37474f/ffffff?text=AirPods+Pro+2', 4, 2);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Tai nghe Sony WH-1000XM5', N'Sony WH-1000XM5 với chống ồn tốt nhất, driver 30mm, pin 30 giờ. Hỗ trợ LDAC, DSEE Extreme, đàm thoại rõ ràng với 4 mic.', 8990000, 10990000, 42, 4.9, 0, 0, 18, '{"screen": "N/A", "cpu": "Integrated Processor V1", "ram": "N/A", "storage": "N/A", "battery": "30 giờ"}', 'https://via.placeholder.com/300x300/212121/ffffff?text=Sony+WH-1000XM5', 4, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Sạc nhanh Anker 735 GaNPrime 65W', N'Anker 735 GaNPrime 65W với 3 cổng USB-C, GaN II thế hệ mới, sạc nhanh PD/PPS. Kích thước nhỏ gọn, an toàn với AI.', 1390000, 1690000, 120, 4.7, 1, 0, 18, '{"screen": "N/A", "cpu": "N/A", "ram": "N/A", "storage": "N/A", "battery": "N/A"}', 'https://via.placeholder.com/300x300/263238/ffffff?text=Anker+735+65W', 4, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Cáp sạc USB-C to Lightning 1m Apple', N'Cáp USB-C to Lightning Apple chính hãng dài 1m, hỗ trợ sạc nhanh 20W, truyền dữ liệu USB 2. Chất liệu bện dây bền bỉ.', 590000, 790000, 200, 4.6, 0, 0, 25, '{"screen": "N/A", "cpu": "N/A", "ram": "N/A", "storage": "N/A", "battery": "N/A"}', 'https://via.placeholder.com/300x300/37474f/ffffff?text=Cáp+USB-C+Lightning', 4, 2);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Chuột không dây Logitech MX Master 3S', N'Logitech MX Master 3S với cảm biến 8000 DPI, cuộn MagSpeed, kết nối Bluetooth/USB. Pin 70 ngày, hỗ trợ Flow đa thiết bị.', 3490000, 3990000, 65, 4.9, 1, 0, 13, '{"screen": "N/A", "cpu": "N/A", "ram": "N/A", "storage": "N/A", "battery": "70 ngày"}', 'https://via.placeholder.com/300x300/546e7a/ffffff?text=MX+Master+3S', 4, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Bàn phím cơ Keychron K3 Pro', N'Keychron K3 Pro bàn phím 75% low-profile, switch Gateron, kết nối Bluetooth/USB-C. Hỗ trố RGB, tương thích Mac/Win.', 2190000, 2590000, 48, 4.7, 1, 0, 15, '{"screen": "N/A", "cpu": "N/A", "ram": "N/A", "storage": "N/A", "battery": "N/A"}', 'https://via.placeholder.com/300x300/455a64/ffffff?text=Keychron+K3+Pro', 4, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Ổ cứng SSD Samsung T7 1TB', N'Samsung T7 1TB SSD di động với tốc độ đọc 1050MB/s, kết nối USB-C 3.2 Gen 2. Vỏ kim loại, chống rơi 2m, mã hóa AES 256-bit.', 2490000, 2990000, 72, 4.8, 0, 0, 17, '{"screen": "N/A", "cpu": "N/A", "ram": "N/A", "storage": "1TB", "battery": "N/A"}', 'https://via.placeholder.com/300x300/78909c/ffffff?text=Samsung+T7+1TB', 4, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Hub chuyển đổi HyperDrive Gen2 7-in-1', N'HyperDrive Gen2 7-in-1 hub USB-C với HDMI 4K@60Hz, 2x USB-A, USB-C PD 100W, thẻ nhớ SD/microSD, Ethernet gigabit.', 1890000, 2290000, 55, 4.6, 1, 0, 17, '{"screen": "N/A", "cpu": "N/A", "ram": "N/A", "storage": "N/A", "battery": "N/A"}', 'https://via.placeholder.com/300x300/90a4ae/ffffff?text=HyperDrive+Gen2', 4, 2);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Balo laptop Targus Newport 15.6 inch', N'Targus Newport backpack cho laptop 15.6 inch, ngăn chống sốc, cổng USB-C sạc ngoài. Chất liệu vải nylon chống nước.', 1690000, 2190000, 85, 4.5, 0, 0, 23, '{"screen": "N/A", "cpu": "N/A", "ram": "N/A", "storage": "N/A", "battery": "N/A"}', 'https://via.placeholder.com/300x300/37474f/ffffff?text=Targus+Newport', 4, 1);

INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id)
VALUES (N'Miếng dán kính cường lực iPhone 15 Pro', N'Miếng dán kính cường lực cho iPhone 15 Pro, độ cứng 9H, trong suốt, chống trầy. Dễ lắp đặt, cảm ứng nhạy.', 290000, 490000, 250, 4.3, 1, 0, 41, '{"screen": "N/A", "cpu": "N/A", "ram": "N/A", "storage": "N/A", "battery": "N/A"}', 'https://via.placeholder.com/300x300/546e7a/ffffff?text=Dán+Kính+iPhone+15+Pro', 4, 2);

GO

-- Inventory - Note: stock_quantity is now stored in Products table
-- This section kept for compatibility with existing records
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (1, 45);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (2, 38);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (3, 62);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (4, 75);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (5, 88);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (6, 54);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (7, 67);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (8, 120);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (9, 45);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (10, 52);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (11, 35);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (12, 68);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (13, 28);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (14, 42);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (15, 22);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (16, 30);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (17, 15);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (18, 12);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (19, 18);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (20, 20);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (21, 35);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (22, 42);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (23, 48);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (24, 25);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (25, 33);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (26, 55);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (27, 40);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (28, 38);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (29, 62);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (30, 58);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (31, 22);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (32, 35);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (33, 45);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (34, 18);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (35, 28);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (36, 52);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (37, 38);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (38, 30);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (39, 42);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (40, 35);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (41, 85);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (42, 42);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (43, 120);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (44, 200);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (45, 65);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (46, 48);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (47, 72);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (48, 55);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (49, 85);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (50, 250);
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
PRINT 'Sample data inserted: 50 products (15 Điện thoại, 15 Laptop, 10 Tablet, 10 Phụ kiện), 3 orders, 3 order items, inventory.';
GO
