using System.Text.RegularExpressions;

namespace LightningRisk.Core;

public static partial class MessageParser
{
    [GeneratedRegex(@"\((\d{4})-(\d{4})\)")]
    private static partial Regex TimeSpanRegex();

    [GeneratedRegex(@"\[CAT Status Update\].+\nAll Sectors Clear \(\d{4}-\d{4}\)")]
    private static partial Regex AllSectorsClearRegex();

    [GeneratedRegex(@"\[CAT Status Update\].+\nCAT 1:")]
    private static partial Regex Cat1HeaderRegex();

    [GeneratedRegex(@"\(\d{4}-\d{4}\)\n(?:.{1,3},*)+")]
    private static partial Regex Cat1DataRegex();

    public static List<Status> Parse(string message, DateOnly date)
    {
        var allSectorsClearMatch = AllSectorsClearRegex().Match(message);
        if (allSectorsClearMatch.Success)
        {
            var timeSpan = TimeSpanRegex().Match(message);

            return
            [
                new Status(
                    [],
                    ParseTime(timeSpan.Groups[1].ValueSpan, date),
                    ParseTime(timeSpan.Groups[2].ValueSpan, date)
                )
            ];
        }

        var cat1HeaderMatch = Cat1HeaderRegex().Match(message);
        if (!cat1HeaderMatch.Success)
        {
            throw new Exception($"Unknown update message: {message}");
        }

        var cat1DataMatches = Cat1DataRegex().Matches(message).ToList();
        ArgumentNullException.ThrowIfNull(cat1DataMatches);

        return cat1DataMatches.Select(data =>
        {
            var timeSpan = TimeSpanRegex().Match(data.Value);
            ArgumentNullException.ThrowIfNull(timeSpan);

            return new Status(
                data.Groups[0].Value.Split('\n').Last().Split(',').Select(sector => new Sector(sector)).ToList(),
                ParseTime(timeSpan.Groups[1].ValueSpan, date),
                ParseTime(timeSpan.Groups[2].ValueSpan, date)
            );
        }).ToList();
    }

    private static DateTime ParseTime(ReadOnlySpan<char> time, DateOnly date)
    {
        if (time.Length != 4)
        {
            throw new FormatException("Invalid time format.");
        }

        var hours = int.Parse(time[..2]);
        var minutes = int.Parse(time[2..]);

        Console.WriteLine($"{hours} {minutes}");

        return new DateTime(
            date.Year,
            date.Month,
            hours == 24 ? date.Day + 1 : date.Day,
            hours == 24 ? 0 : hours,
            minutes, 0
        );
    }
}