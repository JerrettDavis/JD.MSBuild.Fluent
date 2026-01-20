using System.Diagnostics;
using System.IO.Compression;
using FluentAssertions;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace JD.MSBuild.Fluent.Tests;

public sealed class PackagingValidationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public async Task PackOutputsIncludeReadmeAndLicense()
    {
        await Given("repository root and output directory", CreatePackagingContext)
            .Finally("cleanup output directory", CleanupOutputDirectory)
            .When("packing solution", PackSolution)
            .Then("packages are created", VerifyPackagesCreated)
            .And("library package has metadata", ctx => VerifyLibraryPackageMetadata(ctx.packages))
            .And("CLI package has metadata", ctx => VerifyCliPackageMetadata(ctx.packages))
            .AssertPassed();
    }

    #region Helper Methods - Given

    private static (string repoRoot, string outputDir) CreatePackagingContext()
    {
        var repoRoot = FindRepoRoot();
        var outputDir = Path.Combine(Path.GetTempPath(), "JD.MSBuild.Fluent.Tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(outputDir);
        return (repoRoot, outputDir);
    }

    #endregion

    #region Helper Methods - When

    private static (string repoRoot, string outputDir, string[] packages) PackSolution(
        (string repoRoot, string outputDir) ctx)
    {
        RunDotnet($"pack \"{Path.Combine(ctx.repoRoot, "JD.MSBuild.Fluent.sln")}\" -c Release -o \"{ctx.outputDir}\"");
        var packages = Directory.GetFiles(ctx.outputDir, "*.nupkg");
        return (ctx.repoRoot, ctx.outputDir, packages);
    }

    #endregion

    #region Helper Methods - Then

    private static bool VerifyPackagesCreated((string repoRoot, string outputDir, string[] packages) ctx)
    {
        ctx.packages.Should().NotBeEmpty("packages should be created");
        return true;
    }

    private static bool VerifyLibraryPackageMetadata(string[] packages)
    {
        var lib = packages.Single(p =>
            Path.GetFileName(p).StartsWith("JD.MSBuild.Fluent.", StringComparison.Ordinal) &&
            !Path.GetFileName(p).StartsWith("JD.MSBuild.Fluent.Cli.", StringComparison.Ordinal));
        
        AssertPackageHasMetadata(lib, expectXml: true);
        return true;
    }

    private static bool VerifyCliPackageMetadata(string[] packages)
    {
        var cli = packages.Single(p => 
            Path.GetFileName(p).StartsWith("JD.MSBuild.Fluent.Cli.", StringComparison.Ordinal));
        
        AssertPackageHasMetadata(cli, expectXml: true);
        return true;
    }

    #endregion

    #region Helper Methods - Finally

    private static void CleanupOutputDirectory((string repoRoot, string outputDir) ctx)
    {
        try
        {
            if (Directory.Exists(ctx.outputDir))
                Directory.Delete(ctx.outputDir, recursive: true);
        }
        catch
        {
            // Ignore cleanup failures
        }
    }

    #endregion

    #region Private Helper Methods

    private static void AssertPackageHasMetadata(string nupkgPath, bool expectXml)
    {
        using var archive = ZipFile.OpenRead(nupkgPath);
        archive.Entries.Should().Contain(e => e.FullName.Equals("README.md", StringComparison.OrdinalIgnoreCase));
        archive.Entries.Should().Contain(e => e.FullName.Equals("LICENSE", StringComparison.OrdinalIgnoreCase));

        var nuspec = archive.Entries.SingleOrDefault(e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        nuspec.Should().NotBeNull();
        using var nuspecStream = nuspec!.Open();
        using var reader = new StreamReader(nuspecStream);
        var nuspecContent = reader.ReadToEnd();
        nuspecContent.Should().Contain("<readme>README.md</readme>");

        if (expectXml)
            archive.Entries.Should().Contain(e => e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
    }

    private static void RunDotnet(string args)
    {
        var psi = new ProcessStartInfo("dotnet", args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process is null)
            throw new InvalidOperationException("Failed to start dotnet process.");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"dotnet {args} failed: {output}\n{error}");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var sln = Path.Combine(dir.FullName, "JD.MSBuild.Fluent.sln");
            if (File.Exists(sln))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found.");
    }

    #endregion
}
