namespace LightningRisk.Core;

public record Sector(string Code)
{
    public static readonly Sector[] KnownSectors =
    [
        new("1N"),
        new("1S"),
        new("L1"),
        new("L2"),
        new("L3"),
        new("L4"),
        new("02"),
        new("3S"),
        new("3N"),
        new("04"),
        new("05"),
        new("06"),
        new("07"),
        new("8N"),
        new("8S"),
        new("09"),
        new("10N"),
        new("10S"),
        new("11W"),
        new("11E"),
        new("12"),
        new("13N"),
        new("13S"),
        new("14"),
        new("15"),
        new("16N"),
        new("16S"),
        new("17"),
        new("18W"),
        new("18E"),
        new("19N"),
        new("19S"),
    ];

    public string Name
    {
        get
        {
            return Code switch
            {
                "09" => "Kranji Camp 3",
                "19N" => "Pulau Tekong (Rocky Hill)",
                "19S" => "Pulau Tekong (Ladang)",
                _ => ""
            };
        }
    }
}
