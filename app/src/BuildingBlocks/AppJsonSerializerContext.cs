using System.Text.Json.Serialization;

namespace RinhaBackend;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(CreateTransactionRequest))]
[JsonSerializable(typeof(CreateTransactionResponse))]
[JsonSerializable(typeof(GetStatementResponse))]
[JsonSerializable(typeof(CreateTransactionCacheEntry))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}