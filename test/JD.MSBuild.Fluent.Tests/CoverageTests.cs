using FluentAssertions;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.IR;
using JD.MSBuild.Fluent.Parse;
using JD.MSBuild.Fluent.Render;
using JD.MSBuild.Fluent.Tests.Tasks;
using JD.MSBuild.Fluent.Tests.Tasks.Extensions.SampleTask;
using JD.MSBuild.Fluent.Typed;
using JD.MSBuild.Fluent.Validation;

namespace JD.MSBuild.Fluent.Tests;

public sealed class CoverageTests
{
  [Fact]
  public void Fluent_builders_and_renderer_cover_surface()
  {
    var def = Package.Define("Coverage")
      .Description("Coverage package")
      .Props(p => p
        .Comment("props-comment")
        .Import("props-import.props", "'c'=='1'", "Sdk.A")
        .Property<PropertyP0>("V0")
        .PropertyGroup("'pg'=='1'", g => g
          .Comment("pg-comment")
          .Property<PropertyP1>("V1", "'p'=='1'"), label: "PgLabel")
        .Item<MsBuildItemTypes.None>(MsBuildItemOperation.Include, "root.txt", i => i
          .Meta<MetaM0>("V0")
          .MetaAttribute<CopyToOutputDirectoryMeta>("Always"), "'i'=='1'", "exclude.txt")
        .ItemGroup("'ig'=='1'", ig =>
        {
          ig.Comment("ig-comment");
          ig.Include<MsBuildItemTypes.Content>("c.txt", i => i.Exclude("d.txt"));
          ig.Remove<MsBuildItemTypes.Content>("old.txt", "'r'=='1'");
          ig.Update<MsBuildItemTypes.Content>("u.txt", i => i.Meta<MetaU>("1"), "'u'=='1'");
        }, label: "IgLabel")
        .Choose(c => c
          .When("'w'=='1'", wp => wp.PropertyGroup(null, g => g.Property<PropertyW>("1")))
          .Otherwise(op => op.ItemGroup(null, g => g.Include<MsBuildItemTypes.None>("other.txt")))))
      .Targets(t => t
        .Comment("targets-comment")
        .Import("targets-import.targets", "'tc'=='1'", "Sdk.B")
        .UsingTask(new MsBuildTaskReference(new MyTaskName().Name, "path.dll", "My.Assembly", "Factory"), "'ut'=='1'")
        .PropertyGroup("'tpg'=='1'", g => g.Property<PropertyTP>("TV"), label: "TpgLabel")
        .ItemGroup("'tig'=='1'", g => g.Include<MsBuildItemTypes.None>("t.txt"), label: "TigLabel")
        .Target<TargetMyTarget>(tgt => tgt
          .Label("TargetLabel")
          .BeforeTargets(new MsBuildTargets.Build())
          .AfterTargets(new TargetAfterBuild())
          .DependsOnTargets(new TargetOther())
          .Inputs("in")
          .Outputs("out")
          .Condition("'run'=='1'")
          .Comment("inside-target")
          .Message("hello", "Low", "'m'=='1'")
          .Exec("echo hi", "C:\\", "'e'=='1'")
          .Error("err", "E1", "'er'=='1'")
          .Warning("warn", "W1", "'w'=='1'")
          .PropertyGroup("'p'=='1'", g => g.Property<PropertyPG>("V"), label: "Tpg")
          .ItemGroup("'i'=='1'", g => g.Include<MsBuildItemTypes.None>("x.txt"), label: "Tig")
          .Task(new DoStuffTaskName(), task => task
            .Param<ParamP1>("V1")
            .OutputProperty<ParamOut, PropertyProp>("'o'=='1'")
            .OutputItem<ParamOut2, ItemItems>("'o2'=='1'"), "'t'=='1'")))
      .BuildProps(p => p.Property<PropertyBP>("1"))
      .BuildTargets(t => t.Target<TargetBT>(tgt => tgt.Message("hi")))
      .BuildTransitiveProps(p => p.Property<PropertyBTP>("1"))
      .BuildTransitiveTargets(t => t.Target<TargetBTT>(tgt => tgt.Message("hi")))
      .SdkProps(p => p.Property<PropertySP>("1"))
      .SdkTargets(t => t.Target<TargetST>(tgt => tgt.Message("hi")))
      .Pack(o => { o.BuildTransitive = true; o.EmitSdk = true; o.BuildAssetBasename = "Base"; })
      .Build();

    MsBuildValidator.ValidatePackageDefinition(def);
    _ = def.GetBuildProps();
    _ = def.GetBuildTargets();
    _ = def.GetBuildTransitiveProps();
    _ = def.GetBuildTransitiveTargets();
    _ = def.GetSdkProps();
    _ = def.GetSdkTargets();

    var renderer = new MsBuildXmlRenderer();
    renderer.RenderToString(def.GetBuildProps());
    renderer.RenderToString(def.GetBuildTargets());
    renderer.RenderToString(def.GetBuildTransitiveProps());
    renderer.RenderToString(def.GetBuildTransitiveTargets());
    renderer.RenderToString(def.GetSdkProps());
    renderer.RenderToString(def.GetSdkTargets());
  }

