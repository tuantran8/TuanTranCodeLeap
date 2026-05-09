using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TuanTranCodeLeap.Domain.Entities;

namespace TuanTranCodeLeap.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User, Role, int, IdentityUserClaim<int>, UserRole, IdentityUserLogin<int>, IdentityRoleClaim<int>, IdentityUserToken<int>>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure custom table names for ASP.NET Identity
        builder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(e => e.CompanyId);
            entity.Property(e => e.Created);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.IsActive);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Deleted);
            entity.Property(e => e.Guid);
        });

        builder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
        });

        builder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.Property(e => e.CompanyId);
            entity.Property(e => e.IsOwner);
        });

        builder.Entity<IdentityUserClaim<int>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<int>>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        builder.Entity<IdentityRoleClaim<int>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        builder.Entity<IdentityUserToken<int>>(entity =>
        {
            entity.ToTable("UserTokens");
        });

        // Configure Product entity
        builder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.SKU).IsRequired().HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.StockQuantity).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.Created).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.Deleted).IsRequired();
            entity.Property(e => e.Guid).IsRequired();

            entity.HasIndex(e => e.SKU).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Price);
            entity.HasIndex(e => e.StockQuantity);

            // Add check constraints for data validation at database level
            entity.ToTable(table => 
            {
                table.HasCheckConstraint("CK_Product_Price", "[Price] >= 0.01");
                table.HasCheckConstraint("CK_Product_StockQuantity", "[StockQuantity] >= 0");
            });
        });

        // Configure RefreshToken entity
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Expires).IsRequired();
            entity.Property(e => e.IsUsed).IsRequired();
            entity.Property(e => e.IsRevoked).IsRequired();
            entity.Property(e => e.Created).IsRequired();
            entity.Property(e => e.RevokedAt);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token).IsUnique();
        });

        // Configure RevokedToken entity
        builder.Entity<RevokedToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenJti).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.RevokedAt).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(200);

            entity.HasIndex(e => e.TokenJti);
            entity.HasIndex(e => e.ExpiresAt);
        });
    }
}
