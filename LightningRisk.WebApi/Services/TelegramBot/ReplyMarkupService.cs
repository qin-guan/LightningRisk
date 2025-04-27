using LightningRisk.Core;
using LightningRisk.WebApi.Entities;
using SqlSugar;
using Telegram.Bot.Types.ReplyMarkups;

namespace LightningRisk.WebApi.Services.TelegramBot;

public class ReplyMarkupService(ISqlSugarClient sugarClient)
{
    public async Task<InlineKeyboardMarkup> GetInlineKeyboardMarkupAsync(long chatId, CancellationToken ct)
    {
        var existing = await sugarClient.Queryable<Subscription>()
            .Where(s => s.ChatId == chatId)
            .Select(s => s.SectorCode).ToListAsync(ct);

        var markup = new InlineKeyboardMarkup
        {
            InlineKeyboard =
            [
                [
                    new InlineKeyboardButton("Select All", "SELECT_ALL"),
                    new InlineKeyboardButton("Clear All", "CLEAR_ALL"),
                ],

                .. Sector.KnownSectors
                    .OrderBy(s => s.Code)
                    .Select((sector, index) => new
                    {
                        Value = new InlineKeyboardButton(
                            (existing.Contains(sector.Code) ? "✅" : "") + sector.Name,
                            sector.Code
                        ),
                        Index = index
                    })
                    .GroupBy(v => v.Index / 2)
                    .Select(v => v.Select(vv => vv.Value).ToList())
                    .ToList(),

                [
                    new InlineKeyboardButton("Confirm", "CONFIRM"),
                ],
            ]
        };

        return markup;
    }
}