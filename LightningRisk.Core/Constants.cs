namespace LightningRisk.Core;

public static class Constants
{
    public const long ProdChannelId = 1347020644;
    public const long TestingChannelId = 2371601281;

    public static long GetChannelId(bool isProduction) => isProduction ? ProdChannelId : TestingChannelId;
}