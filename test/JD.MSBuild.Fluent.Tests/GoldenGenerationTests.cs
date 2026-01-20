using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.Packaging;
using JD.MSBuild.Fluent.Tests.Tasks;
using JD.MSBuild.Fluent.Tests.Tasks.Extensions.WriteLinesToFile;
using JD.MSBuild.Fluent.Typed;
using TinyBDD.Xunit;
using Xunit.Abstractions;
using static JD.MSBuild.Fluent.Typed.MsBuildExpr;

namespace JD.MSBuild.Fluent.Tests;

public sealed class GoldenGenerationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public async Task EmitsExpectedBuildAssets()
    {
        await Given("package definition with all build assets", CreatePackageWithAllAssets)
            .When("emitting package", EmitPackageToDirectory)
            .Finally("cleanup output directory", CleanupDirectory)
            .Then("build props file matches expected", ctx => VerifyFileMatches(ctx.dir, "build", "Contoso.Build.props"))
            .And("build targets file matches expected", ctx => VerifyFileMatches(ctx.dir, "build", "Contoso.Build.targets"))
            .And("buildTransitive props file matches expected", ctx => VerifyFileMatches(ctx.dir, "buildTransitive", "Contoso.Build.props"))
            .And("buildTransitive targets file matches expected", ctx => VerifyFileMatches(ctx.dir, "buildTransitive", "Contoso.Build.targets"))
            .And("SDK props file matches expected", ctx => VerifySdkFile(ctx.dir, "Contoso.Build", "Sdk.props", "Contoso.Build.props"))
            .And("SDK targets file matches expected", ctx => VerifySdkFile(ctx.dir, "Contoso.Build", "Sdk.targets", "Contoso.Build.targets"))
            .AssertPassed();
    }

    [Fact]
    public async Task RenderingIsDeterministic()
    {
        await Given("package definition", CreateDeterminismPackage)
            .When("emitting twice to different directories", EmitToTwoDirectories)
            .Finally("cleanup output directories", CleanupBothDirectories)
            .Then("build props files are identical", VerifyPropsIdentical)
            .And("build targets files are identical", VerifyTargetsIdentical)
            .AssertPassed();
    }

    #region Helper Methods - Given

    private static PackageDefinition CreatePackageWithAllAssets() =>
        Package.Define("Contoso.Build")
            .Props(p => p
                .Property<ContosoEnabled>("true")
                .ItemGroup(null, ig => ig
                    .Include<MsBuildItemTypes.None>("README.md", i => i.Meta<PackMetadata>("true"))))
            .Targets(t => t
                .Target<ContosoHelloTarget>(tgt => tgt
                    .BeforeTargets(new MsBuildTargets.Build())
                    .Condition(IsTrue<ContosoEnabled>())
                    .Message("Hello")
                    .Exec("dotnet --version")))
            .Pack(o => { o.BuildTransitive = true; o.EmitSdk = true; })
            .Build();

    private static PackageDefinition CreateDeterminismPackage() =>
        Package.Define("Determinism")
            .Props(p => p
                .Property<PropertyB>("2")
                .Property<PropertyA>("1")
                .Item<MsBuildItemTypes.None>(IR.MsBuildItemOperation.Include, "b.txt", i => i.Meta<MetaZ>("z").Meta<MetaA>("a"))
                .Item<MsBuildItemTypes.None>(IR.MsBuildItemOperation.Include, "a.txt", i => i.Meta<MetaB>("b")))
            .Targets(t => t
                .Target<TargetT>(tgt => tgt
                    .Task(new WriteLinesToFileMsBuild.TaskName(), task => task
                        .Lines("x")
                        .File("y"))
                    .Message("m")
                    .Exec("echo hi")))
            .Build();

    #endregion

    #region Helper Methods - When

    private static (PackageDefinition def, string dir) EmitPackageToDirectory(PackageDefinition def)
    {
        var dir = Path.Combine(Path.GetTempPath(), "JD.MSBuild.Fluent.Tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(dir);
        new MsBuildPackageEmitter().Emit(def, dir);
        return (def, dir);
    }

    private static (PackageDefinition def, string dir1, string dir2) EmitToTwoDirectories(PackageDefinition def)
    {
        var dir1 = Path.Combine(Path.GetTempPath(), "JD.MSBuild.Fluent.Tests", Guid.NewGuid().ToString("n"));
        var dir2 = Path.Combine(Path.GetTempPath(), "JD.MSBuild.Fluent.Tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(dir1);
        Directory.CreateDirectory(dir2);
        new MsBuildPackageEmitter().Emit(def, dir1);
        new MsBuildPackageEmitter().Emit(def, dir2);
        return (def, dir1, dir2);
    }

    #endregion

    #region Helper Methods - Then

    private static bool VerifyFileMatches(string dir, string folder, string fileName)
    {
        var actual = Normalize(File.ReadAllText(Path.Combine(dir, folder, fileName)));
        var expected = Normalize(ReadExpected(fileName));
        Assert.Equal(expected, actual);
        return true;
    }

    private static bool VerifySdkFile(string dir, string sdkName, string fileName, string expectedFileName)
    {
        var actual = Normalize(File.ReadAllText(Path.Combine(dir, "Sdk", sdkName, fileName)));
        var expected = Normalize(ReadExpected(expectedFileName));
        Assert.Equal(expected, actual);
        return true;
    }

    private static bool VerifyPropsIdentical((PackageDefinition def, string dir1, string dir2) ctx)
    {
        var props1 = Normalize(File.ReadAllText(Path.Combine(ctx.dir1, "build", "Determinism.props")));
        var props2 = Normalize(File.ReadAllText(Path.Combine(ctx.dir2, "build", "Determinism.props")));
        Assert.Equal(props1, props2);
        return true;
    }

    private static bool VerifyTargetsIdentical((PackageDefinition def, string dir1, string dir2) ctx)
    {
        var targets1 = Normalize(File.ReadAllText(Path.Combine(ctx.dir1, "build", "Determinism.targets")));
        var targets2 = Normalize(File.ReadAllText(Path.Combine(ctx.dir2, "build", "Determinism.targets")));
        Assert.Equal(targets1, targets2);
        return true;
    }

    #endregion

    #region Helper Methods - Finally

    private static void CleanupDirectory((PackageDefinition def, string dir) ctx)
    {
        try
        {
            if (Directory.Exists(ctx.dir))
                Directory.Delete(ctx.dir, recursive: true);
        }
        catch
        {
            // Ignore cleanup failures
        }
    }

    private static void CleanupBothDirectories((PackageDefinition def, string dir1, string dir2) ctx)
    {
        try
        {
            if (Directory.Exists(ctx.dir1))
                Directory.Delete(ctx.dir1, recursive: true);
        }
        catch
        {
            // Ignore cleanup failures
        }
        try
        {
            if (Directory.Exists(ctx.dir2))
                Directory.Delete(ctx.dir2, recursive: true);
        }
        catch
        {
            // Ignore cleanup failures
        }
    }

    #endregion

    #region Private Helper Methods

    private static string ReadExpected(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", "Expected", name));

    private static string Normalize(string s) =>
        s.Replace("\r\n", "\n");

    #endregion

    #region Type Definitions

    private readonly struct ContosoEnabled : IMsBuildPropertyName
    {
        public string Name => "ContosoEnabled";
    }

    private readonly struct ContosoHelloTarget : IMsBuildTargetName
    {
        public string Name => "Contoso_Hello";
    }

    private readonly struct PackMetadata : IMsBuildMetadataName
    {
        public string Name => "Pack";
    }

    private readonly struct PropertyA : IMsBuildPropertyName
    {
        public string Name => "A";
    }

    private readonly struct PropertyB : IMsBuildPropertyName
    {
        public string Name => "B";
    }

    private readonly struct TargetT : IMsBuildTargetName
    {
        public string Name => "T";
    }

    private readonly struct MetaA : IMsBuildMetadataName
    {
        public string Name => "A";
    }

    private readonly struct MetaB : IMsBuildMetadataName
    {
        public string Name => "B";
    }

    private readonly struct MetaZ : IMsBuildMetadataName
    {
        public string Name => "Z";
    }

    #endregion
}
