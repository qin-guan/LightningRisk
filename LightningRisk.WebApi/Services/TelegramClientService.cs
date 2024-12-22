using LightningRisk.Core;
using TL;

namespace LightningRisk.WebApi.Services;

public class TelegramClientService : BackgroundService
{
    public readonly WTelegram.Client Client;

    private readonly ILogger<TelegramClientService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public TelegramClientService(
        ILogger<TelegramClientService> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        WTelegram.Helpers.Log = (level, msg) => _logger.Log((LogLevel)level, "{Msg}", msg);

        Client = new WTelegram.Client(config => config switch
        {
            "api_id" => configuration["Telegram:ApiId"],
            "api_hash" => configuration["Telegram:ApiHash"],
            "phone_number" => configuration["Telegram:PhoneNumber"],
            _ => WTelegram.Client.DefaultConfig(config)
        });

        Client.WithUpdateManager(OnUpdate);
    }

    private async Task OnUpdate(Update update)
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
        var notificationService = scope.ServiceProvider.GetRequiredService<SubscriptionService>();
        var lightningRiskService = scope.ServiceProvider.GetRequiredService<LightningRiskService>();
        
        if (msg.peer_id != lightningRiskService.ChannelId)
        {
            return;
        }

        await notificationService.NotifySubscribersAsync(
            MessageParser.Parse(msg.message, DateOnly.FromDateTime(msg.Date))
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var me = await Client.LoginUserIfNeeded();
        _logger.LogInformation("Logged in as {Username}", me.username);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await Client.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}