using System.Text.Json;

namespace Graduation_Project_Backend.Service.Common
{
    public static class JsonDocumentMapper
    {
        public static JsonDocument? ToJsonDocument(JsonElement? element)
        {
            if (element == null)
                return null;

            JsonElement value = element.Value;
            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
                return null;

            return JsonDocument.Parse(value.GetRawText());
        }

        public static JsonElement? ToJsonElement(JsonDocument? document)
            => document == null ? null : document.RootElement.Clone();
    }
}
