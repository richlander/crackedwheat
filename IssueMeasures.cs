using System.Diagnostics;
using System.Net;
using Octokit;

namespace CrackedWheat;

public class IssueMeasures
{
    public static async Task<List<TimestampMeasure>> GetMeasuresForRepo(IIssuesClient client, Repository repo, int count)
    {
        var openRequest = ("open", new RepositoryIssueRequest()
            {
                State = ItemStateFilter.Open,
                SortProperty = IssueSort.Created,
                SortDirection = SortDirection.Descending
            });
        var closedRequest = ("closed", new RepositoryIssueRequest()
            {
                State = ItemStateFilter.Closed,
                SortProperty = IssueSort.Created,
                SortDirection = SortDirection.Descending
            });

        // Get timestamp measures
        List<TimestampMeasure> measures = [];
        List<(string, RepositoryIssueRequest)> requests = [openRequest, closedRequest];
        foreach (var (kind, request) in requests)
        {
            var timestamps = GetTimestamps(client, request, repo, count);
            var requestMeasure = await Measures.GetTimestampMeasuresForRepoItems(timestamps, kind, count);
            measures.AddRange(requestMeasure);
        }

        return measures;
    }

    // Get repo items from raw query
    public static async IAsyncEnumerable<RepoItem> GetTimestamps(IIssuesClient client, RepositoryIssueRequest request, Repository repo, int count)
    {
        await foreach (var item in GetItems(client, request, repo, count))
        {
            yield return new(item.User.Name ?? throw new Exception(), item.UpdatedAt ?? DateTimeOffset.MinValue);
        }
    }

    private static async IAsyncEnumerable<Issue> GetItems(IIssuesClient client, RepositoryIssueRequest request, Repository repo, int count)
    {    
        int page = 0;
        IReadOnlyList<Issue>? items = null;
        do
        {
            items = await client.GetAllForRepository(repo.Id, request, new ApiOptions()
            {
                PageCount = page++,
                PageSize = count
            });

            foreach(var item in items)
            {
                if (item.PullRequest.Number is 0)
                {
                    continue;
                }

                yield return item;
            }
        } while (items is not null);
    }
}
