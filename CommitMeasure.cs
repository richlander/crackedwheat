using System.Diagnostics;
using Octokit;

namespace CrackedWheat;

public class CommitMeasure
{
    public static async Task<List<TimestampMeasure>> GetMeasuresForRepo(IRepositoryCommitsClient client, Repository repo, int count)
    {
        var kind = "commits";
        var items = GetRepoItems(client, repo, count);
        var measures = await Measures.GetTimestampMeasuresForRepoItems(items, kind, count);
        return measures;
    }

    // Get repo items from raw query
    public static async IAsyncEnumerable<RepoItem> GetRepoItems(IRepositoryCommitsClient client, Repository repo, int count)
    {
        await foreach (var commit in GetCommits(client, repo, count))
        {
            yield return new(commit.Author?.Login ?? "", commit.Commit.Author.Date, commit.Url);
        }
    }

    // Raw query
    private static async IAsyncEnumerable<GitHubCommit> GetCommits(IRepositoryCommitsClient client, Repository repo, int count)
    {    
        int page = 0;
        IReadOnlyList<GitHubCommit>? commits = null;
        do
        {
            page++;
            commits = await client.GetAll(repo.Id, new ApiOptions()
            {
                PageSize = count,
                StartPage = page,
                PageCount = 1
            });

            foreach(var commit in commits)
            {
                yield return commit;
            }
        } while (commits.Count >= count);

    }
}