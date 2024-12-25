using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Update = Telegram.Bot.Types.Update;

namespace LightningRisk.WebApi.Services;

public class TelegramBotUpdateHandlerService(
    ILogger<TelegramBotUpdateHandlerService> logger,
    IServiceProvider serviceProvider
) : IUpdateHandler
{
    public async Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken ct
    )
    {
        logger.LogInformation("Error in update handler: {Exception}", exception);

        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
    }

    public async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        await using var scope = serviceProvider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TelegramBotUpdateFinalHandler>()
            .HandleUpdateAsync(botClient, update, ct);
    }
}