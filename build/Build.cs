using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    OnPushBranches = new[] { "main", "develop", "release/*" },
    OnPullRequestBranches = new[] { "main", "develop", "release/*" },
    InvokedTargets = new[] { nameof(CI) },
    FetchDepth = 0,
    PublishArtifacts = true,
    CacheKeyFiles = new string[] { })]
[GitHubActions(
    "publish",
    GitHubActionsImage.UbuntuLatest,
    OnPushBranches = new[] { "main", "release/*" },
    InvokedTargets = new[] { nameof(Publish) },
    ImportSecrets = new[] { "NUGET_API_KEY" },
    FetchDepth = 0,
    PublishArtifacts = true,
    CacheKeyFiles = new string[] { })]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Pack);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] GitRepository GitRepository;
    [GitVersion(NoFetch = false)] readonly GitVersion GitVersion;

    [Parameter("NuGet API Key")]
    [Secret]
    readonly string NuGetApiKey;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(Solution)
            );
        });

    Target LogInfo => _ => _
        .Executes(() =>
        {
            var versionInfo = GitVersion.ToJson(formatting: Newtonsoft.Json.Formatting.Indented);
            Serilog.Log.Information("Version Info: {VersionInfo}", versionInfo);

            var repositoryInfo = GitRepository.ToJson(formatting: Newtonsoft.Json.Formatting.Indented);
            Serilog.Log.Information("Repository Info: {RepositoryInfo}", repositoryInfo);
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .DependsOn(LogInfo)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetVersion(GitVersion.FullSemVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
            );
        });

    Target Pack => _ => _
        .DependsOn(Clean)
        .DependsOn(Compile)
        .Produces(ArtifactsDirectory / "*.nupkg")
        .Executes(() =>
        {
            Serilog.Log.Information("Packaging version {Version}", GitVersion.SemVer);
            DotNetTasks.DotNetPack(s => s
                .SetProject(Solution.GetProject("dotnet.template.pack"))
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory)
                .EnableNoBuild()
                .DisableTreatWarningsAsErrors()
                .SetVersion(GitVersion.SemVer)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetProperty("PackageVersion", GitVersion.SemVer)
            );

            Serilog.Log.Information("Packaging version {Version}", GitVersion.SemVer);
            DotNetTasks.DotNetPack(s => s
                .SetProject(Solution.GetProject("StencilMiddleware"))
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory)
                .EnableNoBuild()
                .DisableTreatWarningsAsErrors()
                .SetVersion(GitVersion.SemVer)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetProperty("PackageVersion", GitVersion.SemVer)
            );
        });

    Target CI => _ => _
        .DependsOn(Clean)
        .DependsOn(Pack);

    Target Publish => _ => _
        .DependsOn(CI)
        .Executes(() =>
        {
            DotNetTasks.DotNetNuGetPush(s => s
                 .SetTargetPath(ArtifactsDirectory / "*.nupkg")
                 .SetSource("https://api.nuget.org/v3/index.json")
                 .SetApiKey(NuGetApiKey)
             );
        });
}