  [Fact]
  public void Renderer_supports_legacy_lists_and_empty_xmlns()
  {
    var project = new MsBuildProject();
    project.Imports.Add(new MsBuildImport { Project = "legacy.props", Condition = "'c'=='1'", Sdk = "Legacy.Sdk" });
    project.UsingTasks.Add(new MsBuildUsingTask
    {
      TaskName = new LegacyTaskName().Name,
      AssemblyFile = "task.dll",
      AssemblyName = "Legacy.Assembly",
      TaskFactory = "Factory",
      Condition = "'c'=='1'"
    });

    var pg = new MsBuildPropertyGroup { Condition = "'p'=='1'", Label = "LegacyPg" };
    pg.Properties.Add(new MsBuildProperty { Name = new PropertyP1().Name, Value = "V1", Condition = "'p1'=='1'" });
    project.PropertyGroups.Add(pg);

    var ig = new MsBuildItemGroup { Condition = "'i'=='1'", Label = "LegacyIg" };
    ig.Items.Add(new MsBuildItem
    {
      ItemType = new MsBuildItemTypes.None().Name,
      Operation = MsBuildItemOperation.Include,
      Spec = "a.txt",
      Exclude = "b.txt",
      Condition = "'i1'=='1'"
    });
    project.ItemGroups.Add(ig);

    var target = new MsBuildTarget { Name = new TargetLegacy().Name, Condition = "'t'=='1'" };
    target.Elements.Add(new MsBuildMessageStep { Text = "msg", Importance = "High", Condition = "'m'=='1'" });
    target.Elements.Add(new MsBuildTaskStep { TaskName = new LegacyTaskName().Name, Condition = "'t'=='1'" });
    project.Targets.Add(target);

    var options = new MsBuildXmlRenderOptions
    {
      MsBuildXmlns = "",
      CanonicalizePropertyGroups = false,
      CanonicalizeItemGroups = false,
      CanonicalizeItemMetadata = false,
      CanonicalizeUsingTasks = false,
      CanonicalizeTaskParameters = false
    };

    var xml = new MsBuildXmlRenderer(options).RenderToString(project);
    xml.Should().Contain("<Project>");
  }

  [Fact]
  public void Renderer_canonicalizes_entries_and_orders_items()
  {
    var project = new MsBuildProject();

    var pg = new MsBuildPropertyGroup();
    var a = new MsBuildProperty { Name = new PropertyB().Name, Value = "2" };
    var b = new MsBuildProperty { Name = new PropertyA().Name, Value = "1" };
    pg.Entries.Add(a);
    pg.Entries.Add(b);
    pg.Properties.AddRange([a, b]);
    project.Elements.Add(pg);

    var ig = new MsBuildItemGroup();
    var i1 = new MsBuildItem { ItemType = new MsBuildItemTypes.None().Name, Operation = MsBuildItemOperation.Include, Spec = "b.txt" };
    var i2 = new MsBuildItem { ItemType = new MsBuildItemTypes.None().Name, Operation = MsBuildItemOperation.Include, Spec = "a.txt" };
    ig.Entries.Add(i1);
    ig.Entries.Add(i2);
    ig.Items.AddRange([i1, i2]);
    project.Elements.Add(ig);

    var renderer = new MsBuildXmlRenderer();
    var xml = renderer.RenderToString(project);
    xml.Should().Contain("<A>1</A>");
    xml.Should().Contain("<B>2</B>");
  }

