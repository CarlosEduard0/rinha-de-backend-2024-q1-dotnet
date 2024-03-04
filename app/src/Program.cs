using System.Text.Json;
using RinhaBackend;

await Task.WhenAll(RinhaBackendDatabase.Load(), RinhaBackendCache.Load());

var builder = WebApplication.CreateEmptyBuilder(new());

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

builder.Services.AddHostedService<TrimCacheWorker>();

var app = builder.Build();

CreateTransactionCache.Load(app.Lifetime.ApplicationStopping);

app.MapCreateTransactionEndpoint();
app.MapGetStatementEndpoint();

app.Run();
