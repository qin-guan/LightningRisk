using LightningRisk.Core;
using LightningRisk.WebApi.Context;
using LightningRisk.WebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Chat = LightningRisk.WebApi.Entities.Chat;
using Message = Telegram.Bot.Types.Message;
using Update = Telegram.Bot.Types.Update;

namespace LightningRisk.WebApi.Services;

public class TelegramBotUpdateHandlerService(
    ILogger<TelegramBotUpdateHandlerService> logger,
    AppDbContext dbContext,
    ITelegramBotClient bot
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
        var markup = new InlineKeyboardMarkup().AddButton("✅ All").AddNewRow();

        var idx = 0;
        foreach (var sector in Sector.KnownSectors.OrderByDescending(s => s.Value))
        {
            markup = markup.AddButton(
                string.IsNullOrWhiteSpace(sector.Value) ? sector.Key : sector.Value + "nice",
                sector.Key
            );

            if (idx % 2 != 0)
            {
                markup = markup.AddNewRow();
            }

            idx++;
        }

        await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
    }

    private async Task OnMessage(Message msg, CancellationToken ct)
    {
        logger.LogInformation("Receive message type: {MessageType}", msg.Type);

        if (msg.Text is null)
            return;

        var chat = await dbContext.Chats.SingleOrDefaultAsync(s => s.ChatId == msg.Chat.Id, cancellationToken: ct);
        if (chat is null)
        {
            var markup = new InlineKeyboardMarkup().AddButton("✅ All").AddNewRow();

            var idx = 0;
            foreach (var sector in Sector.KnownSectors.OrderByDescending(s => s.Value))
            {
                markup = markup.AddButton(string.IsNullOrWhiteSpace(sector.Value) ? sector.Key : sector.Value,
                    sector.Key);
                if (idx % 2 != 0)
                {
                    markup = markup.AddNewRow();
                }

                idx++;
            }

            await bot.SendMessage(
                msg.Chat,
                """
                What sectors would you like to subscribe to?

                (Key in the sector code or select from the options below)
                """,
                replyMarkup: markup,
                cancellationToken: ct
            );

            // await dbContext.Chats.AddAsync(new Chat
            // {
            //     ChatId = msg.Chat.Id,
            // }, ct);

            await dbContext.SaveChangesAsync(ct);

            return;
        }

        switch (chat.CurrentState)
        {
            case Chat.State.WaitForSectorsSelection:
            {
                var sectors = new List<string>();

                if (msg.Text.Contains(','))
                {
                    sectors.AddRange(msg.Text.Split(',').Distinct());
                }
                else
                {
                    sectors.Add(msg.Text);
                }

                var invalidSectors = sectors.Where(s => !Sector.KnownSectors.ContainsKey(s)).ToList();

                if (invalidSectors.Count != 0)
                {
                    await bot.SendMessage(
                        msg.Chat,
                        $"""
                         The sectors below are invalid, please try again.

                         {string.Join('\n', invalidSectors)}
                         """,
                        cancellationToken: ct
                    );

                    return;
                }

                await dbContext.Subscriptions.AddRangeAsync(
                    sectors.Select(s => new Subscription
                    {
                        ChatId = msg.Chat.Id,
                        SectorCode = s
                    }),
                    ct
                );

                await dbContext.SaveChangesAsync(ct);

                await chat.GoToHome();

                await bot.SendMessage(
                    msg.Chat,
                    $"""
                     You've subscribed to the following sectors:

                     {string.Join('\n', sectors)}
                     """,
                    cancellationToken: ct
                );

                break;
            }
            case Chat.State.Home:
            {
                var sectors = await dbContext.Subscriptions
                    .Where(s => s.ChatId == msg.Chat.Id)
                    .Select(s => s.SectorCode)
                    .ToListAsync(cancellationToken: ct);

                await bot.SendMessage(
                    msg.Chat,
                    $"""
                     You've subscribed to the following sectors:

                     {string.Join('\n', sectors)}
                     """,
                    cancellationToken: ct
                );

                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}