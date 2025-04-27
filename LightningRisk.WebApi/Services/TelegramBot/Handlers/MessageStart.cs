using System.Diagnostics;
using LightningRisk.WebApi.Entities;
using Mediator;
using SqlSugar;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LightningRisk.WebApi.Services.TelegramBot.Handlers;

public sealed record MessageStart(Update Update) : IRequest;

public sealed class StartHandler(ITelegramBotClient botClient, ReplyMarkupService markupService)
    : IRequestHandler<MessageStart>
{
    private static readonly List<List<InlineKeyboardButton>> DefaultInlineSectorButtons =
    [
        [
            new InlineKeyboardButton("Select All", "SELECT_ALL"),
            new InlineKeyboardButton("Clear All", "CLEAR_ALL"),
        ],
        .. Core.Sector.KnownSectors
            .OrderBy(s => s.Code)
            .Select((sector, index) => new
            {
                Value = new InlineKeyboardButton(sector.Name, sector.Code),
                Index = index
            })
            .GroupBy(v => v.Index / 2)
            .Select(v => v.Select(vv => vv.Value).ToList())
            .ToList(),
        [
            new InlineKeyboardButton("Confirm", "CONFIRM"),
        ],
    ];

    public async ValueTask<Unit> Handle(MessageStart request, CancellationToken cancellationToken)
    {
        Debug.Assert(request.Update.Message != null);

        await botClient.SendMessage(request.Update.Message.Chat,
            "Welcome! Please select sectors below!",
            replyMarkup: await markupService.GetInlineKeyboardMarkupAsync(
                request.Update.Message.Chat.Id,
                cancellationToken
            ),
            cancellationToken: cancellationToken
        );

        return Unit.Value;
    }
}