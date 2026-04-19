using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Graduation_Project_Backend.DTOs.Chatbot;
using Graduation_Project_Backend.Service.Common;

namespace Graduation_Project_Backend.Service
{
    public sealed class ChatbotService : IChatbotService
    {
        private const int DefaultMaxResponseTokens = 450;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private static readonly string mall_info = """
        City Mall, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, King Abdullah II St. Um Al Summaq Amman Jordan, Large multi-level shopping mall in west Amman with fashion dining entertainment cinema parking and family-friendly services
        Carrefour, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Hypermarket, GF, Large supermarket for groceries household essentials snacks and daily shopping
        Virgin Megastore, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Electronics & Lifestyle, GF, Popular lifestyle and tech store for gadgets accessories games books and gifts
        Mango, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Women's Fashion, GF, International fashion brand focused on modern women's clothing and accessories
        Women Secret, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Lingerie & Sleepwear, GF, Store specializing in lingerie sleepwear loungewear and personal basics
        Grand Cinemas, Daily 09:00-24:00, Entertainment, GF, Cinema complex for the latest movie releases premium screenings and family outings
        Tommy Hilfiger, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Fashion, GF, Premium casual fashion brand with apparel denim and accessories
        U.S. Polo, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Fashion, GF, Casual lifestyle apparel store with polos shirts denim and everyday wear
        H&M, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Fashion, F1, Fast-fashion retailer offering affordable clothing for women men and kids
        Bath & Body Works, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Beauty & Personal Care, F1, Fragrance and body-care store with lotions candles soaps and gift sets
        The Body Shop, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Beauty & Skincare, F1, Beauty retailer focused on skincare bath products and self-care essentials
        Time Center, Sat-Thu 10:00-23:00 | Fri 12:00-23:00, Watches & Accessories, F1, Watch and accessories retailer carrying fashion and premium timepiece brands
        GAP, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Fashion, F1, Classic casualwear store with basics denim and family apparel
        Zara, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Fashion, F2, Major fashion anchor offering trend-driven clothing and accessories
        Pull and Bear, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Youth Fashion, F2, Casual youth-oriented fashion store with streetwear-inspired collections
        Bershka, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Youth Fashion, F2, Fashion retailer targeting younger shoppers with trend-heavy collections
        Massimo Dutti, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Premium Fashion, F2, Smart-casual brand known for refined clothing footwear and accessories
        Zara Home, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Home Decor, F2, Home-lifestyle store with decor bedding tableware and soft furnishings
        Samsung - Hikmat Yassin Sons, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Electronics, F2, Consumer electronics store for smartphones accessories and smart devices
        Adidas, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Sportswear, F3, Sportswear and footwear store for training running and casual athletic style
        SKECHERS, Sat-Thu 10:00-22:00 | Fri 12:00-22:00, Footwear, F3, Comfort-focused footwear store with casual walking and athletic shoe options
        """;

        private static readonly string ai_instructions = """
        You are a mall customer service assistant.

        Only answer using the provided mall information below.
        Do not add any external information.
        Do not guess, infer, or make up missing details.

        Behavior rules:
        - If the user's question is answered in the provided data, answer it clearly and politely.
        - If the user's question is not covered in the provided data, reply with:
          "Sorry, I can only answer based on the available mall information. Please contact the mall directly at 0791234567."
        - If the user asks for details outside the provided data, do not provide an estimated answer.
        - Always stay in customer service style: polite, short, helpful, and professional.
        """;

        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChatbotService> _logger;

