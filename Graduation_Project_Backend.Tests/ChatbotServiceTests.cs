using Graduation_Project_Backend.DTOs.Chatbot;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Models.User;
using Graduation_Project_Backend.Service;
using Graduation_Project_Backend.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Graduation_Project_Backend.Tests
{
    public sealed class ChatbotServiceTests
    {
        [Fact]
        public async Task AskAsync_ExactFaqMatch_ReturnsFaqAnswerAndLogsConversation()
        {
            using AppDbContext db = TestInfrastructure.CreateDbContext();
            Guid mallId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            Guid faqId = Guid.NewGuid();

            db.Malls.Add(new Mall { Id = mallId, Name = "City Mall", CreatedAt = DateTimeOffset.UtcNow });
            db.UserProfiles.Add(new UserProfile
            {
                Id = userId,
                Name = "User",
                PhoneNumber = "+962700000007",
                PasswordHash = "hash",
                Role = "user",
                MallID = mallId
            });
            db.Faqs.Add(new Faq
            {
                Id = faqId,
                MallID = mallId,
                Question = "What are the mall opening hours?",
                Answer = "The mall is open from 9 AM to 10 PM.",
                IsActive = true,
                Priority = 10,
                UsageCount = 0,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();

            var accessService = new UserAccessService(db, NullLogger<UserAccessService>.Instance);
            var chatbotService = new ChatbotService(db, accessService, NullLogger<ChatbotService>.Instance);

            ChatbotAnswerResponse response = await chatbotService.AskAsync(userId, new AskChatbotRequest
            {
                Message = "What are the mall opening hours?"
            });

            Assert.Equal(faqId, response.MatchedFaqId);
            Assert.Equal("The mall is open from 9 AM to 10 PM.", response.BotResponse);
            Assert.Single(db.ChatbotConversations);
            Assert.Equal(1, db.Faqs.Single(faq => faq.Id == faqId).UsageCount);
        }

        [Fact]
        public async Task AskAsync_FollowUpStoreQuestion_UsesConversationStoreContext()
        {
            using AppDbContext db = TestInfrastructure.CreateDbContext();
            Guid mallId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            Guid storeId = Guid.NewGuid();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            db.Malls.Add(new Mall { Id = mallId, Name = "City Mall", CreatedAt = now });
            db.UserProfiles.Add(new UserProfile
            {
                Id = userId,
                Name = "User",
                PhoneNumber = "+962700000008",
                PasswordHash = "hash",
                Role = "user",
                MallID = mallId
            });
            db.Stores.Add(new Store
            {
                Id = storeId,
                MallID = mallId,
                Name = "Zara",
                FloorNumber = "First Floor",
                OperatingHours = "10 AM - 10 PM"
            });
            db.Offers.Add(new Offer
            {
                Id = 1,
                StoreId = storeId,
                MallID = mallId,
                Title = "Weekend Sale",
                Description = "20% off",
                IsActive = true,
                StartAt = now.AddDays(-1),
                EndAt = now.AddDays(3),
                MadeAt = now.AddHours(-1)
            });
            await db.SaveChangesAsync();

            ChatbotService chatbotService = CreateService(db);

            ChatbotAnswerResponse firstResponse = await chatbotService.AskAsync(userId, new AskChatbotRequest
            {
                Message = "Tell me about Zara"
            });

            ChatbotAnswerResponse secondResponse = await chatbotService.AskAsync(userId, new AskChatbotRequest
            {
                Message = "what about its offers?",
                ConversationSessionId = firstResponse.ConversationSessionId
            });

            Assert.Contains("Zara", firstResponse.BotResponse);
            Assert.Equal("store_lookup", firstResponse.MatchSource);
            Assert.Equal(firstResponse.ConversationSessionId, secondResponse.ConversationSessionId);
            Assert.Equal("store_live_content", secondResponse.MatchSource);
            Assert.Contains("Active offers for Zara", secondResponse.BotResponse);
            Assert.Contains("Weekend Sale", secondResponse.BotResponse);
        }

        [Fact]
        public async Task AskAsync_PointsQuestion_ReturnsCurrentUserPoints()
        {
            using AppDbContext db = TestInfrastructure.CreateDbContext();
            Guid mallId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();

            db.Malls.Add(new Mall { Id = mallId, Name = "City Mall", CreatedAt = DateTimeOffset.UtcNow });
            db.UserProfiles.Add(new UserProfile
            {
                Id = userId,
                Name = "User",
                PhoneNumber = "+962700000009",
                PasswordHash = "hash",
                Role = "user",
                TotalPoints = 125,
                MallID = mallId
            });
            await db.SaveChangesAsync();

            ChatbotService chatbotService = CreateService(db);

            ChatbotAnswerResponse response = await chatbotService.AskAsync(userId, new AskChatbotRequest
            {
                Message = "How many points do I have?"
            });

            Assert.Equal("user_points", response.MatchSource);
            Assert.Contains("125", response.BotResponse);
            Assert.Contains("loyalty points", response.BotResponse, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AskAsync_CategoryQuestion_ReturnsStoresFromCategory()
        {
            using AppDbContext db = TestInfrastructure.CreateDbContext();
            Guid mallId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            Guid techStoreId = Guid.NewGuid();
            Guid gadgetsStoreId = Guid.NewGuid();

            db.Malls.Add(new Mall { Id = mallId, Name = "City Mall", CreatedAt = DateTimeOffset.UtcNow });
            db.UserProfiles.Add(new UserProfile
            {
                Id = userId,
                Name = "User",
                PhoneNumber = "+962700000010",
                PasswordHash = "hash",
                Role = "user",
                MallID = mallId
            });
            db.Stores.AddRange(
                new Store { Id = techStoreId, MallID = mallId, Name = "Tech Hub", FloorNumber = "Ground Floor" },
                new Store { Id = gadgetsStoreId, MallID = mallId, Name = "Gadget World", FloorNumber = "Second Floor" });
            db.Categories.Add(new Category { Id = 1, MallID = mallId, Name = "Electronics" });
            db.StoreCategories.AddRange(
                new StoreCategory { StoreId = techStoreId, CategoryId = 1 },
                new StoreCategory { StoreId = gadgetsStoreId, CategoryId = 1 });
            await db.SaveChangesAsync();

            ChatbotService chatbotService = CreateService(db);

            ChatbotAnswerResponse response = await chatbotService.AskAsync(userId, new AskChatbotRequest
            {
                Message = "What electronics stores are available?"
            });

            Assert.Equal("category_lookup", response.MatchSource);
            Assert.Contains("Electronics", response.BotResponse);
            Assert.Contains("Tech Hub", response.BotResponse);
            Assert.Contains("Gadget World", response.BotResponse);
        }

        [Fact]
        public async Task AskAsync_RecentReceiptsQuestion_ReturnsUserReceiptSummary()
        {
            using AppDbContext db = TestInfrastructure.CreateDbContext();
            Guid mallId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            Guid storeId = Guid.NewGuid();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            db.Malls.Add(new Mall { Id = mallId, Name = "City Mall", CreatedAt = now });
            db.UserProfiles.Add(new UserProfile
            {
                Id = userId,
                Name = "User",
                PhoneNumber = "+962700000011",
                PasswordHash = "hash",
                Role = "user",
                MallID = mallId
            });
            db.Stores.Add(new Store { Id = storeId, MallID = mallId, Name = "Nike" });
            db.Transactions.AddRange(
                new Transaction
                {
                    Id = 1,
                    UserId = userId,
                    StoreId = storeId,
                    ReceiptId = "R-1001",
                    ReceiptDescription = "Shoes",
                    Price = 25.5m,
                    Points = 25,
                    CreatedAt = now.AddDays(-1)
                },
                new Transaction
                {
                    Id = 2,
                    UserId = userId,
                    StoreId = storeId,
                    ReceiptId = "R-1002",
                    ReceiptDescription = "Socks",
                    Price = 10m,
                    Points = 10,
                    CreatedAt = now
                });
            await db.SaveChangesAsync();

            ChatbotService chatbotService = CreateService(db);

            ChatbotAnswerResponse response = await chatbotService.AskAsync(userId, new AskChatbotRequest
            {
                Message = "Show me my recent receipts"
            });

            Assert.Equal("user_receipts", response.MatchSource);
            Assert.Contains("2 recorded receipts", response.BotResponse);
            Assert.Contains("Recent receipts", response.BotResponse);
            Assert.Contains("Nike", response.BotResponse);
        }

        private static ChatbotService CreateService(AppDbContext db)
        {
            var accessService = new UserAccessService(db, NullLogger<UserAccessService>.Instance);
            return new ChatbotService(db, accessService, NullLogger<ChatbotService>.Instance);
        }
    }
}