  [Fact]
  public void Renderer_throws_on_unknown_elements()
  {
    var project = new MsBuildProject();
    project.Elements.Add(new UnknownProjectElement());

    Action act = () => new MsBuildXmlRenderer().RenderToString(project);
    act.Should().Throw<MsBuildValidationException>();

    var badGroupProject = new MsBuildProject();
    var badGroup = new MsBuildPropertyGroup();
    badGroup.Entries.Add(new UnknownPropertyEntry());
    badGroupProject.Elements.Add(badGroup);
    Action badGroupAct = () => new MsBuildXmlRenderer().RenderToString(badGroupProject);
    badGroupAct.Should().Throw<MsBuildValidationException>();

    var badItemProject = new MsBuildProject();
    var badItemGroup = new MsBuildItemGroup();
    badItemGroup.Entries.Add(new UnknownItemEntry());
    badItemProject.Elements.Add(badItemGroup);
    Action badItemAct = () => new MsBuildXmlRenderer().RenderToString(badItemProject);
    badItemAct.Should().Throw<MsBuildValidationException>();

    var badTargetProject = new MsBuildProject();
    var badTarget = new MsBuildTarget { Name = new TargetBad().Name };
    badTarget.Elements.Add(new UnknownTargetElement());
    badTargetProject.Elements.Add(badTarget);
    Action badTargetAct = () => new MsBuildXmlRenderer().RenderToString(badTargetProject);
    badTargetAct.Should().Throw<MsBuildValidationException>();
  }

  [Fact]
  public void Parser_reads_comments_and_metadata()
  {
    var xml = """
<Project>
  <!--project-comment-->
  <Import Project="foo.props" Sdk="Sdk.X" Condition="'c'=='1'" />
  <PropertyGroup Condition="'p'=='1'" Label="pg">
    <!--pg-comment-->
    <Prop Condition="'pc'=='1'">Value</Prop>
  </PropertyGroup>
  <ItemGroup Condition="'i'=='1'" Label="ig">
    <!--ig-comment-->
    <None Include="a.txt" Exclude="b.txt" Condition="'ic'=='1'" Custom="attr">
      <Meta>V</Meta>
    </None>
  </ItemGroup>
  <UsingTask TaskName="Task" AssemblyFile="a.dll" AssemblyName="Asm" TaskFactory="Fact" Condition="'t'=='1'" />
  <Choose>
    <When Condition="'w'=='1'">
      <PropertyGroup><W>1</W></PropertyGroup>
      <ItemGroup><None Include="w.txt" /></ItemGroup>
    </When>
    <Otherwise>
      <PropertyGroup><O>1</O></PropertyGroup>
    </Otherwise>
  </Choose>
  <Target Name="T" Label="lbl" Condition="'tc'=='1'" BeforeTargets="Build" AfterTargets="After" DependsOnTargets="Dep" Inputs="in" Outputs="out">
    <!--target-comment-->
    <PropertyGroup><P>1</P></PropertyGroup>
    <ItemGroup><None Include="x.txt" /></ItemGroup>
    <Message Text="msg" Importance="Low" Condition="'m'=='1'" />
    <Exec Command="cmd" WorkingDirectory="wd" Condition="'e'=='1'" />
    <Error Text="err" Code="E1" Condition="'er'=='1'" />
    <Warning Text="warn" Code="W1" Condition="'w'=='1'" />
    <CustomTask Param="Value" Condition="'c'=='1'">
      <Output TaskParameter="Out" PropertyName="Prop" Condition="'o'=='1'" />
      <Output TaskParameter="Out2" ItemName="Items" />
    </CustomTask>
  </Target>
</Project>
""";

    var parser = new MsBuildXmlParser();
    var project = parser.Parse(xml);

    project.Elements.OfType<MsBuildComment>().Should().NotBeEmpty();
    project.Imports.Single().Sdk.Should().Be("Sdk.X");
    project.PropertyGroups.Single().Entries.OfType<MsBuildComment>().Should().NotBeEmpty();
    project.ItemGroups.Single().Entries.OfType<MsBuildComment>().Should().NotBeEmpty();
    project.ItemGroups.Single().Items.Single().MetadataAttributes.Should().ContainKey("Custom");

    var target = project.Targets.Single();
    target.Elements.OfType<MsBuildTargetComment>().Should().NotBeEmpty();
    target.Elements.OfType<MsBuildWarningStep>().Should().NotBeEmpty();
  }

