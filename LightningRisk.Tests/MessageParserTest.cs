using LightningRisk.Core;

namespace LightningRisk.Tests;

public class MessageParserTest
{
    [Fact]
    public void AllSectorsClear()
    {
        const string message = """
                               [CAT Status Update] ⚡
                               All Sectors Clear (1430-1800)
                               """;

        var output = MessageParser.Parse(message, DateOnly.FromDateTime(DateTime.Now));

        Assert.Single(output);

        Assert.Equal(
            new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 14, 30, 0),
            output.First().StartTime
        );

        Assert.Equal(
            new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 18, 00, 0),
            output.First().EndTime
        );
    }

    [Fact]
    public void SingleTimingMultipleSectors()
    {
        const string message = """
                               [CAT Status Update] ⚡
                               CAT 1:
                               (0345-0430)
                               10N,11W,11E,12,15,16N,16S,17,18W,18E,19N,19S
                               """;

        var output = MessageParser.Parse(message, DateOnly.FromDateTime(DateTime.Now));
    }

    [Fact]
    public void MultipleTimingMultipleSectors()
    {
        const string message = """
                               [CAT Status Update] ⚡
                               CAT 1:
                               (0205-0300)
                               1N,1S,L1,L2,L3,L4,02,3S,3N,04,05,06,07,8N,8S,09,10N,10S,11W,11E,12,13N,13S,14,15,16N,16S,17,18W,19N

                               (0220-0300)
                               18E,19S
                               """;

        var output = MessageParser.Parse(message, DateOnly.FromDateTime(DateTime.Now));

        Assert.Equal(2, output.Count);

        Assert.Equal(30, output[0].Sectors.Count);
        Assert.Equal(2, output[1].Sectors.Count);
    }
}