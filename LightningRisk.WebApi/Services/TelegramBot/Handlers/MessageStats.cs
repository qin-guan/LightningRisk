using System.Diagnostics;
using LightningRisk.WebApi.Entities;
using Mediator;
using SqlSugar;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LightningRisk.WebApi.Services.TelegramBot.Handlers;

public sealed record MessageStats(Update Update) : IRequest;

public sealed class MessageStatsHandler(ITelegramBotClient botClient, ISqlSugarClient sugarClient)
    : IRequestHandler<MessageStats>
{
    public async ValueTask<Unit> Handle(MessageStats request, CancellationToken cancellationToken)
    {
        Debug.Assert(request.Update.Message != null);

        var items = new Dictionary<string, int>();

        var subscriptions = await sugarClient.Queryable<Subscription>()
            .Select(s => s.SectorCode)
            .ToListAsync(cancellationToken);

        foreach (var sub in subscriptions)
        {
            items.TryAdd(sub, 0);
            items[sub]++;
        }

        var msg = items.Aggregate("", ((s, pair) => s += $"{pair.Key}: {pair.Value}\n"));

        await botClient.SendMessage(
            request.Update.Message.Chat,
            msg,
            cancellationToken: cancellationToken
        );

        return Unit.Value;
    }
}