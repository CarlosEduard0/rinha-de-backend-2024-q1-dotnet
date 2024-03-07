using System.Text.Json.Serialization;

namespace RinhaBackend;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(CreateTransactionRequest))]
[JsonSerializable(typeof(CreateTransactionResponse))]
[JsonSerializable(typeof(GetStatementResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}