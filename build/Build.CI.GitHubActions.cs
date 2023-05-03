using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;

[GitHubActions(
	"pr",
	GitHubActionsImage.UbuntuLatest,
	AutoGenerate = false,
	CacheKeyFiles = new string[0],
	EnableGitHubToken = false,
	FetchDepth = 0, // Only a single commit is fetched by default, for the ref/SHA that triggered the workflow. Make sure to fetch whole git history, in order to get GitVersion to work.
	ImportSecrets = new[] { "SIRA_SERVER_CODE" },
	InvokedTargets = new[] { nameof(Compile) },
	OnPushBranches = new[] { "main" },
	OnPullRequestBranches = new[] { "main" },
	PublishArtifacts = true)]
[GitHubActions(
	"publish",
	GitHubActionsImage.UbuntuLatest,
	AutoGenerate = false,
	CacheKeyFiles = new string[0],
	EnableGitHubToken = true,
	FetchDepth = 0, // Only a single commit is fetched by default, for the ref/SHA that triggered the workflow. Make sure to fetch whole git history, in order to get GitVersion to work.
	ImportSecrets = new[] { "SIRA_SERVER_CODE" },
	InvokedTargets = new[] { nameof(CreateGitHubRelease) },
	OnPushTags = new[] { "*.*.*" },
	PublishArtifacts = true)]
partial class Build
{
	[CI] readonly GitHubActions GitHubActions;
}