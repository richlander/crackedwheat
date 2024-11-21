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
            var items = GetRepoItems(client, request, repo, count);
            var requestMeasure = await Measures.GetTimestampMeasuresForRepoItems(items, kind, count);
            measures.AddRange(requestMeasure);
        }

        return measures;
    }

    // Get repo items from raw query
    public static async IAsyncEnumerable<RepoItem> GetRepoItems(IIssuesClient client, RepositoryIssueRequest request, Repository repo, int count)
    {
        await foreach (var issue in GetIssues(client, request, repo, count))
        {
            yield return new(issue.User.Login, issue.UpdatedAt ?? DateTimeOffset.MinValue, issue.Url);
        }
    }

    private static async IAsyncEnumerable<Issue> GetIssues(IIssuesClient client, RepositoryIssueRequest request, Repository repo, int count)
    {    
        int page = 0;
        IReadOnlyList<Issue>? issues = null;
        do
        {
            page++;
            var options = new ApiOptions()
            {
                PageSize = count,
                StartPage = page,
                PageCount = 1
            };            
            issues = await client.GetAllForRepository(repo.Id, request, options);

            foreach(var issue in issues)
            {
                if (issue.PullRequest is {})
                {
                    continue;
                }

                yield return issue;
            }
        } while (issues.Count >= count);
    }
}