  [Fact]
  public void Parser_throws_on_invalid_inputs()
  {
    var parser = new MsBuildXmlParser();

    Action nullXml = () => parser.Parse(null!);
    nullXml.Should().Throw<ArgumentNullException>();

    Action badRoot = () => parser.Parse("<Root />");
    badRoot.Should().Throw<NotSupportedException>();

    Action badElement = () => parser.Parse("<Project><Unknown /></Project>");
    badElement.Should().Throw<NotSupportedException>();

    Action badItem = () => parser.Parse("<Project><ItemGroup><None /></ItemGroup></Project>");
    badItem.Should().Throw<NotSupportedException>();

    Action badTaskChild = () => parser.Parse("""
<Project>
  <Target Name="T">
    <SomeTask>
      <Bad />
    </SomeTask>
  </Target>
</Project>
""");
    badTaskChild.Should().Throw<NotSupportedException>();

    Action badPath = () => parser.ParseFile(" ");
    badPath.Should().Throw<ArgumentException>();
  }

  [Fact]
  public void Validator_reports_errors_for_elements_mode()
  {
    var project = new MsBuildProject();

    var import = new MsBuildImport { Project = "" };
    project.Elements.Add(import);
    project.Elements.Add(import);
    project.Imports.Add(import);
    project.Imports.Add(new MsBuildImport { Project = "missing" });

    var pg = new MsBuildPropertyGroup();
    var prop = new MsBuildProperty { Name = "", Value = null! };
    pg.Entries.Add(new UnknownPropertyEntry());
    pg.Entries.Add(prop);
    pg.Properties.Add(new MsBuildProperty { Name = "P", Value = "V" });
    project.Elements.Add(pg);
    project.PropertyGroups.Add(pg);

    var ig = new MsBuildItemGroup();
    var item = new MsBuildItem
    {
      ItemType = "", Operation = MsBuildItemOperation.Include, Spec = "",
      Metadata =
      {
        [""] = "v"
      },
      MetadataAttributes =
      {
        [""] = null!
      }
    };
    ig.Entries.Add(new UnknownItemEntry());
    ig.Entries.Add(item);
    ig.Items.Add(new MsBuildItem { ItemType = "None", Operation = MsBuildItemOperation.Include, Spec = "x" });
    project.Elements.Add(ig);
    project.ItemGroups.Add(ig);

    var ut = new MsBuildUsingTask { TaskName = "" };
    project.Elements.Add(ut);
    project.UsingTasks.Add(ut);

    var target = new MsBuildTarget { Name = "" };
    target.Elements.Add(new UnknownTargetElement());
    var badTask = new MsBuildTaskStep
    {
      TaskName = "",
      Parameters =
      {
        [""] = null!
      }
    };
    badTask.Outputs.Add(new MsBuildTaskOutput { TaskParameter = "", PropertyName = null, ItemName = null });
    target.Elements.Add(badTask);
    project.Elements.Add(target);
    project.Targets.Add(target);

    var choose = new MsBuildChoose();
    choose.Whens.Add(new MsBuildWhen { Condition = "" });
    choose.Otherwise = new MsBuildOtherwise();
    project.Elements.Add(choose);
    project.Chooses.Add(choose);

    var act = () => MsBuildValidator.ValidateProject(project);
    act.Should().Throw<MsBuildValidationException>();
  }

