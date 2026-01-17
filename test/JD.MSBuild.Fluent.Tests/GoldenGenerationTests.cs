using FluentAssertions;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.Packaging;
using JD.MSBuild.Fluent.Tests.Tasks;
using JD.MSBuild.Fluent.Tests.Tasks.Extensions.WriteLinesToFile;
using JD.MSBuild.Fluent.Typed;
using static JD.MSBuild.Fluent.Typed.MsBuildExpr;

namespace JD.MSBuild.Fluent.Tests;

public sealed class GoldenGenerationTests
{
  [Fact]
  public void Emits_expected_build_assets()
  {
    var def = Package.Define("Contoso.Build")
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

    var dir = Path.Combine(Path.GetTempPath(), "JD.MSBuild.Fluent.Tests", Guid.NewGuid().ToString("n"));
    Directory.CreateDirectory(dir);

    try
    {
      new MsBuildPackageEmitter().Emit(def, dir);

      File.ReadAllText(Path.Combine(dir, "build", "Contoso.Build.props"))
        .Should().Be(Normalize(ReadExpected("Contoso.Build.props")));

      File.ReadAllText(Path.Combine(dir, "build", "Contoso.Build.targets"))
        .Should().Be(Normalize(ReadExpected("Contoso.Build.targets")));

      File.ReadAllText(Path.Combine(dir, "buildTransitive", "Contoso.Build.props"))
        .Should().Be(Normalize(ReadExpected("Contoso.Build.props")));

      File.ReadAllText(Path.Combine(dir, "buildTransitive", "Contoso.Build.targets"))
        .Should().Be(Normalize(ReadExpected("Contoso.Build.targets")));

      File.ReadAllText(Path.Combine(dir, "Sdk", "Contoso.Build", "Sdk.props"))
        .Should().Be(Normalize(ReadExpected("Contoso.Build.props")));

      File.ReadAllText(Path.Combine(dir, "Sdk", "Contoso.Build", "Sdk.targets"))
        .Should().Be(Normalize(ReadExpected("Contoso.Build.targets")));
    }
    finally
    {
      try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
    }
  }

  [Fact]
  public void Rendering_is_deterministic()
  {
    var def = Package.Define("Determinism")
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

    var dir1 = Path.Combine(Path.GetTempPath(), "JD.MSBuild.Fluent.Tests", Guid.NewGuid().ToString("n"));
    var dir2 = Path.Combine(Path.GetTempPath(), "JD.MSBuild.Fluent.Tests", Guid.NewGuid().ToString("n"));

    try
    {
      new MsBuildPackageEmitter().Emit(def, dir1);
      new MsBuildPackageEmitter().Emit(def, dir2);

      Normalize(File.ReadAllText(Path.Combine(dir1, "build", "Determinism.props")))
        .Should().Be(Normalize(File.ReadAllText(Path.Combine(dir2, "build", "Determinism.props"))));

      Normalize(File.ReadAllText(Path.Combine(dir1, "build", "Determinism.targets")))
        .Should().Be(Normalize(File.ReadAllText(Path.Combine(dir2, "build", "Determinism.targets"))));
    }
    finally
    {
      try { Directory.Delete(dir1, true); } catch { }
      try { Directory.Delete(dir2, true); } catch { }
    }
  }

  private static string ReadExpected(string name)
    => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", "Expected", name));

  private static string Normalize(string s)
    => s.Replace("\r\n", "\n");

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
}
