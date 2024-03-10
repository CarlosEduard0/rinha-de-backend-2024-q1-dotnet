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
        await using var connection = await DataSource.OpenConnectionAsync(cancellationToken);
        await using var commandBatch = CreateTransactionBatch();
        commandBatch.Connection = connection;

        commandBatch.BatchCommands[0].Parameters[0].Value = (int)request.Valor;
        commandBatch.BatchCommands[0].Parameters[0].Value = (int)request.Valor;
        commandBatch.BatchCommands[0].Parameters[1].Value = request.Tipo;
        commandBatch.BatchCommands[0].Parameters[2].Value = request.Descricao;
        commandBatch.BatchCommands[0].Parameters[3].Value = request.ClientId;
        commandBatch.BatchCommands[0].Parameters[4].Value = request.SignedAmount;

        commandBatch.BatchCommands[1].Parameters[0].Value = request.SignedAmount;
        commandBatch.BatchCommands[1].Parameters[1].Value = request.ClientId;

        try
        {
            await using var reader = await commandBatch.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            var response = new CreateTransactionResponse(reader.GetInt32(0), reader.GetInt32(1));
            return TypedResults.Ok(response);
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.CheckViolation)
        {
            return TypedResults.UnprocessableEntity();
        }
    }

    private static NpgsqlBatch CreateTransactionBatch()
    {
        return new NpgsqlBatch()
        {
            BatchCommands = {
                CreateTransactionCommand(),
                CreateUpdateBalanceCommand()
            }
        };
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
