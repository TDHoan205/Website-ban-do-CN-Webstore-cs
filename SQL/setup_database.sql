-- =====================================================
-- TechShopWebsite1 - Full Database Setup Script
-- Fixed: ProductImages, Carts, OrderDetails PK, Cart_Items sentinel
-- =====================================================

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
-- MIGRATION: Make Orders.account_id nullable (guest checkout)
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
-- Categories Table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
BEGIN
    CREATE TABLE Categories (
        category_id INT PRIMARY KEY IDENTITY(1,1),
        name NVARCHAR(100) NOT NULL UNIQUE
    );
    PRINT 'Created Categories table';
END
GO

-- =====================================================
-- Suppliers Table
-- =====================================================
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
GO

-- =====================================================
-- Accounts Table
-- =====================================================
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
        role NVARCHAR(20) NOT NULL DEFAULT 'Customer',
        role_id INT NULL,
        reset_token NVARCHAR(64) NULL,
        reset_token_expiry DATETIME NULL
    );
    PRINT 'Created Accounts table';
END
ELSE
BEGIN
    -- Add missing columns for migration
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Object_ID = Object_ID('Accounts') AND name = 'role_id')
        ALTER TABLE Accounts ADD role_id INT NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Object_ID = Object_ID('Accounts') AND name = 'reset_token')
        ALTER TABLE Accounts ADD reset_token NVARCHAR(64) NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Object_ID = Object_ID('Accounts') AND name = 'reset_token_expiry')
        ALTER TABLE Accounts ADD reset_token_expiry DATETIME NULL;
    PRINT 'Updated Accounts table with missing columns';
END
GO

-- =====================================================
-- Roles Table (NEW - for role management)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE Roles (
        role_id INT PRIMARY KEY IDENTITY(1,1),
        role_name NVARCHAR(30) NOT NULL
    );
    CREATE UNIQUE INDEX IX_Roles_RoleName ON Roles(role_name);
    INSERT INTO Roles (role_name) VALUES (N'Admin'), (N'Customer'), (N'Employee');
    PRINT 'Created Roles table';
END
GO

-- =====================================================
-- Employees Table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Employees')
BEGIN
    CREATE TABLE Employees (
        employee_id INT PRIMARY KEY IDENTITY(1,1),
        account_id INT UNIQUE,
        employee_code NVARCHAR(10) NOT NULL UNIQUE,
        position NVARCHAR(50),
        department NVARCHAR(50)
    );
    PRINT 'Created Employees table';
END
GO

-- =====================================================
-- Products Table
-- =====================================================
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
        is_available BIT NOT NULL DEFAULT 1,
        FOREIGN KEY (category_id) REFERENCES Categories(category_id),
        FOREIGN KEY (supplier_id) REFERENCES Suppliers(supplier_id)
    );
    PRINT 'Created Products table';
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Object_ID = Object_ID('Products') AND name = 'is_available')
        ALTER TABLE Products ADD is_available BIT NOT NULL DEFAULT 1;
    PRINT 'Updated Products table with is_available column';
END
GO

-- =====================================================
-- ProductVariants Table
-- =====================================================
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
        sku NVARCHAR(50) NULL,
        FOREIGN KEY (product_id) REFERENCES Products(product_id) ON DELETE CASCADE
    );
    PRINT 'Created ProductVariants table';
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Object_ID = Object_ID('ProductVariants') AND name = 'sku')
        ALTER TABLE ProductVariants ADD sku NVARCHAR(50) NULL;
    PRINT 'Updated ProductVariants table with sku column';
END
GO

