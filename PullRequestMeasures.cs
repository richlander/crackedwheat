using System.Diagnostics;
using System.Net;
using Octokit;

namespace CrackedWheat;

public class PullRequestMeasures
{
    public static async Task<List<TimestampMeasure>> GetMeasuresForRepo(IPullRequestsClient client, Repository repo, int count)
    {
        var openRequest = ("open PRs", new PullRequestRequest()
            {
                State = ItemStateFilter.Open,
                SortProperty = PullRequestSort.Created,
                SortDirection = SortDirection.Descending,
            });
        var closedRequest = ("closed PRs", new PullRequestRequest()
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
            yield return new(item.User.Login, item.UpdatedAt);
        }
    }

    private static async IAsyncEnumerable<PullRequest> GetItems(IPullRequestsClient client, PullRequestRequest request, Repository repo, int count)
    {    
        int page = 0;
        IReadOnlyList<PullRequest>? items = null;
        do
        {
            page++;
            var options = new ApiOptions()
            {
                PageSize = count,
                StartPage = page,
                PageCount = 1
            };
            items = await client.GetAllForRepository(repo.Id, request, options);

#if DEBUG
            Console.WriteLine($"items size: {items.Count}");
#endif

            foreach(var item in items)
            {
                yield return item;
            }
        } while (items.Count > 0);
    }
}
