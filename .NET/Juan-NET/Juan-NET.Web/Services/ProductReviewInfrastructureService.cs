namespace Juan_NET.Web.Services
{
    public static class ProductReviewInfrastructureService
    {
        public static async Task EnsureInfrastructureAsync(AppDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[ProductReviews]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [ProductReviews] (
                        [Id] int NOT NULL IDENTITY,
                        [ProductId] int NOT NULL,
                        [UserId] int NOT NULL,
                        [Rating] decimal(2,1) NOT NULL,
                        [Comment] nvarchar(1000) NULL,
                        [IsVerifiedPurchase] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_ProductReviews_CreatedAt] DEFAULT (GETUTCDATE()),
                        [UpdatedAt] datetime2 NULL,
                        CONSTRAINT [PK_ProductReviews] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_ProductReviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_ProductReviews_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]),
                        CONSTRAINT [CK_ProductReviews_Rating] CHECK ([Rating] >= 1.0 AND [Rating] <= 5.0)
                    );

                    CREATE UNIQUE INDEX [IX_ProductReviews_ProductId_UserId] ON [ProductReviews] ([ProductId], [UserId]);
                    CREATE INDEX [IX_ProductReviews_ProductId_CreatedAt] ON [ProductReviews] ([ProductId], [CreatedAt]);
                    CREATE INDEX [IX_ProductReviews_UserId] ON [ProductReviews] ([UserId]);
                END
                """);
        }
    }
}
