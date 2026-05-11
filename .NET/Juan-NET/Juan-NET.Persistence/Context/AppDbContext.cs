namespace Juan_NET.Persistence.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products => Set<Product>();

        public DbSet<Category> Categories => Set<Category>();

        public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

        public DbSet<Slider> Sliders => Set<Slider>();

        public DbSet<User> Users => Set<User>();

        public DbSet<Subscriber> Subscribers => Set<Subscriber>();

        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

        public DbSet<AdminRole> AdminRoles => Set<AdminRole>();

        public DbSet<AdminRolePermission> AdminRolePermissions => Set<AdminRolePermission>();

        public DbSet<UserAdminRole> UserAdminRoles => Set<UserAdminRole>();

        public DbSet<UserFavoriteCategory> UserFavoriteCategories => Set<UserFavoriteCategory>();

        public DbSet<BasketItem> BasketItems => Set<BasketItem>();

        public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

        public DbSet<SiteFooterSettings> SiteFooterSettings => Set<SiteFooterSettings>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(product => product.Price).HasColumnType("decimal(18,2)");
                entity.Property(product => product.ImageUrl).HasDefaultValue("/main assets/img/product/product-1.jpg");
                entity.Property(product => product.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasIndex(category => category.Name).IsUnique();
            });

            modelBuilder.Entity<ProductCategory>(entity =>
            {
                entity.HasKey(productCategory => new { productCategory.ProductId, productCategory.CategoryId });

                entity.HasOne(productCategory => productCategory.Product)
                    .WithMany(product => product.ProductCategories)
                    .HasForeignKey(productCategory => productCategory.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(productCategory => productCategory.Category)
                    .WithMany(category => category.ProductCategories)
                    .HasForeignKey(productCategory => productCategory.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(user => user.Email).IsUnique();
                entity.Property(user => user.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<AdminRole>(entity =>
            {
                entity.HasIndex(role => role.Name).IsUnique();
                entity.Property(role => role.Color).HasDefaultValue("#e3a51e");
                entity.Property(role => role.DisplayOrder).HasDefaultValue(0);
                entity.Property(role => role.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<AdminRolePermission>(entity =>
            {
                entity.HasKey(permission => new { permission.AdminRoleId, permission.PermissionKey });

                entity.HasOne(permission => permission.AdminRole)
                    .WithMany(role => role.Permissions)
                    .HasForeignKey(permission => permission.AdminRoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserAdminRole>(entity =>
            {
                entity.HasKey(userRole => new { userRole.UserId, userRole.AdminRoleId });

                entity.HasOne(userRole => userRole.User)
                    .WithMany(user => user.AdminRoles)
                    .HasForeignKey(userRole => userRole.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(userRole => userRole.AdminRole)
                    .WithMany(role => role.UserRoles)
                    .HasForeignKey(userRole => userRole.AdminRoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserFavoriteCategory>(entity =>
            {
                entity.HasKey(favorite => new { favorite.UserId, favorite.CategoryId });
                entity.Property(favorite => favorite.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(favorite => favorite.User)
                    .WithMany(user => user.FavoriteCategories)
                    .HasForeignKey(favorite => favorite.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(favorite => favorite.Category)
                    .WithMany(category => category.FavoriteUsers)
                    .HasForeignKey(favorite => favorite.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BasketItem>(entity =>
            {
                entity.HasKey(item => new { item.UserId, item.ProductId });
                entity.Property(item => item.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(item => item.User)
                    .WithMany(user => user.BasketItems)
                    .HasForeignKey(item => item.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(item => item.Product)
                    .WithMany(product => product.BasketItems)
                    .HasForeignKey(item => item.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WishlistItem>(entity =>
            {
                entity.HasKey(item => new { item.UserId, item.ProductId });
                entity.Property(item => item.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(item => item.User)
                    .WithMany(user => user.WishlistItems)
                    .HasForeignKey(item => item.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(item => item.Product)
                    .WithMany(product => product.WishlistItems)
                    .HasForeignKey(item => item.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Subscriber>(entity =>
            {
                entity.HasIndex(subscriber => subscriber.Email).IsUnique();
                entity.Property(subscriber => subscriber.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<ContactMessage>(entity =>
            {
                entity.Property(message => message.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(message => message.Status).HasDefaultValue("New");
            });

            modelBuilder.Entity<SiteFooterSettings>(entity =>
            {
                entity.HasData(new SiteFooterSettings { Id = 1 });
            });
        }
    }
}
