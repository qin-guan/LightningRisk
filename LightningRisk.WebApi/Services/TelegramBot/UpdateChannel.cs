using System.Threading.Channels;
using Telegram.Bot.Types;

namespace LightningRisk.WebApi.Services.TelegramBot;

public class UpdateChannel
{
    private readonly Channel<Update> _channel = Channel.CreateBounded<Update>(
        new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        }
    );

    public ChannelWriter<Update> Writer => _channel.Writer;
    public ChannelReader<Update> Reader => _channel.Reader;
}

