using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RinhaBackend;

public static partial class CreateTransactionEndpoint
{
    public static IEndpointRouteBuilder MapCreateTransactionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/clientes/{id}/transacoes", Handle).ShortCircuit();

        return endpoints;
    }

    private static async Task<Results<Ok<CreateTransactionResponse>, NotFound, UnprocessableEntity>> Handle(int id,
        CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        if (request.IsInvalid())
            return TypedResults.UnprocessableEntity();

        if (id is < 1 or > 5)
            return TypedResults.NotFound();
        request.ClientId = id;

        var result = await  CreateTransactionPersistence.Create(request, cancellationToken);

        return result;
    }
}


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
public record CreateTransactionResponse(int Limite, int Saldo);
