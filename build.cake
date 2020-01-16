#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
#addin nuget:?package=Cake.Figlet
#addin nuget:?package=Cake.VersionReader
#addin nuget:?package=Nuget.Core

#tool "nuget:?package=gitlink&version=2.4.0"
#tool nuget:?package=GitVersion.CommandLine

using NuGet;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDir = Directory("./src/TestClassLibrary/bin") + Directory(configuration);
var nugetDir = Directory("./artifacts");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Information")
    .Does(() => 
    {
        Information(Figlet("TEST"));
    });

Task("Clean")
    .IsDependentOn("Information")
    .Does(() =>
{
    CleanDirectory(buildDir);
    CleanDirectory(nugetDir);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("./src/TestClassLibrary.sln");
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    Information("Building solution");

    if(IsRunningOnWindows())
    {

        var solution = System.IO.Path.Combine("./src/", "TestClassLibrary.sln");
        // Use MSBuild
        MSBuild(solution, settings => settings.SetConfiguration("Debug"));
        MSBuild(solution, settings => settings.SetConfiguration("Release"));
        
        SourceLink(solution);

    //   GitLink("./src/", new GitLinkSettings {
    //         RepositoryUrl = "http://git.local.graw.com",
    //         Branch        = "master",
    //         SolutionFileName = "TestClassLibrary.sln"
    //     });
    }
    else
    {
      // Use XBuild
      XBuild("./src/TestClassLibrary.sln", settings =>
        settings.SetConfiguration(configuration));
    }
});

// Action<string> SourceLink = (solutionFileName) =>
// {
//     GitLink3(GetFiles("./**/*.pdb"), new GitLink3Settings {
//         RepositoryUrl = "http://git.local.graw.com/wstefaniuk/testnugetproject",
//         BaseDir = "./"
//     });

//     // GitLink("./", new GitLinkSettings() {
//     //     RepositoryUrl = "http://git.local.graw.com/wstefaniuk/testnugetproject",
//     //     SolutionFileName = solutionFileName,
//     //     ErrorsAsWarnings = false,
//     // });
// };

Action<string> SourceLink = (solutionFileName) =>
{
    try 
    {
        GitLink("./", new GitLinkSettings() {
            RepositoryUrl = "https://github.com/wojtst/NugetTestProject",
            SolutionFileName = solutionFileName,
        });
    }
    catch (Exception ex)
    {
        Warning("GitLink failed.");
    }
};

Task("Nuget")
    .IsDependentOn("Build")
    .Does(() =>
{
    var nuGetPackSettings = new NuGetPackSettings
    {
        OutputDirectory = "./artifacts",
        IncludeReferencedProjects = true,
        Symbols = false,
        Verbosity = NuGetVerbosity.Detailed,

        Properties = new Dictionary<string, string>
        {
            { "Configuration", "Release" }
        }
    };

    NuGetPack("./src/TestClassLibrary/TestClassLibrary.csproj", nuGetPackSettings);

    var settings = new NuGetPushSettings 
        { 
            Source ="http://192.168.1.101/nugetGallery/api/v2/package",
            ApiKey = "a0fd834b-a324-44a5-903c-acf6db68d6af"       
        };

    var packages = GetFiles("./artifacts/*.nupkg");

    foreach(var package in packages)
    {

            Information($"Publishing \"{package}\".");
            //CopyFile(package, "D:\\LocalNugetRepository\\" + package.GetFilename());
            NuGetPush(package, settings); 
    } 
});
// Task("Run-Unit-Tests")
//     .IsDependentOn("Build")
//     .Does(() =>
// {
//     NUnit3("./src/**/bin/" + configuration + "/*.Tests.dll", new NUnit3Settings {
//         NoResults = true
//         });
// });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);


//////////////////////////////////////////////////////
//                      HELPERS                     //
//////////////////////////////////////////////////////

private bool IsNuGetPublished(FilePath packagePath) 
{
    var package = new ZipPackage(packagePath.FullPath);

    var latestPublishedVersions = NuGetList(
        package.Id,
        new NuGetListSettings 
        {
            Prerelease = false
        }
    );

    try
    {
        return latestPublishedVersions.Any(p => package.Version.Equals(new SemanticVersion(p.Version)));
    }
    catch (System.Exception)
    {
        return false;
    }
}