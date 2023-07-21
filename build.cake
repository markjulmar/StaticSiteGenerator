#addin nuget:?package=Octokit
#addin nuget:?package=Cake.ArgumentHelpers
#addin nuget:?package=Cake.VersionReader

using System.Linq;
using Octokit;

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var solution = "./XamU.MDPGen.sln";

// NOTE: Make sure outputDir doesn't end in a slash or it won't Clean properly (throws a "ZIP in use" error when creating new ZIP archive).
var outputDir = "./Build";
var outputAssemblyPath = @".\SGLMonitor\bin\Release\SGLMonitor.exe";
var desiredOutputs = new[] {
    outputAssemblyPath,
    @".\SGLMonitor\bin\Release\SGLMonitor.exe.config",
    @".\MDPGen\bin\Release\MDPGen.exe",
    @".\MDPGen\bin\Release\libsass.dll",
    @".\Extensions\XamU.SGL.Extensions\bin\Release\XamU.SGL.Extensions.dll",
    @".\Extensions\XamU.Slide.Extensions\bin\Release\XamU.Slide.Extensions.dll"
};
var outputZip = "sgl-bin.zip";

const string environmentVariablePrefix = "SglTools_";

var gitHubReleasePretendMode = Argument<bool>("GitHubReleasePretend", false);
var gitHubReleaseCreateDraftOnly = ArgumentOrEnvironmentVariable("GitHubReleaseCreateDraftOnly", environmentVariablePrefix, false);
var gitHubReleaseIsBeta = ArgumentOrEnvironmentVariable("GitHubReleaseIsBeta", environmentVariablePrefix, false);
// TODO: Ideally, this Tag version would come from the compiled EXE and/or AssemblyInfo.cs.
var gitHubReleaseTag = Argument("GitHubReleaseTag", EnvironmentVariable(environmentVariablePrefix + "GitHubReleaseTag"));
Func<string, string> gitHubReleaseTitleGenerator = (tag) => {
    return !string.IsNullOrEmpty(tag) ? "SGL Tools: v" + tag : null;
};
// TODO: Get a path instead to see if path overload allows Markdown (string doesn't).
var gitHubReleaseNotes = Argument("GitHubReleaseNotes", EnvironmentVariable(environmentVariablePrefix + "GitHubReleaseNotes")) ?? "";
var gitHubAccessToken = Argument("GitHubAccessToken", EnvironmentVariable(environmentVariablePrefix + "GitHubAccessToken"));
var gitHubOwner = Argument("GitHubOwner", EnvironmentVariable(environmentVariablePrefix + "GitHubOwner"));
var gitHubRepository = Argument("GitHubRepository", EnvironmentVariable(environmentVariablePrefix + "GitHubRepository"));

Task("Default")
.IsDependentOn("ZipBuildOutput");

Task("Clean")
.Does(() => {
    CleanDirectories(outputDir);
    CleanDirectories("./**/bin");
    CleanDirectories("./**/obj");
});

Task("RestoreNuGet")
.Does(() => {
    NuGetRestore(
        solution
    );
});

Task("Build")
.IsDependentOn("Clean")
.IsDependentOn("RestoreNuGet")
.Does(() => {
    MSBuild(
        solution,
        configurator => configurator.SetConfiguration(configuration)
    );
    CreateDirectory(outputDir);
    CopyFiles(desiredOutputs, outputDir);

    // Set up assembly version for GitHub tag name.
    gitHubReleaseTag = GetFullVersionNumber(new FilePath(outputAssemblyPath));
    Information(string.Format("Created SGL Tools with version: {0}", gitHubReleaseTag));
});

Task("ZipBuildOutput")
.IsDependentOn("Build")
.Does(() => {
    var outputZipPath = Directory(outputDir) + File(outputZip);
    Zip(outputDir, outputZipPath);
});

