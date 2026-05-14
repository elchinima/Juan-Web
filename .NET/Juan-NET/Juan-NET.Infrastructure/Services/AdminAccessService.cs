namespace Juan_NET.Infrastructure.Services
{
    public class AdminAccessService
    {
        private const string DeveloperEmail = "dmatch96@gmail.com";
        private const string DeveloperRoleName = "Developer";
        private readonly AppDbContext _context;

        public AdminAccessService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminAccessResult> GetUserAccessAsync(ClaimsPrincipal user)
        {
            var idValue = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(idValue, out var userId))
            {
                return new AdminAccessResult();
            }

            var permissions = await _context.UserAdminRoles
                .Where(userRole => userRole.UserId == userId)
                .SelectMany(userRole => userRole.AdminRole.Permissions)
                .Select(permission => permission.PermissionKey)
                .Distinct()
                .ToListAsync();

            return new AdminAccessResult
            {
                Permissions = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase)
            };
        }

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permissionKey)
        {
            var access = await GetUserAccessAsync(user);
            return access.HasPermission(permissionKey);
        }

        public async Task EnsureDeveloperRoleAssignmentAsync(User user)
        {
            if (!string.Equals(user.Email, DeveloperEmail, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await EnsureDeveloperRoleAsync(_context);
        }

        public static async Task EnsureRoleInfrastructureAsync(AppDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[AdminRoles]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [AdminRoles] (
                        [Id] int NOT NULL IDENTITY,
                        [Name] nvarchar(80) NOT NULL,
                        [Color] nvarchar(20) NOT NULL CONSTRAINT [DF_AdminRoles_Color] DEFAULT (N'#e3a51e'),
                        [DisplayOrder] int NOT NULL CONSTRAINT [DF_AdminRoles_DisplayOrder] DEFAULT (0),
                        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
                        CONSTRAINT [PK_AdminRoles] PRIMARY KEY ([Id])
                    );
                    CREATE UNIQUE INDEX [IX_AdminRoles_Name] ON [AdminRoles] ([Name]);
                END

                IF OBJECT_ID(N'[AdminRolePermissions]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [AdminRolePermissions] (
                        [AdminRoleId] int NOT NULL,
                        [PermissionKey] nvarchar(80) NOT NULL,
                        CONSTRAINT [PK_AdminRolePermissions] PRIMARY KEY ([AdminRoleId], [PermissionKey]),
                        CONSTRAINT [FK_AdminRolePermissions_AdminRoles_AdminRoleId] FOREIGN KEY ([AdminRoleId]) REFERENCES [AdminRoles] ([Id]) ON DELETE CASCADE
                    );
                END

                IF OBJECT_ID(N'[UserAdminRoles]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [UserAdminRoles] (
                        [UserId] int NOT NULL,
                        [AdminRoleId] int NOT NULL,
                        CONSTRAINT [PK_UserAdminRoles] PRIMARY KEY ([UserId], [AdminRoleId]),
                        CONSTRAINT [FK_UserAdminRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_UserAdminRoles_AdminRoles_AdminRoleId] FOREIGN KEY ([AdminRoleId]) REFERENCES [AdminRoles] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_UserAdminRoles_AdminRoleId] ON [UserAdminRoles] ([AdminRoleId]);
                END
                """);

            await EnsureRoleDisplayOrderColumnAsync(context);
            await EnsureDeveloperRoleAsync(context);
            await NormalizeRoleDisplayOrderAsync(context);
        }

        private static async Task EnsureRoleDisplayOrderColumnAsync(AppDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync("""
                IF COL_LENGTH(N'[AdminRoles]', N'DisplayOrder') IS NULL
                BEGIN
                    ALTER TABLE [AdminRoles] ADD [DisplayOrder] int NOT NULL CONSTRAINT [DF_AdminRoles_DisplayOrder] DEFAULT (0);
                END
                """);
        }

        private static async Task EnsureDeveloperRoleAsync(AppDbContext context)
        {
            var role = await context.AdminRoles.FirstOrDefaultAsync(item => item.Name == DeveloperRoleName);

            if (role is null)
            {
                role = new AdminRole
                {
                    Name = DeveloperRoleName,
                    Color = "#7c3aed",
                    DisplayOrder = 0
                };
                context.AdminRoles.Add(role);
            }
            else
            {
                role.Color = "#7c3aed";
                role.DisplayOrder = 0;
            }

            await context.SaveChangesAsync();

            var currentPermissions = await context.AdminRolePermissions
                .Where(permission => permission.AdminRoleId == role.Id)
                .Select(permission => permission.PermissionKey)
                .ToListAsync();
            var currentPermissionKeys = currentPermissions.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var permissionKey in AdminPermissionCatalog.AllKeys.Where(permissionKey => !currentPermissionKeys.Contains(permissionKey)))
            {
                context.AdminRolePermissions.Add(new AdminRolePermission { AdminRoleId = role.Id, PermissionKey = permissionKey });
            }

            var userId = await context.Users
                .Where(user => user.Email == DeveloperEmail)
                .Select(user => (int?)user.Id)
                .FirstOrDefaultAsync();

            if (userId.HasValue && !await context.UserAdminRoles.AnyAsync(userRole => userRole.UserId == userId.Value && userRole.AdminRoleId == role.Id))
            {
                context.UserAdminRoles.Add(new UserAdminRole { UserId = userId.Value, AdminRoleId = role.Id });
            }

            await context.SaveChangesAsync();
        }

        private static async Task NormalizeRoleDisplayOrderAsync(AppDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync("""
                IF EXISTS (
                    SELECT 1
                    FROM [AdminRoles]
                    GROUP BY [DisplayOrder]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    ;WITH OrderedRoles AS (
                        SELECT
                            [Id],
                            ROW_NUMBER() OVER (ORDER BY CASE WHEN [Name] = N'Developer' THEN 0 ELSE 1 END, [CreatedAt], [Id]) - 1 AS [NextDisplayOrder]
                        FROM [AdminRoles]
                    )
                    UPDATE role
                    SET [DisplayOrder] = ordered.[NextDisplayOrder]
                    FROM [AdminRoles] role
                    INNER JOIN OrderedRoles ordered ON role.[Id] = ordered.[Id]
                END
                """);
        }
    }
}
