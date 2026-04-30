-- =====================================================
-- TechShopWebsite1 - Full Database Setup Script
-- Merged: init_database + Update_IsAvailable + Fix_Duplicate_Emails
-- =====================================================

-- =====================================================
-- MIGRATION FIX: Make Orders.account_id nullable (guest checkout)
-- Safe to re-run on existing databases
-- =====================================================
BEGIN TRY
    DECLARE @fkName NVARCHAR(128);
    SELECT @fkName = fk.name
    FROM sys.foreign_keys fk
    INNER JOIN sys.tables t ON fk.parent_object_id = t.object_id
    WHERE t.name = 'Orders' AND fk.parent_object_id = OBJECT_ID('Orders');

    IF @fkName IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE Orders DROP CONSTRAINT [' + @fkName + ']');
        PRINT 'Dropped FK constraint on Orders.account_id';
    END

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'account_id'
               AND is_nullable = 0)
    BEGIN
        ALTER TABLE Orders ALTER COLUMN account_id INT NULL;
        PRINT 'Made Orders.account_id nullable';
    END
END TRY
BEGIN CATCH
    PRINT 'Migration note: ' + ERROR_MESSAGE();
END CATCH
GO

-- =====================================================
-- CREATE DATABASE
-- =====================================================
IF DB_ID(N'TechShopWebsite1') IS NULL
BEGIN
    CREATE DATABASE TechShopWebsite1;
END
GO

USE TechShopWebsite1;
GO

-- =====================================================
-- CREATE TABLES
-- =====================================================

-- Categories Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
BEGIN
    CREATE TABLE Categories (
        category_id INT PRIMARY KEY IDENTITY(1,1),
        name NVARCHAR(100) NOT NULL UNIQUE
    );
    PRINT 'Created Categories table';
END
ELSE
    PRINT 'Categories table already exists';
GO

-- Suppliers Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Suppliers')
BEGIN
    CREATE TABLE Suppliers (
        supplier_id INT PRIMARY KEY IDENTITY(1,1),
        name NVARCHAR(255) NOT NULL,
        contact_person NVARCHAR(100),
        phone NVARCHAR(20),
        email NVARCHAR(100),
        address NVARCHAR(255)
    );
    PRINT 'Created Suppliers table';
END
ELSE
    PRINT 'Suppliers table already exists';
GO

-- Accounts Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Accounts')
BEGIN
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
    PRINT 'Created Accounts table';
END
ELSE
    PRINT 'Accounts table already exists';
GO

-- Add password reset columns to Accounts table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Object_ID = Object_ID('Accounts') AND name = 'reset_token')
BEGIN
    ALTER TABLE Accounts ADD reset_token NVARCHAR(64) NULL;
    PRINT 'Added reset_token column';
END
ELSE
    PRINT 'reset_token column already exists';
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Object_ID = Object_ID('Accounts') AND name = 'reset_token_expiry')
BEGIN
    ALTER TABLE Accounts ADD reset_token_expiry DATETIME NULL;
    PRINT 'Added reset_token_expiry column';
END
ELSE
    PRINT 'reset_token_expiry column already exists';
GO

-- Employees Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Employees')
BEGIN
    CREATE TABLE Employees (
        employee_id INT PRIMARY KEY IDENTITY(1,1),
        account_id INT UNIQUE,
        employee_code NVARCHAR(10) NOT NULL UNIQUE,
        position NVARCHAR(50),
        department NVARCHAR(50),
        FOREIGN KEY (account_id) REFERENCES Accounts(account_id)
    );
    PRINT 'Created Employees table';
END
ELSE
    PRINT 'Employees table already exists';
GO

-- Products Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
BEGIN
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
    PRINT 'Created Products table';
END
ELSE
    PRINT 'Products table already exists';
GO

-- Add is_available column (if not exists - from Update_IsAvailable.sql)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Object_ID = Object_ID('Products') AND name = 'is_available')
BEGIN
    ALTER TABLE Products ADD is_available BIT NOT NULL DEFAULT 1;
    PRINT 'Added is_available column to Products';
END
ELSE
    PRINT 'is_available column already exists';
GO

