using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace RinhaBackend;

public static class CreateTransactionPersistence
{
    private static readonly NpgsqlDataSource DataSource = RinhaBackendDatabase.DataSource;

    public static async Task<Results<Ok<CreateTransactionResponse>, NotFound, UnprocessableEntity>> Create(CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var createTransactionCommand = CreateTransactionCommand(request);
        var updateBalanceCommand = CreateUpdateBalanceCommand(request.ClientId, request.SignedAmount);

        await using var connection = await DataSource.OpenConnectionAsync(cancellationToken);
        await using var commandBatch = connection.CreateBatch();
        commandBatch.BatchCommands.Add(createTransactionCommand);
        commandBatch.BatchCommands.Add(updateBalanceCommand);
        await commandBatch.PrepareAsync(cancellationToken);

        await using var reader = await commandBatch.ExecuteReaderAsync(cancellationToken);
        if (!reader.HasRows)
        {
            await reader.DisposeAsync();
            await using var userExistsCommand = connection.CreateCommand();
            userExistsCommand.CommandText = CreateTransactionQueries.UserExistsQuery;
            userExistsCommand.Parameters.Add(new NpgsqlParameter<int>("id", request.ClientId));
            await userExistsCommand.PrepareAsync(cancellationToken);

            var result = await userExistsCommand.ExecuteScalarAsync(cancellationToken);
            if (result is null)
                return TypedResults.NotFound();
            
            return TypedResults.UnprocessableEntity();
        }

        await reader.ReadAsync(cancellationToken);
        return TypedResults.Ok(new CreateTransactionResponse(reader.GetInt32(0), reader.GetInt32(1)));
    }

    private static NpgsqlBatchCommand CreateTransactionCommand(CreateTransactionRequest request) =>
        new(CreateTransactionQueries.CreateTransactionQuery)
            {
                Parameters = {
                        new NpgsqlParameter<int>("amount", (int)request.Valor),
                        new NpgsqlParameter<char>("operationType", request.Tipo),
                        new NpgsqlParameter<string>("description", request.Descricao),
                        new NpgsqlParameter<DateTime>("createdAt", request.CreatedAt),
                        new NpgsqlParameter<int>("clientId", request.ClientId),
                        new NpgsqlParameter<int>("signedAmount", request.SignedAmount),
                    }
            };

    private static NpgsqlBatchCommand CreateUpdateBalanceCommand(int clientId, int signedAmount) =>
        new(CreateTransactionQueries.UpdateBalanceQuery)
            {
                Parameters = {
                    new NpgsqlParameter<int>("signedAmount", signedAmount),
                    new NpgsqlParameter<int>("clientId", clientId),
                }
            };
}
