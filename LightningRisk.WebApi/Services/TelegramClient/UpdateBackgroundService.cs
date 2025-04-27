using LightningRisk.Core;
using LightningRisk.WebApi.Services.TelegramBot.Handlers;
using Mediator;
using TL;

namespace LightningRisk.WebApi.Services.TelegramClient;

public class UpdateBackgroundService : BackgroundService
{
    public readonly WTelegram.Client Client;

    private readonly ILogger<UpdateBackgroundService> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IServiceProvider _serviceProvider;

    public UpdateBackgroundService(
        ILogger<UpdateBackgroundService> logger,
        IConfiguration configuration,
        IHostEnvironment environment,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _environment = environment;
        _serviceProvider = serviceProvider;

        WTelegram.Helpers.Log = (level, msg) => _logger.Log((LogLevel)level, "{Msg}", msg);

        Client = new WTelegram.Client(config => config switch
        {
            "api_id" => configuration["Telegram:ApiId"],
            "api_hash" => configuration["Telegram:ApiHash"],
            "phone_number" => configuration["Telegram:PhoneNumber"],
            "session_pathname" => configuration["Telegram:SessionPathname"],
            _ => WTelegram.Client.DefaultConfig(config)
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Client.LoginUserIfNeeded();
        
        Client.WithUpdateManager(async (update) =>
        {
            _logger.LogInformation("Processing update");

            if (update is not UpdateNewChannelMessage uncm)
            {
                return;
            }

            if (uncm.message is not Message msg)
            {
                return;
            }

            await using var scope = _serviceProvider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            if (msg.peer_id != Constants.GetChannelId(_environment.IsProduction()))
            {
                return;
            }

            await mediator.Send(
                new TelegramClientNewStatus(
                    MessageParser.Parse(msg.message, DateOnly.FromDateTime(msg.Date))
                ),
                stoppingToken
            );
        });
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await Client.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}