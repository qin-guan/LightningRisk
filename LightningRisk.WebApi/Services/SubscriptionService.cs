using LightningRisk.Core;
using LightningRisk.WebApi.Context;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace LightningRisk.WebApi.Services;

public class SubscriptionService(ITelegramBotClient client, AppDbContext dbContext)
{
    private const string WarningHeader = """
                                         ⛈⛈⛈⛈⛈⛈⛈⛈
                                         ⚠️ CAT 1 WARNING! ⚠️
                                         ⛈⛈⛈⛈⛈⛈⛈⛈
                                         """;

    public async Task NotifySubscribersAsync(IList<Status> statuses)
    {
        var sectors = new Dictionary<string, (DateTime, DateTime)>();

        foreach (var status in statuses)
        {
            foreach (var sector in status.Sectors)
            {
                sectors[sector.Code] = (status.StartTime, status.EndTime);
            }
        }

        if (sectors.Count == 0)
        {
            foreach (var chatId in dbContext.Subscriptions.Select(s => s.ChatId).Distinct())
            {
                await client.SendMessage(
                    chatId,
                    $"""
                     🌞🌞🌞🌞🌞🌞🌞🌞
                     🌞 SUNNNNNNNNN 🌞
                     🌞🌞🌞🌞🌞🌞🌞🌞

                     <b>No more CAT 1, carry on with life!</b>

                     {statuses.First().StartTime.ToShortTimeString()} - {statuses.Last().EndTime.ToShortTimeString()}
                     """,
                    ParseMode.Html
                );
            }

            return;
        }

        var users = dbContext.Subscriptions
            .Where(s => sectors.Keys.Contains(s.SectorCode))
            .GroupBy(s => s.ChatId);

        foreach (var user in users)
        {
            var msg = $"""
                       {WarningHeader}

                       The following sectors you have subscribed to have CAT 1 alerts!


                       """;

            foreach (var subscription in user)
            {
                if (string.IsNullOrWhiteSpace(Sector.KnownSectors[subscription.SectorCode]))
                {
                    msg += $"<b>{subscription.SectorCode}</b>";
                }
                else
                {
                    msg += $"<b><u>{Sector.KnownSectors[subscription.SectorCode]}</u></b>";
                }

                msg += "\n";

                msg += $"in {(sectors[subscription.SectorCode].Item1 - DateTime.Now).TotalMinutes:N0} mins" +
                       "\n" +
                       $"{sectors[subscription.SectorCode].Item1.ToShortTimeString()} - {sectors[subscription.SectorCode].Item2.ToShortTimeString()}" +
                       "\n\n";
            }

            await client.SendMessage(
                user.Key,
                msg,
                ParseMode.Html
            );
        }
    }
}