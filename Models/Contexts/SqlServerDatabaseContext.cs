using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebSite.Models.Entities;

namespace WebSite.Models.Contexts;

public class SqlServerDatabaseContext : IdentityDbContext<User>
{
    public DbSet<Image>? Images { get; set; }

    public SqlServerDatabaseContext(DbContextOptions options) : base(options)
    {
        //empty
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Image>(imageBuilder =>
        {
            imageBuilder.HasKey(image => image.Id);

            imageBuilder.HasOne(image => image.User)
                .WithMany(user => user.Images)
                .HasForeignKey(image => image.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            imageBuilder.Property(image => image.Description)
                .HasColumnType("nvarchar(128)")
                .HasMaxLength(128)
                .IsRequired();

            imageBuilder.Property(image => image.Url)
                .HasColumnType("nvarchar(256)")
                .HasMaxLength(256)
                .IsRequired();
        });
        modelBuilder.Entity<Image>()
            .ToTable(nameof(Images), builder =>
            {
                builder.HasCheckConstraint($"CK_{nameof(Images)}_{nameof(Image.Url)}", $"[{nameof(Image.Url)}] != ''");
                builder.HasCheckConstraint($"CK_{nameof(Images)}_{nameof(Image.Description)}", $"[{nameof(Image.Description)}] != ''");
            });
        
        base.OnModelCreating(modelBuilder);
    }
    
}