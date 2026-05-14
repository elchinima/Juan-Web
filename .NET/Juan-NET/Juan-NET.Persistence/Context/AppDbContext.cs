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

        public DbSet<UserAddress> UserAddresses => Set<UserAddress>();

        public DbSet<UserSecurityToken> UserSecurityTokens => Set<UserSecurityToken>();

        public DbSet<Subscriber> Subscribers => Set<Subscriber>();

        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

        public DbSet<AdminRole> AdminRoles => Set<AdminRole>();

        public DbSet<AdminRolePermission> AdminRolePermissions => Set<AdminRolePermission>();

        public DbSet<UserAdminRole> UserAdminRoles => Set<UserAdminRole>();

        public DbSet<UserFavoriteCategory> UserFavoriteCategories => Set<UserFavoriteCategory>();

        public DbSet<FavoriteCategoryDigest> FavoriteCategoryDigests => Set<FavoriteCategoryDigest>();

        public DbSet<BasketItem> BasketItems => Set<BasketItem>();

        public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

        public DbSet<SiteFooterSettings> SiteFooterSettings => Set<SiteFooterSettings>();

        public DbSet<Order> Orders => Set<Order>();

        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

        public DbSet<SupportTicketCreatedDate> SupportTicketCreatedDates => Set<SupportTicketCreatedDate>();

        public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();

        public DbSet<SupportRating> SupportRatings => Set<SupportRating>();

        public DbSet<ProductReview> ProductReviews => Set<ProductReview>();

        public DbSet<SupportOperatorWorkTime> SupportOperatorWorkTimes => Set<SupportOperatorWorkTime>();

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

            modelBuilder.Entity<UserAddress>(entity =>
            {
                entity.Property(address => address.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(address => address.User)
                    .WithMany(user => user.Addresses)
                    .HasForeignKey(address => address.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserSecurityToken>(entity =>
            {
                entity.HasOne(token => token.User)
                    .WithMany(user => user.SecurityTokens)
                    .HasForeignKey(token => token.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(order => order.Subtotal).HasColumnType("decimal(18,2)");
                entity.Property(order => order.DeliveryTotal).HasColumnType("decimal(18,2)");
                entity.Property(order => order.DiscountTotal).HasColumnType("decimal(18,2)");
                entity.Property(order => order.Total).HasColumnType("decimal(18,2)");
                entity.Property(order => order.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(order => order.StripeSessionId).IsUnique().HasFilter("[StripeSessionId] IS NOT NULL");
                entity.HasIndex(order => order.StripePaymentIntentId);

                entity.HasOne(order => order.User)
                    .WithMany(user => user.Orders)
                    .HasForeignKey(order => order.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.Property(item => item.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(item => item.UnitDeliveryPrice).HasColumnType("decimal(18,2)");
                entity.Property(item => item.LineTotal).HasColumnType("decimal(18,2)");

                entity.HasOne(item => item.Order)
                    .WithMany(order => order.Items)
                    .HasForeignKey(item => item.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(item => item.Product)
                    .WithMany(product => product.OrderItems)
                    .HasForeignKey(item => item.ProductId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<SupportTicket>(entity =>
            {
                entity.HasIndex(ticket => ticket.Code).IsUnique();
                entity.Property(ticket => ticket.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(ticket => ticket.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(ticket => ticket.Priority).HasDefaultValue("Medium");
                entity.Property(ticket => ticket.Status).HasDefaultValue("Open");
                entity.Property(ticket => ticket.Topic).HasDefaultValue("Other");
                entity.HasIndex(ticket => ticket.ClosedAt);

                entity.HasOne(ticket => ticket.User)
                    .WithMany(user => user.SupportTickets)
                    .HasForeignKey(ticket => ticket.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ticket => ticket.OperatorUser)
                    .WithMany(user => user.AssignedSupportTickets)
                    .HasForeignKey(ticket => ticket.OperatorUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<SupportTicketCreatedDate>(entity =>
            {
                entity.Property(date => date.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(date => date.SupportTicketId).IsUnique();

                entity.HasOne(date => date.SupportTicket)
                    .WithOne(ticket => ticket.CreatedDate)
                    .HasForeignKey<SupportTicketCreatedDate>(date => date.SupportTicketId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SupportMessage>(entity =>
            {
                entity.Property(message => message.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(message => message.SupportTicket)
                    .WithMany(ticket => ticket.Messages)
                    .HasForeignKey(message => message.SupportTicketId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(message => message.SenderUser)
                    .WithMany(user => user.SupportMessages)
                    .HasForeignKey(message => message.SenderUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<SupportRating>(entity =>
            {
                entity.Property(rating => rating.Rating).HasColumnType("decimal(2,1)");
                entity.Property(rating => rating.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(rating => rating.SupportTicketId).IsUnique();
                entity.HasIndex(rating => new { rating.OperatorUserId, rating.CreatedAt });

                entity.HasOne(rating => rating.SupportTicket)
                    .WithOne(ticket => ticket.Rating)
                    .HasForeignKey<SupportRating>(rating => rating.SupportTicketId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rating => rating.User)
                    .WithMany(user => user.SupportRatings)
                    .HasForeignKey(rating => rating.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(rating => rating.OperatorUser)
                    .WithMany(user => user.OperatorSupportRatings)
                    .HasForeignKey(rating => rating.OperatorUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<ProductReview>(entity =>
            {
                entity.Property(review => review.Rating).HasColumnType("decimal(2,1)");
                entity.Property(review => review.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(review => new { review.ProductId, review.UserId }).IsUnique();
                entity.HasIndex(review => new { review.ProductId, review.CreatedAt });

                entity.HasOne(review => review.Product)
                    .WithMany(product => product.Reviews)
                    .HasForeignKey(review => review.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(review => review.User)
                    .WithMany(user => user.ProductReviews)
                    .HasForeignKey(review => review.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<SupportOperatorWorkTime>(entity =>
            {
                entity.Property(time => time.WorkDate).HasColumnType("date");
                entity.Property(time => time.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(time => new { time.OperatorUserId, time.WorkDate }).IsUnique();

                entity.HasOne(time => time.OperatorUser)
                    .WithMany(user => user.SupportOperatorWorkTimes)
                    .HasForeignKey(time => time.OperatorUserId)
                    .OnDelete(DeleteBehavior.Cascade);
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

            modelBuilder.Entity<FavoriteCategoryDigest>(entity =>
            {
                entity.HasIndex(digest => new { digest.CategoryId, digest.SentForDate }).IsUnique();
                entity.Property(digest => digest.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(digest => digest.Category)
                    .WithMany()
                    .HasForeignKey(digest => digest.CategoryId)
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
