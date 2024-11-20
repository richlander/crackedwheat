using System.Diagnostics;
using Octokit;

namespace CrackedWheat;

public class Measures
{
    public static OpenRatioMeasure GetOpenRatioMeasure(string name, int open, int closed)
    {
        var total = open + closed;
        double openPercentage = total is 0 ? 0 : (open / (float)total);
        int score = Measures.GetOpenPercentageScore(openPercentage);
        return new(name, open, score);
    }

    public static bool IsBot(string name) => name?.EndsWith("[bot]") ?? false;

    public static IEnumerable<DateTimeOffset> FilterOutBots(IEnumerable<(DateTimeOffset, string)> values, int count) => 
        values.Where(v => !v.Item2.EndsWith("[bot]")).Select(v => v.Item1).Take(count);

    public static List<TimestampMeasure> GetTimestampsMeasures(List<DateTimeOffset> timestamps, string kind)
    {
        if (timestamps.Count is 0)
        {
            return [];
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        TimeSpan first = now - timestamps.First();
        TimeSpan last = now - timestamps.Last();
        int middle = 0;

        if (timestamps.Count > 2)
        {
            timestamps.Sort();
            middle = (int)double.Round(timestamps.Count / 2.0);
        }

        TimeSpan median = now - timestamps[middle];

        List<TimestampMeasure> set = timestamps.Count switch
        {
            0 => [],
            1 => Calc((0, first)),
            2 => Calc((0, first), (1, last)),
            _ => Calc((0, first), (middle, now - timestamps[middle]) ,(timestamps.Count - 1, last))
        };

        return set;

        List<TimestampMeasure> Calc(params ReadOnlySpan<(int SetIndex, TimeSpan Latency)> values)
        {
            List<TimestampMeasure> entries = [];

            foreach (var (SetIndex, Latency) in values)
            {
                int score = Measures.GetIssueLatencyScore(Latency);
                var measure = new TimestampMeasure(kind, SetIndex, score, Latency);
                entries.Add(measure);
            }

            return entries;
        }
    }

    public static async Task<List<TimestampMeasure>> GetTimestampMeasuresForRepoItems(IAsyncEnumerable<RepoItem> items, string kind, int count)
    {
        List<DateTimeOffset> timestamps = [];
        int index = 0;
        await foreach (var (name, timestamp) in items)
        {
            index++;
            if (timestamps.Count >= count || index > count * 2)
            {
                break;
            }
            else if (Measures.IsBot(name))
            {
#if DEBUG
                Console.WriteLine($"{name} is bot");
#endif
                continue;
            }

            timestamps.Add(timestamp);
        }
#if DEBUG
        Console.WriteLine($"{kind}; requested count: {count}; item count: {timestamps.Count}; index count: {index}");
#endif
        var measures = Measures.GetTimestampsMeasures(timestamps, kind);
        return measures;
    }

    public static int GetIssueLatencyScore(TimeSpan timespan) => timespan switch
    {
        var t when t < TimeSpan.FromDays(1)   => 10,
        var t when t < TimeSpan.FromDays(3)   => 9,
        var t when t < TimeSpan.FromDays(9)   => 6,
        var t when t < TimeSpan.FromDays(30)  => 4,
        var t when t < TimeSpan.FromDays(90)  => 3,
        var t when t < TimeSpan.FromDays(180) => 1,
        _ => 0
    };

    public static int GetOpenPercentageScore(double percentage) => percentage switch
    {
        var p when p < .01   => 10,
        var p when p < .06   => 9,
        var p when p < 0.12  => 6,
        var p when p < .18   => 3,
        _ => 0
    };
}
