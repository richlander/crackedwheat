using Octokit;

namespace CrackedWheat;

public record TimestampMeasure(string Kind, int Index, int Score, TimeSpan Latency, string Location) : IMeasure;

public record RepoItem(string Author, DateTimeOffset Timestamp, string Location)
{
    public static int CompareByTimestamp(RepoItem item1, RepoItem item2) => item1.Timestamp.CompareTo(item2.Timestamp);
};
