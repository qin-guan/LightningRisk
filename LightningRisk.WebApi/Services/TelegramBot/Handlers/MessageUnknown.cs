using System.Diagnostics;
using Mediator;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LightningRisk.WebApi.Services.TelegramBot.Handlers;

public sealed record MessageUnknown(Update Update) : IRequest;

public sealed class UnknownHandler(ITelegramBotClient botClient) : IRequestHandler<MessageUnknown>
{
    public async ValueTask<Unit> Handle(MessageUnknown request, CancellationToken cancellationToken)
    {
        Debug.Assert(request.Update.Message != null);
        
        await botClient.SendMessage(
            request.Update.Message.Chat,
            """
            Unknown command!

            Need help?

            /start
            """,
            cancellationToken: cancellationToken
        );

        return Unit.Value;
    }
}