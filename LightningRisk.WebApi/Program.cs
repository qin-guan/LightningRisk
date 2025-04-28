using LightningRisk.WebApi.Entities;
using LightningRisk.WebApi.Services;
using LightningRisk.WebApi.Services.TelegramBot;
using LightningRisk.WebApi.Services.TelegramBot.Handlers;
using LightningRisk.WebApi.Services.TelegramClient;
using Polly;
using Polly.Retry;
using SqlSugar;
using Telegram.Bot;
using Telegram.Bot.Polling;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.ConfigureTelegramBot<Microsoft.AspNetCore.Http.Json.JsonOptions>(opt => opt.SerializerOptions);

builder.Services.AddResiliencePipeline("Telegram",
    pipeline =>
    {
        pipeline.AddRetry(new RetryStrategyOptions
        {
            Delay = TimeSpan.FromSeconds(10)
        });
    });

builder.Services.AddResiliencePipeline("Dgs",
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

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ISqlSugarClient>(sp =>
{
    Console.WriteLine(builder.Configuration.GetConnectionString("Sqlite"));
    var sqlSugar = new SqlSugarClient(new ConnectionConfig
        {
            DbType = DbType.Sqlite,
            ConnectionString = builder.Configuration.GetConnectionString("Sqlite"),
            IsAutoCloseConnection = true,
        },
        db =>
        {
            var log = sp.GetRequiredService<ILogger<ISqlSugarClient>>();
            db.Aop.OnLogExecuting = (sql, _) => { log.LogInformation("SQL statement: {Stmt}", sql); };
        });
    return sqlSugar;
});

builder.Services.AddSingleton<UpdateChannel>();
builder.Services.AddMediator(options => { options.ServiceLifetime = ServiceLifetime.Scoped; });

builder.Services.AddTransient<ReplyMarkupService>();
builder.Services.AddTransient<IUpdateHandler, UpdateHandler>();
builder.Services.AddHostedService<PollingBackgroundService>();
builder.Services.AddHostedService<ChannelReaderBackgroundService>();
builder.Services.AddHostedService<UpdateBackgroundService>();

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await using var scope = app.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
db.CodeFirst.InitTables<Subscription>();

app.UseHttpsRedirection();

app.Run();