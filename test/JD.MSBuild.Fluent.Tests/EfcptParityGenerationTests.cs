using Contoso.Build.Tasks;
using Contoso.Build.Tasks.Extensions.DetectSqlProject;
using Contoso.Build.Tasks.Extensions.ResolveInputs;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.Packaging;
using JD.MSBuild.Fluent.Tests.Tasks;
using JD.MSBuild.Fluent.Tests.Tasks.Extensions.Copy;
using JD.MSBuild.Fluent.Typed;
using TinyBDD.Assertions;
using TinyBDD.Xunit;
using Xunit.Abstractions;
using static JD.MSBuild.Fluent.Typed.MsBuildExpr;

namespace JD.MSBuild.Fluent.Tests;

/// <summary>Feature: EfcptParityGeneration</summary>
public sealed class EfcptParityGenerationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
  [Fact]
  public async Task Emits_complex_parity_assets()
  {
    await Given("complex package definition", () =>
      {
        var def = BuildDefinition();
        var tempDir = Path.Combine(Path.GetTempPath(), "JD.MSBuild.Fluent.Tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        return (def, tempDir);
      })
      .When("emitting package", ctx =>
      {
        new MsBuildPackageEmitter().Emit(ctx.def, ctx.tempDir);
        return ctx.tempDir;
      })
      .Then("build.props matches expected", dir =>
        Expect.For(File.ReadAllText(Path.Combine(dir, "build", "Efcpt.Parity.props")), "build.props content")
          .ToBe(Normalize(ReadExpected("Efcpt.Parity.build.props"))))
      .And("build.targets matches expected", dir =>
        Expect.For(File.ReadAllText(Path.Combine(dir, "build", "Efcpt.Parity.targets")), "build.targets content")
          .ToBe(Normalize(ReadExpected("Efcpt.Parity.build.targets"))))
      .And("buildTransitive.props matches expected", dir =>
        Expect.For(File.ReadAllText(Path.Combine(dir, "buildTransitive", "Efcpt.Parity.props")), "buildTransitive.props content")
          .ToBe(Normalize(ReadExpected("Efcpt.Parity.buildTransitive.props"))))
      .And("buildTransitive.targets matches expected", dir =>
        Expect.For(File.ReadAllText(Path.Combine(dir, "buildTransitive", "Efcpt.Parity.targets")), "buildTransitive.targets content")
          .ToBe(Normalize(ReadExpected("Efcpt.Parity.buildTransitive.targets"))))
      .And("Sdk.props matches expected", dir =>
      {
        var def = BuildDefinition();
        return Expect.For(File.ReadAllText(Path.Combine(dir, "Sdk", def.Id, "Sdk.props")), "Sdk.props content")
          .ToBe(Normalize(ReadExpected("Efcpt.Parity.Sdk.props")));
      })
      .And("Sdk.targets matches expected", dir =>
      {
        var def = BuildDefinition();
        return Expect.For(File.ReadAllText(Path.Combine(dir, "Sdk", def.Id, "Sdk.targets")), "Sdk.targets content")
          .ToBe(Normalize(ReadExpected("Efcpt.Parity.Sdk.targets")));
      })
      .Finally(dir =>
      {
        if (dir != null && Directory.Exists(dir))
          try { Directory.Delete(dir, recursive: true); } catch { }
      })
      .AssertPassed();
  }

  #region Helpers

  private static PackageDefinition BuildDefinition()
    => Package.Define("Efcpt.Parity")
      .BuildProps(p => p
        .Property<EfcptIsDirectReference>("true")
        .Import(@"..\buildTransitive\Efcpt.Parity.props"))
      .BuildTargets(t => t
        .Import(@"..\buildTransitive\Efcpt.Parity.targets"))
      .BuildTransitiveProps(p => p
        .Property<EfcptEnabled>("true", condition: IsEmpty<EfcptEnabled>())
        .Property<EfcptOutput>(
          $"{Prop<MsBuildProperties.BaseIntermediateOutputPath>()}efcpt\\",
          condition: IsEmpty<EfcptOutput>())
        .Property<EfcptGeneratedDir>(
          $"{Prop<EfcptOutput>()}Generated\\",
          condition: IsEmpty<EfcptGeneratedDir>())
        .Property<EfcptConfig>("efcpt-config.json", condition: IsEmpty<EfcptConfig>())
        .Property<EfcptRenaming>("efcpt.renaming.json", condition: IsEmpty<EfcptRenaming>())
        .PropertyGroup(null, g => g
          .Property<EfcptConfigRootNamespace>(
            Prop<MsBuildProperties.RootNamespace>(),
            condition: And(
              IsEmpty<EfcptConfigRootNamespace>(),
              NotEmpty<MsBuildProperties.RootNamespace>()))
          .Property<EfcptConfigRootNamespace>(
            Prop<MsBuildProperties.MSBuildProjectName>(),
            condition: IsEmpty<EfcptConfigRootNamespace>())
          .Property<EfcptConfigDbContextName>(
            "",
            condition: IsEmpty<EfcptConfigDbContextName>()))
        .ItemGroup(null, ig => ig
          .Include<MsBuildItemTypes.None>("efcpt-config.json", i => i.Meta<PackMetadata>("true"))
          .Include<MsBuildItemTypes.None>("efcpt.renaming.json", i => i.Meta<PackMetadata>("true"))))
      .BuildTransitiveTargets(t => t
        .UsingTask(ResolveInputsMsBuild.Reference)
        .UsingTask(DetectSqlProjectMsBuild.Reference)
        .Target<EfcptDetectSqlProjectTarget>(tgt => tgt
          .BeforeTargets(new BeforeBuildTarget(), new BeforeRebuildTarget())
          .Task<DetectSqlProject>(task => task
            .ProjectPath(Prop<MsBuildProperties.MSBuildProjectFullPath>())
            .SqlServerVersion(Prop<SqlServerVersion>())
            .DSP(Prop<Dsp>())
            .IsSqlProjectProperty<EfcptIsSqlProject>(), nameStyle: MsBuildTaskNameStyle.Name)
          .PropertyGroup(null, pg => pg
            .Property<EfcptIsSqlProject>("false", condition: IsEmpty<EfcptIsSqlProject>())))
        .Target<EfcptResolveInputsTarget>(tgt => tgt
          .Condition(IsTrue<EfcptEnabled>())
          .Task<ResolveInputs>(task => task
            .ProjectDirectory(Prop<MsBuildProperties.MSBuildProjectDirectory>())
            .OutputDir(Prop<EfcptOutput>())
            .LogVerbosity(Prop<EfcptLogVerbosity>())
            .ResolvedConfigProperty<EfcptResolvedConfig>()
            .ResolvedRenamingProperty<EfcptResolvedRenaming>(), nameStyle: MsBuildTaskNameStyle.Name))
        .Target<EfcptStageInputsTarget>(tgt => tgt
          .DependsOnTargets(new EfcptResolveInputsTarget())
          .Inputs($"{Prop<EfcptOutput>()}input.txt")
          .Outputs($"{Prop<EfcptOutput>()}stamp.txt")
          .ItemGroup(null, ig => ig
            .Include<EfcptStagedInputsItem>($"{Prop<EfcptOutput>()}**\\*", i => i.MetaAttribute<VisibleMetadata>("false")))
          .Task(new CopyMsBuild.TaskName(), task => task
            .SourceFiles(Item<EfcptStagedInputsItem>())
            .DestinationFolder(Prop<EfcptGeneratedDir>())
            .CopiedFilesItem<EfcptCopiedInputsItem>()))
        .Target<EfcptAddToCompileTarget>(tgt => tgt
          .BeforeTargets(new MsBuildTargets.CoreCompile())
          .ItemGroup(null, ig => ig
            .Include<MsBuildItemTypes.Compile>(
              $"{Prop<EfcptGeneratedDir>()}Models\\**\\*.g.cs",
              i => i.MetaAttribute<VisibleMetadata>("false"),
              condition: IsTrue<EfcptSplitOutputs>())
            .Include<MsBuildItemTypes.Compile>(
              $"{Prop<EfcptGeneratedDir>()}**\\*.g.cs",
              i => i.MetaAttribute<VisibleMetadata>("false"),
              condition: IsNotTrue<EfcptSplitOutputs>())
            .Include<EfcptDbContextFilesItem>($"{Prop<EfcptGeneratedDir>()}*.g.cs", i => i.Exclude($"{Prop<EfcptGeneratedDir>()}*Configuration.g.cs")))))
      .SdkProps(p => p
        .Import("Sdk.props", sdk: "Microsoft.NET.Sdk")
        .Property<EfcptCheckForUpdates>("true", condition: IsEmptyWithSpace<EfcptCheckForUpdates>())
        .Import($"{Prop<MsBuildProperties.MSBuildThisFileDirectory>()}Efcpt.Parity.props"))
      .SdkTargets(t => t
        .Import("Sdk.targets", sdk: "Microsoft.NET.Sdk")
        .Import($"{Prop<MsBuildProperties.MSBuildThisFileDirectory>()}Efcpt.Parity.targets"))
      .Pack(o => { o.BuildTransitive = true; o.EmitSdk = true; })
      .Build();

  private static string ReadExpected(string name)
    => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", "Expected", name));

  private static string Normalize(string s)
    => s.Replace("\r\n", "\n");

  private readonly struct EfcptIsDirectReference : IMsBuildPropertyName
  {
    public string Name => "_EfcptIsDirectReference";
  }

  private readonly struct EfcptEnabled : IMsBuildPropertyName
  {
    public string Name => "EfcptEnabled";
  }

  private readonly struct EfcptOutput : IMsBuildPropertyName
  {
    public string Name => "EfcptOutput";
  }

  private readonly struct EfcptGeneratedDir : IMsBuildPropertyName
  {
    public string Name => "EfcptGeneratedDir";
  }

  private readonly struct EfcptConfig : IMsBuildPropertyName
  {
    public string Name => "EfcptConfig";
  }

  private readonly struct EfcptRenaming : IMsBuildPropertyName
  {
    public string Name => "EfcptRenaming";
  }

  private readonly struct EfcptConfigRootNamespace : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigRootNamespace";
  }

  private readonly struct EfcptConfigDbContextName : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigDbContextName";
  }

  private readonly struct EfcptLogVerbosity : IMsBuildPropertyName
  {
    public string Name => "EfcptLogVerbosity";
  }

  private readonly struct EfcptResolvedConfig : IMsBuildPropertyName
  {
    public string Name => "_EfcptResolvedConfig";
  }

  private readonly struct EfcptResolvedRenaming : IMsBuildPropertyName
  {
    public string Name => "_EfcptResolvedRenaming";
  }

  private readonly struct EfcptIsSqlProject : IMsBuildPropertyName
  {
    public string Name => "_EfcptIsSqlProject";
  }

  private readonly struct EfcptCheckForUpdates : IMsBuildPropertyName
  {
    public string Name => "EfcptCheckForUpdates";
  }

  private readonly struct EfcptSplitOutputs : IMsBuildPropertyName
  {
    public string Name => "EfcptSplitOutputs";
  }

  private readonly struct SqlServerVersion : IMsBuildPropertyName
  {
    public string Name => "SqlServerVersion";
  }

  private readonly struct Dsp : IMsBuildPropertyName
  {
    public string Name => "DSP";
  }

  private readonly struct EfcptStagedInputsItem : IMsBuildItemTypeName
  {
    public string Name => "_EfcptStagedInputs";
  }

  private readonly struct EfcptCopiedInputsItem : IMsBuildItemTypeName
  {
    public string Name => "_EfcptCopiedInputs";
  }

  private readonly struct EfcptDbContextFilesItem : IMsBuildItemTypeName
  {
    public string Name => "_EfcptDbContextFiles";
  }

  private readonly struct EfcptDetectSqlProjectTarget : IMsBuildTargetName
  {
    public string Name => "_EfcptDetectSqlProject";
  }

  private readonly struct EfcptResolveInputsTarget : IMsBuildTargetName
  {
    public string Name => "EfcptResolveInputs";
  }

  private readonly struct EfcptStageInputsTarget : IMsBuildTargetName
  {
    public string Name => "EfcptStageInputs";
  }

  private readonly struct EfcptAddToCompileTarget : IMsBuildTargetName
  {
    public string Name => "EfcptAddToCompile";
  }

  private readonly struct BeforeBuildTarget : IMsBuildTargetName
  {
    public string Name => "BeforeBuild";
  }

  private readonly struct BeforeRebuildTarget : IMsBuildTargetName
  {
    public string Name => "BeforeRebuild";
  }

  private readonly struct PackMetadata : IMsBuildMetadataName
  {
    public string Name => "Pack";
  }

  private readonly struct VisibleMetadata : IMsBuildMetadataName
  {
    public string Name => "Visible";
  }

  #endregion
}