  [Fact]
  public void Validator_reports_errors_for_legacy_lists()
  {
    var project = new MsBuildProject();

    project.Imports.Add(new MsBuildImport { Project = "" });
    project.UsingTasks.Add(new MsBuildUsingTask { TaskName = "" });

    var pg = new MsBuildPropertyGroup();
    pg.Properties.Add(new MsBuildProperty { Name = "", Value = null! });
    project.PropertyGroups.Add(pg);

    var ig = new MsBuildItemGroup();
    var item = new MsBuildItem
    {
      ItemType = "", Operation = MsBuildItemOperation.Include, Spec = "",
      Metadata =
      {
        [""] = "v"
      },
      MetadataAttributes =
      {
        [""] = null!
      }
    };
    ig.Items.Add(item);
    project.ItemGroups.Add(ig);

    var target = new MsBuildTarget { Name = "" };
    target.Elements.Add(new MsBuildMessageStep { Text = "" });
    target.Elements.Add(new MsBuildExecStep { Command = "" });
    target.Elements.Add(new MsBuildErrorStep { Text = "" });
    target.Elements.Add(new MsBuildWarningStep { Text = "" });
    project.Targets.Add(target);

    var choose = new MsBuildChoose();
    choose.Whens.Add(new MsBuildWhen { Condition = "" });
    project.Chooses.Add(choose);

    var act = () => MsBuildValidator.ValidateProject(project);
    act.Should().Throw<MsBuildValidationException>();
  }

  [Fact]
  public void Typed_names_and_tasks_reduce_strings()
  {
    var taskRef = MsBuildTaskReference.FromType<SampleTask>(assemblyFile: "tasks.dll");

    var def = Package.Define("Typed")
      .Props(p => p
        .Property<MsBuildProperties.Configuration>("Debug")
        .PropertyGroup(null, g => g.Property<MsBuildProperties.Platform>("AnyCPU"))
        .Item<MsBuildItemTypes.Content>(MsBuildItemOperation.Include, "readme.md", i => i
          .Meta<CopyToOutputDirectoryMeta>("Always"))
        .ItemGroup(null, ig => ig
          .Include<MsBuildItemTypes.None>("notes.txt", i => i.MetaAttribute<CustomMetadata>("1"))
          .Remove<MsBuildItemTypes.Content>("old.txt")
          .Update<MsBuildItemTypes.Content>("new.txt", i => i.Meta<CopyToOutputDirectoryMeta>("Always"))))
      .Targets(t => t
        .UsingTask(taskRef)
        .UsingTask<SampleTask>(assemblyFile: "tasks2.dll", nameStyle: MsBuildTaskNameStyle.Name)
        .Target<MsBuildTargets.CoreCompile>(target => target
          .BeforeTargets(new MsBuildTargets.Build())
          .AfterTargets(new MsBuildTargets.Compile())
          .DependsOnTargets(new MsBuildTargets.Clean(), new MsBuildTargets.Build())
          .Task(taskRef, task => task
            .Param("Value")
            .OutProperty<MsBuildProperties.OutputPath>()
            .ItemsItem<MsBuildItemTypes.Content>())
          .Task<SampleTask>(task => task
            .Param("Value2")
            .OutProperty<MsBuildProperties.OutputPath>()
            .ItemsItem<MsBuildItemTypes.Content>())
          .Task(new CustomTaskName(), task => task.Param<CustomParam2>("Value3"))))
      .Build();

    MsBuildValidator.ValidatePackageDefinition(def);

    var renderer = new MsBuildXmlRenderer();
    var targetsXml = renderer.RenderToString(def.Targets);
    var propsXml = renderer.RenderToString(def.Props);
    targetsXml.Should().Contain("UsingTask");
    targetsXml.Should().Contain("CoreCompile");
    propsXml.Should().Contain("CopyToOutputDirectory");
  }

