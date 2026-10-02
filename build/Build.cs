#nullable enable
using System;
using System.Linq;
using System.IO;
using Extensions;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.Git;
using Fallout.Common.Tools.GitHub;
using Fallout.Common.Tools.NuGet;
using Serilog;
using Project = Fallout.Common.ProjectModel.Project;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

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
        WritePermissions = [GitHubActionsPermissions.Contents, GitHubActionsPermissions.IdToken],
        Submodules = GitHubActionsSubmodules.Recursive,
        CacheIncludePatterns = ["~/.nuget/packages"],
        CacheKeyFiles = ["**/global.json", "**/*.csproj", "**/Directory.Packages.props", "**/packages.lock.json"],
        Lfs = true,
        OnPushTags = ["v*"]),
    GitHubActions("Nuget Release", 
        GitHubActionsImage.WindowsLatest, 
        InvokedTargets = [nameof(PublishNuget)],
        WritePermissions = [GitHubActionsPermissions.IdToken],
        Submodules = GitHubActionsSubmodules.Recursive,
        CacheIncludePatterns = ["~/.nuget/packages"],
        CacheKeyFiles = ["**/global.json", "**/*.csproj", "**/Directory.Packages.props", "**/packages.lock.json"],
        Lfs = true,
        OnWorkflowDispatchRequiredInputs = ["Version"]),
    GitHubActions("Manual Release", 
        GitHubActionsImage.WindowsLatest, 
        InvokedTargets = [nameof(TaggedRelease)],
        EnableGitHubToken = true,
        PublishArtifacts = true,
        WritePermissions = [GitHubActionsPermissions.Contents, GitHubActionsPermissions.IdToken],
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

    private string GetVersionTag() => string.IsNullOrWhiteSpace(Version) 
        ? Repository.Tags?.FirstOrDefault(_versionPredicate) ?? (GitRepository.GetTag(_versionPredicate) is var tag && string.IsNullOrWhiteSpace(tag) 
            ? "1.0.0" 
            : tag) 
        : Version; 
    
    private static readonly Func<string, bool> _versionPredicate = s => s.StartsWith('v') || s.StartsWith('p');
    private static string StripPrefixes(string? str) => str?.TrimStart('v').TrimStart('p') ?? "";
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
        .Executes(() => DotNetRestore(s => s.SetProjectFile(Solution)
            .SetVerbosity(DotNetVerbosity.detailed)));

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
                .EnableNoRestore());
        });

    Target PackCommon => _ => _
        .DependsOn(Test)
        .Produces(PackagesDirectory / "Dawn.Common*.nupkg")
        .Executes(() => PackProject(Solution.Dawn_Common));

    Target PackCommonWindows => _ => _
        .DependsOn(Test)
        .Produces(PackagesDirectory / "Dawn.Common.Windows*.nupkg")
        .Executes(() => PackProject(Solution.Windows.Dawn_Common_Windows));

    Target PackCommonWindowsDesktop => _ => _
        .DependsOn(Test)
        .Produces(PackagesDirectory / "Dawn.Common.Windows.Desktop*.nupkg")
        .Executes(() => PackProject(Solution.Windows.Dawn_Common_Windows_Desktop));

    Target PackAll => _ => _
        .DependsOn(PackCommon, PackCommonWindows, PackCommonWindowsDesktop);

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
        .DependsOn(UpdateChangelog)
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

    Target PublishGithub => _ => _
        .DependsOn(PackAll)
        .Unlisted()
        .Executes(() =>
        {
            // Push to GitHub Packages
            (PackagesDirectory / "*symbols.nupkg").GlobFiles().ForEach(pkg =>
            {
                DotNetNuGetPush(options => options
                    .SetTargetPath(pkg)
                    .SetSource($"https://nuget.pkg.github.com/{Repository.GetGitHubOwner()}/index.json")
                    .SetApiKey(Actions.Token));
            });
        });
    
    Target PublishNuget => _ => _
        .DependsOn(PackAll)
        .OnlyWhenStatic(() => IsServerBuild)
        .Executes(async () =>
        {
            var nugetApiKey = await OIDC.Create("arion");
            if (nugetApiKey.IsNullOrEmpty())
            {
                Assert.Fail("OIDC Nuget API Key not set");
                return;
            }
            (PackagesDirectory / "*symbols.nupkg").GlobFiles().ForEach(pkg =>
            {
                DotNetNuGetPush(options => options
                    .SetTargetPath(pkg)
                    .SetSource("https://api.nuget.org/v3/index.json")
                    .SetApiKey(nugetApiKey));
            });
        });

    Target Publish => _ => _
        .DependsOn(PackAll)
        .OnlyWhenStatic(() => IsServerBuild)
        .DependsOn(PublishGithub, PublishNuget);

    // Tagged release that runs on v* tags
    Target TaggedRelease => _ => _
        .DependsOn(Publish, PushChangelog)
        .Unlisted()
        .OnlyWhenStatic(() => IsServerBuild);

    void BuildProject(Project project)
    {
        DotNetBuild(s => s
            .SetProjectFile(project)
            .SetConfiguration(Configuration)
            .EnableNoRestore());
    }

    void PackProject(Project project, Func<DotNetPackSettings, DotNetPackSettings>? builder = null)
    {
        var version = GetVersionTag().TrimStart('v').TrimStart('p');
        if (string.IsNullOrWhiteSpace(version))
            throw new Exception($"Unable to resolve package version for '{project.Name}'. Supply --version or publish a p*/v* tag.");

        DotNetPack(s =>
        {
            var x = s.SetProject(project)
                .SetConfiguration(Configuration)
                .SetVersion(version)
                .SetRepositoryUrl(Repository.HttpsUrl)
                .SetRepositoryType("git")
                .AddProperty("PackageLicenseExpression", "GPL-2.0-only")
                .SetIncludeSource(true)
                .SetIncludeSymbols(true)
                .SetPackageProjectUrl(Repository.HttpsUrl)
                .SetAuthors("arion")
                .SetOutputDirectory(PackagesDirectory)
                .EnableNoBuild()
                .EnableNoRestore();
            
            if (builder != null)
                x = builder(x);

            return x;
        });
    }
}
