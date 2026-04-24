-- Add password reset columns to Accounts table
USE TechShopWebsite1;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Object_ID = Object_ID('Accounts') AND name = 'reset_token')
BEGIN
    ALTER TABLE Accounts ADD reset_token NVARCHAR(64) NULL;
    PRINT 'Added reset_token column';
END
ELSE
BEGIN
    PRINT 'reset_token column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Object_ID = Object_ID('Accounts') AND name = 'reset_token_expiry')
BEGIN
    ALTER TABLE Accounts ADD reset_token_expiry DATETIME NULL;
    PRINT 'Added reset_token_expiry column';
END
ELSE
BEGIN
    PRINT 'reset_token_expiry column already exists';
END
GO

-- Create ProductVariants table if not exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariants]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ProductVariants](
        [variant_id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [product_id] [int] NOT NULL,
        [color] [nvarchar](50) NULL,
        [storage] [nvarchar](20) NULL,
        [ram] [nvarchar](20) NULL,
        [variant_name] [nvarchar](100) NULL,
        [price] [decimal](10, 2) NOT NULL,
        [original_price] [decimal](10, 2) NULL,
        [stock_quantity] [int] NOT NULL DEFAULT 0,
        [display_order] [int] NOT NULL DEFAULT 0,
        CONSTRAINT [FK_ProductVariants_Products] FOREIGN KEY ([product_id]) REFERENCES [Products]([product_id])
    );
    PRINT 'Created ProductVariants table';
END
ELSE
BEGIN
    PRINT 'ProductVariants table already exists';
END
GO

SELECT 'Migration complete' AS Result;
