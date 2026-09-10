using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Extensions;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.Git;
using Fallout.Common.Tools.GitHub;
using Fallout.Common.Tools.NuGet;
using Octokit;
using Project = Fallout.Common.ProjectModel.Project;

[
    GitHubActions("Run Tests", GitHubActionsImage.WindowsLatest, InvokedTargets = [nameof(Test)],
        On = [GitHubActionsTrigger.WorkflowDispatch],
        CacheIncludePatterns = ["~/.nuget/packages"],
        CacheKeyFiles = ["**/global.json", "**/*.csproj", "**/Directory.Packages.props", "**/packages.lock.json"],
        Lfs = true),
    GitHubActions("CI Build", GitHubActionsImage.WindowsLatest, InvokedTargets = [nameof(PackAll)], PublishArtifacts = true,
        Submodules = GitHubActionsSubmodules.Recursive,
        CacheIncludePatterns = ["~/.nuget/packages"],
        CacheKeyFiles = ["**/global.json", "**/*.csproj", "**/Directory.Packages.props", "**/packages.lock.json"],
        OnWorkflowDispatchOptionalInputs = ["Version"]),
    GitHubActions("Release on Tag", 
        GitHubActionsImage.WindowsLatest,
        InvokedTargets = [nameof(TaggedRelease)],
        EnableGitHubToken = true,
        PublishArtifacts = true,
        WritePermissions = [GitHubActionsPermissions.Contents],
        Submodules = GitHubActionsSubmodules.Recursive,
        CacheIncludePatterns = ["~/.nuget/packages"],
        CacheKeyFiles = ["**/global.json", "**/*.csproj", "**/Directory.Packages.props", "**/packages.lock.json"],
        Lfs = true,
        OnPushTags = ["v*"]),
    GitHubActions("Pre-Release on Tag", 
        GitHubActionsImage.WindowsLatest,
        InvokedTargets = [nameof(TaggedPreRelease)],
        EnableGitHubToken = true,
        PublishArtifacts = true,
        WritePermissions = [GitHubActionsPermissions.Contents],
        Submodules = GitHubActionsSubmodules.Recursive,
        CacheIncludePatterns = ["~/.nuget/packages"],
        CacheKeyFiles = ["**/global.json", "**/*.csproj", "**/Directory.Packages.props", "**/packages.lock.json"],
        Lfs = true,
        OnPushTags = ["p*"]),
    GitHubActions("Manual Release", 
        GitHubActionsImage.WindowsLatest, 
        InvokedTargets = [nameof(TaggedRelease)],
        EnableGitHubToken = true,
        PublishArtifacts = true,
        WritePermissions = [GitHubActionsPermissions.Contents],
        Submodules = GitHubActionsSubmodules.Recursive,
        CacheIncludePatterns = ["~/.nuget/packages"],
        CacheKeyFiles = ["**/global.json", "**/*.csproj", "**/Directory.Packages.props", "**/packages.lock.json"],
        Lfs = true,
        OnWorkflowDispatchRequiredInputs = ["Version"]) 
]
class Build : FalloutBuild
{
    public Build() => NoLogo = true;

    [Solution(GenerateProjects = true)]
    readonly Solution Solution;
    [GitRepository]
    GitRepository Repository;
    private GitHubActions Actions => GitHubActions.Instance;

    public static int Main() => Execute<Build>(x => x.PackAll);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Optional version override. When empty, the current tag (v*/p*) is used if available.")]
    readonly string Version = string.Empty;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PackagesDirectory => ArtifactsDirectory / "packages";
    AbsolutePath ChangelogPath => RootDirectory / "CHANGELOG.md";

    private string GetVersionTag() => Version ?? Repository.Tags?.FirstOrDefault(_versionPredicate) ?? GitRepository.GetTag(_versionPredicate) ?? "1.0.0"; 
    