-- ProductVariants Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductVariants')
BEGIN
    CREATE TABLE ProductVariants (
        variant_id INT PRIMARY KEY IDENTITY(1,1),
        product_id INT NOT NULL,
        color NVARCHAR(50),
        storage NVARCHAR(20),
        ram NVARCHAR(20),
        variant_name NVARCHAR(100),
        price DECIMAL(10, 2) NOT NULL,
        original_price DECIMAL(10, 2),
        stock_quantity INT NOT NULL DEFAULT 0,
        display_order INT NOT NULL DEFAULT 0,
        FOREIGN KEY (product_id) REFERENCES Products(product_id) ON DELETE CASCADE
    );
    PRINT 'Created ProductVariants table';
END
ELSE
    PRINT 'ProductVariants table already exists';
GO

-- Inventory Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Inventory')
BEGIN
    CREATE TABLE Inventory (
        inventory_id INT PRIMARY KEY IDENTITY(1,1),
        product_id INT NOT NULL UNIQUE,
        quantity_in_stock INT NOT NULL DEFAULT 0,
        last_updated_date DATETIME NOT NULL DEFAULT GETDATE(),
        FOREIGN KEY (product_id) REFERENCES Products(product_id)
    );
    PRINT 'Created Inventory table';
END
ELSE
    PRINT 'Inventory table already exists';
GO

-- Orders Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
BEGIN
    CREATE TABLE Orders (
        order_id INT PRIMARY KEY IDENTITY(1,1),
        account_id INT NULL,
        order_date DATETIME NOT NULL DEFAULT GETDATE(),
        total_amount DECIMAL(10, 2) NOT NULL,
        status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
        customer_name NVARCHAR(100),
        customer_phone NVARCHAR(20),
        customer_address NVARCHAR(255),
        notes NVARCHAR(500)
    );
    PRINT 'Created Orders table';
END
ELSE
    PRINT 'Orders table already exists';
GO

-- OrderDetails Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderDetails')
BEGIN
    CREATE TABLE OrderDetails (
        OrderID INT NOT NULL,
        ProductID INT NOT NULL,
        VariantID INT NULL,
        Quantity INT NOT NULL,
        Price DECIMAL(18, 2) NOT NULL,
        PRIMARY KEY (OrderID, ProductID),
        FOREIGN KEY (OrderID) REFERENCES Orders(order_id),
        FOREIGN KEY (ProductID) REFERENCES Products(product_id),
        FOREIGN KEY (VariantID) REFERENCES ProductVariants(variant_id)
    );
    PRINT 'Created OrderDetails table';
END
ELSE
    PRINT 'OrderDetails table already exists';
GO

-- Cart_Items Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Cart_Items')
BEGIN
    CREATE TABLE Cart_Items (
        cart_item_id INT PRIMARY KEY IDENTITY(1,1),
        account_id INT,
        product_id INT NOT NULL,
        variant_id INT,
        quantity INT NOT NULL,
        added_date DATETIME NOT NULL DEFAULT GETDATE(),
        UNIQUE (account_id, product_id, variant_id),
        FOREIGN KEY (account_id) REFERENCES Accounts(account_id),
        FOREIGN KEY (product_id) REFERENCES Products(product_id),
        FOREIGN KEY (variant_id) REFERENCES ProductVariants(variant_id)
    );
    PRINT 'Created Cart_Items table';
END
ELSE
    PRINT 'Cart_Items table already exists';
GO

-- AIConversationLogs Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AIConversationLogs')
BEGIN
    CREATE TABLE [AIConversationLogs] (
        [log_id] int NOT NULL IDENTITY,
        [session_id] uniqueidentifier NOT NULL,
        [user_message] nvarchar(max) NOT NULL,
        [ai_response] nvarchar(max) NOT NULL,
        [intent_detected] nvarchar(50) NULL,
        [confidence_score] decimal(18,2) NULL,
        [was_escalated] bit NOT NULL,
        [user_rating] int NULL,
        [created_at] datetime2 NOT NULL,
        CONSTRAINT [PK_AIConversationLogs] PRIMARY KEY ([log_id])
    );
    PRINT 'Created AIConversationLogs table';
END
ELSE
    PRINT 'AIConversationLogs table already exists';
GO

