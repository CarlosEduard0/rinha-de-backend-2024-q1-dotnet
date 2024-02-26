namespace RinhaBackend;

public static partial class GetStatementEndpoint
{
    public static IEndpointRouteBuilder MapGetStatementEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/clientes/{id}/extrato", (int id, CancellationToken cancellationToken) =>
            GetStatementReader.GetStatement(id, cancellationToken));

        return endpoints;
    }
}