Task("PublishToGitHubRelease")
.IsDependentOn("ReadMe-GitHubReleaseIntegration") // Quick-fail validation (before building)
.IsDependentOn("ZipBuildOutput")
.IsDependentOn("GitHubReleaseInfoDump")
.Does(() => {
    if (!gitHubReleasePretendMode) {
        if (string.IsNullOrEmpty(gitHubReleaseTag)) {
            throw new Exception("To publish a GitHub Release, we must either find the assembly version from the Build task, or you must provide a release tag with the `GitHubReleaseTag` command line parameter (e.g.,  `-ScriptArgs '-GitHubReleaseTag=\"1.0.####.#####\"'`).");
        }

        var draftRelease = gitHubReleaseCreateDraftOnly;
        var preRelease = gitHubReleaseIsBeta;
        var outputZipPath = Directory(outputDir) + File(outputZip);
        var artifactPaths = new[] { outputZipPath.Path };
        var artifactNames = new[] { "sgl-bin.zip" };
        var artifactMimeTypes = new[] { "application/zip" };
        var tag = gitHubReleaseTag;
        var releaseTitle = gitHubReleaseTitleGenerator(gitHubReleaseTag);
        var releaseNotes = gitHubReleaseNotes;

        // Upload sgl-bin.zip to a new GitHub release on xamarin/XamarinUniversity
        try {
            const string gitHubApiBaseUrl = "https://api.github.com";
            var client = new GitHubClient(new ProductHeaderValue("XamU.SglTools"), new Uri(gitHubApiBaseUrl))
            {
                Credentials = new Credentials(gitHubAccessToken)
            };

            var newRelease = new NewRelease(tag)
            {
                Name = releaseTitle,
                Body = releaseNotes,
                Draft = draftRelease,
                Prerelease = preRelease
            };

            int releaseId;
            try
            {
                var result = client.Repository
                    .Release
                    .Create(gitHubOwner, gitHubRepository, newRelease)
                    .Result;

                Information(string.Format("Created GitHub release with Id {0}", result.Id));

                releaseId = result.Id;
            }
            catch (AggregateException exception)
            {
                var innerException = exception.InnerException;
                var innerExceptionMessage = "Unknown error occured while creating release";
                if (innerException != null) {
                    innerExceptionMessage = innerException.Message;
                }
                throw new CakeException(innerExceptionMessage);
            }

            Information("Uploading Artifacts...");
            for (var r = 0; r < artifactPaths.Length; r++)
            {
                var artifactPath = artifactPaths[r];
                var artifactName = artifactNames[r];
                var artifactMimeType = artifactMimeTypes[r];

                using (var archiveContents = System.IO.File.OpenRead(artifactPath.FullPath))
                {
                    var assetUpload = new ReleaseAssetUpload
                    {
                        FileName = artifactName,
                        ContentType = artifactMimeType,
                        RawData = archiveContents
                    };

                    try
                    {
                        var release = client.Repository
                            .Release
                            .Get(gitHubOwner, gitHubRepository, releaseId)
                            .Result;

                        var asset = client.Repository
                        .Release
                        .UploadAsset(release, assetUpload)
                        .Result;

                        Information(string.Format("Uploaded artifact {0} to GitHub. Id {1}", artifactPath.FullPath, asset.Id));
                    }
                    catch (Exception exception)
                    {
                        var innerException = exception.InnerException;
                        var innerExceptionMessage = "Unknown error occured while creating release";
                        if (innerException != null) {
                            innerExceptionMessage = innerException.Message;
                        }
                        throw new CakeException(innerExceptionMessage);
                    }
                }
            }
        }
        catch (Exception ex) {
            Information(ex.ToString());
            if (ex.InnerException != null) {
                Information(ex.InnerException.ToString());
            }
            throw;
        }
    }
    else {
        Information("Pretend mode active! Not really submitting to GitHub.");
    }
});

Task("GitHubReleaseInfoDump")
.Does(() => {
    if (gitHubReleasePretendMode) {
        Information("***GitHub Release pretend mode activated!!!***");
    }
    var maskedToken = new string(gitHubAccessToken.Take(5).Concat(new string('*', gitHubAccessToken.Length - 5)).ToArray());
    Information(string.Format("GitHub token: {0}", maskedToken));
    Information(string.Format("Publishing GitHub Release to {0}/{1} with the following info:", gitHubOwner, gitHubRepository));
    Information(string.Format("\tTag: {0}", gitHubReleaseTag));
    Information(string.Format("\tDraft?: {0}", gitHubReleaseCreateDraftOnly));
    Information(string.Format("\tPrerelease?: {0}", gitHubReleaseIsBeta));
    Information(string.Format("\tTitle: {0}", gitHubReleaseTitleGenerator(gitHubReleaseTag)));
    Information(string.Format("\tNotes: {0}", gitHubReleaseNotes));
});

Task("ReadMe-GitHubReleaseIntegration")
.Does(() => {
    // Validate GitHub inputs (env or command line) required to publish a release.
    if (string.IsNullOrEmpty(gitHubAccessToken)
        || string.IsNullOrEmpty(gitHubOwner)
        || string.IsNullOrEmpty(gitHubRepository)) {
        // TODO: Fail with precise error message when something isn't right (exact variable and how to set it).
        throw new Exception("To publish a GitHub Release, you must provide the access token and GitHub owner/repository names (either environment variables or command-line options).");
    }
    // NOTE: The gitHubReleaseTitle is currently dervied from the gitHubReleaseTag, so not explicitly verified here.
    if (string.IsNullOrEmpty(gitHubReleaseNotes)) {
        throw new Exception("To publish a GitHub Release, you must provide a release notes with the `GitHubReleaseNotes` command line parameter (e.g.,  `-ScriptArgs '-GitHubReleaseNotes=\"Fixed some cool stuff!\"'`).");
    }
});

RunTarget(target);