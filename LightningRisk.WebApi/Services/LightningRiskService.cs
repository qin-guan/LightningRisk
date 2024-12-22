using LightningRisk.Core;
using TL;

namespace LightningRisk.WebApi.Services;

public class LightningRiskService(IWebHostEnvironment environment)
{
    public const long ProdChannelId = 1347020644;
    public const long TestingChannelId = 2371601281;

    public long ChannelId => environment.IsDevelopment() ? TestingChannelId : ProdChannelId;
}