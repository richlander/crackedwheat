using System.Diagnostics;
using System.Diagnostics.Metrics;
using CrackedWheat;
using Octokit;

const string CW_GITHUB_PAT = "CW_GITHUB_PAT";
const string CW_ORG = "CW_ORG";
string? org = Environment.GetEnvironmentVariable(CW_ORG);
string? pat = Environment.GetEnvironmentVariable(CW_GITHUB_PAT);

if (string.IsNullOrEmpty(pat) ||
    string.IsNullOrEmpty(org))
{
    Console.WriteLine($"{CW_GITHUB_PAT} and {CW_ORG} must be set");
    return;
}

var client = new GitHubClient(new ProductHeaderValue("org-health-app"))
{
    Credentials = new(pat)
};

var repos = await client.Repository.GetAllForOrg(org);
var issues = client.Issue;
var pulls = client.PullRequest;
var commits = client.Repository.Commit;
int count = 10;

#if DEBUG
Console.WriteLine($"Running query for: {org}");
#endif

Console.WriteLine($"Name,Score");
int totalScore = 0;
int repoCount = 0;
foreach (var repo in repos)
{
#if DEBUG
    Console.WriteLine($"Querying {repo.FullName}; Archived: {repo.Archived}");
#endif

    if (repo.Archived)
    {
        continue;
    }

    Stopwatch watch = Stopwatch.StartNew();
    Score score = new();
    var commitsTimestampMeasure = await CommitMeasure.GetMeasuresForRepo(commits, repo, count);
    score.Add(commitsTimestampMeasure);
    var issuesTimestampMeasure = await IssueMeasures.GetMeasuresForRepo(issues, repo, count);
    score.Add(issuesTimestampMeasure);
    var pullsTimestampMeasure = await PullRequestMeasures.GetMeasuresForRepo(pulls, repo, count);
    score.Add(pullsTimestampMeasure);
    
#if DEBUG
    Console.WriteLine($"Query time: {watch.ElapsedMilliseconds}");
#endif

    int repoScore = score.Get();
    Console.WriteLine($"{repo.FullName},{repoScore}");
    totalScore += repoScore;
    repoCount++;
}

Console.WriteLine($"{org},{totalScore / repoCount}");

