using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
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
    FetchDepth = 0)]
[GitHubActions(
    "publish",
    GitHubActionsImage.UbuntuLatest,
    OnPushTags = new[] { "v*" },
    InvokedTargets = new[] { nameof(Publish) },
    ImportSecrets = new[] { "NUGET_API_KEY" },
    FetchDepth = 0)]
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

    [GitVersion] readonly GitVersion GitVersion;

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

    Target Compile => _ => _
        .DependsOn(Restore)
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
            Serilog.Log.Information("Packaging version {Version}", GitVersion.NuGetVersionV2);
            DotNetTasks.DotNetPack(s => s
                .SetProject(Solution.GetProject("DotnetWebapiStencil"))
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory)
                .EnableNoBuild()
                .DisableTreatWarningsAsErrors()
                .SetVersion(GitVersion.NuGetVersionV2)
            );

            Serilog.Log.Information("Packaging version {Version}", GitVersion.NuGetVersionV2);
            DotNetTasks.DotNetPack(s => s
                .SetProject(Solution.GetProject("StencilMiddleware"))
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory)
                .EnableNoBuild()
                .DisableTreatWarningsAsErrors()
                .SetVersion(GitVersion.NuGetVersionV2)
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
