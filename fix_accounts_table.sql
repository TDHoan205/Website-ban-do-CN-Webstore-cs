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

SELECT 'Migration complete' AS Result;
