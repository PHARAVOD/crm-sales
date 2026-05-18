-- =====================================================
-- MIGRATION: InitialCreate
-- CRM Database for SQL Server
-- Дата: 2026-05-18
-- =====================================================

USE CrmDatabase;
GO

-- 1. Создание таблиц (если не созданы)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Products' AND xtype='U')
BEGIN
    CREATE TABLE Products (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Price DECIMAL(18,2) NOT NULL,
        Description NVARCHAR(500) NULL,
        StockQuantity INT DEFAULT 0,
        Category NVARCHAR(100) NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        IsActive BIT DEFAULT 1
    );
    PRINT '✅ Таблица Products создана';
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Leads' AND xtype='U')
BEGIN
    CREATE TABLE Leads (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Phone NVARCHAR(50) NULL,
        Email NVARCHAR(200) NULL UNIQUE,
        Company NVARCHAR(100) NULL,
        Source NVARCHAR(50) NULL,
        Status NVARCHAR(50) DEFAULT 'new',
        Score INT DEFAULT 0,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL
    );
    PRINT '✅ Таблица Leads создана';
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Orders' AND xtype='U')
BEGIN
    CREATE TABLE Orders (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        LeadId INT NULL,
        ContactId INT NULL,
        TotalAmount DECIMAL(18,2) DEFAULT 0,
        Stage NVARCHAR(50) DEFAULT 'new',
        Status NVARCHAR(50) DEFAULT 'active',
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        ClosedAt DATETIME2 NULL,
        
        CONSTRAINT FK_Orders_Leads FOREIGN KEY (LeadId) REFERENCES Leads(Id)
    );
    PRINT '✅ Таблица Orders создана';
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OrderLines' AND xtype='U')
BEGIN
    CREATE TABLE OrderLines (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OrderId INT NOT NULL,
        ProductId INT NOT NULL,
        ProductName NVARCHAR(200) NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        Quantity INT NOT NULL,
        
        CONSTRAINT FK_OrderLines_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
        CONSTRAINT FK_OrderLines_Products FOREIGN KEY (ProductId) REFERENCES Products(Id)
    );
    PRINT '✅ Таблица OrderLines создана';
END
GO

-- 2. Добавление начальных данных
INSERT INTO Products (Name, Price, StockQuantity, Category, IsActive)
SELECT * FROM (VALUES
    ('Ноутбук', 50000, 10, 'Электроника', 1),
    ('Мышь', 1000, 50, 'Электроника', 1),
    ('Клавиатура', 3000, 30, 'Электроника', 1),
    ('Монитор', 25000, 15, 'Электроника', 1)
) AS src(Name, Price, StockQuantity, Category, IsActive)
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = src.Name);
PRINT '✅ Начальные данные добавлены в Products';
GO

-- 3. Демонстрация: товар из каталога доступен в заказах
PRINT '========================================';
PRINT 'ДЕМОНСТРАЦИЯ: Товар из каталога доступен в заказах';
PRINT '========================================';

-- Показываем все товары
SELECT Id, Name, Price, Category FROM Products;
PRINT '⬆️ Товары из каталога (модуль А)';

-- Показываем что товар есть в заказах
SELECT 
    o.Id AS OrderId,
    ol.ProductId,
    p.Name AS ProductName,
    ol.Quantity,
    ol.UnitPrice
FROM OrderLines ol
JOIN Products p ON ol.ProductId = p.Id
JOIN Orders o ON ol.OrderId = o.Id;
PRINT '⬆️ Товары в заказах (модуль В) - те же самые ID товаров!';

PRINT '========================================';
PRINT '✅ МИГРАЦИЯ ЗАВЕРШЕНА УСПЕШНО';
PRINT '========================================';