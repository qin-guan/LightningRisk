using LightningRisk.Core;
using LightningRisk.WebApi.Context;
using LightningRisk.WebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Message = Telegram.Bot.Types.Message;
using Update = Telegram.Bot.Types.Update;

namespace LightningRisk.WebApi.Services;

public class TelegramBotUpdateFinalHandler(
    ILogger<TelegramBotUpdateHandlerService> logger,
    AppDbContext dbContext,
    ITelegramBotClient bot
)
{
    private const string SelectAllSectors = "ALL";
    private const string SelectZeroSectors = "NONE";
    private const string ConfirmSectors = "CONFIRM";

    public async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        await (update switch
        {
            { Message: { } message } => OnMessage(message, ct),
            { EditedMessage: { } message } => OnMessage(message, ct),
            { CallbackQuery: { } callbackQuery } => OnCallbackQuery(callbackQuery, ct),
            _ => UnknownUpdateHandlerAsync(update)
        });
    }

    private async Task OnCallbackQuery(CallbackQuery callbackQuery, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(callbackQuery.Message);

        var markup = new InlineKeyboardMarkup();

        switch (callbackQuery.Data)
        {
            case SelectAllSectors:
            {
                AddSectorButtons(markup, Sector.KnownSectors);

                var existing = await dbContext.Subscriptions
                    .Where(s =>
                        s.ChatId == callbackQuery.Message.Chat.Id
                    )
                    .Select(s => s.SectorCode)
                    .ToListAsync(cancellationToken: ct);

                var needed = Sector.KnownSectors
                    .Where(s => !existing.Contains(s.Code))
                    .Select(s => new Subscription
                    {
                        ChatId = callbackQuery.Message.Chat.Id,
                        SectorCode = s.Code
                    });

                await dbContext.Subscriptions.AddRangeAsync(needed, ct);

                break;
            }
            case SelectZeroSectors:
            {
                AddSectorButtons(markup, []);

                var existing = dbContext.Subscriptions
                    .Where(s =>
                        s.ChatId == callbackQuery.Message.Chat.Id
                    );

                dbContext.RemoveRange(existing);

                break;
            }
            case ConfirmSectors:
            {
                var sectors = await dbContext.Subscriptions.Where(s => s.ChatId == callbackQuery.Message.Chat.Id)
                    .ToListAsync(cancellationToken: ct);

                var msg = """
                          You've subscribed to the following sectors:


                          """;

                foreach (var s in sectors
                             .Select(sector => new Sector(sector.SectorCode))
                             .OrderByDescending(s => s.Name)
                        )
                {
                    msg += "- " + s.Code;

                    if (!string.IsNullOrWhiteSpace(s.Name))
                    {
                        msg += "(" + s.Name + ")";
                    }

                    msg += "\n";
                }

                msg += """

                       Send any message to show this prompt again.

                       GOOD DAY!
                       """;

                await bot.SendMessage(
                    new ChatId(callbackQuery.Message.Chat.Id),
                    msg,
                    cancellationToken: ct
                );

                break;
            }
            default:
            {
                var existing = await dbContext.Subscriptions
                    .Where(s => s.ChatId == callbackQuery.Message.Chat.Id)
                    .ToListAsync(cancellationToken: ct);

                var toggledSector = existing.SingleOrDefault(s => s.SectorCode == callbackQuery.Data);

                if (toggledSector is not null)
                {
                    dbContext.Remove(toggledSector);
                    AddSectorButtons(
                        markup,
                        existing
                            .Where(s => s.SectorCode != callbackQuery.Data)
                            .Select(s => new Sector(s.SectorCode))
                            .ToArray()
                    );
                }
                else
                {
                    await dbContext.Subscriptions.AddAsync(
                        new Subscription
                        {
                            ChatId = callbackQuery.Message.Chat.Id,
                            SectorCode = callbackQuery.Data ?? throw new InvalidOperationException()
                        }, ct);

                    AddSectorButtons(
                        markup,
                        existing
                            .Select(s => new Sector(s.SectorCode))
                            .Concat([new Sector(callbackQuery.Data)])
                            .ToArray()
                    );
                }


                break;
            }
        }

        await dbContext.SaveChangesAsync(ct);

        await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
        await bot.EditMessageReplyMarkup(
            new ChatId((callbackQuery.Message ?? throw new Exception("Message is null in callback query.")).Chat.Id),
            callbackQuery.Message.Id,
            markup,
            cancellationToken: ct
        );
    }

    private async Task OnMessage(Message msg, CancellationToken ct)
    {
        logger.LogInformation("Receive message type: {MessageType}", msg.Type);

        if (msg.Text is null)
            return;

        switch (msg.Text)
        {
            case "/stats":
            {
                var totalSubscriptions = await dbContext.Subscriptions.LongCountAsync(cancellationToken: ct);
                var users = await dbContext.Subscriptions.Select(s => s.ChatId).Distinct()
                    .CountAsync(cancellationToken: ct);

                await bot.SendMessage(
                    msg.Chat,
                    $"""
                     Total subscriptions: {totalSubscriptions}
                     Users: {users}
                     """,
                    cancellationToken: ct
                );

                break;
            }
            default:
            {
                var markup = new InlineKeyboardMarkup();

                var existing = await dbContext.Subscriptions.Where(s => s.ChatId == msg.Chat.Id)
                    .ToListAsync(cancellationToken: ct);

                AddSectorButtons(markup, existing.Select(s => new Sector(s.SectorCode)).ToArray());

                await bot.SendMessage(
                    msg.Chat,
                    "What sectors would you like to subscribe to?",
                    replyMarkup: markup,
                    cancellationToken: ct
                );

                break;
            }
        }
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    private static InlineKeyboardMarkup AddSectorButtons(InlineKeyboardMarkup markup, params Sector[] sectors)
    {
        var allSelected = sectors.Length == Sector.KnownSectors.Length;

        if (allSelected)
        {
            markup.AddButton("Select none", SelectZeroSectors).AddNewRow();
        }
        else
        {
            markup.AddButton("Select all", SelectAllSectors).AddNewRow();
        }

        var idx = 0;
        var orderedSectors = Sector.KnownSectors.OrderByDescending(s => s.Name);

        foreach (var sector in orderedSectors)
        {
            var sectorName = string.IsNullOrWhiteSpace(sector.Name) ? sector.Code : sector.Name;

            var tick = allSelected || sectors.Select(s => s.Code).Contains(sector.Code)
                ? "✅ "
                : "";

            markup = markup.AddButton(tick + sectorName, sector.Code);

            if (idx % 2 != 0)
            {
                markup = markup.AddNewRow();
            }

            idx++;
        }

        markup.AddNewRow();
        markup.AddButton("Confirm", ConfirmSectors);

        return markup;
    }
}