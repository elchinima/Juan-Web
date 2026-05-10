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

            modelBuilder.Entity<Subscriber>(entity =>
            {
                entity.HasIndex(subscriber => subscriber.Email).IsUnique();
                entity.Property(subscriber => subscriber.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<ContactMessage>(entity =>
            {
                entity.Property(message => message.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}
