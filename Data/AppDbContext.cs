using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Models.User;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Graduation_Project_Backend.Data
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<UserSession> UserSessions => Set<UserSession>();
        public DbSet<Mall> Malls => Set<Mall>();
        public DbSet<Store> Stores => Set<Store>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<StoreCategory> StoreCategories => Set<StoreCategory>();
        public DbSet<Manager> Managers => Set<Manager>();
        public DbSet<Management> Management => Set<Management>();
        public DbSet<Offer> Offers => Set<Offer>();
        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<Faq> Faqs => Set<Faq>();
        public DbSet<ChatbotConversation> ChatbotConversations => Set<ChatbotConversation>();
        public DbSet<MallSetting> MallSettings => Set<MallSetting>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<Coupon> Coupons => Set<Coupon>();
        public DbSet<UserCoupon> UserCoupons => Set<UserCoupon>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.ToTable("user_profiles");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("Id").IsRequired();
                entity.Property(e => e.Name).HasColumnName("Name").IsRequired();
                entity.Property(e => e.PhoneNumber).HasColumnName("PhoneNumber").IsRequired();
                entity.Property(e => e.PasswordHash).HasColumnName("PasswordHash").IsRequired();
                entity.Property(e => e.TotalPoints).HasColumnName("TotalPoints").HasDefaultValue(0);
                entity.Property(e => e.Role).HasColumnName("Role").HasDefaultValue("user");
                entity.Property(e => e.MallID).HasColumnName("mall_id").IsRequired();

                entity.HasIndex(e => e.PhoneNumber).IsUnique();
            });

            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.ToTable("user_sessions");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("Id").IsRequired();
                entity.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
                entity.Property(e => e.CreatedAtUtc).HasColumnName("CreatedAtUtc").IsRequired();

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Mall>(entity =>
            {
                entity.ToTable("malls");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id").IsRequired();
                entity.Property(e => e.Name).HasColumnName("name").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
            });

            modelBuilder.Entity<Store>(entity =>
            {
                entity.ToTable("stores");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                      .HasColumnName("id")
                      .IsRequired();

                entity.Property(e => e.Name)
                      .HasColumnName("name")
                      .IsRequired();

                entity.Property(e => e.MallID)
                      .HasColumnName("mall_id")
                      .IsRequired();

                entity.Property(e => e.OperatingHours)
                      .HasColumnName("operating_hours");

                ConfigureJsonDocumentProperty(entity.Property(e => e.SocialMediaLinks))
                    .HasColumnName("social_media_links")
                    .HasColumnType("jsonb");

                entity.Property(e => e.Description)
                      .HasColumnName("description");

                entity.Property(e => e.PhoneNumber)
                      .HasColumnName("phone_number");

                entity.Property(e => e.Email)
                      .HasColumnName("email");

                entity.Property(e => e.FloorNumber)
                      .HasColumnName("floor_number");

                entity.Property(e => e.StoreImageUrl)
                      .HasColumnName("store_image_url");
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("categories");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id").IsRequired();
                entity.Property(e => e.Name).HasColumnName("name").IsRequired();
                entity.Property(e => e.MallID).HasColumnName("mall_id").IsRequired();
            });

            modelBuilder.Entity<StoreCategory>(entity =>
            {
                entity.ToTable("store_category");

                entity.HasKey(e => new { e.StoreId, e.CategoryId });

                entity.Property(e => e.StoreId).HasColumnName("store_id").IsRequired();
                entity.Property(e => e.CategoryId).HasColumnName("category_id").IsRequired();
            });

            modelBuilder.Entity<Manager>(entity =>
            {
                entity.ToTable("managers");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id").IsRequired();
                entity.Property(e => e.Name).HasColumnName("name").IsRequired();
                entity.Property(e => e.Role).HasColumnName("role").IsRequired();
                entity.Property(e => e.MallID).HasColumnName("mall_id").IsRequired();
            });

            modelBuilder.Entity<Management>(entity =>
            {
                entity.ToTable("management");

                entity.HasKey(e => new { e.ManagerId, e.StoreId });

                entity.Property(e => e.ManagerId).HasColumnName("manager_id").IsRequired();
                entity.Property(e => e.StoreId).HasColumnName("store_id").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
            });

            modelBuilder.Entity<Offer>(entity =>
            {
                entity.ToTable("offers");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                      .HasColumnName("id")
                      .ValueGeneratedOnAdd();

                entity.Property(e => e.StoreId)
                      .HasColumnName("store_id")
                      .IsRequired();

                entity.Property(e => e.Title)
                      .HasColumnName("title")
                      .IsRequired();

                entity.Property(e => e.Description)
                      .HasColumnName("description");

                entity.Property(e => e.StartAt)
                      .HasColumnName("start_at")
                      .HasColumnType("timestamptz")
                      .IsRequired();

                entity.Property(e => e.EndAt)
                      .HasColumnName("end_at")
                      .HasColumnType("timestamptz")
                      .IsRequired();

                entity.Property(e => e.IsActive)
                      .HasColumnName("is_active")
                      .HasDefaultValue(true);

                entity.Property(e => e.MadeAt)
                      .HasColumnName("made_at")
                      .HasColumnType("timestamptz")
                      .HasDefaultValueSql("now()");

                entity.Property(e => e.MallID)
                      .HasColumnName("mall_id");
            });

            modelBuilder.Entity<Announcement>(entity =>
            {
                entity.ToTable("announcements");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id").IsRequired();
                entity.Property(e => e.MallID).HasColumnName("mall_id").IsRequired();
                entity.Property(e => e.StoreId).HasColumnName("store_id");
                entity.Property(e => e.ManagerId).HasColumnName("manager_id").IsRequired();
                entity.Property(e => e.Title).HasColumnName("title").IsRequired();
                entity.Property(e => e.Content).HasColumnName("content").IsRequired();
                entity.Property(e => e.AnnouncementType).HasColumnName("announcement_type").IsRequired();
                entity.Property(e => e.Priority).HasColumnName("priority").IsRequired();
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.IsPinned).HasColumnName("is_pinned");
                entity.Property(e => e.ImageUrl).HasColumnName("image_url");
                entity.Property(e => e.StartDate).HasColumnName("start_date").HasColumnType("timestamptz");
                entity.Property(e => e.EndDate).HasColumnName("end_date").HasColumnType("timestamptz");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
            });

            modelBuilder.Entity<Faq>(entity =>
            {
                entity.ToTable("faqs");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id").IsRequired();
                entity.Property(e => e.MallID).HasColumnName("mall_id").IsRequired();
                entity.Property(e => e.Question).HasColumnName("question").IsRequired();
                entity.Property(e => e.Answer).HasColumnName("answer").IsRequired();
                entity.Property(e => e.Category).HasColumnName("category");
                entity.Property(e => e.Keywords).HasColumnName("keywords");
                entity.Property(e => e.Language).HasColumnName("language");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.Priority).HasColumnName("priority");
                entity.Property(e => e.UsageCount).HasColumnName("usage_count");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
            });

            modelBuilder.Entity<ChatbotConversation>(entity =>
            {
                entity.ToTable("chatbot_conversations");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id").IsRequired();
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.SessionId).HasColumnName("session_id").IsRequired();
                entity.Property(e => e.UserMessage).HasColumnName("user_message").IsRequired();
                entity.Property(e => e.BotResponse).HasColumnName("bot_response").IsRequired();
                entity.Property(e => e.MatchedFaqId).HasColumnName("matched_faq_id");
                entity.Property(e => e.ResponseTimeMs).HasColumnName("response_time_ms");
                entity.Property(e => e.WasHelpful).HasColumnName("was_helpful");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
            });

            modelBuilder.Entity<MallSetting>(entity =>
            {
                entity.ToTable("mall_settings");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id").IsRequired();
                entity.Property(e => e.MallID).HasColumnName("mall_id").IsRequired();
                ConfigureJsonDocumentProperty(entity.Property(e => e.OperatingHours)).HasColumnName("operating_hours").HasColumnType("jsonb");
                ConfigureJsonDocumentProperty(entity.Property(e => e.ContactInfo)).HasColumnName("contact_info").HasColumnType("jsonb");
                entity.Property(e => e.ParkingInfo).HasColumnName("parking_info");
                ConfigureJsonDocumentProperty(entity.Property(e => e.Services)).HasColumnName("services").HasColumnType("jsonb");
                ConfigureJsonDocumentProperty(entity.Property(e => e.LoyaltyPointsConfig)).HasColumnName("loyalty_points_config").HasColumnType("jsonb");
                ConfigureJsonDocumentProperty(entity.Property(e => e.NotificationSettings)).HasColumnName("notification_settings").HasColumnType("jsonb");
                entity.Property(e => e.MapImageUrl).HasColumnName("map_image_url");
                entity.Property(e => e.LogoUrl).HasColumnName("logo_url");
                ConfigureJsonDocumentProperty(entity.Property(e => e.ThemeColors)).HasColumnName("theme_colors").HasColumnType("jsonb");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("notifications");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id").IsRequired();
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.Title).HasColumnName("title").IsRequired();
                entity.Property(e => e.Message).HasColumnName("message").IsRequired();
                entity.Property(e => e.NotificationType).HasColumnName("notification_type");
                entity.Property(e => e.CategoryId).HasColumnName("category_id");
                entity.Property(e => e.IsRead).HasColumnName("is_read");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
                entity.Property(e => e.ScheduledFor).HasColumnName("scheduled_for").HasColumnType("timestamptz");
                entity.Property(e => e.SentAt).HasColumnName("sent_at").HasColumnType("timestamptz");
                ConfigureJsonDocumentProperty(entity.Property(e => e.Metadata)).HasColumnName("metadata").HasColumnType("jsonb");
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("transactions");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                      .HasColumnName("id")
                      .ValueGeneratedOnAdd();

                entity.Property(e => e.UserId)
                      .HasColumnName("user_id")
                      .IsRequired();

                entity.Property(e => e.StoreId)
                      .HasColumnName("store_id")
                      .IsRequired();

                entity.Property(e => e.ReceiptId)
                      .HasColumnName("receipt_id")
                      .IsRequired();

                entity.HasIndex(e => e.ReceiptId).IsUnique();

                entity.Property(e => e.Price)
                      .HasColumnName("price")
                      .HasPrecision(18, 2)
                      .IsRequired();

                entity.Property(e => e.Points)
                      .HasColumnName("points")
                      .IsRequired();

                entity.Property(e => e.ReceiptDescription)
                      .HasColumnName("receipt_description");

                entity.Property(e => e.CreatedAt)
                      .HasColumnName("created_at")
                      .HasColumnType("timestamptz")
                      .HasDefaultValueSql("now()");

                entity.Property(e => e.ReceiptUrl)
                      .HasColumnName("receipt_url");

                entity.Property(e => e.ReceiptImageUrl)
                      .HasColumnName("receipt_image_url");

                entity.Property(e => e.TransactionStatus)
                      .HasColumnName("transaction_status");

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Store)
                      .WithMany()
                      .HasForeignKey(e => e.StoreId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Coupon>(entity =>
            {
                entity.ToTable("coupons");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                      .HasColumnName("id")
                      .ValueGeneratedOnAdd();

                entity.Property(e => e.CreatedAt)
                      .HasColumnName("created_at")
                      .HasColumnType("timestamptz")
                      .HasDefaultValueSql("now()");

                entity.Property(e => e.ManagerId)
                      .HasColumnName("manager_id")
                      .IsRequired();

                entity.Property(e => e.Type)
                      .HasColumnName("type")
                      .IsRequired();

                entity.Property(e => e.StartAt)
                      .HasColumnName("start_at")
                      .HasColumnType("timestamptz")
                      .IsRequired();

                entity.Property(e => e.EndAt)
                      .HasColumnName("end_at")
                      .HasColumnType("timestamptz")
                      .IsRequired();

                entity.Property(e => e.Discription)
                      .HasColumnName("description");

                entity.Property(e => e.IsActive)
                      .HasColumnName("is_active")
                      .HasDefaultValue(true);

                entity.Property(e => e.CostPoint)
                      .HasColumnName("cost_point");

                entity.Property(e => e.MallID)
                      .HasColumnName("mall_id")
                      .IsRequired();
            });

            modelBuilder.Entity<UserCoupon>(entity =>
            {
                entity.ToTable("users_coupons");

                entity.HasKey(e => e.SerialNumber);

                entity.Property(e => e.SerialNumber)
                      .HasColumnName("serial_number")
                      .HasMaxLength(8)
                      .IsRequired();

                entity.Property(e => e.UserId)
                      .HasColumnName("user_id")
                      .IsRequired();

                entity.Property(e => e.CouponId)
                      .HasColumnName("coupon_id")
                      .IsRequired();

                entity.Property(e => e.IsRedeemed)
                      .HasColumnName("is_redeemed")
                      .HasDefaultValue(false);

                entity.Property(e => e.CreatedAt)
                      .HasColumnName("created_at")
                      .HasDefaultValueSql("now()");

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Coupon)
                      .WithMany()
                      .HasForeignKey(e => e.CouponId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static PropertyBuilder<JsonDocument?> ConfigureJsonDocumentProperty(PropertyBuilder<JsonDocument?> propertyBuilder)
        {
            propertyBuilder.HasConversion(
                document => document == null ? null : document.RootElement.GetRawText(),
                json => ParseJsonDocument(json));

            propertyBuilder.Metadata.SetValueComparer(new ValueComparer<JsonDocument?>(
                (left, right) => JsonEquals(left, right),
                document => document == null ? 0 : document.RootElement.GetRawText().GetHashCode(),
                document => CloneJsonDocument(document)));

            return propertyBuilder;
        }

        private static JsonDocument? ParseJsonDocument(string? json)
            => string.IsNullOrWhiteSpace(json) ? null : JsonDocument.Parse(json, default);

        private static JsonDocument? CloneJsonDocument(JsonDocument? document)
            => document == null ? null : JsonDocument.Parse(document.RootElement.GetRawText(), default);

        private static bool JsonEquals(JsonDocument? left, JsonDocument? right)
        {
            if (left == null && right == null)
                return true;

            if (left == null || right == null)
                return false;

            return left.RootElement.GetRawText() == right.RootElement.GetRawText();
        }
    }
}
