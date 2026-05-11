namespace Juan_NET.Web.Services
{
    public static class OrderInfrastructureService
    {
        public static async Task EnsureInfrastructureAsync(AppDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync("""
                IF COL_LENGTH(N'[Users]', N'DeliveryRecipientFullName') IS NULL
                BEGIN
                    ALTER TABLE [Users] ADD [DeliveryRecipientFullName] nvarchar(80) NULL;
                END

                IF COL_LENGTH(N'[Users]', N'DeliveryAddressLine1') IS NULL
                BEGIN
                    ALTER TABLE [Users] ADD [DeliveryAddressLine1] nvarchar(180) NULL;
                END

                IF COL_LENGTH(N'[Users]', N'DeliveryAddressLine2') IS NULL
                BEGIN
                    ALTER TABLE [Users] ADD [DeliveryAddressLine2] nvarchar(180) NULL;
                END

                IF COL_LENGTH(N'[Users]', N'DeliveryFin') IS NULL
                BEGIN
                    ALTER TABLE [Users] ADD [DeliveryFin] nvarchar(7) NULL;
                END

                IF OBJECT_ID(N'[Orders]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Orders] (
                        [Id] int NOT NULL IDENTITY,
                        [UserId] int NOT NULL,
                        [RecipientFullName] nvarchar(80) NOT NULL,
                        [AddressLine1] nvarchar(180) NOT NULL,
                        [AddressLine2] nvarchar(180) NULL,
                        [Fin] nvarchar(7) NOT NULL,
                        [StripeSessionId] nvarchar(120) NULL,
                        [PromoCode] nvarchar(80) NULL,
                        [Currency] nvarchar(12) NOT NULL,
                        [Status] nvarchar(40) NOT NULL CONSTRAINT [DF_Orders_Status] DEFAULT (N'Paid'),
                        [Subtotal] decimal(18,2) NOT NULL,
                        [DeliveryTotal] decimal(18,2) NOT NULL,
                        [DiscountTotal] decimal(18,2) NOT NULL,
                        [Total] decimal(18,2) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Orders_CreatedAt] DEFAULT (GETUTCDATE()),
                        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Orders_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_Orders_UserId] ON [Orders] ([UserId]);
                    CREATE UNIQUE INDEX [IX_Orders_StripeSessionId] ON [Orders] ([StripeSessionId]) WHERE [StripeSessionId] IS NOT NULL;
                END

                IF OBJECT_ID(N'[OrderItems]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [OrderItems] (
                        [Id] int NOT NULL IDENTITY,
                        [OrderId] int NOT NULL,
                        [ProductId] int NOT NULL,
                        [ProductName] nvarchar(120) NOT NULL,
                        [ProductImageUrl] nvarchar(300) NULL,
                        [UnitPrice] decimal(18,2) NOT NULL,
                        [UnitDeliveryPrice] decimal(18,2) NOT NULL,
                        [Quantity] int NOT NULL,
                        [LineTotal] decimal(18,2) NOT NULL,
                        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id])
                    );

                    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
                    CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);
                END
                """);
        }
    }
}
