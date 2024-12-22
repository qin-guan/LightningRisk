using System.Net.Http.Headers;
using LightningRisk.WebApi.Context;
using LightningRisk.WebApi.Services;
using Polly;
using Polly.Retry;
using Telegram.Bot;
using Telegram.Bot.Polling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSqlite<AppDbContext>(builder.Configuration.GetConnectionString("Sqlite"));

builder.Services.ConfigureTelegramBot<Microsoft.AspNetCore.Http.Json.JsonOptions>(opt => opt.SerializerOptions);

builder.Services.AddResiliencePipeline("Telegram",
    pipeline =>
    {
        pipeline.AddRetry(new RetryStrategyOptions
        {
            Delay = TimeSpan.FromSeconds(10)
        });
    });

builder.Services.AddHttpClient(nameof(TelegramBotClient))
    .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
        new TelegramBotClient(
            builder.Configuration["Telegram:Token"] ?? throw new Exception("Telegram bot token is missing."),
            httpClient
        )
    );

builder.Services.AddScoped<LightningRiskService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<IUpdateHandler, TelegramBotUpdateHandlerService>();

builder.Services.AddHostedService<TelegramClientService>();
builder.Services.AddHostedService<TelegramBotPollingService>();

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await using var scope = app.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.EnsureCreatedAsync();

app.UseHttpsRedirection();

app.Run();