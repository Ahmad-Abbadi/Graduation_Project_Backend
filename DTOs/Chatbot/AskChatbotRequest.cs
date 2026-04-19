using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Graduation_Project_Backend.DTOs.Chatbot
{
    public sealed class AskChatbotRequest
    {
        [MaxLength(1000)]
        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [MaxLength(1000)]
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [MaxLength(1000)]
        [JsonPropertyName("messege")]
        public string? Messege { get; set; }

        [MaxLength(1000)]
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [MaxLength(1000)]
        [JsonPropertyName("question")]
        public string? Question { get; set; }

        public Guid? ConversationSessionId { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtraFields { get; set; }

        public string? GetMessage()
            => FirstNonEmpty(
                Msg,
                Message,
                Messege,
                Text,
                Question,
                GetExtraString("msg"),
                GetExtraString("message"),
                GetExtraString("messege"),
                GetExtraString("text"),
                GetExtraString("question"));

        private string? GetExtraString(string key)
        {
            if (ExtraFields == null)
                return null;

            foreach (KeyValuePair<string, JsonElement> field in ExtraFields)
            {
                if (!string.Equals(field.Key, key, StringComparison.OrdinalIgnoreCase))
                    continue;

                return field.Value.ValueKind == JsonValueKind.String
                    ? field.Value.GetString()
                    : field.Value.ToString();
            }

            return null;
        }

        private static string? FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
