using System.Diagnostics;
using System.Diagnostics.Metrics;
using CrackedWheat;
using Octokit;

string user = "dotnet";
string PAT_ENV = "GITHUB_PAT";
string? pat = Environment.GetEnvironmentVariable(PAT_ENV);

if (string.IsNullOrEmpty(pat))
{
    Console.WriteLine($"{PAT_ENV} must be set");
}

var client = new GitHubClient(new ProductHeaderValue("org-health-app"))
{
    Credentials = new(pat)
};
var repos = await client.Repository.GetAllForOrg(user);
var issues = client.Issue;
var pulls = client.PullRequest;
var commits = client.Repository.Commit;
int count = 10;

Console.WriteLine($"Running query for: {user}");

Console.WriteLine($"Name, Score");
foreach (var repo in repos)
{
    if (repo.Archived)
    {
        continue;
    }

    Stopwatch watch = Stopwatch.StartNew();
    Score score = new();
    // var commitsTimestampMeasure = await CommitMeasure.GetMeasuresForRepo(commits, repo, count);
    // score.Add(commitsTimestampMeasure);
    var issuesTimestampMeasure = await IssueMeasures.GetMeasuresForRepo(issues, repo, count);
    score.Add(issuesTimestampMeasure);
    var pullsTimestampMeasure = await PullRequestMeasures.GetMeasuresForRepo(pulls, repo, count);
    score.Add(pullsTimestampMeasure);
    
#if DEBUG
    Console.WriteLine($"Query time: {watch.ElapsedMilliseconds}");
#endif

    Console.WriteLine($"{repo.FullName}, {score.Get()}");
}

