using System.Diagnostics;
using System.Net;
using Octokit;

namespace CrackedWheat;

public class IssueMeasures
{
    public static async Task<List<TimestampMeasure>> GetMeasuresForRepo(IIssuesClient client, Repository repo, int count)
    {
        var openRequest = ("open issues", new RepositoryIssueRequest()
            {
                State = ItemStateFilter.Open,
                SortProperty = IssueSort.Created,
                SortDirection = SortDirection.Descending
            });
        var closedRequest = ("closed issues", new RepositoryIssueRequest()
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
            yield return new(item.User.Login, item.UpdatedAt ?? DateTimeOffset.MinValue);
        }
    }

    private static async IAsyncEnumerable<Issue> GetItems(IIssuesClient client, RepositoryIssueRequest request, Repository repo, int count)
    {    
        int page = 0;
        IReadOnlyList<Issue>? items = null;
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

            foreach(var item in items)
            {
                if (item.PullRequest is {})
                {
                    continue;
                }

                yield return item;
            }
        } while (items.Count > 0);
    }
}
