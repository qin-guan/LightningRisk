using Polly.Registry;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace LightningRisk.WebApi.Services.TelegramBot;

/// <summary>
/// Polls Telegram for new messages sent to the bot
/// </summary>
public class PollingBackgroundService(
    ILogger<PollingBackgroundService> logger,
    ResiliencePipelineProvider<string> resiliencePipelineProvider,
    ITelegramBotClient telegramBotClient,
    IUpdateHandler updateHandler
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting Telegram Bot Polling Background Service");

        var pipeline = resiliencePipelineProvider.GetPipeline("Telegram");
        await pipeline.ExecuteAsync(async (state, ct2) =>
            {
                var receiverOptions = new ReceiverOptions();
                var me = await state.telegramBotClient.GetMe(ct2);

                state.logger.LogInformation(
                    "Telegram Bot Polling Background Service receiving updates using username = {Username}",
                    me.Username ?? "Unknown");
                
                await state.telegramBotClient.ReceiveAsync(updateHandler, receiverOptions, ct2);
            },
            (logger, telegramBotClient),
            ct
        );
    }
}