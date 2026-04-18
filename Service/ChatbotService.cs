using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.DTOs.Chatbot;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Models.User;
using Graduation_Project_Backend.Service.Common;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service
{
    public sealed class ChatbotService : IChatbotService
    {
        private static readonly Regex MultiWhitespaceRegex = new("\\s+", RegexOptions.Compiled);

        private static readonly HashSet<string> HoursKeywords = ["hour", "hours", "open", "opening", "close", "closing", "time", "times", "today"];
        private static readonly HashSet<string> ParkingKeywords = ["parking", "park", "car"];
        private static readonly HashSet<string> ServicesKeywords = ["service", "services", "wifi", "atm", "prayer", "wheelchair", "kids", "play", "cinema", "restroom"];
        private static readonly HashSet<string> ContactKeywords = ["contact", "phone", "email", "address", "call", "desk"];
        private static readonly HashSet<string> OfferKeywords = ["offer", "offers", "promotion", "promotions", "discount", "sale", "sales", "deal", "deals"];
        private static readonly HashSet<string> AnnouncementKeywords = ["announcement", "announcements", "news", "event", "events", "update", "updates", "happening"];
        private static readonly HashSet<string> LoyaltyKeywords = ["loyalty", "reward", "rewards", "point", "points"];
        private static readonly HashSet<string> CouponKeywords = ["coupon", "coupons", "voucher", "vouchers", "redeem", "redeemed"];
        private static readonly HashSet<string> ReceiptKeywords = ["receipt", "receipts", "transaction", "transactions", "purchase", "purchases", "bought", "buy", "spend", "spent"];
        private static readonly HashSet<string> PersonalKeywords = ["my", "me", "mine", "i", "im", "i'm", "have", "earned", "balance", "own"];
        private static readonly HashSet<string> RecentKeywords = ["recent", "latest", "last", "history"];
        private static readonly HashSet<string> LocationKeywords = ["where", "location", "located", "floor", "find", "map", "near"];
        private static readonly HashSet<string> DetailsKeywords = ["about", "details", "detail", "info", "information", "describe", "description"];
        private static readonly HashSet<string> FollowUpKeywords = ["it", "its", "they", "them", "that", "this", "those", "these", "same", "also", "another"];
        private static readonly HashSet<string> StoreKeywords = ["store", "stores", "shop", "shops", "branch", "branches"];
        private static readonly HashSet<string> GreetingKeywords = ["hi", "hello", "hey", "welcome"];

        private readonly AppDbContext _db;
        private readonly IUserAccessService _userAccessService;
        private readonly ILogger<ChatbotService> _logger;

        public ChatbotService(AppDbContext db, IUserAccessService userAccessService, ILogger<ChatbotService> logger)
        {
            _db = db;
            _userAccessService = userAccessService;
            _logger = logger;
        }

        public async Task<ChatbotAnswerResponse> AskAsync(Guid currentUserId, AskChatbotRequest request, CancellationToken cancellationToken = default)
        {
            string userMessage = NormalizeRequired(request.Message, "Message is required.");
            UserAccessContext access = await _userAccessService.GetUserAccessContextAsync(currentUserId, cancellationToken);
            Guid conversationSessionId = request.ConversationSessionId ?? Guid.NewGuid();
            DateTimeOffset createdAt = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            string normalizedMessage = NormalizeForMatching(userMessage);
            HashSet<string> tokens = Tokenize(normalizedMessage);

            List<Faq> faqs = await _db.Faqs
                .AsNoTracking()
                .Where(faq => faq.MallID == access.MallID && faq.IsActive)
                .OrderByDescending(faq => faq.Priority)
                .ThenByDescending(faq => faq.UpdatedAt)
                .ToListAsync(cancellationToken);

            List<StoreKnowledge> stores = await LoadStoreKnowledgeAsync(access.MallID, cancellationToken);
            ConversationContext conversationContext = await BuildConversationContextAsync(
                currentUserId,
                conversationSessionId,
                stores,
                cancellationToken);

            ChatbotResolution resolution =
                TryResolveGreeting(tokens)
                ?? TryResolveExactFaq(faqs, normalizedMessage)
                ?? await TryBuildPersonalizedResponseAsync(currentUserId, access.MallID, normalizedMessage, tokens, createdAt, cancellationToken)
                ?? await TryBuildStoreOrCategoryResponseAsync(access.MallID, normalizedMessage, tokens, stores, conversationContext, createdAt, cancellationToken)
                ?? await TryBuildMallSettingsResponseAsync(access.MallID, normalizedMessage, tokens, createdAt, cancellationToken)
                ?? await TryBuildOffersAndAnnouncementsResponseAsync(access.MallID, tokens, createdAt, cancellationToken)
                ?? TryResolveKeywordFaq(faqs, normalizedMessage, tokens)
                ?? BuildFallbackResolution(stores);

            if (resolution.MatchedFaqId.HasValue)
            {
                Faq? trackedFaq = await _db.Faqs.SingleOrDefaultAsync(faq => faq.Id == resolution.MatchedFaqId.Value, cancellationToken);
                if (trackedFaq != null)
                    trackedFaq.UsageCount += 1;
            }

            stopwatch.Stop();
            int responseTimeMs = Math.Max(1, (int)stopwatch.ElapsedMilliseconds);

            var conversation = new ChatbotConversation
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                SessionId = conversationSessionId,
                UserMessage = userMessage,
                BotResponse = resolution.BotResponse,
                MatchedFaqId = resolution.MatchedFaqId,
                ResponseTimeMs = responseTimeMs,
                WasHelpful = null,
                CreatedAt = createdAt
            };

            _db.ChatbotConversations.Add(conversation);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Logged chatbot interaction {ConversationId} for user {UserId} with source {MatchSource}.",
                conversation.Id,
                currentUserId,
                resolution.MatchSource);

            return new ChatbotAnswerResponse
            {
                ConversationId = conversation.Id,
                ConversationSessionId = conversation.SessionId,
                UserMessage = userMessage,
                BotResponse = resolution.BotResponse,
                MatchedFaqId = resolution.MatchedFaqId,
                MatchSource = resolution.MatchSource,
                ResponseTimeMs = responseTimeMs,
                CreatedAt = createdAt
            };
        }

        public async Task<IReadOnlyList<ChatbotHistoryItemResponse>> GetHistoryAsync(Guid currentUserId, Guid? conversationSessionId, CancellationToken cancellationToken = default)
        {
            IQueryable<ChatbotConversation> query = _db.ChatbotConversations
                .AsNoTracking()
                .Where(conversation => conversation.UserId == currentUserId);

            if (conversationSessionId.HasValue)
                query = query.Where(conversation => conversation.SessionId == conversationSessionId.Value);

            return await query
                .OrderByDescending(conversation => conversation.CreatedAt)
                .Select(conversation => new ChatbotHistoryItemResponse
                {
                    ConversationId = conversation.Id,
                    ConversationSessionId = conversation.SessionId,
                    UserMessage = conversation.UserMessage,
                    BotResponse = conversation.BotResponse,
                    MatchedFaqId = conversation.MatchedFaqId,
                    ResponseTimeMs = conversation.ResponseTimeMs,
                    WasHelpful = conversation.WasHelpful,
                    CreatedAt = conversation.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }

        private static ChatbotResolution? TryResolveGreeting(HashSet<string> messageTokens)
        {
            if (!messageTokens.Overlaps(GreetingKeywords))
                return null;

            return new ChatbotResolution(
                "Hello. I can help with mall hours, parking, services, store details, current offers and announcements, your points, your coupons, and your recent receipts.",
                "greeting");
        }

        private static ChatbotResolution? TryResolveExactFaq(IReadOnlyList<Faq> faqs, string normalizedMessage)
        {
            foreach (Faq faq in faqs)
            {
                if (NormalizeForMatching(faq.Question) == normalizedMessage)
                    return new ChatbotResolution(faq.Answer, "faq_exact", faq.Id);
            }

            return null;
        }

        private static ChatbotResolution? TryResolveKeywordFaq(IReadOnlyList<Faq> faqs, string normalizedMessage, HashSet<string> messageTokens)
        {
            int bestScore = 0;
            Faq? matchedFaq = null;

            foreach (Faq faq in faqs)
            {
                HashSet<string> faqTokens = Tokenize(NormalizeForMatching(faq.Question));
                if (faq.Keywords != null)
                {
                    foreach (string keyword in faq.Keywords)
                    {
                        foreach (string token in Tokenize(NormalizeForMatching(keyword)))
                            faqTokens.Add(token);
                    }
                }

                if (!string.IsNullOrWhiteSpace(faq.Category))
                {
                    foreach (string token in Tokenize(NormalizeForMatching(faq.Category)))
                        faqTokens.Add(token);
                }

                int overlap = faqTokens.Intersect(messageTokens).Count();
                if (overlap == 0)
                    continue;

                bool categoryMatch = !string.IsNullOrWhiteSpace(faq.Category) &&
                    normalizedMessage.Contains(NormalizeForMatching(faq.Category), StringComparison.Ordinal);

                bool looseOneWordHit = overlap == 1 && messageTokens.Count > 3 && faqTokens.Count > 4 && !categoryMatch;
                if (looseOneWordHit)
                    continue;

                int score = overlap * 12 + faq.Priority + (categoryMatch ? 6 : 0);
                if (score > bestScore)
                {
                    bestScore = score;
                    matchedFaq = faq;
                }
            }

            return matchedFaq == null ? null : new ChatbotResolution(matchedFaq.Answer, "faq_keyword", matchedFaq.Id);
        }

        private async Task<ChatbotResolution?> TryBuildPersonalizedResponseAsync(
            Guid currentUserId,
            Guid mallId,
            string normalizedMessage,
            HashSet<string> messageTokens,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            bool asksAboutPoints = messageTokens.Overlaps(LoyaltyKeywords);
            bool asksAboutCoupons = messageTokens.Overlaps(CouponKeywords);
            bool asksAboutReceipts = messageTokens.Overlaps(ReceiptKeywords);
            bool isPersonalRequest = IsPersonalRequest(normalizedMessage, messageTokens);

            if (!isPersonalRequest || (!asksAboutPoints && !asksAboutCoupons && !asksAboutReceipts))
                return null;

            var sections = new List<string>();
            var sourceParts = new List<string>();

            if (asksAboutPoints)
            {
                UserProfile? user = await _db.UserProfiles
                    .AsNoTracking()
                    .SingleOrDefaultAsync(profile => profile.Id == currentUserId, cancellationToken);

                if (user != null)
                {
                    sections.Add($"You currently have {user.TotalPoints} loyalty point{Suffix(user.TotalPoints)}.");
                    sourceParts.Add("points");
                }
            }

            if (asksAboutCoupons)
            {
                List<UserCouponSnapshot> coupons = await _db.UserCoupons
                    .AsNoTracking()
                    .Where(userCoupon => userCoupon.UserId == currentUserId)
                    .Join(
                        _db.Coupons.AsNoTracking().Where(coupon => coupon.MallID == mallId),
                        userCoupon => userCoupon.CouponId,
                        coupon => coupon.Id,
                        (userCoupon, coupon) => new UserCouponSnapshot(
                            userCoupon.SerialNumber,
                            coupon.Type,
                            coupon.StartAt,
                            coupon.EndAt,
                            coupon.IsActive,
                            userCoupon.IsRedeemed))
                    .OrderByDescending(coupon => coupon.EndAt)
                    .ToListAsync(cancellationToken);

                int activeCount = coupons.Count(coupon => coupon.IsActive && !coupon.IsRedeemed && coupon.StartAt <= now && coupon.EndAt >= now);
                int redeemedCount = coupons.Count(coupon => coupon.IsRedeemed);

                if (coupons.Count == 0)
                {
                    sections.Add("You do not have any coupons in this mall yet.");
                }
                else
                {
                    string summary = $"You have {activeCount} active coupon{Suffix(activeCount)}";
                    if (redeemedCount > 0)
                        summary += $" and {redeemedCount} redeemed coupon{Suffix(redeemedCount)}";

                    List<string> activeCoupons = coupons
                        .Where(coupon => coupon.IsActive && !coupon.IsRedeemed && coupon.StartAt <= now && coupon.EndAt >= now)
                        .Take(3)
                        .Select(coupon => $"{coupon.Type} ({coupon.SerialNumber}, valid until {FormatDate(coupon.EndAt)})")
                        .ToList();

                    summary += activeCoupons.Count > 0
                        ? $". Active coupons: {string.Join("; ", activeCoupons)}."
                        : ".";

                    sections.Add(summary);
                }

                sourceParts.Add("coupons");
            }

            if (asksAboutReceipts)
            {
                IQueryable<UserReceiptSnapshot> query = _db.Transactions
                    .AsNoTracking()
                    .Where(transaction => transaction.UserId == currentUserId)
                    .Join(
                        _db.Stores.AsNoTracking().Where(store => store.MallID == mallId),
                        transaction => transaction.StoreId,
                        store => store.Id,
                        (transaction, store) => new UserReceiptSnapshot(
                            transaction.Id,
                            transaction.ReceiptId,
                            transaction.Price,
                            transaction.Points,
                            transaction.CreatedAt,
                            store.Name,
                            transaction.TransactionStatus));

                int receiptCount = await query.CountAsync(cancellationToken);
                if (receiptCount == 0)
                {
                    sections.Add("You do not have any recorded receipts yet.");
                }
                else
                {
                    decimal totalSpend = await query.SumAsync(receipt => receipt.Price, cancellationToken);
                    List<UserReceiptSnapshot> recentReceipts = await query
                        .OrderByDescending(receipt => receipt.CreatedAt)
                        .Take(3)
                        .ToListAsync(cancellationToken);

                    UserReceiptSnapshot latestReceipt = recentReceipts[0];
                    var summaryBuilder = new StringBuilder();
                    summaryBuilder.Append($"You have {receiptCount} recorded receipt{Suffix(receiptCount)} with total spend {FormatAmount(totalSpend)}. ");
                    summaryBuilder.Append($"Latest purchase: {latestReceipt.StoreName} on {FormatDate(latestReceipt.CreatedAt)} for {FormatAmount(latestReceipt.Price)}");

                    if (latestReceipt.Points > 0)
                        summaryBuilder.Append($" and {latestReceipt.Points} point{Suffix(latestReceipt.Points)} earned");

                    summaryBuilder.Append('.');

                    if (messageTokens.Overlaps(RecentKeywords) || normalizedMessage.Contains("latest", StringComparison.Ordinal) || normalizedMessage.Contains("last", StringComparison.Ordinal))
                    {
                        List<string> recentItems = recentReceipts
                            .Select(receipt => $"{receipt.StoreName} on {FormatDate(receipt.CreatedAt)} for {FormatAmount(receipt.Price)}")
                            .ToList();

                        summaryBuilder.Append($" Recent receipts: {string.Join("; ", recentItems)}.");
                    }

                    sections.Add(summaryBuilder.ToString());
                }

                sourceParts.Add("receipts");
            }

            if (sections.Count == 0)
                return null;

            string source = sourceParts.Count == 1 ? $"user_{sourceParts[0]}" : "user_context";
            return new ChatbotResolution(string.Join(" ", sections), source);
        }

        private async Task<ChatbotResolution?> TryBuildStoreOrCategoryResponseAsync(
            Guid mallId,
            string normalizedMessage,
            HashSet<string> messageTokens,
            IReadOnlyList<StoreKnowledge> stores,
            ConversationContext conversationContext,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            StoreKnowledge? explicitStore = FindExplicitStoreMatch(stores, normalizedMessage, messageTokens);
            StoreKnowledge? store = explicitStore;

            if (store == null && conversationContext.ReferencedStore != null && ShouldUseConversationStore(messageTokens, normalizedMessage))
                store = conversationContext.ReferencedStore;

            if (store != null)
            {
                bool wantsOffers = messageTokens.Overlaps(OfferKeywords);
                bool wantsAnnouncements = messageTokens.Overlaps(AnnouncementKeywords);
                bool wantsLocation = messageTokens.Overlaps(LocationKeywords);
                bool wantsHours = messageTokens.Overlaps(HoursKeywords);
                bool wantsContact = messageTokens.Overlaps(ContactKeywords);
                bool wantsDetails = messageTokens.Overlaps(DetailsKeywords) ||
                    normalizedMessage == store.NormalizedName ||
                    normalizedMessage.Contains("tell me about", StringComparison.Ordinal) ||
                    normalizedMessage.Contains("show me", StringComparison.Ordinal);

                if (wantsOffers || wantsAnnouncements)
                    return await BuildStoreContentResponseAsync(store, wantsOffers, wantsAnnouncements, now, cancellationToken);

                var sections = new List<string>();

                if (store.Categories.Count > 0 && (wantsDetails || (!wantsLocation && !wantsHours && !wantsContact)))
                    sections.Add($"{store.Name} is in {string.Join(", ", store.Categories)}.");
                else
                    sections.Add($"{store.Name} is available in this mall.");

                if (wantsLocation || (!wantsHours && !wantsContact))
                {
                    if (!string.IsNullOrWhiteSpace(store.FloorNumber))
                        sections.Add($"It is located on {store.FloorNumber}.");
                    else if (wantsLocation)
                        sections.Add("Its floor is not recorded yet.");
                }

                if (wantsHours || (!wantsLocation && !wantsContact))
                {
                    if (!string.IsNullOrWhiteSpace(store.OperatingHours))
                        sections.Add($"Recorded hours: {store.OperatingHours}.");
                    else if (wantsHours)
                        sections.Add("Its operating hours are not recorded yet.");
                }

                if (wantsDetails && !string.IsNullOrWhiteSpace(store.Description))
                    sections.Add($"Description: {store.Description.Trim()}.");

                if (wantsContact || wantsDetails || (!wantsLocation && !wantsHours))
                {
                    List<string> contactParts = [];
                    if (!string.IsNullOrWhiteSpace(store.PhoneNumber))
                        contactParts.Add($"phone {store.PhoneNumber}");
                    if (!string.IsNullOrWhiteSpace(store.Email))
                        contactParts.Add($"email {store.Email}");

                    string? socialLinks = FormatJsonDocument(store.SocialMediaLinks);
                    if (!string.IsNullOrWhiteSpace(socialLinks))
                        contactParts.Add($"social {socialLinks}");

                    if (contactParts.Count > 0)
                        sections.Add($"Contact: {string.Join(", ", contactParts)}.");
                    else if (wantsContact)
                        sections.Add("I do not have contact details for this store yet.");
                }

                return new ChatbotResolution(string.Join(" ", sections), explicitStore == null ? "store_follow_up" : "store_lookup");
            }

            List<CategoryMatch> matchedCategories = FindCategoryMatches(stores, normalizedMessage, messageTokens);
            if (matchedCategories.Count == 0)
                return null;

            List<StoreKnowledge> matchingStores = stores
                .Where(store => store.Categories.Any(category =>
                    matchedCategories.Any(match => string.Equals(match.Name, category, StringComparison.OrdinalIgnoreCase))))
                .Take(5)
                .ToList();

            if (matchingStores.Count == 0)
                return null;

            string categoryLabel = string.Join(", ", matchedCategories.Select(match => match.Name).Distinct(StringComparer.OrdinalIgnoreCase));
            List<string> storeSummaries = matchingStores
                .Select(store => !string.IsNullOrWhiteSpace(store.FloorNumber)
                    ? $"{store.Name} ({store.FloorNumber})"
                    : store.Name)
                .ToList();

            return new ChatbotResolution($"Stores in {categoryLabel}: {string.Join("; ", storeSummaries)}.", "category_lookup");
        }

        private async Task<ChatbotResolution> BuildStoreContentResponseAsync(
            StoreKnowledge store,
            bool wantsOffers,
            bool wantsAnnouncements,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            bool includeOffers = wantsOffers || !wantsAnnouncements;
            bool includeAnnouncements = wantsAnnouncements || !wantsOffers;
            var sections = new List<string>();

            if (includeOffers)
            {
                List<string> offers = await _db.Offers
                    .AsNoTracking()
                    .Where(offer => offer.StoreId == store.Id && offer.IsActive && offer.StartAt <= now && offer.EndAt >= now)
                    .OrderBy(offer => offer.EndAt)
                    .Select(offer => $"{offer.Title} (until {FormatDate(offer.EndAt)})")
                    .Take(3)
                    .ToListAsync(cancellationToken);

                if (offers.Count > 0)
                    sections.Add($"Active offers for {store.Name}: {string.Join("; ", offers)}.");
                else if (wantsOffers)
                    sections.Add($"There are no active offers for {store.Name} right now.");
            }

            if (includeAnnouncements)
            {
                List<string> announcements = await _db.Announcements
                    .AsNoTracking()
                    .Where(announcement => announcement.StoreId == store.Id && announcement.IsActive && announcement.StartDate <= now && announcement.EndDate >= now)
                    .OrderByDescending(announcement => announcement.IsPinned)
                    .ThenBy(announcement => announcement.EndDate)
                    .Select(announcement => $"{announcement.Title} (until {FormatDate(announcement.EndDate)})")
                    .Take(3)
                    .ToListAsync(cancellationToken);

                if (announcements.Count > 0)
                    sections.Add($"Current announcements for {store.Name}: {string.Join("; ", announcements)}.");
                else if (wantsAnnouncements)
                    sections.Add($"There are no active announcements for {store.Name} right now.");
            }

            if (sections.Count == 0)
                sections.Add($"There are no active offers or announcements for {store.Name} right now.");

            return new ChatbotResolution(string.Join(" ", sections), "store_live_content");
        }

        private async Task<ChatbotResolution?> TryBuildMallSettingsResponseAsync(
            Guid mallId,
            string normalizedMessage,
            HashSet<string> messageTokens,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            bool wantsHours = messageTokens.Overlaps(HoursKeywords);
            bool wantsParking = messageTokens.Overlaps(ParkingKeywords);
            bool wantsServices = messageTokens.Overlaps(ServicesKeywords);
            bool wantsContact = messageTokens.Overlaps(ContactKeywords);
            bool wantsMap = messageTokens.Overlaps(LocationKeywords) && normalizedMessage.Contains("mall", StringComparison.Ordinal);
            bool wantsLoyaltyProgram = messageTokens.Overlaps(LoyaltyKeywords) && !IsPersonalRequest(normalizedMessage, messageTokens);

            if (!wantsHours && !wantsParking && !wantsServices && !wantsContact && !wantsMap && !wantsLoyaltyProgram)
                return null;

            MallSetting? settings = await _db.MallSettings
                .AsNoTracking()
                .SingleOrDefaultAsync(setting => setting.MallID == mallId, cancellationToken);

            if (settings == null)
                return null;

            var sections = new List<string>();

            if (wantsHours)
            {
                string? todayHours = TryFormatTodayHours(settings.OperatingHours, now);
                string? fullHours = FormatJsonDocument(settings.OperatingHours);
                string? hoursSection = todayHours ?? fullHours;

                if (!string.IsNullOrWhiteSpace(hoursSection))
                    sections.Add($"Mall hours: {hoursSection}.");
            }

            if (wantsParking && !string.IsNullOrWhiteSpace(settings.ParkingInfo))
                sections.Add($"Parking information: {settings.ParkingInfo.Trim()}.");

            if (wantsServices)
            {
                string? services = FormatJsonDocument(settings.Services);
                if (!string.IsNullOrWhiteSpace(services))
                    sections.Add($"Available services: {services}.");
            }

            if (wantsContact)
            {
                string? contact = FormatJsonDocument(settings.ContactInfo);
                if (!string.IsNullOrWhiteSpace(contact))
                    sections.Add($"Contact information: {contact}.");
            }

            if (wantsMap && !string.IsNullOrWhiteSpace(settings.MapImageUrl))
                sections.Add($"Mall map: {settings.MapImageUrl.Trim()}.");

            if (wantsLoyaltyProgram)
            {
                string? loyaltyInfo = FormatJsonDocument(settings.LoyaltyPointsConfig);
                if (!string.IsNullOrWhiteSpace(loyaltyInfo))
                    sections.Add($"Loyalty program: {loyaltyInfo}.");
            }

            return sections.Count == 0 ? null : new ChatbotResolution(string.Join(" ", sections), "mall_settings");
        }

        private async Task<ChatbotResolution?> TryBuildOffersAndAnnouncementsResponseAsync(
            Guid mallId,
            HashSet<string> messageTokens,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            bool wantsOffers = messageTokens.Overlaps(OfferKeywords);
            bool wantsAnnouncements = messageTokens.Overlaps(AnnouncementKeywords);
            bool wantsContent = wantsOffers || wantsAnnouncements;

            if (!wantsContent)
                return null;

            bool includeOffers = wantsOffers || !wantsAnnouncements;
            bool includeAnnouncements = wantsAnnouncements || !wantsOffers;
            var sections = new List<string>();

            if (includeOffers)
            {
                List<string> offers = await _db.Offers
                    .AsNoTracking()
                    .Where(offer => offer.MallID == mallId && offer.IsActive && offer.StartAt <= now && offer.EndAt >= now)
                    .GroupJoin(
                        _db.Stores.AsNoTracking(),
                        offer => offer.StoreId,
                        store => store.Id,
                        (offer, stores) => new
                        {
                            offer.Title,
                            offer.EndAt,
                            StoreName = stores.Select(store => store.Name).FirstOrDefault()
                        })
                    .OrderBy(offer => offer.EndAt)
                    .Take(5)
                    .Select(offer => string.IsNullOrWhiteSpace(offer.StoreName)
                        ? $"{offer.Title} (until {FormatDate(offer.EndAt)})"
                        : $"{offer.StoreName}: {offer.Title} (until {FormatDate(offer.EndAt)})")
                    .ToListAsync(cancellationToken);

                if (offers.Count > 0)
                    sections.Add($"Current offers: {string.Join("; ", offers)}.");
                else if (wantsOffers)
                    sections.Add("There are no active offers right now.");
            }

            if (includeAnnouncements)
            {
                List<string> announcements = await _db.Announcements
                    .AsNoTracking()
                    .Where(announcement => announcement.MallID == mallId && announcement.IsActive && announcement.StartDate <= now && announcement.EndDate >= now)
                    .GroupJoin(
                        _db.Stores.AsNoTracking(),
                        announcement => announcement.StoreId,
                        store => store.Id,
                        (announcement, stores) => new
                        {
                            announcement.Title,
                            announcement.IsPinned,
                            announcement.EndDate,
                            StoreName = stores.Select(store => store.Name).FirstOrDefault()
                        })
                    .OrderByDescending(announcement => announcement.IsPinned)
                    .ThenBy(announcement => announcement.EndDate)
                    .Take(5)
                    .Select(announcement => string.IsNullOrWhiteSpace(announcement.StoreName)
                        ? $"{announcement.Title} (until {FormatDate(announcement.EndDate)})"
                        : $"{announcement.StoreName}: {announcement.Title} (until {FormatDate(announcement.EndDate)})")
                    .ToListAsync(cancellationToken);

                if (announcements.Count > 0)
                    sections.Add($"Current announcements: {string.Join("; ", announcements)}.");
                else if (wantsAnnouncements)
                    sections.Add("There are no active announcements right now.");
            }

            return sections.Count == 0 ? null : new ChatbotResolution(string.Join(" ", sections), "live_content");
        }

        private async Task<List<StoreKnowledge>> LoadStoreKnowledgeAsync(Guid mallId, CancellationToken cancellationToken)
        {
            List<Store> stores = await _db.Stores
                .AsNoTracking()
                .Where(store => store.MallID == mallId)
                .OrderBy(store => store.Name)
                .ToListAsync(cancellationToken);

            if (stores.Count == 0)
                return [];

            HashSet<Guid> storeIds = stores.Select(store => store.Id).ToHashSet();

            List<StoreCategory> storeCategories = await _db.StoreCategories
                .AsNoTracking()
                .Where(storeCategory => storeIds.Contains(storeCategory.StoreId))
                .ToListAsync(cancellationToken);

            List<long> categoryIds = storeCategories
                .Select(storeCategory => storeCategory.CategoryId)
                .Distinct()
                .ToList();

            Dictionary<long, string> categoryNames = await _db.Categories
                .AsNoTracking()
                .Where(category => category.MallID == mallId && categoryIds.Contains(category.Id))
                .ToDictionaryAsync(category => category.Id, category => category.Name, cancellationToken);

            var categoriesByStoreId = new Dictionary<Guid, List<string>>();
            foreach (StoreCategory link in storeCategories)
            {
                if (!categoryNames.TryGetValue(link.CategoryId, out string? categoryName))
                    continue;

                if (!categoriesByStoreId.TryGetValue(link.StoreId, out List<string>? values))
                {
                    values = [];
                    categoriesByStoreId[link.StoreId] = values;
                }

                values.Add(categoryName);
            }

            return stores
                .Select(store =>
                {
                    string normalizedName = NormalizeForMatching(store.Name);
                    return new StoreKnowledge(
                        store.Id,
                        store.Name,
                        normalizedName,
                        Tokenize(normalizedName),
                        categoriesByStoreId.TryGetValue(store.Id, out List<string>? categories)
                            ? categories.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(category => category).ToList()
                            : [],
                        store.OperatingHours,
                        store.Description,
                        store.PhoneNumber,
                        store.Email,
                        store.FloorNumber,
                        store.StoreImageUrl,
                        store.SocialMediaLinks);
                })
                .ToList();
        }

        private async Task<ConversationContext> BuildConversationContextAsync(
            Guid userId,
            Guid conversationSessionId,
            IReadOnlyList<StoreKnowledge> stores,
            CancellationToken cancellationToken)
        {
            List<ChatbotConversation> recentTurns = await _db.ChatbotConversations
                .AsNoTracking()
                .Where(conversation => conversation.UserId == userId && conversation.SessionId == conversationSessionId)
                .OrderByDescending(conversation => conversation.CreatedAt)
                .Take(6)
                .ToListAsync(cancellationToken);

            foreach (ChatbotConversation turn in recentTurns)
            {
                string normalizedUserMessage = NormalizeForMatching(turn.UserMessage);
                StoreKnowledge? store = FindExplicitStoreMatch(stores, normalizedUserMessage, Tokenize(normalizedUserMessage));
                if (store != null)
                    return new ConversationContext(store);

                string normalizedBotResponse = NormalizeForMatching(turn.BotResponse);
                store = FindExplicitStoreMatch(stores, normalizedBotResponse, Tokenize(normalizedBotResponse));
                if (store != null)
                    return new ConversationContext(store);
            }

            return new ConversationContext(null);
        }

        private static StoreKnowledge? FindExplicitStoreMatch(
            IReadOnlyList<StoreKnowledge> stores,
            string normalizedMessage,
            HashSet<string> messageTokens)
        {
            StoreKnowledge? directMatch = stores
                .Where(store => normalizedMessage.Contains(store.NormalizedName, StringComparison.Ordinal))
                .OrderByDescending(store => store.NormalizedName.Length)
                .FirstOrDefault();

            if (directMatch != null)
                return directMatch;

            int bestScore = 0;
            StoreKnowledge? bestMatch = null;

            foreach (StoreKnowledge store in stores)
            {
                int overlap = store.NameTokens.Intersect(messageTokens).Count();
                if (overlap == 0)
                    continue;

                int requiredOverlap = store.NameTokens.Count == 1 ? 1 : Math.Min(2, store.NameTokens.Count);
                if (overlap < requiredOverlap)
                    continue;

                int score = overlap * 10 + store.NormalizedName.Length;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = store;
                }
            }

            return bestMatch;
        }

        private static bool ShouldUseConversationStore(HashSet<string> messageTokens, string normalizedMessage)
        {
            bool storeIntent =
                messageTokens.Overlaps(HoursKeywords) ||
                messageTokens.Overlaps(ContactKeywords) ||
                messageTokens.Overlaps(LocationKeywords) ||
                messageTokens.Overlaps(OfferKeywords) ||
                messageTokens.Overlaps(AnnouncementKeywords) ||
                messageTokens.Overlaps(DetailsKeywords) ||
                messageTokens.Overlaps(StoreKeywords);

            bool followUpTone =
                messageTokens.Overlaps(FollowUpKeywords) ||
                normalizedMessage.StartsWith("what about", StringComparison.Ordinal) ||
                normalizedMessage.StartsWith("and ", StringComparison.Ordinal) ||
                messageTokens.Count <= 4;

            return storeIntent && followUpTone;
        }

        private static List<CategoryMatch> FindCategoryMatches(
            IReadOnlyList<StoreKnowledge> stores,
            string normalizedMessage,
            HashSet<string> messageTokens)
        {
            if (!messageTokens.Overlaps(StoreKeywords) &&
                !messageTokens.Overlaps(DetailsKeywords) &&
                !messageTokens.Overlaps(OfferKeywords) &&
                !messageTokens.Overlaps(LocationKeywords) &&
                !messageTokens.Overlaps(RecentKeywords) &&
                messageTokens.Count > 4)
            {
                return [];
            }

            var matches = new List<CategoryMatch>();
            foreach (string category in stores.SelectMany(store => store.Categories).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string normalizedCategory = NormalizeForMatching(category);
                HashSet<string> categoryTokens = Tokenize(normalizedCategory);

                bool directMatch = normalizedMessage.Contains(normalizedCategory, StringComparison.Ordinal);
                int overlap = categoryTokens.Intersect(messageTokens).Count();
                int requiredOverlap = categoryTokens.Count == 1 ? 1 : Math.Min(2, categoryTokens.Count);

                if (directMatch || overlap >= requiredOverlap)
                    matches.Add(new CategoryMatch(category, normalizedCategory));
            }

            return matches
                .OrderByDescending(match => match.NormalizedName.Length)
                .ToList();
        }

        private static ChatbotResolution BuildFallbackResolution(IReadOnlyList<StoreKnowledge> stores)
        {
            List<string> sampleStores = stores
                .Take(3)
                .Select(store => store.Name)
                .ToList();

            string examples = sampleStores.Count == 0
                ? "You can ask about a specific store."
                : $"You can ask about stores like {string.Join(", ", sampleStores)}.";

            return new ChatbotResolution(
                $"I could not find a precise answer yet. I can help with mall hours, parking, services, store details, live offers and announcements, your points, your coupons, and your recent receipts. {examples}",
                "fallback");
        }

        private static bool IsPersonalRequest(string normalizedMessage, HashSet<string> messageTokens)
            => messageTokens.Overlaps(PersonalKeywords) ||
               normalizedMessage.Contains("how many", StringComparison.Ordinal) ||
               normalizedMessage.Contains("do i have", StringComparison.Ordinal) ||
               normalizedMessage.Contains("my latest", StringComparison.Ordinal) ||
               normalizedMessage.Contains("my recent", StringComparison.Ordinal);

        private static string NormalizeRequired(string? value, string message)
        {
            string normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ApiValidationException(message, "VALUE_REQUIRED");

            return normalized;
        }

        private static string NormalizeForMatching(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (char character in value.ToLowerInvariant())
            {
                builder.Append(char.IsLetterOrDigit(character) || char.IsWhiteSpace(character) ? character : ' ');
            }

            return MultiWhitespaceRegex.Replace(builder.ToString(), " ").Trim();
        }

        private static HashSet<string> Tokenize(string normalizedValue)
            => normalizedValue
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.Ordinal);

        private static string? TryFormatTodayHours(JsonDocument? document, DateTimeOffset now)
        {
            if (document == null || document.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            string dayName = now.DayOfWeek.ToString();
            if (TryGetPropertyIgnoreCase(document.RootElement, dayName, out JsonElement value) ||
                TryGetPropertyIgnoreCase(document.RootElement, dayName[..3], out value) ||
                TryGetPropertyIgnoreCase(document.RootElement, "today", out value))
            {
                string? formatted = FormatJsonElement(value);
                if (!string.IsNullOrWhiteSpace(formatted))
                    return $"{dayName}: {formatted}";
            }

            return null;
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static string? FormatJsonDocument(JsonDocument? document)
            => document == null ? null : FormatJsonElement(document.RootElement);

        private static string? FormatJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => FormatJsonObject(element),
                JsonValueKind.Array => FormatJsonArray(element),
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        private static string? FormatJsonObject(JsonElement element)
        {
            var parts = new List<string>();
            foreach (JsonProperty property in element.EnumerateObject())
            {
                string? value = FormatJsonElement(property.Value);
                if (!string.IsNullOrWhiteSpace(value))
                    parts.Add($"{property.Name}: {value}");
            }

            return parts.Count == 0 ? null : string.Join(", ", parts);
        }

        private static string? FormatJsonArray(JsonElement element)
        {
            List<string> values = element.EnumerateArray()
                .Select(FormatJsonElement)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToList();

            return values.Count == 0 ? null : string.Join(", ", values);
        }

        private static string FormatAmount(decimal amount)
            => amount.ToString("0.##", CultureInfo.InvariantCulture);

        private static string FormatDate(DateTimeOffset value)
            => value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

        private static string Suffix(int count)
            => count == 1 ? string.Empty : "s";

        private sealed record ChatbotResolution(string BotResponse, string MatchSource, Guid? MatchedFaqId = null);

        private sealed record StoreKnowledge(
            Guid Id,
            string Name,
            string NormalizedName,
            HashSet<string> NameTokens,
            IReadOnlyList<string> Categories,
            string? OperatingHours,
            string? Description,
            string? PhoneNumber,
            string? Email,
            string? FloorNumber,
            string? StoreImageUrl,
            JsonDocument? SocialMediaLinks);

        private sealed record ConversationContext(StoreKnowledge? ReferencedStore);

        private sealed record CategoryMatch(string Name, string NormalizedName);

        private sealed record UserCouponSnapshot(
            string SerialNumber,
            string Type,
            DateTimeOffset StartAt,
            DateTimeOffset EndAt,
            bool IsActive,
            bool IsRedeemed);

        private sealed record UserReceiptSnapshot(
            long Id,
            string ReceiptId,
            decimal Price,
            int Points,
            DateTimeOffset CreatedAt,
            string StoreName,
            string? Status);
    }
}
