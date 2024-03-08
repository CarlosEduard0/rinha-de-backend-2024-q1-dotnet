namespace RinhaBackend;

public static class CreateTransactionQueries
{
    public const string CreateTransactionQuery = """
        INSERT INTO "Transactions" ("Amount", "OperationType", "Description", "ClientId")
        SELECT $1, $2, $3, $4
        WHERE EXISTS (
            SELECT 1 FROM "Clients"
            WHERE
                "Id" = $4 AND
                "Balance" + $5 >= "Limit" * -1
        )
    """;

    public const string UpdateBalanceQuery = """
        UPDATE "Clients"
            SET "Balance" = "Balance" + $1
        WHERE
            "Id" = $2 AND
            "Balance" + $1 >= "Limit" * -1
        RETURNING "Limit", "Balance"
    """;

    public const string UserExistsQuery = """SELECT 1 FROM "Clients" WHERE "Id" = $1""";
}
