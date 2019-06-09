using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
using static Nuke.Common.Tools.NuGet.NuGetTasks;
using Nuke.Common.Tools.NUnit;
using static Nuke.Common.Tools.NUnit.NUnitTasks;
using System.IO;
using Nuke.Common.Tools.OpenCover;
using static Nuke.Common.Tools.OpenCover.OpenCoverTasks;
using Nuke.Common.Tools.MSBuild;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
	/// Support plugins are available for:
	///   - JetBrains ReSharper        https://nuke.build/resharper
	///   - JetBrains Rider            https://nuke.build/rider
	///   - Microsoft VisualStudio     https://nuke.build/visualstudio
	///   - Microsoft VSCode           https://nuke.build/vscode

	public static int Main() => Execute<Build>(x => x.PublishGitHubRelease);

	[Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
	readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

	[Solution] readonly Solution Solution;
	[GitRepository] readonly GitRepository GitRepository;
	[GitVersion] readonly GitVersion GitVersion;

	AbsolutePath OutputDirectory => RootDirectory / "output";
	AbsolutePath SolutionDirectory => RootDirectory;
	AbsolutePath WebApplicationDirectory => RootDirectory / "WebApplication";

	Target Clean => _ => _
		.Before(Restore)
		.Executes(() =>
		{
			EnsureCleanDirectory(OutputDirectory);
		});

	Target Restore => _ => _
		.Executes(() =>
		{
			DotNetRestore(s => s
				.SetProjectFile(Solution));
			NuGetRestore(s => s.SetWorkingDirectory(SolutionDirectory));
		});

	Target Compile => _ => _
		.DependsOn(Restore)
		.Executes(() =>
		{
			MSBuild(s => s
			.SetWorkingDirectory(SolutionDirectory)
			.SetVerbosity(MSBuildVerbosity.Quiet)
			);
			//DotNetBuild(s => s
			//  .SetProjectFile(Solution)
			//.SetConfiguration(Configuration)
			//.SetAssemblyVersion(GitVersion.GetNormalizedAssemblyVersion())
			//.SetFileVersion(GitVersion.GetNormalizedFileVersion())
			//.SetInformationalVersion(GitVersion.InformationalVersion)
			//.EnableNoRestore());
		});

	Target TestAndCoverage => _ => _
	.DependsOn(Compile)
	.Executes(() =>
	{
		var assemblies = GlobFiles(SolutionDirectory, $"*/bin/{Configuration}/net4*/ScrewTurn.Wiki.*.Tests.dll").NotEmpty();
		var nunitSettings = new NUnit3Settings()
			.AddInputFiles(assemblies)
			.AddResults(OutputDirectory / "tests.xml");

		if(EnvironmentInfo.IsWin)
		{
			var searchDirectories = nunitSettings.InputFiles.Select(x => Path.GetDirectoryName(x));

			OpenCoverTasks.OpenCover(s => s
				.SetOutput(OutputDirectory / "coverage.xml")
				.SetTargetSettings(nunitSettings)
				.SetSearchDirectories(searchDirectories)
				.SetWorkingDirectory(RootDirectory)
				.SetRegistration(RegistrationType.User)
				.SetTargetExitCodeOffset(targetExitCodeOffset: 0)
				.SetFilters(
					"+[*]*",
					"-[xunit.*]*",
					"-[FluentAssertions.*]*")
				.SetExcludeByAttributes(
					"*.Explicit*",
					"*.Ignore*",
					"System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute")
				.SetExcludeByFile(
					"*/*.Generated.cs",
					"*/*.Designer.cs",
					"*/*.g.cs",
					"*/*.g.i.cs"));

			// Or using static imports and default settings:
			/*OpenCover(s => s
				.SetOutput(OutputDirectory / "coverage.xml")
				.SetTargetSettings(nunitSettings)
				.SetSearchDirectories(searchDirectories));*/
		}
		else
		{
			NUnit3(s => nunitSettings);
		}
	});

	Target PublishGitHubRelease => _ => _
	.DependsOn(Compile)
	.OnlyWhenStatic(() => GitVersion.BranchName.Equals("master") || GitVersion.BranchName.Equals("origin/master"))
	.Executes(() =>
	{
		MSBuildSettings buildSettings = new MSBuildSettings()
		.SetWorkingDirectory(WebApplicationDirectory)
		.SetProjectFile("WebApplication.csproj")
		.SetOutDir("C:\\publish\\ScrewTurnWiki\\WebApplication")
		.AddProperty("PublishProfile", "FolderProfile")
		.SetVerbosity(MSBuildVerbosity.Normal);
		MSBuild(buildSettings);

		string PluginsDir = @"C:\Publish\ScrewTurnWiki\WebApplication\_PublishedWebsites\Plugins\";
		if(!Directory.Exists(PluginsDir))
		{
			Directory.CreateDirectory(PluginsDir);
		}

		File.Copy(SolutionDirectory / @"DownloadCounterPlugin\bin\Debug\net472\DownloadCounterPlugin.dll", PluginsDir + "DownloadCounterPlugin.dll", true);
		File.Copy(SolutionDirectory / @"FootnotesPlugin\bin\Debug\net472\FootnotesPlugin.dll", PluginsDir + "FootnotesPlugin.dll", true);
		File.Copy(SolutionDirectory / @"MultilanguageContentPlugin\bin\Debug\net472\MultilanguageContentPlugin.dll", PluginsDir + "MultilanguageContentPlugin.dll", true);
		File.Copy(SolutionDirectory / @"RatingManagerPlugin\bin\Debug\net472\RatingManagerPlugin.dll", PluginsDir + "RatingManagerPlugin.dll", true);
		File.Copy(SolutionDirectory / @"RssFeedDisplayPlugin\bin\Debug\net472\RssFeedDisplayPlugin.dll", PluginsDir + "RssFeedDisplayPlugin.dll", true);
		File.Copy(SolutionDirectory / @"ActiveDirectoryProvider\bin\Debug\net472\ActiveDirectoryProvider.dll", PluginsDir + "ActiveDirectoryProvider.dll", true);
	});
}
