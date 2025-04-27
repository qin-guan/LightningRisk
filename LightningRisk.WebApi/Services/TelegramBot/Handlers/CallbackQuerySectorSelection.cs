using System.Diagnostics;
using Dm.util;
using LightningRisk.Core;
using LightningRisk.WebApi.Entities;
using Mediator;
using SqlSugar;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LightningRisk.WebApi.Services.TelegramBot.Handlers;

public sealed record CallbackQuerySectorSelection(Update Update) : IRequest;

public sealed class CallbackQuerySectorSelectionHandler(
    ITelegramBotClient botClient,
    ISqlSugarClient sugarClient,
    ReplyMarkupService markupService)
    : IRequestHandler<CallbackQuerySectorSelection>
{
    public async ValueTask<Unit> Handle(CallbackQuerySectorSelection request, CancellationToken cancellationToken)
    {
        Debug.Assert(request.Update.CallbackQuery != null);
        Debug.Assert(request.Update.CallbackQuery.Message != null);

        var existing = await sugarClient.Queryable<Subscription>()
            .Where(s => s.ChatId == request.Update.CallbackQuery.Message.Chat.Id)
            .Select(s => s.SectorCode)
            .ToListAsync(cancellationToken);

        switch (request.Update.CallbackQuery)
        {
            case { Data: "SELECT_ALL" }:
            {
                await sugarClient
                    .Insertable(
                        Sector.KnownSectors
                            .Where(s => !existing.Contains(s.Code))
                            .Select(s => new Subscription
                            {
                                SectorCode = s.Code,
                                ChatId = request.Update.CallbackQuery.Message.Chat.Id
                            })
                            .ToList()
                    )
                    .ExecuteCommandAsync(cancellationToken);

                break;
            }

            case { Data: "CLEAR_ALL" }:
            {
                await sugarClient
                    .Deleteable<Subscription>(s => s.ChatId == request.Update.CallbackQuery.Message.Chat.Id)
                    .ExecuteCommandAsync(cancellationToken);

                break;
            }

            case { Data: "CONFIRM" }:
            {
                await botClient.EditMessageReplyMarkup(
                    request.Update.CallbackQuery.Message.Chat.Id,
                    request.Update.CallbackQuery.Message.Id,
                    new InlineKeyboardMarkup(),
                    cancellationToken: cancellationToken
                );

                await botClient.SendMessage(
                    request.Update.CallbackQuery.Message.Chat.Id,
                    """
                    You've updated your subscription! 

                    To change it again, simply type /start.
                    """,
                    cancellationToken: cancellationToken
                );

                break;
            }

            default:
            {
                if (existing.Contains(request.Update.CallbackQuery.Data))
                {
                    await sugarClient.Deleteable<Subscription>(s =>
                        s.ChatId == request.Update.CallbackQuery.Message.Chat.Id &&
                        s.SectorCode == request.Update.CallbackQuery.Data
                    ).ExecuteCommandAsync(cancellationToken);
                }
                else
                {
                    await sugarClient
                        .Insertable(
                            new Subscription
                            {
                                ChatId = request.Update.CallbackQuery.Message.Chat.Id,
                                SectorCode = request.Update.CallbackQuery.Data,
                            }
                        )
                        .ExecuteCommandAsync(cancellationToken);
                }

                break;
            }
        }

        var tasks = new List<Task>();

        var selected = await sugarClient.Queryable<Subscription>()
            .Where(s => s.ChatId == request.Update.CallbackQuery.Message.Chat.Id)
            .Select(s => s.SectorCode)
            .ToListAsync(cancellationToken);

        if (!existing.SequenceEqual(selected))
        {
            var markup = await markupService.GetInlineKeyboardMarkupAsync(
                request.Update.CallbackQuery.Message.Chat.Id,
                cancellationToken
            );

            tasks.Add(botClient.EditMessageReplyMarkup(
                request.Update.CallbackQuery.Message.Chat.Id,
                request.Update.CallbackQuery.Message.Id,
                markup,
                cancellationToken: cancellationToken
            ));
        }

        tasks.Add(botClient.AnswerCallbackQuery(
            request.Update.CallbackQuery.Id,
            "Updated!",
            cancellationToken: cancellationToken
        ));

        await Task.WhenAll(tasks);

        return Unit.Value;
    }
}