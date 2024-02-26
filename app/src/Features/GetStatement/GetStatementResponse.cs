using System.Text.Json.Serialization;

namespace RinhaBackend;

public record GetStatementResponse
{
    public Balance Saldo { get; init; }

    [JsonPropertyName("ultimas_transacoes")]
    public IEnumerable<Transaction> UltimasTransacoes { get; init; }

    public GetStatementResponse(Balance balance, IEnumerable<Transaction> transactions)
    {
        Saldo = balance;
        UltimasTransacoes = transactions;
    }
};

public record Balance
{
    public int Total { get; init; }

    [JsonPropertyName("data_extrato")]
    public DateTime DataExtrato { get; init; }
    public int Limite { get; init; }

    public Balance(int total, DateTime dataExtrato, int limite)
    {
        Total = total;
        DataExtrato = dataExtrato;
        Limite = limite;
    }
};

public record Transaction
{
    public int Valor { get; init; }
    public char Tipo { get; init; }
    public string Descricao { get; init; }

    [JsonPropertyName("realizada_em")]
    public DateTime RealizadaEm { get; init; }

    public Transaction(int valor, char tipo, string descricao, DateTime realizadaEm)
    {
        Valor = valor;
        Tipo = tipo;
        Descricao = descricao;
        RealizadaEm = realizadaEm;
    }
};
