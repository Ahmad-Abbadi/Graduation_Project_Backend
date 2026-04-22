using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Graduation_Project_Backend.DTOs.Chatbot;
using Graduation_Project_Backend.Service;
using Graduation_Project_Backend.Service.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Graduation_Project_Backend.Tests
{
    public sealed class ChatbotServiceTests
    {
        [Fact]
        public async Task AskAsync_UsesMessageAndStaticMallInfo()
        {
            var handler = new RecordingAiHandler();
            handler.EnqueueResponse("The mall opens Saturday to Thursday from 10:00 AM to 10:00 PM.");
            ChatbotService chatbotService = CreateService(handler);

            ChatbotAnswerResponse response = await chatbotService.AskAsync(new AskChatbotRequest
            {
                Message = "When does the mall open?"
            });

            Assert.Equal("ai_model", response.MatchSource);
            Assert.Null(response.MatchedFaqId);
            Assert.Equal("When does the mall open?", response.UserMessage);
            Assert.Equal("The mall opens Saturday to Thursday from 10:00 AM to 10:00 PM.", response.BotResponse);

            Assert.Equal("Bearer", handler.Authorization?.Scheme);
            Assert.Equal("test-api-key", handler.Authorization?.Parameter);

            using JsonDocument requestJson = JsonDocument.Parse(handler.RequestBodies.Single());
            JsonElement root = requestJson.RootElement;
            Assert.Equal("test-model", root.GetProperty("model").GetString());

            JsonElement[] messages = root.GetProperty("messages").EnumerateArray().ToArray();
            Assert.Equal(2, messages.Length);
            Assert.Equal("system", messages[0].GetProperty("role").GetString());
            Assert.Contains("mall_info", messages[0].GetProperty("content").GetString());
            Assert.Contains("City Mall", messages[0].GetProperty("content").GetString());
            Assert.Equal("user", messages[1].GetProperty("role").GetString());
            Assert.Equal("When does the mall open?", messages[1].GetProperty("content").GetString());
        }

        [Fact]
        public async Task AskAsync_RetriesTransientProviderFailuresUntilSuccess()
        {
            var handler = new RecordingAiHandler();
            handler.EnqueueStatus(HttpStatusCode.ServiceUnavailable, "temporary outage");
            handler.EnqueueException(new HttpRequestException("connection dropped"));
            handler.EnqueueResponse("City Mall is open Saturday to Thursday from 10:00 AM to 10:00 PM.");
            ChatbotService chatbotService = CreateService(handler, new Dictionary<string, string?>
            {
                ["AI_RETRY_DELAY_MS"] = "1"
            });

            ChatbotAnswerResponse response = await chatbotService.AskAsync(new AskChatbotRequest
            {
                Message = "When is City Mall open?"
            });

            Assert.Equal("City Mall is open Saturday to Thursday from 10:00 AM to 10:00 PM.", response.BotResponse);
            Assert.Equal(3, handler.RequestBodies.Count);
        }

        [Fact]
        public async Task AskAsync_DoesNotRetryProviderConfigurationErrors()
        {
            var handler = new RecordingAiHandler();
            handler.EnqueueStatus(HttpStatusCode.Unauthorized, "bad api key");
            ChatbotService chatbotService = CreateService(handler, new Dictionary<string, string?>
            {
                ["AI_RETRY_DELAY_MS"] = "1"
            });

            var exception = await Assert.ThrowsAsync<ApiExternalServiceException>(
                () => chatbotService.AskAsync(new AskChatbotRequest
                {
                    Message = "When is City Mall open?"
                }));

            Assert.Equal("AI_PROVIDER_ERROR", exception.Code);
            Assert.Single(handler.RequestBodies);
        }

        [Fact]
        public async Task AskAsync_RejectsMissingMessage()
        {
            ChatbotService chatbotService = CreateService(new RecordingAiHandler());

            var exception = await Assert.ThrowsAsync<ApiValidationException>(
                () => chatbotService.AskAsync(new AskChatbotRequest()));

            Assert.Equal("message is required.", exception.Message);
            Assert.Equal("VALUE_REQUIRED", exception.Code);
        }

        [Fact]
        public async Task GetHistoryAsync_ReturnsEmptyListBecauseChatbotDoesNotUseDatabase()
        {
            ChatbotService chatbotService = CreateService(new RecordingAiHandler());

            IReadOnlyList<ChatbotHistoryItemResponse> history = await chatbotService.GetHistoryAsync();

            Assert.Empty(history);
        }

        private static ChatbotService CreateService(
            RecordingAiHandler handler,
            Dictionary<string, string?>? settings = null)
        {
            var configurationValues = new Dictionary<string, string?>
            {
                ["AI_API_KEY"] = "test-api-key",
                ["AI_API_URL"] = "https://ai-provider.test/v1/chat/completions",
                ["AI_MODEL"] = "test-model"
            };

            if (settings is not null)
            {
                foreach (KeyValuePair<string, string?> setting in settings)
                    configurationValues[setting.Key] = setting.Value;
            }

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationValues)
                .Build();

            return new ChatbotService(
                configuration,
                new HttpClient(handler),
                NullLogger<ChatbotService>.Instance);
        }

        private sealed class RecordingAiHandler : HttpMessageHandler
        {
            private readonly Queue<Func<HttpResponseMessage>> _responses = new();

            public List<string> RequestBodies { get; } = [];
            public AuthenticationHeaderValue? Authorization { get; private set; }

            public void EnqueueResponse(string response)
                => _responses.Enqueue(() => CreateChatCompletionResponse(response));

            public void EnqueueStatus(HttpStatusCode statusCode, string responseBody)
                => _responses.Enqueue(() => new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(responseBody)
                });

            public void EnqueueException(Exception exception)
                => _responses.Enqueue(() => throw exception);

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Authorization = request.Headers.Authorization;
                RequestBodies.Add(request.Content == null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken));

                Func<HttpResponseMessage> response = _responses.Count == 0
                    ? () => CreateChatCompletionResponse(string.Empty)
                    : _responses.Dequeue();

                return response();
            }

            private static HttpResponseMessage CreateChatCompletionResponse(string response)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new
                    {
                        choices = new[]
                        {
                            new
                            {
                                message = new
                                {
                                    content = response
                                }
                            }
                        }
                    })
                };
            }
        }
    }
}
