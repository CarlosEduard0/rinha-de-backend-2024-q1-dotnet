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
        getClientCommand.Parameters.Add(new() { NpgsqlDbType = NpgsqlDbType.Integer });
        await getClientCommand.PrepareAsync(cancellationToken);
        getClientCommand.Parameters[0].Value = id;

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
        command.Parameters.Add(new() { NpgsqlDbType = NpgsqlDbType.Integer });
        await command.PrepareAsync(cancellationToken);
        command.Parameters[0].Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!reader.HasRows)
            return [];

        var transactions = new List<Transaction>(10);
        while (await reader.ReadAsync(cancellationToken))
        {
            transactions.Add(new(reader.GetInt32(0), reader.GetChar(1), reader.GetString(2), reader.GetDateTime(3)));
        }

        return transactions;
    }
}
