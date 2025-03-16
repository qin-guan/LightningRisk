using System.Text.Json;
using System.Text.Json.Serialization;
using LightningRisk.Services.LightningObservations;
using LightningRisk.Services.LightningObservations.Weather;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Polly.Registry;
using R3;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace LightningRisk.WebApi.Services;

public class DgsLightningBackgroundService(
    ILogger<DgsLightningBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation($"Starting {nameof(DgsLightningBackgroundService)} service");

        await Observable.Interval(TimeSpan.FromSeconds(5), ct)
            .SelectAwait(async (_, ct2) =>
            {
                var authProvider = new AnonymousAuthenticationProvider();
                var adapter = new HttpClientRequestAdapter(authProvider);
                var client = new LightningObservationsClient(adapter);

                var res = await client.Weather.GetAsWeatherGetResponseAsync(
                    o => { o.QueryParameters.Api = "lightning"; },
                    ct2
                );

                return res.Data.Records.SelectMany(r =>
                {
                    return r.Item.Readings.Select(rr => { return rr.Location; });
                }).ToObservable();
            }, AwaitOperation.Drop)
            .SelectMany(r => r)
            .SelectAwait(async (r, ct2) =>
            {
                var client = new HttpClient();
                
                Console.WriteLine("{0},{1}",r.Latitude, r.Longitude);
                
                return await client.GetFromJsonAsync<OneMapRes>(
                    $"https://www.onemap.gov.sg/api/public/revgeocode?location={r.Latitude},{r.Longitude}&buffer=500&addressType=All&otherFeatures=N",
                    cancellationToken: ct2
                );
            }, AwaitOperation.Parallel)
            .SelectAwait(async (r, ct2) =>
            {
                Console.WriteLine(JsonSerializer.Serialize(r.GeocodeInfo));
                return true;
            })
            .LastOrDefaultAsync(cancellationToken: ct);

        Console.WriteLine("done");
    }
}

internal record OneMapRes(List<OneMapRes_GeocodeInfo> GeocodeInfo);

internal record OneMapRes_GeocodeInfo(
    [property: JsonPropertyName("BUILDINGNAME")]
    string BuildingName,
    [property: JsonPropertyName("BLOCK")] string Block,
    [property: JsonPropertyName("ROAD")] string Road,
    [property: JsonPropertyName("POSTALCODE")]
    string PostalCode
);