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
            var items = GetRepoItems(client, request, repo, count);
            var requestMeasure = await Measures.GetTimestampMeasuresForRepoItems(items, kind, count);
            measures.AddRange(requestMeasure);
        }

        return measures;
    }

    // Get repo items from raw query
    public static async IAsyncEnumerable<RepoItem> GetRepoItems(IPullRequestsClient client, PullRequestRequest request, Repository repo, int count)
    {
        await foreach (var pr in GetPullRequests(client, request, repo, count))
        {
            yield return new(pr.User.Login, pr.UpdatedAt, pr.Url);
        }
    }

    private static async IAsyncEnumerable<PullRequest> GetPullRequests(IPullRequestsClient client, PullRequestRequest request, Repository repo, int count)
    {    
        int page = 0;
        IReadOnlyList<PullRequest>? pulls = null;
        do
        {
            page++;
            pulls = await client.GetAllForRepository(repo.Id, request, new ApiOptions()
            {
                PageSize = count,
                StartPage = page,
                PageCount = 1
            });

#if DEBUG
            Console.WriteLine($"items size: {pulls.Count}");
#endif

            foreach(var pull in pulls)
            {
                yield return pull;
            }
        } while (pulls.Count >= count);
    }
}
