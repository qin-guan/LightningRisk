using Polly.Registry;
using R3;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace LightningRisk.WebApi.Services;

public class DgsLightningBackgroundService(
    ILogger<DgsLightningBackgroundService> logger,
    IServiceProvider serviceProvider,
    ResiliencePipelineProvider<string> resiliencePipelineProvider
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation($"Starting {nameof(DgsLightningBackgroundService)} service");

        var tcs = new TaskCompletionSource();

        Observable.Interval(TimeSpan.FromSeconds(5), ct)
            .SubscribeAwait(async (_, ct2) =>
            {
                if (ct2.IsCancellationRequested)
                {
                    logger.LogInformation("Cancellation requested");
                    tcs.SetResult();
                    return;
                }

                await using var scope = serviceProvider.CreateAsyncScope();
                var dgsApi = scope.ServiceProvider.GetRequiredService<IDgsApi>();

                await dgsApi.GetLightningAsync();
            }, AwaitOperation.Drop);

        await tcs.Task;
        logger.LogInformation("complete");
    }
}