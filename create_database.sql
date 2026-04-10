-- Create Database
CREATE DATABASE TechShopWebsite1;
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
    FOREIGN KEY (account_id) REFERENCES Accounts(account_id)
);
GO

-- Create OrderDetails Table (OrderItem)
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

-- Insert Sample Data (Optional)
-- Categories
INSERT INTO Categories (name) VALUES (N'Điện thoại');
INSERT INTO Categories (name) VALUES (N'Laptop');
INSERT INTO Categories (name) VALUES (N'Tablet');
INSERT INTO Categories (name) VALUES (N'Phụ kiện');
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
VALUES (N'admin', '$2a$11$nOuq6HfBz.K7xrK2iqWIuOqBCNCpEPXk1lmQsAWKKAp8JdhM7RKCW', 'admin@techshop.vn', N'Quản trị viên', '0917111111', N'TP. Hồ Chí Minh', 'Admin');
INSERT INTO Accounts (username, password_hash, email, full_name, phone, address, role) 
VALUES (N'employee1', '$2a$11$nOuq6HfBz.K7xrK2iqWIuOqBCNCpEPXk1lmQsAWKKAp8JdhM7RKCW', 'employee@techshop.vn', N'Nhân viên 1', '0918222222', N'Hà Nội', 'Employee');
INSERT INTO Accounts (username, password_hash, email, full_name, phone, address, role) 
VALUES (N'customer1', '$2a$11$nOuq6HfBz.K7xrK2iqWIuOqBCNCpEPXk1lmQsAWKKAp8JdhM7RKCW', 'customer@email.com', N'Khách hàng 1', '0919333333', N'TP. Hồ Chí Minh', 'Customer');
GO

-- Employees
INSERT INTO Employees (account_id, employee_code, position, department) 
VALUES (1, 'ADM001', N'Quản trị viên hệ thống', N'Quản trị');
INSERT INTO Employees (account_id, employee_code, position, department) 
VALUES (2, 'EMP001', N'Nhân viên bán hàng', N'Bán hàng');
GO

-- Products
INSERT INTO Products (name, description, price, category_id, supplier_id) 
VALUES (N'iPhone 15', N'Điện thoại thông minh Apple', CAST(29990000 AS DECIMAL(10, 2)), 1, 2);
INSERT INTO Products (name, description, price, category_id, supplier_id) 
VALUES (N'Samsung Galaxy S24', N'Điện thoại thông minh Samsung', CAST(24990000 AS DECIMAL(10, 2)), 1, 1);
INSERT INTO Products (name, description, price, category_id, supplier_id) 
VALUES (N'MacBook Pro 16', N'Laptop cao cấp của Apple', CAST(59990000 AS DECIMAL(10, 2)), 2, 2);
INSERT INTO Products (name, description, price, category_id, supplier_id) 
VALUES (N'OLED Tablet Pro', N'Máy tính bảng công nghệ OLED', CAST(12990000 AS DECIMAL(10, 2)), 3, 1);
GO

-- Inventory
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (1, 50);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (2, 75);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (3, 20);
INSERT INTO Inventory (product_id, quantity_in_stock) VALUES (4, 100);
GO

-- Orders
INSERT INTO Orders (account_id, total_amount, status) 
VALUES (3, CAST(29990000 AS DECIMAL(10, 2)), 'Pending');
INSERT INTO Orders (account_id, total_amount, status) 
VALUES (3, CAST(49980000 AS DECIMAL(10, 2)), 'Completed');
GO

-- OrderDetails
INSERT INTO OrderDetails (OrderID, ProductID, Quantity, Price) 
VALUES (1, 1, 1, CAST(29990000 AS DECIMAL(18, 2)));
INSERT INTO OrderDetails (OrderID, ProductID, Quantity, Price) 
VALUES (2, 2, 2, CAST(24990000 AS DECIMAL(18, 2)));
GO

-- Cart Items
INSERT INTO Cart_Items (account_id, product_id, quantity) 
VALUES (3, 3, 1);
GO

-- Receipts_Shipments
INSERT INTO Receipts_Shipments (product_id, movement_type, quantity, related_order_id) 
VALUES (1, 'Out', 1, 1);
INSERT INTO Receipts_Shipments (product_id, movement_type, quantity, related_order_id) 
VALUES (2, 'Out', 2, 2);
GO

-- Verify
PRINT 'Database created successfully!';
PRINT 'Tables created: Categories, Suppliers, Accounts, Employees, Products, Inventory, Orders, OrderDetails, Cart_Items, Receipts_Shipments';
PRINT 'Sample data inserted.';
GO