-- FAQs Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FAQs')
BEGIN
    CREATE TABLE [FAQs] (
        [faq_id] int NOT NULL IDENTITY,
        [question] nvarchar(max) NOT NULL,
        [answer] nvarchar(max) NOT NULL,
        [category] nvarchar(50) NOT NULL,
        [keywords] nvarchar(max) NULL,
        [priority] int NOT NULL,
        [is_active] bit NOT NULL,
        [created_at] datetime2 NOT NULL,
        CONSTRAINT [PK_FAQs] PRIMARY KEY ([faq_id])
    );
    PRINT 'Created FAQs table';
END
ELSE
    PRINT 'FAQs table already exists';
GO

-- Knowledge Chunks Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KnowledgeChunks')
BEGIN
    CREATE TABLE [KnowledgeChunks] (
        [chunk_id] int NOT NULL IDENTITY,
        [source_type] nvarchar(20) NOT NULL,
        [source_id] int NOT NULL,
        [chunk_type] nvarchar(30) NOT NULL,
        [raw_text] nvarchar(max) NOT NULL,
        [normalized_text] nvarchar(max) NOT NULL,
        [embedding] nvarchar(max) NOT NULL,
        [price] decimal(10,2) NULL,
        [category] nvarchar(100) NULL,
        [priority] int NOT NULL,
        [created_at] datetime2 NOT NULL,
        CONSTRAINT [PK_KnowledgeChunks] PRIMARY KEY ([chunk_id])
    );
    PRINT 'Created KnowledgeChunks table';
END
ELSE
    PRINT 'KnowledgeChunks table already exists';
GO

-- ChatSessions Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatSessions')
BEGIN
    CREATE TABLE [ChatSessions] (
        [session_id] uniqueidentifier NOT NULL,
        [account_id] int NULL,
        [status] nvarchar(20) NOT NULL,
        [assigned_to] int NULL,
        [started_at] datetime2 NOT NULL,
        [ended_at] datetime2 NULL,
        CONSTRAINT [PK_ChatSessions] PRIMARY KEY ([session_id]),
        CONSTRAINT [FK_ChatSessions_Accounts_account_id] FOREIGN KEY ([account_id]) REFERENCES [Accounts] ([account_id])
    );
    PRINT 'Created ChatSessions table';
END
ELSE
    PRINT 'ChatSessions table already exists';
GO

-- Notifications Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
BEGIN
    CREATE TABLE [Notifications] (
        [notification_id] int NOT NULL IDENTITY,
        [account_id] int NULL,
        [type] nvarchar(50) NOT NULL,
        [message] nvarchar(max) NOT NULL,
        [is_read] bit NOT NULL,
        [link] nvarchar(255) NULL,
        [created_at] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([notification_id]),
        CONSTRAINT [FK_Notifications_Accounts_account_id] FOREIGN KEY ([account_id]) REFERENCES [Accounts] ([account_id])
    );
    PRINT 'Created Notifications table';
END
ELSE
    PRINT 'Notifications table already exists';
GO

-- ChatMessages Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatMessages')
BEGIN
    CREATE TABLE [ChatMessages] (
        [message_id] int NOT NULL IDENTITY,
        [session_id] uniqueidentifier NOT NULL,
        [message] nvarchar(max) NOT NULL,
        [sender_type] nvarchar(10) NOT NULL,
        [created_at] datetime2 NOT NULL,
        [metadata] nvarchar(max) NULL,
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([message_id]),
        CONSTRAINT [FK_ChatMessages_ChatSessions_session_id] FOREIGN KEY ([session_id]) REFERENCES [ChatSessions] ([session_id]) ON DELETE CASCADE
    );
    PRINT 'Created ChatMessages table';
END
ELSE
    PRINT 'ChatMessages table already exists';
GO

-- =====================================================
-- SAMPLE DATA
-- =====================================================

-- Categories (skip if exists)
IF NOT EXISTS (SELECT * FROM Categories)
BEGIN
    INSERT INTO Categories (name) VALUES (N'Điện thoại');
    INSERT INTO Categories (name) VALUES (N'Laptop');
    INSERT INTO Categories (name) VALUES (N'Tablet');
    INSERT INTO Categories (name) VALUES (N'Phụ kiện');
    PRINT 'Inserted Categories data';
END
ELSE
    PRINT 'Categories already has data';
GO