-- =====================================================
-- ProductImages Table (NEW - for product/variant images)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductImages')
BEGIN
    CREATE TABLE ProductImages (
        image_id INT PRIMARY KEY IDENTITY(1,1),
        product_id INT NOT NULL,
        variant_id INT NULL,
        image_url NVARCHAR(500) NOT NULL,
        is_primary BIT NOT NULL DEFAULT 0,
        is_thumbnail BIT NOT NULL DEFAULT 0,
        display_order INT NOT NULL DEFAULT 0,
        alt_text NVARCHAR(255) NULL,
        FOREIGN KEY (product_id) REFERENCES Products(product_id) ON DELETE CASCADE,
        FOREIGN KEY (variant_id) REFERENCES ProductVariants(variant_id) ON DELETE SET NULL
    );
    PRINT 'Created ProductImages table';
END
ELSE
BEGIN
    -- Add missing columns for migration
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Object_ID = Object_ID('ProductImages') AND name = 'display_order')
        ALTER TABLE ProductImages ADD display_order INT NOT NULL DEFAULT 0;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Object_ID = Object_ID('ProductImages') AND name = 'is_thumbnail')
        ALTER TABLE ProductImages ADD is_thumbnail BIT NOT NULL DEFAULT 0;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Object_ID = Object_ID('ProductImages') AND name = 'alt_text')
        ALTER TABLE ProductImages ADD alt_text NVARCHAR(255) NULL;
    PRINT 'Updated ProductImages table with missing columns';
END
GO

-- =====================================================
-- Inventory Table
-- =====================================================
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
GO

-- =====================================================
-- Orders Table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
BEGIN
    CREATE TABLE Orders (
        order_id INT PRIMARY KEY IDENTITY(1,1),
        account_id INT NULL,  -- NULL for guest checkout
        order_date DATETIME NOT NULL DEFAULT GETDATE(),
        total_amount DECIMAL(10, 2) NOT NULL,
        status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
        customer_name NVARCHAR(100),
        customer_phone NVARCHAR(20),
        customer_address NVARCHAR(255),
        notes NVARCHAR(500),
        shipping_address NVARCHAR(500) NULL,
        payment_method NVARCHAR(50) NULL
    );
    PRINT 'Created Orders table';
END
GO

-- =====================================================
-- OrderDetails Table (FIXED - has order_detail_id as PK)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderDetails')
BEGIN
    CREATE TABLE OrderDetails (
        order_detail_id INT PRIMARY KEY IDENTITY(1,1),
        OrderID INT NOT NULL,
        ProductID INT NOT NULL,
        VariantID INT NULL,
        Quantity INT NOT NULL,
        Price DECIMAL(18, 2) NOT NULL,
        FOREIGN KEY (OrderID) REFERENCES Orders(order_id),
        FOREIGN KEY (ProductID) REFERENCES Products(product_id),
        FOREIGN KEY (VariantID) REFERENCES ProductVariants(variant_id)
    );
    PRINT 'Created OrderDetails table';
END
ELSE
BEGIN
    -- Ensure order_detail_id exists for migration
    IF COL_LENGTH('dbo.OrderDetails', 'order_detail_id') IS NULL
    BEGIN
        -- Need to drop and recreate since we can't add IDENTITY to existing column
        BEGIN TRY
            -- Try to add as new column if PK is composite
            ALTER TABLE OrderDetails ADD order_detail_id INT IDENTITY(1,1) NOT NULL;
        END TRY BEGIN CATCH END CATCH
    END

    IF COL_LENGTH('dbo.OrderDetails', 'VariantID') IS NULL
    BEGIN
        ALTER TABLE OrderDetails ADD VariantID INT NULL;
    END
    PRINT 'Updated OrderDetails table with missing columns';
END
GO

-- =====================================================
-- OrderItems Table (alias for OrderDetails compatibility)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderItems')
BEGIN
    CREATE TABLE OrderItems (
        order_item_id INT PRIMARY KEY IDENTITY(1,1),
        order_id INT NOT NULL,
        product_id INT NOT NULL,
        variant_id INT NULL,
        quantity INT NOT NULL,
        price DECIMAL(18, 2) NOT NULL,
        FOREIGN KEY (order_id) REFERENCES Orders(order_id),
        FOREIGN KEY (product_id) REFERENCES Products(product_id),
        FOREIGN KEY (variant_id) REFERENCES ProductVariants(variant_id)
    );
    PRINT 'Created OrderItems table';
