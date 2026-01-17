using FluentAssertions;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.IR;
using JD.MSBuild.Fluent.Typed;
using TinyBDD.Xunit;
using Xunit.Abstractions;
using static JD.MSBuild.Fluent.Typed.MsBuildExpr;

namespace JD.MSBuild.Fluent.Tests;

/// <summary>Feature: RealWorldPatterns</summary>
public sealed class BddRealWorldPatternsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public async Task Scenario_Multi_TFM_assembly_resolution()
    {
        await Given("a multi-TFM package", () => Package.Define("MultiTfmPkg"))
            .When("configuring runtime-specific assembly paths", b => b
                .Targets(t => t.Target<TResolveAssembly>(tgt => tgt
                    .BeforeTargets("ResolveAssemblyReferences")
                    .ItemGroup(null, ig => ig
                        .Include<MsBuildItemTypes.Reference>("$(MSBuildThisFileDirectory)../lib/net8.0/MyLib.dll",
                            item => item.Meta("TargetFramework", "net8.0"),
                            condition: "'$(TargetFramework)' == 'net8.0'")
                        .Include<MsBuildItemTypes.Reference>("$(MSBuildThisFileDirectory)../lib/net6.0/MyLib.dll",
                            item => item.Meta("TargetFramework", "net6.0"),
                            condition: "'$(TargetFramework)' == 'net6.0'"))))
                .Build())
            .Then("target contains conditional items", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TResolveAssembly");
                var igElement = target.Elements.OfType<MsBuildItemGroupElement>().FirstOrDefault();
                return igElement?.Group.Items.Count == 2;
            })
            .And("items have conditions", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TResolveAssembly");
                var igElement = target.Elements.OfType<MsBuildItemGroupElement>().First();
                return igElement.Group.Items.All(item => item.Condition != null);
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Complex_conditional_logic()
    {
        await Given("a package with complex conditions", () => Package.Define("ConditionalPkg"))
            .When("using Choose/When/Otherwise", b => b
                .Props(p => p.Choose(c => c
                    .When(IsTrue<TIsDebug>(), when => when
                        .Property("OutputPath", "bin/Debug"))
                    .When(And(IsTrue<TIsRelease>(), NotEmpty<TConfiguration>()), when => when
                        .Property("OutputPath", "bin/Release"))
                    .Otherwise(otherwise => otherwise
                        .Property("OutputPath", "bin/Unknown"))))
                .Build())
            .Then("Choose element exists", d => d.Props.Chooses.Count > 0)
            .And("Choose has multiple When clauses", d => d.Props.Chooses[0].Whens.Count == 2)
            .And("Choose has Otherwise clause", d => d.Props.Chooses[0].Otherwise != null)
            .And("When conditions use MsBuildExpr", d =>
                d.Props.Chooses[0].Whens[0].Condition.Contains("TIsDebug"))
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Lifecycle_hooks_pattern()
    {
        await Given("a package with lifecycle hooks", () => Package.Define("LifecyclePkg"))
            .When("defining pre and post hooks", b => b
                .Targets(t => t
                    .Target<TPreBuild>(tgt => tgt
                        .BeforeTargets("CoreCompile")
                        .Message("Pre-build hook", "High"))
                    .Target<TPostBuild>(tgt => tgt
                        .AfterTargets("CoreCompile")
                        .DependsOnTargets("PreBuild")
                        .Message("Post-build hook", "High"))
                    .Target<TCleanup>(tgt => tgt
                        .AfterTargets("Clean")
                        .Message("Cleanup hook", "High")))
                .Build())
            .Then("all lifecycle targets are defined", d => d.Targets.Targets.Count == 3)
            .And("PreBuild runs before CoreCompile", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TPreBuild");
                return target.BeforeTargets?.Contains("CoreCompile") == true;
            })
            .And("PostBuild runs after CoreCompile", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TPostBuild");
                return target.AfterTargets?.Contains("CoreCompile") == true;
            })
            .And("PostBuild depends on PreBuild", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TPostBuild");
                return target.DependsOnTargets?.Contains("PreBuild") == true;
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Task_output_chaining()
    {
        await Given("a package with task output chaining", () => Package.Define("ChainingPkg"))
            .When("chaining task outputs through properties", b => b
                .Targets(t => t.Target<TChainedTarget>(tgt => tgt
                    .Task("Task1", t1 => t1
                        .Param("Input", "StartValue")
                        .OutputProperty("Result", "IntermediateResult"))
                    .Task("Task2", t2 => t2
                        .Param("Input", "$(IntermediateResult)")
                        .OutputProperty("Result", "FinalResult"))
                    .Message("Final: $(FinalResult)", "High")))
                .Build())
            .Then("tasks are chained in target", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TChainedTarget");
                var tasks = target.Elements.OfType<MsBuildTaskStep>().ToList();
                return tasks.Count == 2;
            })
            .And("first task outputs to property", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TChainedTarget");
                var task1 = target.Elements.OfType<MsBuildTaskStep>().First();
                return task1.Outputs.Any(o => o.PropertyName == "IntermediateResult");
            })
            .And("second task uses first task output", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TChainedTarget");
                var task2 = target.Elements.OfType<MsBuildTaskStep>().Skip(1).First();
                return task2.Parameters["Input"] == "$(IntermediateResult)";
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Choose_When_Otherwise_constructs()
    {
        await Given("a package with nested conditions", () => Package.Define("ChoosePkg"))
            .When("using Choose for platform detection", b => b
                .Props(p => p.Choose(c => c
                    .When("'$(OS)' == 'Windows_NT'", when => when
                        .Property("PlatformSpecificPath", "C:\\Program Files")
                        .Property("PathSeparator", ";"))
                    .When("'$(OS)' == 'Unix'", when => when
                        .Property("PlatformSpecificPath", "/usr/local")
                        .Property("PathSeparator", ":"))
                    .Otherwise(otherwise => otherwise
                        .Property("PlatformSpecificPath", "unknown")
                        .Property("PathSeparator", ","))))
                .Build())
            .Then("Choose has When clauses for each OS", d =>
                d.Props.Chooses[0].Whens.Count == 2)
            .And("Each When sets platform-specific properties", d =>
            {
                var when = d.Props.Chooses[0].Whens[0];
                return when.PropertyGroups.Any(pg => 
                    pg.Properties.Any(p => p.Name == "PlatformSpecificPath"));
            })
            .And("Otherwise provides fallback", d =>
            {
                var otherwise = d.Props.Chooses[0].Otherwise;
                return otherwise?.PropertyGroups.Any(pg => 
                    pg.Properties.Any(p => p.Value == "unknown")) == true;
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Dynamic_property_evaluation()
    {
        await Given("a package with dynamic properties", () => Package.Define("DynamicPkg"))
            .When("setting up property evaluation chain", b => b
                .Props(p => p
                    .Property("BaseDir", "$(MSBuildThisFileDirectory)")
                    .Property("LibDir", "$(BaseDir)lib\\")
                    .Property("RuntimeDir", "$(LibDir)$(TargetFramework)\\")
                    .Property("AssemblyPath", "$(RuntimeDir)MyAssembly.dll"))
                .Targets(t => t.Target<TTarget>(tgt => tgt
                    .PropertyGroup(null, pg => pg
                        .Property("ComputedValue", "$([System.IO.Path]::Combine('$(BaseDir)', 'bin'))"))
                    .Message("Computed: $(ComputedValue)", "High")))
                .Build())
            .Then("properties reference each other", d =>
            {
                var props = d.Props.PropertyGroups.SelectMany(pg => pg.Properties);
                return props.Any(p => p.Value.Contains("$(BaseDir)")) &&
                       props.Any(p => p.Value.Contains("$(LibDir)"));
            })
            .And("target computes property at runtime", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                var pgElement = target.Elements.OfType<MsBuildPropertyGroupElement>().FirstOrDefault();
                var computedProp = pgElement?.Group.Properties.FirstOrDefault(p => p.Name == "ComputedValue");
                return computedProp?.Value.Contains("[System.IO.Path]") == true;
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Item_metadata_transformation()
    {
        await Given("a package with item transformations", () => Package.Define("TransformPkg"))
            .When("setting up items with metadata", b => b
                .Targets(t => t.Target<TTarget>(tgt => tgt
                    .ItemGroup(null, ig => ig
                        .Include<MsBuildItemTypes.Compile>("**/*.cs", 
                            item => item
                                .Meta("Link", "%(RecursiveDir)%(Filename)%(Extension)")
                                .Meta("Visible", "false"))
                        .Update<MsBuildItemTypes.Compile>("Generated/**/*.cs",
                            item => item.Meta("AutoGen", "true")))
                    .Message("Files: @(Compile->Count())", "High")))
                .Build())
            .Then("items have metadata", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                var igElement = target.Elements.OfType<MsBuildItemGroupElement>().First();
                var item = igElement.Group.Items.FirstOrDefault();
                return item?.Metadata.Count > 0;
            })
            .And("metadata uses well-known transforms", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                var igElement = target.Elements.OfType<MsBuildItemGroupElement>().First();
                var item = igElement.Group.Items.First();
                return item.Metadata["Link"].Contains("%(RecursiveDir)");
            })
            .And("Update operation modifies existing items", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                var igElement = target.Elements.OfType<MsBuildItemGroupElement>().First();
                return igElement.Group.Items.Any(i => i.Operation == MsBuildItemOperation.Update);
            })
            .AssertPassed();
    }

    private readonly struct TResolveAssembly : IMsBuildTargetName { public string Name => "TResolveAssembly"; }
    private readonly struct TIsDebug : IMsBuildPropertyName { public string Name => "TIsDebug"; }
    private readonly struct TIsRelease : IMsBuildPropertyName { public string Name => "TIsRelease"; }
    private readonly struct TConfiguration : IMsBuildPropertyName { public string Name => "TConfiguration"; }
    private readonly struct TPreBuild : IMsBuildTargetName { public string Name => "TPreBuild"; }
    private readonly struct TPostBuild : IMsBuildTargetName { public string Name => "TPostBuild"; }
    private readonly struct TCleanup : IMsBuildTargetName { public string Name => "TCleanup"; }
    private readonly struct TChainedTarget : IMsBuildTargetName { public string Name => "TChainedTarget"; }
    private readonly struct TTarget : IMsBuildTargetName { public string Name => "TTarget"; }
}