        public ChatbotService(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<ChatbotService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ChatbotAnswerResponse> AskAsync(AskChatbotRequest request, CancellationToken cancellationToken = default)
        {
            string userMessage = NormalizeRequired(request.GetMessage(), "msg is required.");

            Guid conversationSessionId = request.ConversationSessionId ?? Guid.NewGuid();
            DateTimeOffset createdAt = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            string botResponse = await AskAiModelAsync(userMessage, cancellationToken);

            stopwatch.Stop();
            int responseTimeMs = Math.Max(1, (int)stopwatch.ElapsedMilliseconds);

            return new ChatbotAnswerResponse
            {
                ConversationId = Guid.NewGuid(),
                ConversationSessionId = conversationSessionId,
                UserMessage = userMessage,
                BotResponse = botResponse,
                MatchedFaqId = null,
                MatchSource = "ai_model",
                ResponseTimeMs = responseTimeMs,
                CreatedAt = createdAt
            };
        }

        public Task<IReadOnlyList<ChatbotHistoryItemResponse>> GetHistoryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ChatbotHistoryItemResponse>>([]);

        private async Task<string> AskAiModelAsync(string userMessage, CancellationToken cancellationToken)
        {
            string apiKey = GetRequiredSetting("AI_API_KEY", "Chatbot:ApiKey");
            string apiUrl = GetRequiredSetting("AI_API_URL", "Chatbot:ApiUrl");
            string model = GetRequiredSetting("AI_MODEL", "Chatbot:Model");
            int maxTokens = GetIntSetting("AI_MAX_TOKENS", "Chatbot:MaxTokens", DefaultMaxResponseTokens);

            var payload = new AiChatCompletionRequest
            {
                Model = model,
                Messages = BuildAiMessages(userMessage),
                Temperature = 0.2m,
                MaxTokens = maxTokens
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            AddOptionalHeader(httpRequest, "HTTP-Referer", GetOptionalSetting("AI_HTTP_REFERER", "Chatbot:HttpReferer"));
            AddOptionalHeader(httpRequest, "X-Title", GetOptionalSetting("AI_APP_TITLE", "Chatbot:AppTitle"));
            httpRequest.Content = JsonContent.Create(payload, options: JsonOptions);

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!httpResponse.IsSuccessStatusCode)
            {
                string errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Chatbot AI provider returned {StatusCode}: {ErrorBody}",
                    (int)httpResponse.StatusCode,
                    Truncate(errorBody, 500));

                throw new ApiExternalServiceException("The chatbot AI provider is currently unavailable.", "AI_PROVIDER_ERROR");
            }

            AiChatCompletionResponse? completion = await httpResponse.Content.ReadFromJsonAsync<AiChatCompletionResponse>(JsonOptions, cancellationToken);
            string? answer = completion?.Choices.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrWhiteSpace(answer))
                throw new ApiExternalServiceException("The chatbot AI provider returned an empty response.", "AI_EMPTY_RESPONSE");

            return answer.Trim();
        }

        private static List<AiChatMessage> BuildAiMessages(string userMessage)
            =>
            [
                new("system", $"{ai_instructions}\n\nmall_info:\n{mall_info}"),
                new("user", userMessage)
            ];

        private string GetRequiredSetting(params string[] keys)
        {
            string? value = GetOptionalSetting(keys);
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            throw new ApiValidationException($"Missing required chatbot setting: {keys[0]}.", "CHATBOT_SETTING_MISSING");
        }

        private string? GetOptionalSetting(params string[] keys)
        {
            foreach (string key in keys)
            {
                string? value = _configuration[key] ?? Environment.GetEnvironmentVariable(key.Replace(':', '_'));
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return null;
        }

        private int GetIntSetting(string environmentKey, string configurationKey, int fallback)
        {
            string? value = GetOptionalSetting(environmentKey, configurationKey);
            return int.TryParse(value, out int parsedValue) && parsedValue > 0 ? parsedValue : fallback;
        }

        private static string NormalizeRequired(string? value, string requiredMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ApiValidationException(requiredMessage, "VALUE_REQUIRED");

            return value.Trim();
        }

        private static void AddOptionalHeader(HttpRequestMessage request, string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                request.Headers.TryAddWithoutValidation(name, value);
        }

        private static string Truncate(string value, int maxLength)
            => value.Length <= maxLength ? value : value[..maxLength];

        private sealed class AiChatCompletionRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; init; } = string.Empty;

            [JsonPropertyName("messages")]
            public IReadOnlyList<AiChatMessage> Messages { get; init; } = [];

            [JsonPropertyName("temperature")]
            public decimal Temperature { get; init; }

            [JsonPropertyName("max_tokens")]
            public int MaxTokens { get; init; }
        }

        private sealed record AiChatMessage(
            [property: JsonPropertyName("role")] string Role,
            [property: JsonPropertyName("content")] string Content);

        private sealed class AiChatCompletionResponse
        {
            [JsonPropertyName("choices")]
            public List<AiChatChoice> Choices { get; init; } = [];
        }

        private sealed class AiChatChoice
        {
            [JsonPropertyName("message")]
            public AiChatMessageResponse? Message { get; init; }
        }

        private sealed class AiChatMessageResponse
        {
            [JsonPropertyName("content")]
            public string? Content { get; init; }
        }
    }
}
