using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace LightningRisk.WebApi.Services.TelegramBot;

public class UpdateHandler(
    ILogger<UpdateHandler> logger,
    UpdateChannel updateChannel
) : IUpdateHandler
{
    public async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Update received from {ChatId}", update.Message?.Chat.Id);

        await updateChannel.Writer.WriteAsync(update, cancellationToken);
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Error received");
        return Task.CompletedTask;
    }
}