  private sealed class UnknownProjectElement : IMsBuildProjectElement
  {
  }

  private sealed class UnknownPropertyEntry : IMsBuildPropertyGroupEntry
  {
  }

  private sealed class UnknownItemEntry : IMsBuildItemGroupEntry
  {
  }

  private sealed class UnknownTargetElement : MsBuildTargetElement
  {
  }

  private readonly struct PropertyP0 : IMsBuildPropertyName
  {
    public string Name => "P0";
  }

  private readonly struct PropertyP1 : IMsBuildPropertyName
  {
    public string Name => "P1";
  }

  private readonly struct PropertyW : IMsBuildPropertyName
  {
    public string Name => "W";
  }

  private readonly struct PropertyTP : IMsBuildPropertyName
  {
    public string Name => "TP";
  }

  private readonly struct PropertyPG : IMsBuildPropertyName
  {
    public string Name => "PG";
  }

  private readonly struct PropertyBP : IMsBuildPropertyName
  {
    public string Name => "BP";
  }

  private readonly struct PropertyBTP : IMsBuildPropertyName
  {
    public string Name => "BTP";
  }

  private readonly struct PropertySP : IMsBuildPropertyName
  {
    public string Name => "SP";
  }

  private readonly struct PropertyA : IMsBuildPropertyName
  {
    public string Name => "A";
  }

  private readonly struct PropertyB : IMsBuildPropertyName
  {
    public string Name => "B";
  }

  private readonly struct PropertyProp : IMsBuildPropertyName
  {
    public string Name => "Prop";
  }

  private readonly struct TargetMyTarget : IMsBuildTargetName
  {
    public string Name => "MyTarget";
  }

  private readonly struct TargetBT : IMsBuildTargetName
  {
    public string Name => "BT";
  }

  private readonly struct TargetBTT : IMsBuildTargetName
  {
    public string Name => "BTT";
  }

  private readonly struct TargetST : IMsBuildTargetName
  {
    public string Name => "ST";
  }

  private readonly struct TargetLegacy : IMsBuildTargetName
  {
    public string Name => "LegacyTarget";
  }

  private readonly struct TargetBad : IMsBuildTargetName
  {
    public string Name => "BadTarget";
  }

  private readonly struct TargetAfterBuild : IMsBuildTargetName
  {
    public string Name => "AfterBuild";
  }

  private readonly struct TargetOther : IMsBuildTargetName
  {
    public string Name => "Other";
  }

  private readonly struct MyTaskName : IMsBuildTaskName
  {
    public string Name => "MyTask";
  }

  private readonly struct DoStuffTaskName : IMsBuildTaskName
  {
    public string Name => "DoStuff";
  }

  private readonly struct LegacyTaskName : IMsBuildTaskName
  {
    public string Name => "LegacyTask";
  }

  private readonly struct ParamP1 : IMsBuildTaskParameterName
  {
    public string Name => "P1";
  }

  private readonly struct ParamOut : IMsBuildTaskParameterName
  {
    public string Name => "Out";
  }

  private readonly struct ParamOut2 : IMsBuildTaskParameterName
  {
    public string Name => "Out2";
  }

  private readonly struct ItemItems : IMsBuildItemTypeName
  {
    public string Name => "Items";
  }

  private readonly struct MetaM0 : IMsBuildMetadataName
  {
    public string Name => "M0";
  }

  private readonly struct MetaU : IMsBuildMetadataName
  {
    public string Name => "U";
  }

  private readonly struct CustomTaskName : IMsBuildTaskName
  {
    public string Name => "CustomTask";
  }

  private readonly struct CustomParam2 : IMsBuildTaskParameterName
  {
    public string Name => "Param2";
  }

  private readonly struct CopyToOutputDirectoryMeta : IMsBuildMetadataName
  {
    public string Name => "CopyToOutputDirectory";
  }

  private readonly struct CustomMetadata : IMsBuildMetadataName
  {
    public string Name => "Custom";
  }
}
