using System.Diagnostics;
using System.IO.Compression;
using FluentAssertions;

namespace JD.MSBuild.Fluent.Tests;

public sealed class PackagingValidationTests
{
  [Fact]
  public void Pack_outputs_include_readme_and_license()
  {
    var repoRoot = FindRepoRoot();
    var outputDir = Path.Combine(Path.GetTempPath(), "JD.MSBuild.Fluent.Tests", Guid.NewGuid().ToString("n"));
    Directory.CreateDirectory(outputDir);

    try
    {
      RunDotnet($"pack \"{Path.Combine(repoRoot, "JD.MSBuild.Fluent.sln")}\" -c Release -o \"{outputDir}\"");

      var packages = Directory.GetFiles(outputDir, "*.nupkg");
      packages.Should().NotBeEmpty();

      var lib = packages.Single(p =>
        Path.GetFileName(p).StartsWith("JD.MSBuild.Fluent.", StringComparison.Ordinal) &&
        !Path.GetFileName(p).StartsWith("JD.MSBuild.Fluent.Cli.", StringComparison.Ordinal));
      var cli = packages.Single(p => Path.GetFileName(p).StartsWith("JD.MSBuild.Fluent.Cli.", StringComparison.Ordinal));

      AssertPackageHasMetadata(lib, expectXml: true);
      AssertPackageHasMetadata(cli, expectXml: true);
    }
    finally
    {
      try { Directory.Delete(outputDir, recursive: true); } catch { }
    }
  }

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
}
