using Polly.Registry;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace LightningRisk.WebApi.Services;

public class TelegramBotPollingService(
    ILogger<TelegramBotPollingService> logger,
    IServiceProvider serviceProvider,
    ResiliencePipelineProvider<string> resiliencePipelineProvider
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting polling service");
        await DoWork(ct);
    }

    private async Task DoWork(CancellationToken ct)
    {
        var pipeline = resiliencePipelineProvider.GetPipeline("Telegram");

        await pipeline.ExecuteAsync(
            async (sp, ct2) =>
            {
                await using var scope = sp.CreateAsyncScope();
                var bot = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
                var updateHandler = scope.ServiceProvider.GetRequiredService<IUpdateHandler>();

                var receiverOptions = new ReceiverOptions();

                var me = await bot.GetMe(ct2);

                logger.LogInformation("Start receiving updates for {BotName}", me.Username ?? "My Awesome Bot");

                await bot.ReceiveAsync(updateHandler, receiverOptions, ct2);
            },
            serviceProvider,
            ct
        );
    }
}