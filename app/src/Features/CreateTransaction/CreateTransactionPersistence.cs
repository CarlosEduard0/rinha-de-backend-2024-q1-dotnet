using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using NpgsqlTypes;

namespace RinhaBackend;

public static class CreateTransactionPersistence
{
    private static readonly NpgsqlDataSource DataSource = RinhaBackendDatabase.DataSource;

    public static async Task<Results<Ok<CreateTransactionResponse>, NotFound, UnprocessableEntity>> Create(CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var createTransactionCommand = CreateTransactionCommand();
        var updateBalanceCommand = CreateUpdateBalanceCommand();

        await using var connection = await DataSource.OpenConnectionAsync(cancellationToken);
        await using var commandBatch = connection.CreateBatch();
        commandBatch.BatchCommands.Add(createTransactionCommand);
        commandBatch.BatchCommands.Add(updateBalanceCommand);
        await commandBatch.PrepareAsync(cancellationToken);

        createTransactionCommand.Parameters[0].Value = (int)request.Valor;
        createTransactionCommand.Parameters[1].Value = request.Tipo;
        createTransactionCommand.Parameters[2].Value = request.Descricao;
        createTransactionCommand.Parameters[3].Value = request.ClientId;
        createTransactionCommand.Parameters[4].Value = request.SignedAmount;

        updateBalanceCommand.Parameters[0].Value = request.SignedAmount;
        updateBalanceCommand.Parameters[1].Value = request.ClientId;

        try
        {
            await using var reader = await commandBatch.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            return TypedResults.Ok(new CreateTransactionResponse(reader.GetInt32(0), reader.GetInt32(1)));
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.CheckViolation)
        {
            return TypedResults.UnprocessableEntity();
        }
    }

    private static NpgsqlBatchCommand CreateTransactionCommand()
    {
        return new(CreateTransactionQueries.CreateTransactionQuery)
        {
            Parameters = {
                new() { NpgsqlDbType = NpgsqlDbType.Integer },
                new() { NpgsqlDbType = NpgsqlDbType.Char },
                new() { NpgsqlDbType = NpgsqlDbType.Varchar },
                new() { NpgsqlDbType = NpgsqlDbType.Integer },
                new() { NpgsqlDbType = NpgsqlDbType.Integer },
            }
        };
    }

    private static NpgsqlBatchCommand CreateUpdateBalanceCommand()
    {
        return new(CreateTransactionQueries.UpdateBalanceQuery)
        {
            Parameters = {
                new() { NpgsqlDbType = NpgsqlDbType.Integer },
                new() { NpgsqlDbType = NpgsqlDbType.Integer },
            }
        };
    }
}
