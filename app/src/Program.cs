using System.Text.Json;
using RinhaBackend;

AppContext.SetSwitch("Npgsql.EnableSqlRewriting", false);

await RinhaBackendDatabase.Load();

var builder = WebApplication.CreateEmptyBuilder(new(){ Args = args });

builder.WebHost.UseKestrelCore();
builder.WebHost.ConfigureKestrel(options =>
{
    var unixSocket = Environment.GetEnvironmentVariable("UNIX_SOCKET");
    if (string.IsNullOrEmpty(unixSocket))
        return;
    
    Console.WriteLine($"Listening on unix socket: {unixSocket}");
    options.ListenUnixSocket(unixSocket);
});

builder.Services.AddRoutingCore();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

app.MapCreateTransactionEndpoint();
app.MapGetStatementEndpoint();

app.Run();
