using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.Typed;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace JD.MSBuild.Fluent.Tests;

/// <summary>Feature: MSBuildPackageStructure</summary>
public sealed class BddPackagingTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public async Task Scenario_Emit_to_build_folder()
    {
        await Given("a package with build folder configuration", () => Package.Define("MyPkg"))
            .When("explicitly configuring build props and targets", b => b
                .BuildProps(p => p.Property("BuildProp", "Value1"))
                .BuildTargets(t => t.Target<TTarget>(tgt => tgt.Message("Build")))
                .Build())
            .Then("build props is set", d => d.BuildProps != null)
            .And("build targets is set", d => d.BuildTargets != null)
            .And("build props contains property", d =>
                d.BuildProps?.PropertyGroups.Any(pg => pg.Properties.Any(p => p.Name == "BuildProp")) == true)
            .And("build targets contains target", d =>
                d.BuildTargets?.Targets.Any(t => t.Name == "TTarget") == true)
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Emit_to_buildTransitive_folder()
    {
        await Given("a package with buildTransitive configuration", () => Package.Define("MyPkg"))
            .When("configuring buildTransitive props and targets", b => b
                .BuildTransitiveProps(p => p.Property("TransProp", "Value2"))
                .BuildTransitiveTargets(t => t.Target<TTarget>(tgt => tgt.Message("Transitive")))
                .Pack(o => o.BuildTransitive = true)
                .Build())
            .Then("buildTransitive props is set", d => d.BuildTransitiveProps != null)
            .And("buildTransitive targets is set", d => d.BuildTransitiveTargets != null)
            .And("packaging option is enabled", d => d.Packaging.BuildTransitive)
            .And("buildTransitive props contains property", d =>
                d.BuildTransitiveProps?.PropertyGroups.Any(pg => pg.Properties.Any(p => p.Name == "TransProp")) == true)
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Emit_to_Sdk_folder()
    {
        await Given("a package with SDK configuration", () => Package.Define("MyPkg"))
            .When("configuring SDK props and targets", b => b
                .SdkProps(p => p.Property("SdkProp", "SdkValue"))
                .SdkTargets(t => t.Target<TTarget>(tgt => tgt.Message("SDK")))
                .Pack(o => o.EmitSdk = true)
                .Build())
            .Then("SDK props is set", d => d.SdkProps != null)
            .And("SDK targets is set", d => d.SdkTargets != null)
            .And("EmitSdk option is enabled", d => d.Packaging.EmitSdk)
            .And("SDK props contains property", d =>
                d.SdkProps?.PropertyGroups.Any(pg => pg.Properties.Any(p => p.Name == "SdkProp")) == true)
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Configure_packaging_options()
    {
        await Given("a package builder", () => Package.Define("MyPkg"))
            .When("setting all packaging options", b => b
                .Pack(o =>
                {
                    o.BuildTransitive = true;
                    o.EmitSdk = true;
                    o.BuildAssetBasename = "CustomBasename";
                })
                .Build())
            .Then("BuildTransitive is set", d => d.Packaging.BuildTransitive)
            .And("EmitSdk is set", d => d.Packaging.EmitSdk)
            .And("BuildAssetBasename is set", d => d.Packaging.BuildAssetBasename == "CustomBasename")
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Validate_folder_structure()
    {
        await Given("a complete package definition", () => Package.Define("MyPkg"))
            .When("configuring all folder types", b => b
                .Props(p => p.Property("BaseProp", "V1"))
                .Targets(t => t.Target<TTarget>(tgt => tgt.Message("Base")))
                .BuildProps(p => p.Property("BuildProp", "V2"))
                .BuildTargets(t => t.Target<TTarget2>(tgt => tgt.Message("Build")))
                .BuildTransitiveProps(p => p.Property("TransProp", "V3"))
                .BuildTransitiveTargets(t => t.Target<TTarget3>(tgt => tgt.Message("Trans")))
                .SdkProps(p => p.Property("SdkProp", "V4"))
                .SdkTargets(t => t.Target<TTarget4>(tgt => tgt.Message("Sdk")))
                .Build())
            .Then("all folder types are defined", d =>
                d.Props != null &&
                d.Targets != null &&
                d.BuildProps != null &&
                d.BuildTargets != null &&
                d.BuildTransitiveProps != null &&
                d.BuildTransitiveTargets != null &&
                d.SdkProps != null &&
                d.SdkTargets != null)
            .And("GetBuildProps returns BuildProps", d => d.GetBuildProps() == d.BuildProps)
            .And("GetBuildTargets returns BuildTargets", d => d.GetBuildTargets() == d.BuildTargets)
            .And("GetBuildTransitiveProps returns BuildTransitiveProps", d =>
                d.GetBuildTransitiveProps() == d.BuildTransitiveProps)
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Skip_empty_sections()
    {
        await Given("a minimal package", () => Package.Define("MinimalPkg"))
            .When("building without optional sections", b => b.Build())
            .Then("BuildProps is null", d => d.BuildProps == null)
            .And("BuildTargets is null", d => d.BuildTargets == null)
            .And("BuildTransitiveProps is null", d => d.BuildTransitiveProps == null)
            .And("BuildTransitiveTargets is null", d => d.BuildTransitiveTargets == null)
            .And("SdkProps is null", d => d.SdkProps == null)
            .And("SdkTargets is null", d => d.SdkTargets == null)
            .And("GetBuildProps falls back to Props", d => d.GetBuildProps() == d.Props)
            .And("GetBuildTargets falls back to Targets", d => d.GetBuildTargets() == d.Targets)
            .AssertPassed();
    }

    private readonly struct TTarget : IMsBuildTargetName { public string Name => "TTarget"; }
    private readonly struct TTarget2 : IMsBuildTargetName { public string Name => "TTarget2"; }
    private readonly struct TTarget3 : IMsBuildTargetName { public string Name => "TTarget3"; }
    private readonly struct TTarget4 : IMsBuildTargetName { public string Name => "TTarget4"; }
}
