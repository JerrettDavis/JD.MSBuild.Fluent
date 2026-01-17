using FluentAssertions;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.IR;
using JD.MSBuild.Fluent.Typed;
using TinyBDD.Xunit;
using Xunit.Abstractions;
using static JD.MSBuild.Fluent.Typed.MsBuildExpr;

namespace JD.MSBuild.Fluent.Tests;

/// <summary>Feature: Fluent API - Package Definition</summary>
public sealed class BddFluentApiTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public async Task Scenario_Define_basic_package()
    {
        await Given("a package ID", () => "MyPackage")
            .When("defining with Package.Define", id => Package.Define(id).Build())
            .Then("package ID should be set", def => def.Id == "MyPackage")
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Add_properties_to_package()
    {
        await Given("a package builder", () => Package.Define("Test"))
            .When("adding properties", b => b.Props(p => p.Property<TProp>("Val")).Build())
            .Then("property exists in build props", d =>
            {
                var props = d.GetBuildProps();
                return props.PropertyGroups.Any(pg => pg.Properties.Any(p => p.Name == "TProp"));
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Add_items_to_package()
    {
        await Given("a package builder", () => Package.Define("Test"))
            .When("adding items", b => b.Props(p => p.Item<MsBuildItemTypes.None>(MsBuildItemOperation.Include, "file.txt")).Build())
            .Then("item exists in build props", d =>
            {
                var props = d.GetBuildProps();
                return props.ItemGroups.Any(ig => ig.Items.Any(i => i.Spec == "file.txt"));
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Add_targets_with_Message_task()
    {
        await Given("a package builder", () => Package.Define("Test"))
            .When("adding target with Message", b => b.Targets(t => t.Target<TTarget>(tgt => tgt.Message("Hi"))).Build())
            .Then("target exists in build targets", d =>
            {
                var targets = d.GetBuildTargets();
                return targets.Targets.Any(t => t.Name == "TTarget");
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Chain_Props_and_Targets()
    {
        await Given("package definition", () => Package.Define("Test"))
            .When("chaining Props and Targets", b => b
                .Props(p => p.Property<TProp>("V"))
                .Targets(t => t.Target<TTarget>(tgt => tgt.Message("M")))
                .Build())
            .Then("has both props and targets", d =>
            {
                var hasProps = d.GetBuildProps().PropertyGroups.Any();
                var hasTargets = d.GetBuildTargets().Targets.Any();
                return hasProps && hasTargets;
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Use_MsBuildExpr_conditionals()
    {
        await Given("condition helper", () => IsTrue<TProp>())
            .When("applied to property", cond => Package.Define("Test")
                .Props(p => p.Property<TProp>("V", cond))
                .Build())
            .Then("condition contains property name", d =>
            {
                var prop = d.GetBuildProps().PropertyGroups.First().Properties.First();
                return prop.Condition?.Contains("TProp") == true;
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Configure_packaging_options()
    {
        await Given("package builder", () => Package.Define("Test"))
            .When("setting PackagingOptions", b => b
                .Pack(o => { o.BuildTransitive = true; o.EmitSdk = true; })
                .Build())
            .Then("options are set", d =>
            {
                return d.Packaging.BuildTransitive && d.Packaging.EmitSdk;
            })
            .AssertPassed();
    }

    private readonly struct TProp : IMsBuildPropertyName { public string Name => "TProp"; }
    private readonly struct TTarget : IMsBuildTargetName { public string Name => "TTarget"; }
}
