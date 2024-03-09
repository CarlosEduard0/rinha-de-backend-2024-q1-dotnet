namespace RinhaBackend;

public static class CreateTransactionQueries
{
    public const string CreateTransactionQuery = """
        INSERT INTO "Transactions" ("Amount", "OperationType", "Description", "ClientId")
        VALUES ($1, $2, $3, $4)
    """;

    public const string UpdateBalanceQuery = """
        UPDATE "Clients"
            SET "Balance" = "Balance" + $1
        WHERE
            "Id" = $2
        RETURNING "Limit", "Balance"
    """;
}
