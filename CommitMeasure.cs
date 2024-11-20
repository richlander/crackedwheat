using System.Diagnostics;
using Octokit;

namespace CrackedWheat;

public class CommitMeasure
{
    public static async Task<List<TimestampMeasure>> GetMeasuresForRepo(IRepositoryCommitsClient client, Repository repo, int count)
    {
        var kind = "commits";
        var timestamps = GetTimestamps(client, repo, count);
        var measures = await Measures.GetTimestampMeasuresForRepoItems(timestamps, kind, count);
        return measures;
    }

    // Get repo items from raw query
    public static async IAsyncEnumerable<RepoItem> GetTimestamps(IRepositoryCommitsClient client, Repository repo, int count)
    {
        await foreach (var commit in GetItems(client, repo, count))
        {
            yield return new(commit.Author.Login, commit.Commit.Author.Date);
        }
    }

    // Raw query
    private static async IAsyncEnumerable<GitHubCommit> GetItems(IRepositoryCommitsClient client, Repository repo, int count)
    {    
        int page = 0;
        IReadOnlyList<GitHubCommit>? items = null;
        do
        {
            page++;
            var options = new ApiOptions()
            {
                PageSize = count,
                StartPage = page,
                PageCount = 1
            };            
            items = await client.GetAll(repo.Id, options);

            foreach(var item in items)
            {
                yield return item;
            }
        } while (items is not null);

    }
}