using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Graduation_Project_Backend.DTOs.Chatbot;
using Graduation_Project_Backend.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Graduation_Project_Backend.Tests
{
    public sealed class ChatbotServiceTests
    {
        [Fact]
        public async Task AskAsync_UsesOnlyMsgAndStaticMallInfo()
        {
            var handler = new RecordingAiHandler();
            handler.EnqueueResponse("The mall opens Saturday to Thursday from 10:00 AM to 10:00 PM.");
            ChatbotService chatbotService = CreateService(handler);

            ChatbotAnswerResponse response = await chatbotService.AskAsync(new AskChatbotRequest
            {
                Msg = "When does the mall open?"
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
        public async Task AskAsync_AcceptsMessegeAlias()
        {
            var handler = new RecordingAiHandler();
            handler.EnqueueResponse("Parking is free in the outdoor and basement parking areas.");
            ChatbotService chatbotService = CreateService(handler);

            ChatbotAnswerResponse response = await chatbotService.AskAsync(new AskChatbotRequest
            {
                Messege = "Where can I park?"
            });

            Assert.Equal("Where can I park?", response.UserMessage);

            using JsonDocument requestJson = JsonDocument.Parse(handler.RequestBodies.Single());
            JsonElement[] messages = requestJson.RootElement.GetProperty("messages").EnumerateArray().ToArray();
            Assert.Equal("Where can I park?", messages[1].GetProperty("content").GetString());
        }

        [Fact]
        public async Task GetHistoryAsync_ReturnsEmptyListBecauseChatbotDoesNotUseDatabase()
        {
            ChatbotService chatbotService = CreateService(new RecordingAiHandler());

            IReadOnlyList<ChatbotHistoryItemResponse> history = await chatbotService.GetHistoryAsync();

            Assert.Empty(history);
        }

        private static ChatbotService CreateService(RecordingAiHandler handler)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AI_API_KEY"] = "test-api-key",
                    ["AI_API_URL"] = "https://ai-provider.test/v1/chat/completions",
                    ["AI_MODEL"] = "test-model"
                })
                .Build();

            return new ChatbotService(
                configuration,
                new HttpClient(handler),
                NullLogger<ChatbotService>.Instance);
        }

        private sealed class RecordingAiHandler : HttpMessageHandler
        {
            private readonly Queue<string> _responses = new();

            public List<string> RequestBodies { get; } = [];
            public AuthenticationHeaderValue? Authorization { get; private set; }

            public void EnqueueResponse(string response)
                => _responses.Enqueue(response);

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Authorization = request.Headers.Authorization;
                RequestBodies.Add(request.Content == null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken));

                string response = _responses.Count == 0 ? string.Empty : _responses.Dequeue();

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
