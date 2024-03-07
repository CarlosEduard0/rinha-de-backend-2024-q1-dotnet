using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace RinhaBackend;

public static class GetStatementReader
{
    private const string GetTransactionsQuery = """
        SELECT "Amount", "OperationType", "Description", "CreatedAt"
        FROM "Transactions"
        WHERE "ClientId" = @id
        ORDER BY "Id" DESC
        LIMIT 10
    """;

    private const string GetClientByIdQuery = """SELECT "Limit", "Balance" FROM "Clients" WHERE "Id" = @id;""";

    private static readonly NpgsqlDataSource DataSource = RinhaBackendDatabase.DataSource;

    public static async Task<Results<Ok<GetStatementResponse>, NotFound>> GetStatement(int id, CancellationToken cancellationToken)
    {
        await using var connection = await DataSource.OpenConnectionAsync(cancellationToken);

        var balance = await GetBalance(id, connection, cancellationToken);
        if (balance is null)
            return TypedResults.NotFound();

        var transactions = await GetTransactions(id, connection, cancellationToken);

        return TypedResults.Ok(new GetStatementResponse(balance, transactions));
    }

    private static async Task<Balance?> GetBalance(int id, NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var getClientCommand = connection.CreateCommand();
        getClientCommand.CommandText = GetClientByIdQuery;
        getClientCommand.Parameters.Add(new NpgsqlParameter<int>("id", id));
        await getClientCommand.PrepareAsync(cancellationToken);

        await using var getClientReader = await getClientCommand.ExecuteReaderAsync(cancellationToken);
        if (!getClientReader.HasRows)
            return null;

        await getClientReader.ReadAsync(cancellationToken);

        return new Balance(getClientReader.GetInt32(1), DateTime.UtcNow, getClientReader.GetInt32(0));
    }

    private static async Task<IReadOnlyList<Transaction>> GetTransactions(int id, NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = GetTransactionsQuery;
        command.Parameters.Add(new NpgsqlParameter<int>("id", id));
        await command.PrepareAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!reader.HasRows)
            return Array.Empty<Transaction>();

        await reader.ReadAsync(cancellationToken);
        var transactions = new List<Transaction>(10)
        {
            new(reader.GetInt32(0), reader.GetChar(1), reader.GetString(2), reader.GetDateTime(3))
        };

        while (await reader.ReadAsync(cancellationToken))
        {
            transactions.Add(new(reader.GetInt32(0), reader.GetChar(1), reader.GetString(2), reader.GetDateTime(3)));
        }

        return transactions;
    }
}
