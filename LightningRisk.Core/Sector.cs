namespace LightningRisk.Core;

public record Sector
{
    public static readonly Dictionary<string, string> KnownSectors = new()
    {
        { "1N", "" },
        { "1S", "" },
        { "L1", "" },
        { "L2", "" },
        { "L3", "" },
        { "L4", "" },
        { "02", "" },
        { "3S", "" },
        { "3N", "" },
        { "04", "" },
        { "05", "" },
        { "06", "" },
        { "07", "" },
        { "8N", "" },
        { "8S", "" },
        { "09", "Kranji Camp 3" },
        { "10N", "" },
        { "10S", "" },
        { "11W", "" },
        { "11E", "" },
        { "12", "" },
        { "13N", "" },
        { "13S", "" },
        { "14", "" },
        { "15", "" },
        { "16N", "" },
        { "16S", "" },
        { "17", "" },
        { "18W", "" },
        { "18E", "" },
        { "19N", "Pulau Tekong (Rocky Hill)" },
        { "19S", "Pulau Tekong (Ladang)" },
    };

    public string Code { get; }

    public string Name => KnownSectors.GetValueOrDefault(Code, "");

    public Sector(string code)
    {
        if (!KnownSectors.ContainsKey(code))
        {
            throw new ArgumentException($"Unknown sector code: {code}");
        }

        Code = code;
    }
}