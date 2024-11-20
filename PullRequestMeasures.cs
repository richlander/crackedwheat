using System.Diagnostics;
using System.Net;
using Octokit;

namespace CrackedWheat;

public class PullRequestMeasures
{
    public static async Task<List<TimestampMeasure>> GetMeasuresForRepo(IPullRequestsClient client, Repository repo, int count)
    {
        var openRequest = ("open", new PullRequestRequest()
            {
                State = ItemStateFilter.Open,
                SortProperty = PullRequestSort.Created,
                SortDirection = SortDirection.Descending,
            });
        var closedRequest = ("closed", new PullRequestRequest()
            {
                State = ItemStateFilter.Closed,
                SortProperty = PullRequestSort.Created,
                SortDirection = SortDirection.Descending
            });

        // Get timestamp measures
        List<TimestampMeasure> measures = [];
        List<(string, PullRequestRequest)> requests = [openRequest, closedRequest];
        foreach (var (kind, request) in requests)
        {
            var timestamps = GetTimestamps(client, request, repo, count);
            var requestMeasure = await Measures.GetTimestampMeasuresForRepoItems(timestamps, kind, count);
            measures.AddRange(requestMeasure);
        }

        return measures;
    }

    // Get repo items from raw query
    public static async IAsyncEnumerable<RepoItem> GetTimestamps(IPullRequestsClient client, PullRequestRequest request, Repository repo, int count)
    {
        await foreach (var item in GetItems(client, request, repo, count))
        {
            yield return new(item.User.Name, item.UpdatedAt);
        }
    }

    private static async IAsyncEnumerable<PullRequest> GetItems(IPullRequestsClient client, PullRequestRequest request, Repository repo, int count)
    {    
        int page = 0;
        IReadOnlyList<PullRequest>? items = null;
        do
        {
            items = await client.GetAllForRepository(repo.Id, request, new ApiOptions()
            {
                PageCount = page++,
                PageSize = count
            });

            foreach(var item in items)
            {
                yield return item;
            }
        } while (items is not null);
    }
}


// using Octokit;

// namespace CrackedWheat;

// public class PullRequestMeasures
// {
//     public static async Task<(OpenRatioMeasure, List<TimestampMeasure>)> GetMeasuresForRepo(IPullRequestsClient client, Repository repo, int count)
//     {
//         var apiOptions = new ApiOptions()
//         {
//             PageCount = 1,
//             PageSize = count
//         };
//         var openRequest = new PullRequestRequest()
//             {
//                 State = ItemStateFilter.Open,
//                 SortProperty = PullRequestSort.Updated,
//                 SortDirection = SortDirection.Descending,
//             };
//         var openPulls = await client.GetAllForRepository(repo.Id, openRequest, apiOptions);

//         var closedRequest = new PullRequestRequest()
//             {
//                 State = ItemStateFilter.Closed,
//                 SortProperty = PullRequestSort.Updated,
//                 SortDirection = SortDirection.Descending
//             };
//         var closedPulls = await client.GetAllForRepository(repo.Id, closedRequest, apiOptions);

//         // Get timestamp measures
//         List<TimestampMeasure> measures = [];
//         foreach (var (kind, issues) in (ReadOnlySpan<(string, IReadOnlyList<PullRequest>)>)[("open", openPulls), ("closed", closedPulls)])
//         {
//             // AddMeasures(issues, count, kind, measures);
//             var timestamps = GetTimeStamps(issues, count);
//             var m = Measures.GetTimestampsMeasures(timestamps, kind);
//             measures.AddRange(m);
//         }

//         // Get open ratio measures
//         int openCount = openPulls.Count;
//         int closedCount = closedPulls.Count;
//         var ratio = Measures.GetOpenRatioMeasure("pulls", openCount, closedCount);
//         return (ratio, measures);
//     }

//     public static IEnumerable<DateTimeOffset> GetTimeStamps(IReadOnlyList<PullRequest>? pulls, int count)
//     {
//         if (pulls is null or {Count: 0})
//         {
//             return [];
//         }

//         DateTimeOffset tomorrow = DateTimeOffset.UtcNow.AddDays(1);
//         return pulls.Take(count).Select( i => i.UpdatedAt);
//     }
// }