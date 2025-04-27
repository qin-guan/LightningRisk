using LightningRisk.Core;
using LightningRisk.WebApi.Entities;
using Mediator;
using SqlSugar;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace LightningRisk.WebApi.Services.TelegramBot.Handlers;

public sealed record TelegramClientNewStatus(List<Status> Status) : IRequest;

public sealed class TelegramClientNewStatusHandler(ITelegramBotClient botClient, ISqlSugarClient sugarClient)
    : IRequestHandler<TelegramClientNewStatus>
{
    private const string WarningHeader = """
                                         ⛈⛈⛈⛈⛈⛈⛈⛈
                                         ⚠️ CAT 1 WARNING! ⚠️
                                         ⛈⛈⛈⛈⛈⛈⛈⛈
                                         """;

    public async ValueTask<Unit> Handle(TelegramClientNewStatus request, CancellationToken cancellationToken)
    {
        var sectors = new Dictionary<string, (DateTime, DateTime)>();

        foreach (var status in request.Status)
        {
            foreach (var sector in status.Sectors)
            {
                sectors[sector.Code] = (status.StartTime, status.EndTime);
            }
        }

        if (sectors is { Count : 0 })
        {
            var chats = await sugarClient.Queryable<Subscription>().Select(s => s.ChatId).Distinct()
                .ToListAsync(cancellationToken);
            await Parallel.ForEachAsync(chats, async (id, ct) =>
            {
                await botClient.SendMessage(
                    id,
                    $"""
                     🌞🌞🌞🌞🌞🌞🌞🌞
                     🌞 SUNNNNNNNNN 🌞
                     🌞🌞🌞🌞🌞🌞🌞🌞

                     <b>No more CAT 1, carry on with life!</b>

                     {request.Status.First().StartTime.ToShortTimeString()} - {request.Status.Last().EndTime.ToShortTimeString()}
                     """,
                    ParseMode.Html
                );
            });
        }

        else
        {
            var chats = (await sugarClient.Queryable<Subscription>()
                    .Where(s => sectors.Keys.Contains(s.SectorCode))
                    .ToListAsync(cancellationToken))
                .GroupBy(s => s.ChatId);

            await Parallel.ForEachAsync(chats, cancellationToken, async (group, ct) =>
            {
                var msg = $"""
                           {WarningHeader}


                           """;

                foreach (var subscription in group)
                {
                    if (string.IsNullOrWhiteSpace(Sector.KnownSectors.Single(s => s.Code == subscription.SectorCode)
                            .Name))
                    {
                        msg += $"<b>{subscription.SectorCode}</b>";
                    }
                    else
                    {
                        msg +=
                            $"<b><u>{Sector.KnownSectors.Single(s => s.Code == subscription.SectorCode).Name}</u></b>";
                    }

                    msg += "\n";

                    if (sectors[subscription.SectorCode].Item1 <= DateTime.Now)
                    {
                        msg += $"now";
                    }
                    else
                    {
                        msg += $"in {(sectors[subscription.SectorCode].Item1 - DateTime.Now).TotalMinutes:N0} mins";
                    }

                    msg += "\n";

                    msg +=
                        $"{sectors[subscription.SectorCode].Item1.ToShortTimeString()} - {sectors[subscription.SectorCode].Item2.ToShortTimeString()}" +
                        "\n\n";
                }

                await botClient.SendMessage(
                    group.Key,
                    msg,
                    ParseMode.Html,
                    cancellationToken: ct
                );
            });
        }

        return Unit.Value;
    }
}