END
GO

-- =====================================================
-- Carts Table (NEW - for cart management)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Carts')
BEGIN
    CREATE TABLE Carts (
        cart_id INT PRIMARY KEY IDENTITY(1,1),
        account_id INT NULL,
        session_id NVARCHAR(64) NULL,
        role_name NVARCHAR(30) NULL,
        created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Created Carts table';
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.Carts', 'session_id') IS NULL
        ALTER TABLE Carts ADD session_id NVARCHAR(64) NULL;
    IF COL_LENGTH('dbo.Carts', 'role_name') IS NULL
        ALTER TABLE Carts ADD role_name NVARCHAR(30) NULL;
    PRINT 'Updated Carts table with missing columns';
END
GO

-- =====================================================
-- Cart_Items Table (FIXED - nullable FKs, sentinel column)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Cart_Items')
BEGIN
    CREATE TABLE Cart_Items (
        cart_item_id INT PRIMARY KEY IDENTITY(1,1),
        cart_id INT NULL,
        account_id INT NULL,
        product_id INT NOT NULL,
        variant_id INT NULL,
        quantity INT NOT NULL,
        added_date DATETIME NOT NULL DEFAULT GETDATE(),
        variant_sentinel AS (ISNULL(VariantID, -999999)) PERSISTED,
        FOREIGN KEY (product_id) REFERENCES Products(product_id),
        FOREIGN KEY (variant_id) REFERENCES ProductVariants(variant_id)
    );
    PRINT 'Created Cart_Items table';
END
ELSE
BEGIN
    -- Ensure all columns exist for migration
    IF COL_LENGTH('dbo.Cart_Items', 'cart_item_id') IS NULL
        ALTER TABLE Cart_Items ADD cart_item_id INT IDENTITY(1,1) NOT NULL;
    IF COL_LENGTH('dbo.Cart_Items', 'cart_id') IS NULL
        ALTER TABLE Cart_Items ADD cart_id INT NULL;
    IF COL_LENGTH('dbo.Cart_Items', 'variant_id') IS NULL
        ALTER TABLE Cart_Items ADD variant_id INT NULL;
    IF COL_LENGTH('dbo.Cart_Items', 'variant_sentinel') IS NULL
        ALTER TABLE Cart_Items ADD variant_sentinel AS (ISNULL(VariantID, -999999)) PERSISTED;
    PRINT 'Updated Cart_Items table with missing columns';
END
GO

-- Add unique constraint with sentinel (safe for NULLs)
BEGIN TRY
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'IX_Cart_Items_Cart_Product_Variant_Sentinel'
                   AND parent_object_id = OBJECT_ID('Cart_Items'))
    BEGIN
        ALTER TABLE Cart_Items ADD CONSTRAINT IX_Cart_Items_Cart_Product_Variant_Sentinel
            UNIQUE NONCLUSTERED (cart_id, product_id, variant_sentinel);
        PRINT 'Added unique constraint on Cart_Items';
    END
END TRY BEGIN CATCH
    PRINT 'Note: ' + ERROR_MESSAGE();
END CATCH
GO

-- =====================================================
-- Receipts_Shipments Table (NEW - for inventory tracking)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Receipts_Shipments')
BEGIN
    CREATE TABLE Receipts_Shipments (
        movement_id INT PRIMARY KEY IDENTITY(1,1),
        product_id INT NOT NULL,
        movement_type NVARCHAR(10) NOT NULL,  -- 'receipt' or 'shipment'
        quantity INT NOT NULL,
        movement_date DATETIME2 NOT NULL DEFAULT GETDATE(),
        related_order_id INT NULL,
        FOREIGN KEY (product_id) REFERENCES Products(product_id)
    );
    PRINT 'Created Receipts_Shipments table';
