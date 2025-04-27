using LightningRisk.WebApi.Services.TelegramBot.Handlers;
using Mediator;
using Telegram.Bot;

namespace LightningRisk.WebApi.Services.TelegramBot;

public class ChannelReaderBackgroundService(UpdateChannel updateChannel, IServiceProvider sp) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Parallel.ForEachAsync(updateChannel.Reader.ReadAllAsync(stoppingToken),
            new ParallelOptions { MaxDegreeOfParallelism = 1000 },
            async (update, ct) =>
            {
                await using var scope = sp.CreateAsyncScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                switch (update)
                {
                    case { Message: { Text: "/start" } }:
                        await mediator.Send(new MessageStart(update), ct);
                        break;
                    case { Message: { Text: "/stats" } }:
                        await mediator.Send(new MessageStats(update), ct);
                        break;
                    case { Message: not null }:
                        await mediator.Send(new MessageUnknown(update), ct);
                        break;
                    case { CallbackQuery: not null }: 
                        await mediator.Send(new CallbackQuerySectorSelection(update), ct);
                        break;
                }
            });
    }
}