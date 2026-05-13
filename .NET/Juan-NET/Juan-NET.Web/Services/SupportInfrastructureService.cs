namespace Juan_NET.Web.Services
{
    public static class SupportInfrastructureService
    {
        public static async Task EnsureInfrastructureAsync(AppDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[SupportTickets]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [SupportTickets] (
                        [Id] int NOT NULL IDENTITY,
                        [Code] nvarchar(32) NOT NULL,
                        [UserId] int NOT NULL,
                        [OperatorUserId] int NULL,
                        [Subject] nvarchar(160) NOT NULL,
                        [Priority] nvarchar(20) NOT NULL CONSTRAINT [DF_SupportTickets_Priority] DEFAULT (N'Medium'),
                        [Status] nvarchar(40) NOT NULL CONSTRAINT [DF_SupportTickets_Status] DEFAULT (N'Open'),
                        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_SupportTickets_CreatedAt] DEFAULT (GETUTCDATE()),
                        [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_SupportTickets_UpdatedAt] DEFAULT (GETUTCDATE()),
                        [ClosedAt] datetime2 NULL,
                        CONSTRAINT [PK_SupportTickets] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_SupportTickets_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_SupportTickets_Users_OperatorUserId] FOREIGN KEY ([OperatorUserId]) REFERENCES [Users] ([Id])
                    );

                    CREATE UNIQUE INDEX [IX_SupportTickets_Code] ON [SupportTickets] ([Code]);
                    CREATE INDEX [IX_SupportTickets_UserId] ON [SupportTickets] ([UserId]);
                    CREATE INDEX [IX_SupportTickets_OperatorUserId] ON [SupportTickets] ([OperatorUserId]);
                    CREATE INDEX [IX_SupportTickets_ClosedAt] ON [SupportTickets] ([ClosedAt]);
                END

                IF COL_LENGTH(N'[SupportTickets]', N'ClosedAt') IS NULL
                BEGIN
                    ALTER TABLE [SupportTickets] ADD [ClosedAt] datetime2 NULL;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_SupportTickets_ClosedAt' AND [object_id] = OBJECT_ID(N'[SupportTickets]'))
                BEGIN
                    CREATE INDEX [IX_SupportTickets_ClosedAt] ON [SupportTickets] ([ClosedAt]);
                END

                UPDATE [SupportTickets]
                SET [ClosedAt] = [UpdatedAt]
                WHERE [Status] = N'Resolved' AND [ClosedAt] IS NULL;

                IF OBJECT_ID(N'[SupportTicketCreatedDates]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [SupportTicketCreatedDates] (
                        [Id] int NOT NULL IDENTITY,
                        [SupportTicketId] int NOT NULL,
                        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_SupportTicketCreatedDates_CreatedAt] DEFAULT (GETUTCDATE()),
                        CONSTRAINT [PK_SupportTicketCreatedDates] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_SupportTicketCreatedDates_SupportTickets_SupportTicketId] FOREIGN KEY ([SupportTicketId]) REFERENCES [SupportTickets] ([Id]) ON DELETE CASCADE
                    );

                    CREATE UNIQUE INDEX [IX_SupportTicketCreatedDates_SupportTicketId] ON [SupportTicketCreatedDates] ([SupportTicketId]);
                END

                IF OBJECT_ID(N'[SupportMessages]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [SupportMessages] (
                        [Id] int NOT NULL IDENTITY,
                        [SupportTicketId] int NOT NULL,
                        [SenderUserId] int NOT NULL,
                        [IsOperator] bit NOT NULL,
                        [Text] nvarchar(2000) NULL,
                        [ImageUrl] nvarchar(300) NULL,
                        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_SupportMessages_CreatedAt] DEFAULT (GETUTCDATE()),
                        CONSTRAINT [PK_SupportMessages] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_SupportMessages_SupportTickets_SupportTicketId] FOREIGN KEY ([SupportTicketId]) REFERENCES [SupportTickets] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_SupportMessages_Users_SenderUserId] FOREIGN KEY ([SenderUserId]) REFERENCES [Users] ([Id])
                    );

                    CREATE INDEX [IX_SupportMessages_SupportTicketId] ON [SupportMessages] ([SupportTicketId]);
                    CREATE INDEX [IX_SupportMessages_SenderUserId] ON [SupportMessages] ([SenderUserId]);
                END
                """);
        }
    }
}
