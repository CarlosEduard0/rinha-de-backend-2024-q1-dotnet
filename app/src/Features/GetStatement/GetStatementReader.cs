using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using NpgsqlTypes;

namespace RinhaBackend;

public static class GetStatementReader
{
    private const string GetTransactionsQuery = """
        SELECT "Amount", "OperationType", "Description", "CreatedAt"
        FROM "Transactions"
        WHERE "ClientId" = $1
        ORDER BY "Id" DESC
        LIMIT 10
    """;

    private const string GetClientByIdQuery = """SELECT "Limit", "Balance" FROM "Clients" WHERE "Id" = $1""";

    private static readonly NpgsqlDataSource DataSource = RinhaBackendDatabase.DataSource;

    public static async Task<Results<Ok<GetStatementResponse>, NotFound>> GetStatement(int id, CancellationToken cancellationToken)
    {
        if (id is < 1 or > 5)
            return TypedResults.NotFound();

        await using var connection = await DataSource.OpenConnectionAsync(cancellationToken);
        await using var batch = GetStatementBatch();
        batch.Connection = connection;

        batch.BatchCommands[0].Parameters[0].Value = id;
        batch.BatchCommands[1].Parameters[0].Value = id;

        await using var reader = await batch.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        var balance = new Balance(reader.GetInt32(1), DateTime.UtcNow, reader.GetInt32(0));

        await reader.NextResultAsync(cancellationToken);
        if(!reader.HasRows)
            return TypedResults.Ok(new GetStatementResponse(balance, []));

        var transactions = new List<Transaction>(10);
        while (await reader.ReadAsync(cancellationToken))
        {
            transactions.Add(new Transaction(reader.GetInt32(0), reader.GetChar(1), reader.GetString(2), reader.GetDateTime(3)));
        }

        return TypedResults.Ok(new GetStatementResponse(balance, transactions));
    }

    private static NpgsqlBatch GetStatementBatch() =>
        new()
        {
            BatchCommands = {
                GetBalanceCommand(),
                GetTransactionsCommand(),
            }
        };

    private static NpgsqlBatchCommand GetTransactionsCommand() =>
        new(GetTransactionsQuery)
        {
            Parameters = {
                new() { NpgsqlDbType = NpgsqlDbType.Integer }
            }
        };

    private static NpgsqlBatchCommand GetBalanceCommand() =>
        new(GetClientByIdQuery)
        {
            Parameters = {
                new() { NpgsqlDbType = NpgsqlDbType.Integer }
            }
        };
}
