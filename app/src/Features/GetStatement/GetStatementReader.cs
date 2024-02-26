using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using StackExchange.Redis;

namespace RinhaBackend;

public static class GetStatementReader
{
    private const string GetStatementQuery = """
        SELECT "Balance", "Limit", "Amount", "OperationType", "Description", "CreatedAt"
        FROM "Clients"
        LEFT JOIN "Transactions" ON "Clients"."Id" = "Transactions"."ClientId"
        WHERE "Id" = @id
        ORDER BY "CreatedAt" DESC
        LIMIT 10
    """;

    private static readonly NpgsqlDataSource DataSource = RinhaBackendDatabase.DataSource;

    public static async Task<Results<Ok<GetStatementResponse>, NotFound>> GetStatement(int id, CancellationToken cancellationToken)
    {
        var cacheResult =  await GetStatementFromCache(id);
        if (cacheResult.Result is Ok<GetStatementResponse>)
            return cacheResult;

        return await GetStatementFromDatabase(id, cancellationToken);
    }

    private static async Task<Results<Ok<GetStatementResponse>, NotFound>> GetStatementFromCache(int id)
    {
        var cacheKey = $"user:{id}:last_transactions";
        var getTransactionsTask = RinhaBackendCache.Database.SortedSetRangeByRankAsync(cacheKey, 0, 9, Order.Descending);
        var getBalanceTask = GetClientById(id);

        await Task.WhenAll(getTransactionsTask, getBalanceTask);

        var result = await getTransactionsTask;
        var balance = await getBalanceTask;
        if (result.Length == 0 || balance is null)
            return TypedResults.NotFound();

        var transactions = new List<Transaction>(10);
        foreach (var entry in result)
        {
            var cacheEntry = JsonSerializer.Deserialize(entry!, AppJsonSerializerContext.Default.CreateTransactionCacheEntry);
            transactions.Add(cacheEntry!.Transaction);
        }
        var response = new GetStatementResponse(balance, transactions);

        return TypedResults.Ok(response);
    }

    private const string GetClientByIdQuery = """SELECT "Limit", "Balance" FROM "Clients" WHERE "Id" = @id;""";
    private static async Task<Balance?> GetClientById(int id)
    {
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = GetClientByIdQuery;
        command.Parameters.Add(new NpgsqlParameter<int>("id", id));
        await command.PrepareAsync();

        await using var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows)
            return null;

        await reader.ReadAsync();

        return new Balance(reader.GetInt32(1), DateTime.UtcNow, reader.GetInt32(0));
    }

    private static async Task<Results<Ok<GetStatementResponse>, NotFound>> GetStatementFromDatabase(int id, CancellationToken cancellationToken)
    {
        await using var connection = await DataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = GetStatementQuery;
        command.Parameters.Add(new NpgsqlParameter<int>("id", id));
        await command.PrepareAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!reader.HasRows)
            return TypedResults.NotFound();

        await reader.ReadAsync(cancellationToken);
        Balance balance = new(reader.GetInt32(0), DateTime.UtcNow, reader.GetInt32(1));
        if (reader.IsDBNull(2))
        {
            return TypedResults.Ok(new GetStatementResponse(balance, Array.Empty<Transaction>()));
        }
        var transactions = new List<Transaction>(10)
        {
            new(reader.GetInt32(2), reader.GetChar(3), reader.GetString(4), reader.GetDateTime(5))
        };

        while (await reader.ReadAsync(cancellationToken))
        {
            transactions.Add(new(reader.GetInt32(2), reader.GetChar(3), reader.GetString(4), reader.GetDateTime(5)));
        }

        return TypedResults.Ok(new GetStatementResponse(balance, transactions));
    }
}
