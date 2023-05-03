using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BeatSaberModdingTools.Nuke.Components;
using GlobExpressions;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Octokit;
using Octokit.Internal;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[ShutdownDotNetAfterServerBuild]
partial class Build : NukeBuild, ICleanRefs, IDeserializeManifest, IDownloadGameRefs, IDownloadBeatModsDependencies
{
	/// Support plugins are available for:
	///   - JetBrains ReSharper        https://nuke.build/resharper
	///   - JetBrains Rider            https://nuke.build/rider
	///   - Microsoft VisualStudio     https://nuke.build/visualstudio
	///   - Microsoft VSCode           https://nuke.build/vscode
	public static int Main() => Execute<Build>(x => x.Compile);

	[Nuke.Common.Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")] readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

	[Solution(GenerateProjects = true)] readonly Solution Solution;

	[GitVersion] readonly GitVersion GitVersion;

	Target Clean => _ => _
        .TryBefore<ICleanRefs>()
        .Executes(() =>
		{
			DotNetClean(s => s.SetProject(Solution.SongCore));
		});

	Target GrabRefs => _ => _
		.After(RestorePackages)
		.OnlyWhenStatic(() => IsServerBuild)
		.WhenSkipped(DependencyBehavior.Skip)
        .DependsOn<ICleanRefs>()
		.DependsOn<IDownloadGameRefs>()
		.DependsOn<IDownloadBeatModsDependencies>();

	Target RestorePackages => _ => _
		.DependsOn(Clean)
		.Executes(() => DotNetRestore(settings => settings.SetProjectFile(Solution.SongCore)));

	Target Compile => _ => _
		.DependsOn(RestorePackages)
		.DependsOn(GrabRefs)
		.Executes(() =>
		{
			DotNetBuild(settings => settings
				.EnableNoRestore()
				.SetProjectFile(Solution.SongCore)
				.SetConfiguration(Configuration)
				.SetVersion(GitVersion.FullSemVer)
				.SetAssemblyVersion(GitVersion.AssemblySemVer)
				.SetFileVersion(GitVersion.AssemblySemFileVer)
				.SetInformationalVersion(GitVersion.InformationalVersion));
		});

	[GitRepository]
	readonly GitRepository GitRepository;

	Target CreateGitHubRelease => _ => _
		.DependsOn(Compile)
		.Requires(() => Configuration == Configuration.Release)
		.Executes(async () =>
		{
			// Set credentials for authorized actions
			var credentials = new Credentials(GitHubActions.Token);
			GitHubTasks.GitHubClient = new GitHubClient(
				new ProductHeaderValue(nameof(NukeBuild)),
				new InMemoryCredentialStore(credentials));

			var (repositoryOwner, repositoryName) = (GitRepository.GetGitHubOwner(), GitRepository.GetGitHubName());

			// Create release
			var releaseTag = GitVersion.NuGetVersion;
			var newRelease = new NewRelease(releaseTag)
			{
				TargetCommitish = GitVersion.Sha,
				Draft = true,
				Name = $"{repositoryName} {releaseTag}",
				GenerateReleaseNotes = true
			};

			var createdRelease = await GitHubTasks.GitHubClient
				.Repository
				.Release
				.Create(repositoryOwner, repositoryName, newRelease);

			// Glob artifacts
			var globbingPath = Solution.SongCore.Directory / "bin" / Configuration;
			var artifactPaths = Glob
				.Files(globbingPath, "**/*.zip")
				.Select(relativePath => globbingPath / relativePath)
				.ToArray();

			// Add artifact to release
			Assert.NotEmpty(artifactPaths);
			var assetUploadTasks = artifactPaths
				.Select(filePath => AddArtifactToRelease(createdRelease, filePath));
			await Task.WhenAll(assetUploadTasks);
		});

	static async Task AddArtifactToRelease(Release createdRelease, string filePath)
	{
		Assert.FileExists(filePath);
		Log.Information("Uploading file at location {FilePath}", filePath);

		var artifactName = Path.GetFileName(filePath);
		await using var fileStream = File.OpenRead(filePath);
		var releaseAssetUpload = new ReleaseAssetUpload
		{
			FileName = artifactName,
			RawData = fileStream,
			ContentType = "application/octet-stream"
		};

		await GitHubTasks.GitHubClient
			.Repository
			.Release
			.UploadAsset(createdRelease, releaseAssetUpload);
	}
}