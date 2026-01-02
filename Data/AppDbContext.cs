using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Models.User;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<UserSession> UserSessions => Set<UserSession>();
        public DbSet<Offer> Offers => Set<Offer>();
        public DbSet<Store> Stores => Set<Store>();
        public DbSet<Transaction> Transactions => Set<Transaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PhoneNumber).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.HasIndex(e => e.PhoneNumber).IsUnique();
            });

            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.CreatedAtUtc).IsRequired();
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Offer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Store)
                    .WithMany(s => s.Offers)
                    .HasForeignKey(e => e.StoreId);
            });

            modelBuilder.Entity<Store>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("transaction");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.HasIndex(e => e.ReceiptId)
                    .IsUnique();

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.StoreId);
                entity.HasIndex(e => e.CreatedAt);

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .IsRequired();

                entity.Property(e => e.StoreId)
                    .HasColumnName("store_id")
                    .IsRequired();

                entity.Property(e => e.ReceiptId)
                    .HasColumnName("receipt_id")
                    .IsRequired();

                entity.Property(e => e.Price)
                    .HasColumnName("price")
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(e => e.Points)
                    .HasColumnName("points")
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("timestamptz")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.ReceiptDescription)
                    .HasColumnName("receipt_description");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                
            });

        }
    }
}
