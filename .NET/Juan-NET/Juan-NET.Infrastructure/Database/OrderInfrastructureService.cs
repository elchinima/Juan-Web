namespace Juan_NET.Infrastructure.Database
{
    public static class OrderInfrastructureService
    {
        public static async Task EnsureInfrastructureAsync(AppDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[UserAddresses]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [UserAddresses] (
                        [Id] int NOT NULL IDENTITY,
                        [UserId] int NOT NULL,
                        [RecipientFullName] nvarchar(80) NOT NULL,
                        [AddressLine1] nvarchar(180) NOT NULL,
                        [AddressLine2] nvarchar(180) NULL,
                        [Fin] nvarchar(7) NOT NULL,
                        [IsDefault] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_UserAddresses_CreatedAt] DEFAULT (GETUTCDATE()),
                        CONSTRAINT [PK_UserAddresses] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_UserAddresses_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_UserAddresses_UserId] ON [UserAddresses] ([UserId]);
                END

                IF OBJECT_ID(N'[UserSecurityTokens]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [UserSecurityTokens] (
                        [Id] int NOT NULL IDENTITY,
                        [UserId] int NOT NULL,
                        [IsTwoFactorEnabled] bit NOT NULL,
                        [TwoFactorCodeHash] nvarchar(120) NULL,
                        [TwoFactorCodeSalt] nvarchar(60) NULL,
                        [TwoFactorCodeExpiresAt] datetime2 NULL,
                        [PasswordResetTokenHash] nvarchar(120) NULL,
                        [PasswordResetTokenSalt] nvarchar(60) NULL,
                        [PasswordResetTokenExpiresAt] datetime2 NULL,
                        [PendingPasswordHash] nvarchar(120) NULL,
                        [PendingPasswordSalt] nvarchar(60) NULL,
                        [PasswordChangeTokenHash] nvarchar(120) NULL,
                        [PasswordChangeTokenSalt] nvarchar(60) NULL,
                        [PasswordChangeTokenExpiresAt] datetime2 NULL,
                        CONSTRAINT [PK_UserSecurityTokens] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_UserSecurityTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_UserSecurityTokens_UserId] ON [UserSecurityTokens] ([UserId]);
                END

                IF COL_LENGTH(N'[Users]', N'DeliveryRecipientFullName') IS NOT NULL
                BEGIN
                    EXEC(N'
                        INSERT INTO [UserAddresses] ([UserId], [RecipientFullName], [AddressLine1], [AddressLine2], [Fin], [IsDefault], [CreatedAt])
                        SELECT
                            [Id],
                            COALESCE([DeliveryRecipientFullName], N''''),
                            COALESCE([DeliveryAddressLine1], N''''),
                            [DeliveryAddressLine2],
                            COALESCE([DeliveryFin], N''''),
                            CAST(1 AS bit),
                            [CreatedAt]
                        FROM [Users] AS [user]
                        WHERE NOT EXISTS (
                            SELECT 1
                            FROM [UserAddresses] AS [address]
                            WHERE [address].[UserId] = [user].[Id]
                        )
                        AND (
                            [DeliveryRecipientFullName] IS NOT NULL OR
                            [DeliveryAddressLine1] IS NOT NULL OR
                            [DeliveryAddressLine2] IS NOT NULL OR
                            [DeliveryFin] IS NOT NULL
                        );
                    ');
                END

                IF COL_LENGTH(N'[Users]', N'IsTwoFactorEnabled') IS NOT NULL
                BEGIN
                    EXEC(N'
                        INSERT INTO [UserSecurityTokens] (
                            [UserId],
                            [IsTwoFactorEnabled],
                            [TwoFactorCodeHash],
                            [TwoFactorCodeSalt],
                            [TwoFactorCodeExpiresAt],
                            [PasswordResetTokenHash],
                            [PasswordResetTokenSalt],
                            [PasswordResetTokenExpiresAt],
                            [PendingPasswordHash],
                            [PendingPasswordSalt],
                            [PasswordChangeTokenHash],
                            [PasswordChangeTokenSalt],
                            [PasswordChangeTokenExpiresAt]
                        )
                        SELECT
                            [Id],
                            [IsTwoFactorEnabled],
                            [TwoFactorCodeHash],
                            [TwoFactorCodeSalt],
                            [TwoFactorCodeExpiresAt],
                            [PasswordResetTokenHash],
                            [PasswordResetTokenSalt],
                            [PasswordResetTokenExpiresAt],
                            [PendingPasswordHash],
                            [PendingPasswordSalt],
                            [PasswordChangeTokenHash],
                            [PasswordChangeTokenSalt],
                            [PasswordChangeTokenExpiresAt]
                        FROM [Users] AS [user]
                        WHERE NOT EXISTS (
                            SELECT 1
                            FROM [UserSecurityTokens] AS [token]
                            WHERE [token].[UserId] = [user].[Id]
                        );
                    ');
                END

                DECLARE @ColumnsToDrop TABLE ([Name] sysname NOT NULL);
                INSERT INTO @ColumnsToDrop ([Name])
                VALUES
                    (N'DeliveryRecipientFullName'),
                    (N'DeliveryAddressLine1'),
                    (N'DeliveryAddressLine2'),
                    (N'DeliveryFin'),
                    (N'IsTwoFactorEnabled'),
                    (N'TwoFactorCodeHash'),
                    (N'TwoFactorCodeSalt'),
                    (N'TwoFactorCodeExpiresAt'),
                    (N'PasswordResetTokenHash'),
                    (N'PasswordResetTokenSalt'),
                    (N'PasswordResetTokenExpiresAt'),
                    (N'PendingPasswordHash'),
                    (N'PendingPasswordSalt'),
                    (N'PasswordChangeTokenHash'),
                    (N'PasswordChangeTokenSalt'),
                    (N'PasswordChangeTokenExpiresAt');

                DECLARE @ColumnName sysname;
                DECLARE @ConstraintName sysname;
                DECLARE @Sql nvarchar(max);

                DECLARE column_cursor CURSOR LOCAL FAST_FORWARD FOR
                    SELECT [Name]
                    FROM @ColumnsToDrop
                    WHERE COL_LENGTH(N'[Users]', [Name]) IS NOT NULL;

                OPEN column_cursor;
                FETCH NEXT FROM column_cursor INTO @ColumnName;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SELECT @ConstraintName = [default_constraints].[name]
                    FROM [sys].[default_constraints]
                    INNER JOIN [sys].[columns]
                        ON [columns].[default_object_id] = [default_constraints].[object_id]
                    WHERE [default_constraints].[parent_object_id] = OBJECT_ID(N'[Users]')
                        AND [columns].[name] = @ColumnName;

                    IF @ConstraintName IS NOT NULL
                    BEGIN
                        SET @Sql = N'ALTER TABLE [Users] DROP CONSTRAINT ' + QUOTENAME(@ConstraintName);
                        EXEC sp_executesql @Sql;
                    END

                    SET @Sql = N'ALTER TABLE [Users] DROP COLUMN ' + QUOTENAME(@ColumnName);
                    EXEC sp_executesql @Sql;

                    SET @ConstraintName = NULL;
                    FETCH NEXT FROM column_cursor INTO @ColumnName;
                END

                CLOSE column_cursor;
                DEALLOCATE column_cursor;

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
                        [StripePaymentIntentId] nvarchar(120) NULL,
                        [PromoCode] nvarchar(80) NULL,
                        [Currency] nvarchar(12) NOT NULL,
                        [Status] nvarchar(40) NOT NULL CONSTRAINT [DF_Orders_Status] DEFAULT (N'Paid'),
                        [Subtotal] decimal(18,2) NOT NULL,
                        [DeliveryTotal] decimal(18,2) NOT NULL,
                        [DiscountTotal] decimal(18,2) NOT NULL,
                        [Total] decimal(18,2) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Orders_CreatedAt] DEFAULT (GETUTCDATE()),
                        [RefundRequestedAt] datetime2 NULL,
                        [RefundedAt] datetime2 NULL,
                        [RefundedByUserId] int NULL,
                        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Orders_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_Orders_UserId] ON [Orders] ([UserId]);
                    CREATE INDEX [IX_Orders_StripePaymentIntentId] ON [Orders] ([StripePaymentIntentId]);
                    CREATE UNIQUE INDEX [IX_Orders_StripeSessionId] ON [Orders] ([StripeSessionId]) WHERE [StripeSessionId] IS NOT NULL;
                END

                IF COL_LENGTH(N'[Orders]', N'StripePaymentIntentId') IS NULL
                BEGIN
                    ALTER TABLE [Orders] ADD [StripePaymentIntentId] nvarchar(120) NULL;
                    CREATE INDEX [IX_Orders_StripePaymentIntentId] ON [Orders] ([StripePaymentIntentId]);
                END

                IF COL_LENGTH(N'[Orders]', N'RefundRequestedAt') IS NULL
                BEGIN
                    ALTER TABLE [Orders] ADD [RefundRequestedAt] datetime2 NULL;
                END

                IF COL_LENGTH(N'[Orders]', N'RefundedAt') IS NULL
                BEGIN
                    ALTER TABLE [Orders] ADD [RefundedAt] datetime2 NULL;
                END

                IF COL_LENGTH(N'[Orders]', N'RefundedByUserId') IS NULL
                BEGIN
                    ALTER TABLE [Orders] ADD [RefundedByUserId] int NULL;
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
