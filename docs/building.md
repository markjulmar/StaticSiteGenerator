# How to build the tools

This will walk you through building the tools on your local machine. If you wish to have the tools built for release, submit a pull request to the master branch and get it merged. A continuous integration system will fire from Azure DevOps to build a new release and publish the resulting artifact to the [releases on GitHub](https://github.com/xamarinhq/XamU-StaticContentGenerator/releases).

## Prerequisites
- Visual Studio 2017
- Powershell (for command-line builds)

## Create ZIP archive

A Cake build script is provided for generating the ZIP distribution to the curriculum authors.

To simply make a ZIP archive of the SGL tools, just run the following PowerShell command.

> `./build.ps1`

## Create a GitHub Release

To publish an official [SGL Tools GitHub release](github.com/xamarin/XamarinUniversity/releases), adjust and execute the following commands in PowerShell to set up the prerequisite variables. This will set session-level Environment variables that last until you close your PowerShell session.

```bash
$env:SglTools_GitHubAccessToken = "{put-github-auth-token-here}"; # see: https://github.com/settings/tokens
$env:SglTools_GitHubOwner = "xamarinhq";
$env:SglTools_GitHubRepository = "XamU-StaticContentGenerator";
$env:SglTools_GitHubReleaseTag = "1.0.####.#####"; # get this from title of running SGL Previewer
$env:SglTools_GitHubReleaseNotes = "Release notes (no Markdown supported yet, but it can be edited later)";
```

Alternatively, you can permanently set User-level Environment variables. You will need to exit and re-launch PowerShell for permanently-set variables to take effect.

> `[Environment]::SetEnvironmentVariable("TestVariable", "Test value.", "User")`

You can also set variables via the Cake script command line. Variables are typically the same as the Environment variable names, but without the `SglTools_` prefix.

> `./build.ps1 ... -ScriptArgs '-GitHubReleasePretend=true'`

Once you have the variables set up, run the proper build target on the Cake build script.

> `./build.ps1 -target PublishToGitHubRelease`
