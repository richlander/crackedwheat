using System.Diagnostics;
using Octokit;

namespace CrackedWheat;

public class Measures
{
    public static bool IsBot(string name) => name?.EndsWith("[bot]") ?? false;

    public static List<TimestampMeasure> GetTimestampsMeasures(List<RepoItem> timestamps, string kind)
    {
        if (timestamps.Count is 0)
        {
            return [];
        }

        RepoItem first = timestamps.First();
        RepoItem last = timestamps.Last();
        int middle = 0;

        if (timestamps.Count > 2)
        {
            timestamps.Sort(RepoItem.CompareByTimestamp);
            middle = timestamps.Count / 2;
        }

        RepoItem median = timestamps[middle];

        List<TimestampMeasure> set = timestamps.Count switch
        {
            0 => [],
            1 => Calc((0, first)),
            2 => Calc((0, first), (1, last)),
            _ => Calc((0, first), (middle, median) ,(timestamps.Count - 1, last))
        };

        return set;

        List<TimestampMeasure> Calc(params ReadOnlySpan<(int SetIndex, RepoItem Item)> values)
        {
            List<TimestampMeasure> entries = [];
            var now = DateTime.UtcNow;

            foreach (var (SetIndex, Item) in values)
            {
                TimeSpan latency = now - Item.Timestamp;
                int score = Measures.GetIssueLatencyScore(latency);
                var measure = new TimestampMeasure(kind, SetIndex, score, latency, Item.Location);
                entries.Add(measure);
#if DEBUG
    Console.WriteLine(measure);
#endif
            }

            return entries;
        }
    }

    public static async Task<List<TimestampMeasure>> GetTimestampMeasuresForRepoItems(IAsyncEnumerable<RepoItem> items, string kind, int count)
    {
        List<RepoItem> repoItems = [];
        int index = 0;
        // This algorithm is primarily in place to remove bots
        // and to requests the least number of items in a streaming fashion
        await foreach (var item in items)
        {
            string name = item.Author;
            index++;
            if (repoItems.Count >= count || index > count * 2)
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

            repoItems.Add(item);
        }
#if DEBUG
        Console.WriteLine($"{kind}; requested count: {count}; item count: {repoItems.Count}; index count: {index}");
#endif
        var measures = Measures.GetTimestampsMeasures(repoItems, kind);
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

    // This was an experiment for open issue percentage
    public static int GetOpenPercentageScore(double percentage) => percentage switch
    {
        var p when p < .01   => 10,
        var p when p < .06   => 9,
        var p when p < 0.12  => 6,
        var p when p < .18   => 3,
        _ => 0
    };
}
