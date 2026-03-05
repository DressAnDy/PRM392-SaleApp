using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using SaleApp.Domain.Entities;

namespace SaleApp.Infrastructure.Data;

public class SaleAppDbContext : DbContext
{
    public SaleAppDbContext(DbContextOptions<SaleAppDbContext> options) : base(options)
    {
    }

    public SaleAppDbContext()
    {
    }

    // DbSets
    public DbSet<Role> Roles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ChatConversation> ChatConversations { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<StoreLocation> StoreLocations { get; set; }

     public static string GetConnectionString(string connectionStringName)
    {
        string? envConnectionString = Environment.GetEnvironmentVariable($"ConnectionStrings__{connectionStringName}");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            return envConnectionString;
        }

        var basePath = FindAppSettingsDirectory();

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string? connectionString = config.GetConnectionString(connectionStringName);
        return connectionString ?? string.Empty;
    }

    private static string FindAppSettingsDirectory()
    {
        var candidates = new[]
        {
            AppDomain.CurrentDomain.BaseDirectory,
            Directory.GetCurrentDirectory(),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, "appsettings.json")))
                return candidate;
        }

        // Walk up and also check sibling directories at each level
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "appsettings.json")))
                return dir.FullName;

            // Check siblings (e.g. SaleApp.API sitting next to SaleApp.Infrastructure)
            if (dir.Parent != null)
            {
                foreach (var sibling in dir.Parent.EnumerateDirectories())
                {
                    if (File.Exists(Path.Combine(sibling.FullName, "appsettings.json")))
                        return sibling.FullName;
                }
            }

            dir = dir.Parent;
        }

        // Fallback
        return Directory.GetCurrentDirectory();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        if(!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql(GetConnectionString("DefaultConnection"))
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.RoleId);
            entity.Property(r => r.RoleName).HasMaxLength(50).IsRequired();
            entity.HasIndex(r => r.RoleName).IsUnique();
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);
            entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Password).HasMaxLength(255).IsRequired();
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);
            entity.Property(u => u.Address).HasMaxLength(255);
            
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        // UserRole configuration
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });
            
            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.CategoryId);
            entity.Property(c => c.CategoryName).HasMaxLength(100).IsRequired();
            entity.HasIndex(c => c.CategoryName).IsUnique();
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.ProductId);
            entity.Property(p => p.ProductName).HasMaxLength(150).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(255);
            entity.Property(p => p.CurrentPrice).HasPrecision(18, 2).IsRequired();
            entity.Property(p => p.ImageUrl).HasMaxLength(255);
            
            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Cart configuration
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(c => c.CartId);
            entity.Property(c => c.Status).HasMaxLength(30);
            
            entity.HasOne(c => c.User)
                .WithMany(u => u.Carts)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CartItem configuration
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(ci => ci.CartItemId);
            entity.Property(ci => ci.UnitPrice).HasPrecision(18, 2);
            
            entity.HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(ci => ci.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(ci => new { ci.CartId, ci.ProductId }).IsUnique();
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.OrderId);
            entity.Property(o => o.OrderStatus).HasMaxLength(30);
            entity.Property(o => o.PaymentMethod).HasMaxLength(50);
            entity.Property(o => o.ShippingAddress).HasMaxLength(255);
            entity.Property(o => o.BillingAddress).HasMaxLength(255);
            entity.Property(o => o.Subtotal).HasPrecision(18, 2);
            entity.Property(o => o.ShippingFee).HasPrecision(18, 2);
            entity.Property(o => o.DiscountAmount).HasPrecision(18, 2);
            
            entity.HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.OrderItemId);
            entity.Property(oi => oi.ProductNameSnapshot).HasMaxLength(150).IsRequired();
            entity.Property(oi => oi.UnitPriceSnapshot).HasPrecision(18, 2);
            
            entity.HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Feedback configuration
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(f => f.FeedbackId);
            
            entity.HasOne(f => f.User)
                .WithMany(u => u.Feedbacks)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(f => f.Product)
                .WithMany(p => p.Feedbacks)
                .HasForeignKey(f => f.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Payment configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.PaymentId);
            entity.Property(p => p.Amount).HasPrecision(18, 2);
            entity.Property(p => p.Currency).HasMaxLength(10);
            entity.Property(p => p.Method).HasMaxLength(50);
            entity.Property(p => p.ProviderTransactionId).HasMaxLength(100);
            entity.Property(p => p.PaymentStatus).HasMaxLength(30);
            
            entity.HasOne(p => p.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.NotificationId);
            entity.Property(n => n.Type).HasMaxLength(50);
            entity.Property(n => n.Title).HasMaxLength(150);
            entity.Property(n => n.Message).HasMaxLength(500);
            entity.Property(n => n.DeepLink).HasMaxLength(255);
            
            entity.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatConversation configuration
        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.HasKey(cc => cc.ConversationId);
            entity.Property(cc => cc.Status).HasMaxLength(30);
            
            entity.HasOne(cc => cc.User)
                .WithMany(u => u.ChatConversations)
                .HasForeignKey(cc => cc.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatMessage configuration
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(cm => cm.ChatMessageId);
            entity.Property(cm => cm.SenderType).HasMaxLength(20);
            
            entity.HasOne(cm => cm.ChatConversation)
                .WithMany(cc => cc.ChatMessages)
                .HasForeignKey(cm => cm.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StoreLocation configuration
        modelBuilder.Entity<StoreLocation>(entity =>
        {
            entity.HasKey(sl => sl.LocationId);
            entity.Property(sl => sl.Address).HasMaxLength(255);
            entity.Property(sl => sl.Latitude).HasPrecision(9, 6);
            entity.Property(sl => sl.Longitude).HasPrecision(9, 6);
        });
    }
}
