namespace Juan_NET.Web.Services
{
    public static class ShopListInfrastructureService
    {
        public static async Task EnsureInfrastructureAsync(AppDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[BasketItems]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [BasketItems] (
                        [UserId] int NOT NULL,
                        [ProductId] int NOT NULL,
                        [Quantity] int NOT NULL DEFAULT (1),
                        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
                        CONSTRAINT [PK_BasketItems] PRIMARY KEY ([UserId], [ProductId]),
                        CONSTRAINT [FK_BasketItems_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_BasketItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_BasketItems_ProductId] ON [BasketItems] ([ProductId]);
                END

                IF OBJECT_ID(N'[WishlistItems]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [WishlistItems] (
                        [UserId] int NOT NULL,
                        [ProductId] int NOT NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
                        CONSTRAINT [PK_WishlistItems] PRIMARY KEY ([UserId], [ProductId]),
                        CONSTRAINT [FK_WishlistItems_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_WishlistItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_WishlistItems_ProductId] ON [WishlistItems] ([ProductId]);
                END
                """);
        }
    }
}
