using WebApiClientCore.Attributes;

namespace LightningRisk.WebApi.Services;

[LoggingFilter(LogResponse = false)]
[HttpHost("https://api-open.data.gov.sg")]
public interface IDgsApi
{
    [HttpGet("/v2/real-time/api/weather?api=lightning")]
    public Task<string> GetLightningAsync();

    [HttpGet("/v2/real-time/api/weather?api=wbgt")]
    public Task<string> GetWbgtAsync();
}