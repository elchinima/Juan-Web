namespace Juan_NET.Infrastructure.Database
{
    public static class FavoriteCategoryInfrastructureService
    {
        public static async Task EnsureInfrastructureAsync(AppDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[UserFavoriteCategories]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [UserFavoriteCategories] (
                        [UserId] int NOT NULL,
                        [CategoryId] int NOT NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
                        CONSTRAINT [PK_UserFavoriteCategories] PRIMARY KEY ([UserId], [CategoryId]),
                        CONSTRAINT [FK_UserFavoriteCategories_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_UserFavoriteCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_UserFavoriteCategories_CategoryId] ON [UserFavoriteCategories] ([CategoryId]);
                END

                IF OBJECT_ID(N'[FavoriteCategoryDigests]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [FavoriteCategoryDigests] (
                        [Id] int NOT NULL IDENTITY,
                        [CategoryId] int NOT NULL,
                        [SentForDate] datetime2 NOT NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
                        CONSTRAINT [PK_FavoriteCategoryDigests] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_FavoriteCategoryDigests_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
                    );

                    CREATE UNIQUE INDEX [IX_FavoriteCategoryDigests_CategoryId_SentForDate] ON [FavoriteCategoryDigests] ([CategoryId], [SentForDate]);
                END
                """);
        }
    }
}
