using System.Xml.Linq;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.IR;
using JD.MSBuild.Fluent.Parse;
using JD.MSBuild.Fluent.Packaging;
using JD.MSBuild.Fluent.Render;
using JD.MSBuild.Fluent.Typed;
using TinyBDD.Assertions;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace JD.MSBuild.Fluent.Tests;

/// <summary>Feature: EfcptCanonicalParity</summary>
public sealed class EfcptCanonicalParityTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
  private static readonly MsBuildXmlParser Parser = new();
  private static readonly MsBuildXmlRenderOptions ParityOptions = new()
  {
    MsBuildXmlns = "",
    CanonicalizePropertyGroups = false,
    CanonicalizeItemGroups = false,
    CanonicalizeItemMetadata = false,
    CanonicalizeUsingTasks = false,
    CanonicalizeTaskParameters = false
  };

  [Fact]
  public async Task Emits_JD_Efcpt_Build_assets_with_parity()
  {
    await Given("expected JD.Efcpt.Build assets parsed from golden files", () =>
      {
        var buildProps = ParseExpected("JD.Efcpt.Build.build.props");
        var buildTargets = ParseExpected("JD.Efcpt.Build.build.targets");
        var buildTransitiveProps = ParseExpected("JD.Efcpt.Build.buildTransitive.props");
        var buildTransitiveTargets = ParseExpected("JD.Efcpt.Build.buildTransitive.targets");
        return (buildProps, buildTargets, buildTransitiveProps, buildTransitiveTargets);
      })
      .When("creating package definition and rewriting assets", ctx =>
      {
        var def = new PackageDefinition { Id = "JD.Efcpt.Build" };
        def.BuildProps = RewriteProps(ctx.buildProps);
        def.BuildTargets = RewriteTargets(ctx.buildTargets);
        def.BuildTransitiveProps = RewriteProps(ctx.buildTransitiveProps);
        def.BuildTransitiveTargets = RewriteTargets(ctx.buildTransitiveTargets);
        def.Packaging.BuildTransitive = true;
        return (def, ctx.buildProps, ctx.buildTargets, ctx.buildTransitiveProps, ctx.buildTransitiveTargets);
      })
      .Then("rewritten props match expected", ctx => AssertProjectsMatch(ctx.buildProps, ctx.def.BuildProps!))
      .And("rewritten targets match expected", ctx => AssertProjectsMatch(ctx.buildTargets, ctx.def.BuildTargets!))
      .And("rewritten buildTransitive props match expected", ctx => AssertProjectsMatch(ctx.buildTransitiveProps, ctx.def.BuildTransitiveProps!))
      .And("rewritten buildTransitive targets match expected", ctx => AssertProjectsMatch(ctx.buildTransitiveTargets, ctx.def.BuildTransitiveTargets!))
      .And("emitted files match golden files", ctx =>
      {
        var dir = Path.Combine(Path.GetTempPath(), "JD.MSBuild.Fluent.Tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(dir);

        try
        {
          var emitter = new MsBuildPackageEmitter(new MsBuildXmlRenderer(ParityOptions));
          emitter.Emit(ctx.def, dir);

          AssertProjectEquivalent(ctx.buildProps, Path.Combine(dir, "build", "JD.Efcpt.Build.props"));
          AssertProjectEquivalent(ctx.buildTargets, Path.Combine(dir, "build", "JD.Efcpt.Build.targets"));
          AssertProjectEquivalent(ctx.buildTransitiveProps, Path.Combine(dir, "buildTransitive", "JD.Efcpt.Build.props"));
          AssertProjectEquivalent(ctx.buildTransitiveTargets, Path.Combine(dir, "buildTransitive", "JD.Efcpt.Build.targets"));
          AssertTextParity("JD.Efcpt.Build.build.props", Path.Combine(dir, "build", "JD.Efcpt.Build.props"));
          AssertTextParity("JD.Efcpt.Build.build.targets", Path.Combine(dir, "build", "JD.Efcpt.Build.targets"));
          AssertTextParity("JD.Efcpt.Build.buildTransitive.props", Path.Combine(dir, "buildTransitive", "JD.Efcpt.Build.props"));
          AssertTextParity("JD.Efcpt.Build.buildTransitive.targets", Path.Combine(dir, "buildTransitive", "JD.Efcpt.Build.targets"));
          
          return true;
        }
        finally
        {
          try { Directory.Delete(dir, recursive: true); } catch { }
        }
      })
      .AssertPassed();
  }

  [Fact]
  public async Task Emits_JD_Efcpt_Sdk_assets_with_parity()
  {
    await Given("expected JD.Efcpt.Sdk assets parsed from golden files", () =>
      {
        var buildProps = ParseExpected("JD.Efcpt.Sdk.build.props");
        var buildTargets = ParseExpected("JD.Efcpt.Sdk.build.targets");
        var sdkProps = ParseExpected("JD.Efcpt.Sdk.Sdk.props");
        var sdkTargets = ParseExpected("JD.Efcpt.Sdk.Sdk.targets");
        return (buildProps, buildTargets, sdkProps, sdkTargets);
      })
      .When("creating SDK package definition and rewriting assets", ctx =>
      {
        var def = new PackageDefinition { Id = "JD.Efcpt.Sdk" };
        def.BuildProps = RewriteProps(ctx.buildProps);
        def.BuildTargets = RewriteTargets(ctx.buildTargets);
        def.SdkProps = RewriteProps(ctx.sdkProps);
        def.SdkTargets = RewriteTargets(ctx.sdkTargets);
        def.Packaging.EmitSdk = true;
        return (def, ctx.buildProps, ctx.buildTargets, ctx.sdkProps, ctx.sdkTargets);
      })
      .Then("rewritten props match expected", ctx => AssertProjectsMatch(ctx.buildProps, ctx.def.BuildProps!))
      .And("rewritten targets match expected", ctx => AssertProjectsMatch(ctx.buildTargets, ctx.def.BuildTargets!))
      .And("rewritten SDK props match expected", ctx => AssertProjectsMatch(ctx.sdkProps, ctx.def.SdkProps!))
      .And("rewritten SDK targets match expected", ctx => AssertProjectsMatch(ctx.sdkTargets, ctx.def.SdkTargets!))
      .And("emitted files match golden files", ctx =>
      {
        var dir = Path.Combine(Path.GetTempPath(), "JD.MSBuild.Fluent.Tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(dir);

        try
        {
          var emitter = new MsBuildPackageEmitter(new MsBuildXmlRenderer(ParityOptions));
          emitter.Emit(ctx.def, dir);

          AssertProjectEquivalent(ctx.buildProps, Path.Combine(dir, "build", "JD.Efcpt.Sdk.props"));
          AssertProjectEquivalent(ctx.buildTargets, Path.Combine(dir, "build", "JD.Efcpt.Sdk.targets"));
          AssertProjectEquivalent(ctx.sdkProps, Path.Combine(dir, "Sdk", ctx.def.Id, "Sdk.props"));
          AssertProjectEquivalent(ctx.sdkTargets, Path.Combine(dir, "Sdk", ctx.def.Id, "Sdk.targets"));
          AssertTextParity("JD.Efcpt.Sdk.build.props", Path.Combine(dir, "build", "JD.Efcpt.Sdk.props"));
          AssertTextParity("JD.Efcpt.Sdk.build.targets", Path.Combine(dir, "build", "JD.Efcpt.Sdk.targets"));
          AssertTextParity("JD.Efcpt.Sdk.Sdk.props", Path.Combine(dir, "Sdk", ctx.def.Id, "Sdk.props"));
          AssertTextParity("JD.Efcpt.Sdk.Sdk.targets", Path.Combine(dir, "Sdk", ctx.def.Id, "Sdk.targets"));
          
          return true;
        }
        finally
        {
          try { Directory.Delete(dir, recursive: true); } catch { }
        }
      })
      .AssertPassed();
  }

  #region Helpers

  private static MsBuildProject ParseExpected(string name)
    => Parser.ParseFile(Path.Combine(AppContext.BaseDirectory, "Golden", "Expected", name));

  private static void AssertProjectEquivalent(MsBuildProject expected, MsBuildProject actual)
  {
    var renderer = new MsBuildXmlRenderer(ParityOptions);
    var expectedXml = renderer.RenderToString(expected);
    var actualXml = renderer.RenderToString(actual);
    Expect.For(actualXml, "rendered project XML").ToBe(expectedXml).GetAwaiter().GetResult();
  }

  private static void AssertProjectEquivalent(MsBuildProject expected, string actualPath)
  {
    var actual = Parser.ParseFile(actualPath);
    AssertProjectEquivalent(expected, actual);
  }

  private static void AssertTextParity(string expectedName, string actualPath)
  {
    var expected = NormalizeForParity(ReadExpected(expectedName));
    var actual = NormalizeForParity(File.ReadAllText(actualPath));
    Expect.For(actual, $"normalized content of {Path.GetFileName(actualPath)}")
      .ToBe(expected).GetAwaiter().GetResult();
  }

  private static string ReadExpected(string name)
    => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", "Expected", name));

  private static string NormalizeForParity(string s)
  {
    var doc = XDocument.Parse(s, LoadOptions.None);
    NormalizeAttributes(doc.Root);
    return doc.ToString(SaveOptions.DisableFormatting);
  }

  private static void NormalizeAttributes(XElement? element)
  {
    if (element is null) return;

    var attrs = element.Attributes()
      .OrderBy(a => a.Name.LocalName, StringComparer.Ordinal)
      .ThenBy(a => a.Name.NamespaceName, StringComparer.Ordinal)
      .ToList();

    element.RemoveAttributes();
    foreach (var attr in attrs)
      element.Add(attr);

    foreach (var child in element.Elements())
      NormalizeAttributes(child);
  }

  private static MsBuildProject RewriteProps(MsBuildProject source)
  {
    var dest = new MsBuildProject { Label = source.Label };
    var builder = PropsBuilder.For(dest);

    foreach (var element in source.Elements)
    {
      switch (element)
      {
        case MsBuildComment comment:
          builder.Comment(comment.Text);
          break;
        case MsBuildImport import:
          builder.Import(import.Project, import.Condition, import.Sdk);
          break;
        case MsBuildPropertyGroup pg:
          builder.PropertyGroup(pg.Condition, g => RewriteProperties(g, pg), pg.Label);
          break;
        case MsBuildItemGroup ig:
          builder.ItemGroup(ig.Condition, g => RewriteItems(g, ig), ig.Label);
          break;
        case MsBuildChoose choose:
          builder.Choose(c => RewriteChoose(c, choose));
          break;
        default:
          throw new NotSupportedException($"Unsupported props element: {element.GetType().Name}");
      }
    }

    return dest;
  }

  private static MsBuildProject RewriteTargets(MsBuildProject source)
  {
    var dest = new MsBuildProject { Label = source.Label };
    var builder = TargetsBuilder.For(dest);

    foreach (var element in source.Elements)
    {
      switch (element)
      {
        case MsBuildComment comment:
          builder.Comment(comment.Text);
          break;
        case MsBuildImport import:
          builder.Import(import.Project, import.Condition, import.Sdk);
          break;
        case MsBuildPropertyGroup pg:
          builder.PropertyGroup(pg.Condition, g => RewriteProperties(g, pg), pg.Label);
          break;
        case MsBuildItemGroup ig:
          builder.ItemGroup(ig.Condition, g => RewriteItems(g, ig), ig.Label);
          break;
        case MsBuildUsingTask ut:
          builder.UsingTask(new MsBuildTaskReference(ut.TaskName, ut.AssemblyFile, ut.AssemblyName, ut.TaskFactory), ut.Condition);
          break;
        case MsBuildTarget target:
          builder.Target(new MsBuildTargetName(target.Name), t => RewriteTarget(t, target));
          break;
        default:
          throw new NotSupportedException($"Unsupported targets element: {element.GetType().Name}");
      }
    }

    return dest;
  }

  private static void RewriteProperties(PropsGroupBuilder builder, MsBuildPropertyGroup group)
  {
    if (group.Entries.Count > 0)
    {
      foreach (var entry in group.Entries)
      {
        switch (entry)
        {
          case MsBuildComment comment:
            builder.Comment(comment.Text);
            break;
          case MsBuildProperty prop:
            builder.Property(new MsBuildPropertyName(prop.Name), prop.Value, prop.Condition);
            break;
          default:
            throw new NotSupportedException($"Unsupported property entry: {entry.GetType().Name}");
        }
      }
      return;
    }

    foreach (var prop in group.Properties)
      builder.Property(new MsBuildPropertyName(prop.Name), prop.Value, prop.Condition);
  }

  private static void RewriteItems(ItemGroupBuilder builder, MsBuildItemGroup group)
  {
    if (group.Entries.Count > 0)
    {
      foreach (var entry in group.Entries)
      {
        switch (entry)
        {
          case MsBuildComment comment:
            builder.Comment(comment.Text);
            break;
          case MsBuildItem item:
            RewriteItemEntry(builder, item);
            break;
          default:
            throw new NotSupportedException($"Unsupported item entry: {entry.GetType().Name}");
        }
      }
      return;
    }

    foreach (var item in group.Items)
      RewriteItemEntry(builder, item);
  }

  private static void RewriteItemEntry(ItemGroupBuilder builder, MsBuildItem item)
  {
    switch (item.Operation)
    {
      case MsBuildItemOperation.Include:
        builder.Include(new MsBuildItemTypeName(item.ItemType), item.Spec, i => RewriteItem(i, item), item.Condition, item.Exclude);
        break;
      case MsBuildItemOperation.Remove:
        builder.Remove(new MsBuildItemTypeName(item.ItemType), item.Spec, item.Condition);
        break;
      case MsBuildItemOperation.Update:
        builder.Update(new MsBuildItemTypeName(item.ItemType), item.Spec, i => RewriteItem(i, item), item.Condition);
        break;
      default:
        throw new NotSupportedException($"Unsupported item operation: {item.Operation}");
    }
  }

  private static void RewriteItem(ItemBuilder builder, MsBuildItem item)
  {
    if (!string.IsNullOrWhiteSpace(item.Exclude))
      builder.Exclude(item.Exclude);

    foreach (var attr in item.MetadataAttributes)
      builder.MetaAttribute(new MsBuildMetadataName(attr.Key), attr.Value);

    foreach (var meta in item.Metadata)
      builder.Meta(new MsBuildMetadataName(meta.Key), meta.Value);
  }

  private static void RewriteChoose(ChooseBuilder builder, MsBuildChoose choose)
  {
    foreach (var when in choose.Whens)
    {
      builder.When(when.Condition, p =>
      {
        foreach (var pg in when.PropertyGroups)
          p.PropertyGroup(pg.Condition, g => RewriteProperties(g, pg), pg.Label);
        foreach (var ig in when.ItemGroups)
          p.ItemGroup(ig.Condition, g => RewriteItems(g, ig), ig.Label);
      });
    }

    if (choose.Otherwise is not null)
    {
      builder.Otherwise(p =>
      {
        foreach (var pg in choose.Otherwise.PropertyGroups)
          p.PropertyGroup(pg.Condition, g => RewriteProperties(g, pg), pg.Label);
        foreach (var ig in choose.Otherwise.ItemGroups)
          p.ItemGroup(ig.Condition, g => RewriteItems(g, ig), ig.Label);
      });
    }
  }

  private static void RewriteTarget(TargetBuilder builder, MsBuildTarget target)
  {
    if (!string.IsNullOrWhiteSpace(target.Label))
      builder.Label(target.Label);
    if (!string.IsNullOrWhiteSpace(target.BeforeTargets))
      builder.BeforeTargets(SplitTargets(target.BeforeTargets));
    if (!string.IsNullOrWhiteSpace(target.AfterTargets))
      builder.AfterTargets(SplitTargets(target.AfterTargets));
    if (!string.IsNullOrWhiteSpace(target.DependsOnTargets))
      builder.DependsOnTargets(SplitTargets(target.DependsOnTargets));
    if (!string.IsNullOrWhiteSpace(target.Inputs))
      builder.Inputs(target.Inputs);
    if (!string.IsNullOrWhiteSpace(target.Outputs))
      builder.Outputs(target.Outputs);
    if (!string.IsNullOrWhiteSpace(target.Condition))
      builder.Condition(target.Condition);

    foreach (var element in target.Elements)
      RewriteTargetElement(builder, element);
  }

  private static void RewriteTargetElement(TargetBuilder builder, MsBuildTargetElement element)
  {
    switch (element)
    {
      case MsBuildPropertyGroupElement pg:
        builder.PropertyGroup(pg.Group.Condition, g => RewriteProperties(g, pg.Group), pg.Group.Label);
        break;
      case MsBuildItemGroupElement ig:
        builder.ItemGroup(ig.Group.Condition, g => RewriteItems(g, ig.Group), ig.Group.Label);
        break;
      case MsBuildTargetComment comment:
        builder.Comment(comment.Text);
        break;
      case MsBuildMessageStep msg:
        builder.Message(msg.Text, msg.Importance, msg.Condition);
        break;
      case MsBuildExecStep exec:
        builder.Exec(exec.Command, exec.WorkingDirectory, exec.Condition);
        break;
      case MsBuildErrorStep err:
        builder.Error(err.Text, err.Condition, err.Code);
        break;
      case MsBuildWarningStep warn:
        builder.Warning(warn.Text, warn.Condition, warn.Code);
        break;
      case MsBuildTaskStep task:
        builder.Task(new MsBuildTaskReference(task.TaskName), t => RewriteTask(t, task), task.Condition);
        break;
      default:
        throw new NotSupportedException($"Unsupported target element: {element.GetType().Name}");
    }
  }

  private static void RewriteTask(TaskInvocationBuilder builder, MsBuildTaskStep task)
  {
    foreach (var param in task.Parameters)
      builder.Param(new MsBuildTaskParameterName(param.Key), param.Value);

    foreach (var output in task.Outputs)
    {
      if (!string.IsNullOrWhiteSpace(output.PropertyName))
        builder.OutputProperty(new MsBuildTaskParameterName(output.TaskParameter), new MsBuildPropertyName(output.PropertyName), output.Condition);
      if (!string.IsNullOrWhiteSpace(output.ItemName))
        builder.OutputItem(new MsBuildTaskParameterName(output.TaskParameter), new MsBuildItemTypeName(output.ItemName), output.Condition);
    }
  }

  private static IMsBuildTargetName[] SplitTargets(string list)
  {
    return list
      .Split([';'], StringSplitOptions.RemoveEmptyEntries)
      .Select(name => (IMsBuildTargetName)new MsBuildTargetName(name.Trim()))
      .ToArray();
  }

  private static bool AssertProjectsMatch(MsBuildProject expected, MsBuildProject actual)
  {
    AssertProjectEquivalent(expected, actual);
    return true;
  }

  #endregion
}
