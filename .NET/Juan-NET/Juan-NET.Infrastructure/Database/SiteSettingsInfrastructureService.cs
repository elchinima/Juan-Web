namespace Juan_NET.Infrastructure.Database
{
    public static class SiteSettingsInfrastructureService
    {
        public static async Task EnsureInfrastructureAsync(AppDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync("""
                IF COL_LENGTH(N'[ContactMessages]', N'Status') IS NULL
                BEGIN
                    ALTER TABLE [ContactMessages] ADD [Status] nvarchar(40) NOT NULL CONSTRAINT [DF_ContactMessages_Status] DEFAULT (N'New');
                END

                IF COL_LENGTH(N'[ContactMessages]', N'StatusChangedByEmail') IS NULL
                BEGIN
                    ALTER TABLE [ContactMessages] ADD [StatusChangedByEmail] nvarchar(180) NULL;
                END

                IF COL_LENGTH(N'[ContactMessages]', N'StatusChangedAt') IS NULL
                BEGIN
                    ALTER TABLE [ContactMessages] ADD [StatusChangedAt] datetime2 NULL;
                END

                IF COL_LENGTH(N'[ContactMessages]', N'AdminNote') IS NULL
                BEGIN
                    ALTER TABLE [ContactMessages] ADD [AdminNote] nvarchar(100) NULL;
                END

                IF OBJECT_ID(N'[SiteFooterSettings]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [SiteFooterSettings] (
                        [Id] int NOT NULL,
                        [Address] nvarchar(250) NOT NULL,
                        [Email] nvarchar(180) NOT NULL,
                        [Phone] nvarchar(40) NOT NULL,
                        [AllProductsUrl] nvarchar(250) NOT NULL,
                        [CategoriesUrl] nvarchar(250) NOT NULL,
                        [HomeUrl] nvarchar(250) NOT NULL,
                        [AboutUrl] nvarchar(250) NOT NULL,
                        [ContactUrl] nvarchar(250) NOT NULL,
                        [PrivacyUrl] nvarchar(250) NOT NULL,
                        [FacebookUrl] nvarchar(250) NOT NULL,
                        [TwitterUrl] nvarchar(250) NOT NULL,
                        [LinkedinUrl] nvarchar(250) NOT NULL,
                        [InstagramUrl] nvarchar(250) NOT NULL,
                        CONSTRAINT [PK_SiteFooterSettings] PRIMARY KEY ([Id])
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM [SiteFooterSettings] WHERE [Id] = 1)
                BEGIN
                    INSERT INTO [SiteFooterSettings] (
                        [Id], [Address], [Email], [Phone], [AllProductsUrl], [CategoriesUrl], [HomeUrl],
                        [AboutUrl], [ContactUrl], [PrivacyUrl], [FacebookUrl], [TwitterUrl], [LinkedinUrl], [InstagramUrl]
                    )
                    VALUES (
                        1, N'184 Main Rd E, St Albans VIC 3021, Australia', N'yourmail@gmail.com', N'+ 00 254 254565',
                        N'/Products', N'/Categories', N'/', N'#', N'/Home/Contact', N'#', N'#', N'#', N'#', N'#'
                    );
                END
                """);
        }
    }
}
