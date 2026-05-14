namespace Juan_NET.Infrastructure.Database
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
                        [Topic] nvarchar(40) NOT NULL CONSTRAINT [DF_SupportTickets_Topic] DEFAULT (N'Other'),
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
                    ALTER TABLE [SupportTickets] ADD [ClosedAt] datetime2 NULL;

                IF COL_LENGTH(N'[SupportTickets]', N'Topic') IS NULL
                    ALTER TABLE [SupportTickets] ADD [Topic] nvarchar(40) NOT NULL CONSTRAINT [DF_SupportTickets_Topic] DEFAULT (N'Other');

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_SupportTickets_ClosedAt' AND [object_id] = OBJECT_ID(N'[SupportTickets]'))
                    CREATE INDEX [IX_SupportTickets_ClosedAt] ON [SupportTickets] ([ClosedAt]);

                EXEC(N'UPDATE [SupportTickets]
                    SET [ClosedAt] = [UpdatedAt]
                    WHERE [Status] = N''Resolved'' AND [ClosedAt] IS NULL;');

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

                IF OBJECT_ID(N'[SupportRatings]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [SupportRatings] (
                        [Id] int NOT NULL IDENTITY,
                        [SupportTicketId] int NOT NULL,
                        [UserId] int NOT NULL,
                        [OperatorUserId] int NOT NULL,
                        [Rating] decimal(2,1) NOT NULL,
                        [Comment] nvarchar(1000) NULL,
                        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_SupportRatings_CreatedAt] DEFAULT (GETUTCDATE()),
                        CONSTRAINT [PK_SupportRatings] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_SupportRatings_SupportTickets_SupportTicketId] FOREIGN KEY ([SupportTicketId]) REFERENCES [SupportTickets] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_SupportRatings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]),
                        CONSTRAINT [FK_SupportRatings_Users_OperatorUserId] FOREIGN KEY ([OperatorUserId]) REFERENCES [Users] ([Id]),
                        CONSTRAINT [CK_SupportRatings_Rating] CHECK ([Rating] >= 1.0 AND [Rating] <= 5.0)
                    );

                    CREATE UNIQUE INDEX [IX_SupportRatings_SupportTicketId] ON [SupportRatings] ([SupportTicketId]);
                    CREATE INDEX [IX_SupportRatings_OperatorUserId_CreatedAt] ON [SupportRatings] ([OperatorUserId], [CreatedAt]);
                    CREATE INDEX [IX_SupportRatings_UserId] ON [SupportRatings] ([UserId]);
                END

                IF OBJECT_ID(N'[SupportOperatorWorkTimes]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [SupportOperatorWorkTimes] (
                        [Id] int NOT NULL IDENTITY,
                        [OperatorUserId] int NOT NULL,
                        [WorkDate] date NOT NULL,
                        [TotalSeconds] int NOT NULL CONSTRAINT [DF_SupportOperatorWorkTimes_TotalSeconds] DEFAULT (0),
                        [LastStartedAt] datetime2 NULL,
                        [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_SupportOperatorWorkTimes_UpdatedAt] DEFAULT (GETUTCDATE()),
                        CONSTRAINT [PK_SupportOperatorWorkTimes] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_SupportOperatorWorkTimes_Users_OperatorUserId] FOREIGN KEY ([OperatorUserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
                    );

                    CREATE UNIQUE INDEX [IX_SupportOperatorWorkTimes_OperatorUserId_WorkDate] ON [SupportOperatorWorkTimes] ([OperatorUserId], [WorkDate]);
                END
                """);
        }
    }
}
