namespace LightningRisk.Core;

public record Status(IList<Sector> Sectors, DateTime StartTime, DateTime EndTime);