END
GO

-- =====================================================
-- AI Conversation Tables
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AIConversationLogs')
BEGIN
    CREATE TABLE AIConversationLogs (
        log_id INT PRIMARY KEY IDENTITY(1,1),
        session_id UNIQUEIDENTIFIER NOT NULL,
        user_message NVARCHAR(MAX) NOT NULL,
        ai_response NVARCHAR(MAX) NOT NULL,
        intent_detected NVARCHAR(50) NULL,
        confidence_score DECIMAL(18,2) NULL,
        was_escalated BIT NOT NULL DEFAULT 0,
        user_rating INT NULL,
        created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Created AIConversationLogs table';
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatSessions')
BEGIN
    CREATE TABLE ChatSessions (
        session_id UNIQUEIDENTIFIER PRIMARY KEY,
        account_id INT NULL,
        status NVARCHAR(20) NOT NULL,
        assigned_to INT NULL,
        started_at DATETIME2 NOT NULL,
        ended_at DATETIME2 NULL,
        FOREIGN KEY (account_id) REFERENCES Accounts(account_id)
    );
    PRINT 'Created ChatSessions table';
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatMessages')
BEGIN
    CREATE TABLE ChatMessages (
        message_id INT PRIMARY KEY IDENTITY(1,1),
        session_id UNIQUEIDENTIFIER NOT NULL,
        message NVARCHAR(MAX) NOT NULL,
        sender_type NVARCHAR(10) NOT NULL,
        created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        metadata NVARCHAR(MAX) NULL,
        FOREIGN KEY (session_id) REFERENCES ChatSessions(session_id) ON DELETE CASCADE
    );
    PRINT 'Created ChatMessages table';
END
GO

-- =====================================================
-- FAQs Table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FAQs')
BEGIN
    CREATE TABLE FAQs (
        faq_id INT PRIMARY KEY IDENTITY(1,1),
        question NVARCHAR(MAX) NOT NULL,
        answer NVARCHAR(MAX) NOT NULL,
        category NVARCHAR(50) NOT NULL,
        keywords NVARCHAR(MAX) NULL,
        priority INT NOT NULL DEFAULT 0,
        is_active BIT NOT NULL DEFAULT 1,
        created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Created FAQs table';
END
GO

-- =====================================================
-- KnowledgeChunks Table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KnowledgeChunks')
BEGIN
    CREATE TABLE KnowledgeChunks (
        chunk_id INT PRIMARY KEY IDENTITY(1,1),
        source_type NVARCHAR(20) NOT NULL,
        source_id INT NOT NULL,
        chunk_type NVARCHAR(30) NOT NULL,
        raw_text NVARCHAR(MAX) NOT NULL,
        normalized_text NVARCHAR(MAX) NOT NULL,
        embedding NVARCHAR(MAX) NOT NULL,
        price DECIMAL(10,2) NULL,
        category NVARCHAR(100) NULL,
        priority INT NOT NULL DEFAULT 0,
        created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE NONCLUSTERED INDEX IX_KnowledgeChunks_Source ON KnowledgeChunks(source_type, source_id);
    CREATE NONCLUSTERED INDEX IX_KnowledgeChunks_Category ON KnowledgeChunks(category);
    PRINT 'Created KnowledgeChunks table';
END
GO

-- =====================================================
-- Notifications Table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
BEGIN
    CREATE TABLE Notifications (
        notification_id INT PRIMARY KEY IDENTITY(1,1),
        account_id INT NULL,
        type NVARCHAR(50) NOT NULL,
        message NVARCHAR(MAX) NOT NULL,
        is_read BIT NOT NULL DEFAULT 0,
        link NVARCHAR(255) NULL,
        created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        FOREIGN KEY (account_id) REFERENCES Accounts(account_id)
    );
    PRINT 'Created Notifications table';
END
GO

-- =====================================================
-- SAMPLE DATA
-- =====================================================

-- Categories
IF NOT EXISTS (SELECT * FROM Categories)
BEGIN
    INSERT INTO Categories (name) VALUES (N'Điện thoại');
    INSERT INTO Categories (name) VALUES (N'Laptop');
    INSERT INTO Categories (name) VALUES (N'Tablet');
    INSERT INTO Categories (name) VALUES (N'Phụ kiện');
    INSERT INTO Categories (name) VALUES (N'Đồng hồ thông minh');
    PRINT 'Inserted Categories data';
END
GO

-- Suppliers
IF NOT EXISTS (SELECT * FROM Suppliers)
BEGIN
    INSERT INTO Suppliers (name, contact_person, phone, email, address)
    VALUES (N'Samsung Việt Nam', N'Nguyễn Văn A', '0912345678', 'contact@samsung.vn', N'TP. Hồ Chí Minh'),
           (N'Apple Vietnam', N'Trần Thị B', '0987654321', 'contact@apple.vn', N'Hà Nội'),
           (N'Xiaomi Việt Nam', N'Lê Văn C', '0977123456', 'contact@xiaomi.vn', N'TP. Đà Nẵng');
    PRINT 'Inserted Suppliers data';
END
GO

-- Accounts
IF NOT EXISTS (SELECT * FROM Accounts)
BEGIN
    INSERT INTO Accounts (username, password_hash, email, full_name, phone, address, role, is_active)
    VALUES (N'admin', 'admin123', 'admin@techshop.vn', N'Quản trị viên', '0917111111', N'TP. Hồ Chí Minh', 'Admin', 1),
           (N'customer1', 'customer123', 'customer@email.com', N'Khách hàng 1', '0919222222', N'TP. Hồ Chí Minh', 'Customer', 1),
           (N'customer2', 'customer123', 'customer2@email.com', N'Khách hàng 2', '0919333333', N'Hà Nội', 'Customer', 1);
    PRINT 'Inserted Accounts data';
END
GO

-- Products
IF NOT EXISTS (SELECT * FROM Products)
BEGIN
    INSERT INTO Products (name, description, price, original_price, stock_quantity, rating, is_new, is_hot, discount_percent, specifications, image_url, category_id, supplier_id, is_available)
    VALUES
    (N'iPhone 15 Pro Max', N'iPhone 15 Pro Max với chip A17 Pro, thiết kế titan cao cấp.', 32990000, 34990000, 100, 4.9, 1, 1, 6, '{"cpu": "A17 Pro", "screen": "6.7 inch"}', '/images/products/iphone15promax.png', 1, 2, 1),
    (N'Samsung Galaxy S24 Ultra', N'Samsung Galaxy S24 Ultra với AI tiên tiến và bút S Pen.', 28990000, 31990000, 80, 4.8, 1, 1, 9, '{"cpu": "Snapdragon 8 Gen 3", "screen": "6.8 inch"}', '/images/products/galaxys24ultra.png', 1, 1, 1),
    (N'MacBook Air M3', N'Laptop siêu mỏng nhẹ với chip M3 cực mạnh.', 27990000, 29990000, 50, 4.9, 1, 1, 7, '{"cpu": "Apple M3", "ram": "8GB"}', '/images/products/macbookairm3.png', 2, 2, 1);
    PRINT 'Inserted Products data';
END
GO

-- Product Variants
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
GO

-- =====================================================
-- DATA FIXES
-- =====================================================

-- Normalize all emails to lowercase
UPDATE Accounts SET Email = LOWER(LTRIM(RTRIM(Email))) WHERE Email IS NOT NULL;
PRINT 'Normalized all emails to lowercase';
GO

-- =====================================================
-- DONE
-- =====================================================
PRINT '';
PRINT '==========================================';
PRINT 'Database TechShopWebsite1 setup complete!';
PRINT '==========================================';
PRINT '';
