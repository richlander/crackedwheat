using Octokit;

namespace CrackedWheat;

public record TimestampMeasure(string Kind, int Index, int Score, TimeSpan Latency) : IMeasure;

public record RepoItem(string Author, DateTimeOffset Timestamp);

public record OpenRatioMeasure(string Kind, int Index, int Score) : IMeasure;