    private static readonly Func<string, bool> _versionPredicate = s => s.StartsWith('v') || s.StartsWith('p');
    private static string StripPrefixes(string? str) => str?.TrimStart('v').TrimStart('p');
    private string GetVersion() => StripPrefixes(GetVersionTag());
    private static string Quote(string str) => $"\"{str}\"";
    
    
    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(AbsolutePathExtensions.DeleteDirectory);
            ArtifactsDirectory.CreateOrCleanDirectory();
            PackagesDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s.SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            BuildProject(Solution.Dawn_Common);
            BuildProject(Solution.Windows.Dawn_Common_Windows);
            BuildProject(Solution.Windows.Dawn_Common_Windows_Desktop);
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution.Tests.Dawn_Common_Windows_Tests)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .EnableNoRestore());
        });

    Target PackCommon => _ => _
        .DependsOn(Test)
        .Produces(PackagesDirectory / "Dawn.Common*.nupkg")
        .Executes(() =>
        {
            PackProject(Solution.Dawn_Common);
        });

    Target PackCommonWindows => _ => _
        .DependsOn(Test)
        .Produces(PackagesDirectory / "Dawn.Common.Windows*.nupkg")
        .Executes(() =>
        {
            PackProject(Solution.Windows.Dawn_Common_Windows);
        });

    Target PackCommonWindowsDesktop => _ => _
        .DependsOn(Test)
        .Produces(PackagesDirectory / "Dawn.Common.Windows.Desktop*.nupkg")
        .Executes(() =>
        {
            PackProject(Solution.Windows.Dawn_Common_Windows_Desktop);
        });

    Target PackAll => _ => _
        .DependsOn(PackCommon, PackCommonWindows, PackCommonWindowsDesktop)
        .Executes(() => { });

    // Update changelog: move Unreleased to versioned section
    Target UpdateChangelog => _ => _
        .Unlisted()
        .Executes(() =>
        {
            var unreleasedNotes = ReadChangelog(ChangelogPath).Unreleased;
            var noNewChanges = unreleasedNotes?.EndIndex <= unreleasedNotes?.StartIndex;
            if (unreleasedNotes == null || noNewChanges)
                return;

            // Moves the changes in Unreleased to the latest tag
            FinalizeChangelog(ChangelogPath, GetVersion(), Repository);
        });

    // Push updated changelog back to default branch (used in CI after creating release)
    Target PushChangelog => _ => _
        .Unlisted()
        .Executes(() =>
        {
            var defaultBranch = Git("remote show origin")
                .FirstOrDefault(x => x.Text.Trim().StartsWith("HEAD branch:"))
                .Text.Split(':')[1]
                .Trim();
            
            Git($"config --global user.name {Quote("github-actions[bot]")}");
            Git($"config --global user.email {Quote("github-actions[bot]@users.noreply.github.com")}");
            
            Git($"add {ChangelogPath}");
            Git($"commit -m {Quote($"chore: {Path.GetFileName(ChangelogPath)} for {GetVersion()}")}");
            Git($"push origin HEAD:{defaultBranch}");
        });

    [Secret, Optional, Parameter("Private Access Token for publishing Nuget packages to GitHub")]
    string GithubNugetPAT;
    Target Publish => _ => _
        .DependsOn(PackAll)
        .OnlyWhenStatic(() => IsServerBuild)
        .Executes(() =>
        {
            if (string.IsNullOrWhiteSpace(GithubNugetPAT))
                GithubNugetPAT = Actions.Token;

            GithubNugetPAT.NotNullOrWhiteSpace();
            var source = $"https://nuget.pkg.github.com/{Repository.GetGitHubOwner()}/index.json";
            
            var preExisting = true;
            if (!NuGetSourcesList().Any(x => x.Text.Contains("github")))
            {
                preExisting = false;
                NuGetSourcesAdd(options => options
                    .SetName("github")
                    .SetUserName(Repository.GetGitHubOwner())
                    .SetPassword(GithubNugetPAT)
                    .SetSource(source));
            }

            NuGetPush(options => options
                .SetApiKey(GithubNugetPAT)
                .SetSource(source)
                .SetTargetPath((PackagesDirectory / "*.nupkg").GlobFiles().First()));

            if (!preExisting)
                NuGetSourcesRemove(options => options
                    .SetName("github"));
        });

    // Tagged pre-release target (uploads assets to GH releases as prerelease)
    Target TaggedPreRelease => _ => _
        .DependsOn(PackAll, Test)
        .Unlisted()
        .OnlyWhenStatic(() => IsServerBuild);

    // Tagged release that runs on v* tags
    Target TaggedRelease => _ => _
        .DependsOn(PackAll, Test, Publish)
        .Unlisted()
        .OnlyWhenStatic(() => IsServerBuild)
        .Executes(() =>
        {
            Execute<Build>(x => x.UpdateChangelog);
            Execute<Build>(x => x.PushChangelog);
        });

    void BuildProject(Project project)
    {
        DotNetBuild(s => s
            .SetProjectFile(project)
            .SetConfiguration(Configuration)
            .EnableNoRestore());
    }

    void PackProject(Project project, Func<DotNetPackSettings, DotNetPackSettings>? builder = null)
    {
        var version = GetVersionTag();
        if (string.IsNullOrWhiteSpace(version))
            throw new Exception($"Unable to resolve package version for '{project.Name}'. Supply --version or publish a p*/v* tag.");

        DotNetPack(s =>
        {
            var x = s.SetProject(project)
                .SetConfiguration(Configuration)
                .SetVersion(version)
                .SetOutputDirectory(PackagesDirectory)
                .EnableNoBuild()
                .EnableNoRestore();
            
            if (builder != null)
                x = builder(x);

            return x;
        });
    }
}