-- Suppliers (skip if exists)
IF NOT EXISTS (SELECT * FROM Suppliers)
BEGIN
    INSERT INTO Suppliers (name, contact_person, phone, email, address)
    VALUES (N'Samsung Việt Nam', N'Nguyễn Văn A', '0912345678', 'contact@samsung.vn', N'TP. Hồ Chí Minh'),
           (N'Apple Vietnam', N'Trần Thị B', '0987654321', 'contact@apple.vn', N'Hà Nội');
    PRINT 'Inserted Suppliers data';
END
ELSE
    PRINT 'Suppliers already has data';
GO

-- Accounts (skip if exists)
IF NOT EXISTS (SELECT * FROM Accounts)
BEGIN
    INSERT INTO Accounts (username, password_hash, email, full_name, phone, address, role)
    VALUES (N'admin', 'admin123', 'admin@techshop.vn', N'Quản trị viên', '0917111111', N'TP. Hồ Chí Minh', 'Admin'),
           (N'customer1', 'customer123', 'customer@email.com', N'Khách hàng 1', '0919333333', N'TP. Hồ Chí Minh', 'Customer');
    PRINT 'Inserted Accounts data';
END
ELSE
    PRINT 'Accounts already has data';
GO

-- Products (skip if exists)
IF NOT EXISTS (SELECT * FROM Products)
BEGIN
    INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id, is_available)
    VALUES
    (N'iPhone 15 Pro Max', N'iPhone 15 Pro Max với chip A17 Pro, thiết kế titan cao cấp.', 32990000, 34990000, 100, 4.9, 1, 1, 6, '{"cpu": "A17 Pro", "screen": "6.7 inch"}', '/images/products/iPhone_15_Pro_Max.png', 1, 2, 1),
    (N'Samsung Galaxy S24 Ultra', N'Samsung Galaxy S24 Ultra với AI tiên tiến và bút S Pen.', 28990000, 31990000, 80, 4.8, 1, 1, 9, '{"cpu": "Snapdragon 8 Gen 3", "screen": "6.8 inch"}', '/images/products/Galaxy_S24_Ultra.png', 1, 1, 1),
    (N'MacBook Air M3', N'Laptop siêu mỏng nhẹ với chip M3 cực mạnh.', 27990000, 29990000, 50, 4.9, 1, 1, 7, '{"cpu": "Apple M3", "ram": "8GB"}', '/images/products/MacBook_Air_M3.png', 2, 2, 1);
    PRINT 'Inserted Products data';
END
ELSE
    PRINT 'Products already has data';
GO

-- Product Variants (skip if exists)
IF NOT EXISTS (SELECT * FROM ProductVariants)
BEGIN
    INSERT INTO ProductVariants (product_id, color, storage, ram, variant_name, price, original_price, stock_quantity, display_order)
    VALUES
    (1, N'Titan Tự Nhiên', '256GB', '8GB', N'Titan Tự Nhiên / 256GB', 32990000, 34990000, 20, 1),
    (1, N'Titan Tự Nhiên', '512GB', '8GB', N'Titan Tự Nhiên / 512GB', 37990000, 39990000, 15, 2),
    (1, N'Titan Xanh', '256GB', '8GB', N'Titan Xanh / 256GB', 32990000, 34990000, 25, 3),
    (2, N'Xám Titanium', '256GB', '12GB', N'Xám Titanium / 256GB', 28990000, 31990000, 30, 1),
    (2, N'Đen Titanium', '512GB', '12GB', N'Đen Titanium / 512GB', 33990000, 36990000, 20, 2),
    (3, N'Silver', '256GB', '8GB', N'Silver / 256GB / 8GB RAM', 27990000, 29990000, 15, 1),
    (3, N'Space Gray', '512GB', '16GB', N'Space Gray / 512GB / 16GB RAM', 35990000, 38990000, 10, 2);
    PRINT 'Inserted ProductVariants data';
END
ELSE
    PRINT 'ProductVariants table already has data';
GO

-- =====================================================
-- DATA FIXES (from Fix_Duplicate_Emails.sql)
-- =====================================================

-- Normalize all emails to lowercase to prevent duplicate registration errors
UPDATE [dbo].[Accounts]
SET [Email] = LOWER(LTRIM(RTRIM([Email])))
WHERE [Email] IS NOT NULL;

PRINT 'Normalized all emails to lowercase';

-- =====================================================
-- DONE
-- =====================================================
PRINT '';
PRINT '==========================================';
PRINT 'Database setup complete!';
PRINT '==========================================';
PRINT '';
