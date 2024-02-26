namespace RinhaBackend;

public static class CreateTransactionQueries
{
    public const string CreateTransactionQuery = """
        INSERT INTO "Transactions" ("Amount", "OperationType", "Description", "CreatedAt", "ClientId")
        SELECT @amount, @operationType, @description, @createdAt, @clientId
        WHERE EXISTS (
            SELECT 1 FROM "Clients"
            WHERE
                "Id" = @clientId AND
                "Balance" + @signedAmount >= "Limit" * -1
        );
    """;

    public const string UpdateBalanceQuery = """
        UPDATE "Clients"
            SET "Balance" = "Balance" + @signedAmount
        WHERE
            "Id" = @clientId AND
            "Balance" + @signedAmount >= "Limit" * -1
        RETURNING "Limit", "Balance";
    """;

    public const string UserExistsQuery = """SELECT 1 FROM "Clients" WHERE "Id" = @id;""";
}
