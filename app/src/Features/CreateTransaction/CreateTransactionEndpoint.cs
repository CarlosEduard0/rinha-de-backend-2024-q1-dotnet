using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RinhaBackend;

public static partial class CreateTransactionEndpoint
{
    public static IEndpointRouteBuilder MapCreateTransactionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/clientes/{id}/transacoes", Handle);

        return endpoints;
    }

    private static async Task<Results<Ok<CreateTransactionResponse>, NotFound, UnprocessableEntity>> Handle(int id,
        CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        if (request.IsInvalid())
            return TypedResults.UnprocessableEntity();

        request.ClientId = id;

        var result = await  CreateTransactionPersistence.Create(request, cancellationToken);
        if (result.Result is not Ok<CreateTransactionResponse> okResult)
            return result;

        var transaction = new Transaction((int)request.Valor, request.Tipo, request.Descricao, request.CreatedAt);
        var cacheEntry = new CreateTransactionCacheEntry(id, transaction);

        CreateTransactionCache.Write(cacheEntry);

        return okResult;
    }
}

public record CreateTransactionCacheEntry(int ClientId, Transaction Transaction);


public static class OperationType
{
    public const char CreditPrefix = 'c';
    public const char DebitPrefix = 'd';
}

public record CreateTransactionRequest(float Valor, char Tipo, string Descricao)
{
    [JsonIgnore]
    public int ClientId { get; set; }

    [JsonIgnore]
    public int SignedAmount => (int)(Tipo is OperationType.CreditPrefix ? Valor : -Valor);
    
    [JsonIgnore]
    public readonly DateTime CreatedAt = DateTime.UtcNow;

    public bool IsInvalid()
    {
        if (Tipo is not (OperationType.CreditPrefix or OperationType.DebitPrefix))
            return true;

        if ((Descricao?.Length ?? 0) is < 1 or > 10)
            return true;

        if (!float.IsInteger(Valor))
            return true;
        
        return false;
    }
}
public readonly record struct CreateTransactionResponse(int Limite, int Saldo);
