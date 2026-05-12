using Juan_NET.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Juan_NET.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260512130000_SplitUserAddressAndSecurityToken")]
    public partial class SplitUserAddressAndSecurityToken : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RecipientFullName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    Fin = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAddresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSecurityTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IsTwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorCodeHash = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    TwoFactorCodeSalt = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    TwoFactorCodeExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordResetTokenHash = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PasswordResetTokenSalt = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    PasswordResetTokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PendingPasswordHash = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PendingPasswordSalt = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    PasswordChangeTokenHash = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PasswordChangeTokenSalt = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    PasswordChangeTokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSecurityTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSecurityTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAddresses_UserId",
                table: "UserAddresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSecurityTokens_UserId",
                table: "UserSecurityTokens",
                column: "UserId");

            migrationBuilder.Sql("""
                INSERT INTO [UserAddresses] ([UserId], [RecipientFullName], [AddressLine1], [AddressLine2], [Fin], [IsDefault], [CreatedAt])
                SELECT
                    [Id],
                    COALESCE([DeliveryRecipientFullName], N''),
                    COALESCE([DeliveryAddressLine1], N''),
                    [DeliveryAddressLine2],
                    COALESCE([DeliveryFin], N''),
                    CAST(1 AS bit),
                    [CreatedAt]
                FROM [Users]
                WHERE
                    [DeliveryRecipientFullName] IS NOT NULL OR
                    [DeliveryAddressLine1] IS NOT NULL OR
                    [DeliveryAddressLine2] IS NOT NULL OR
                    [DeliveryFin] IS NOT NULL;
                """);

            migrationBuilder.Sql("""
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
                FROM [Users];
                """);

            migrationBuilder.DropColumn(
                name: "DeliveryAddressLine1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeliveryAddressLine2",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeliveryFin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeliveryRecipientFullName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsTwoFactorEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordChangeTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordChangeTokenHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordChangeTokenSalt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenSalt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PendingPasswordHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PendingPasswordSalt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorCodeExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorCodeHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorCodeSalt",
                table: "Users");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddressLine1",
                table: "Users",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddressLine2",
                table: "Users",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryFin",
                table: "Users",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryRecipientFullName",
                table: "Users",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTwoFactorEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordChangeTokenExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordChangeTokenHash",
                table: "Users",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordChangeTokenSalt",
                table: "Users",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenHash",
                table: "Users",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenSalt",
                table: "Users",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingPasswordHash",
                table: "Users",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingPasswordSalt",
                table: "Users",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TwoFactorCodeExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorCodeHash",
                table: "Users",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorCodeSalt",
                table: "Users",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [user]
                SET
                    [DeliveryRecipientFullName] = [address].[RecipientFullName],
                    [DeliveryAddressLine1] = [address].[AddressLine1],
                    [DeliveryAddressLine2] = [address].[AddressLine2],
                    [DeliveryFin] = [address].[Fin]
                FROM [Users] AS [user]
                OUTER APPLY (
                    SELECT TOP(1)
                        [Id],
                        [RecipientFullName],
                        [AddressLine1],
                        [AddressLine2],
                        [Fin]
                    FROM [UserAddresses] AS [address]
                    WHERE [address].[UserId] = [user].[Id]
                    ORDER BY [address].[IsDefault] DESC, [address].[Id]
                ) AS [address]
                WHERE [address].[Id] IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE [user]
                SET
                    [IsTwoFactorEnabled] = [token].[IsTwoFactorEnabled],
                    [TwoFactorCodeHash] = [token].[TwoFactorCodeHash],
                    [TwoFactorCodeSalt] = [token].[TwoFactorCodeSalt],
                    [TwoFactorCodeExpiresAt] = [token].[TwoFactorCodeExpiresAt],
                    [PasswordResetTokenHash] = [token].[PasswordResetTokenHash],
                    [PasswordResetTokenSalt] = [token].[PasswordResetTokenSalt],
                    [PasswordResetTokenExpiresAt] = [token].[PasswordResetTokenExpiresAt],
                    [PendingPasswordHash] = [token].[PendingPasswordHash],
                    [PendingPasswordSalt] = [token].[PendingPasswordSalt],
                    [PasswordChangeTokenHash] = [token].[PasswordChangeTokenHash],
                    [PasswordChangeTokenSalt] = [token].[PasswordChangeTokenSalt],
                    [PasswordChangeTokenExpiresAt] = [token].[PasswordChangeTokenExpiresAt]
                FROM [Users] AS [user]
                OUTER APPLY (
                    SELECT TOP(1)
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
                    FROM [UserSecurityTokens] AS [token]
                    WHERE [token].[UserId] = [user].[Id]
                    ORDER BY [token].[Id]
                ) AS [token]
                WHERE [token].[Id] IS NOT NULL;
                """);

            migrationBuilder.DropTable(
                name: "UserAddresses");

            migrationBuilder.DropTable(
                name: "UserSecurityTokens");
        }
    }
}
