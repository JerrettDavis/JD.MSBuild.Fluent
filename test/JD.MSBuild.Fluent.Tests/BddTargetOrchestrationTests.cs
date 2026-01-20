using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.Typed;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace JD.MSBuild.Fluent.Tests;

/// <summary>Feature: TargetOrchestration</summary>
public sealed class BddTargetOrchestrationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public async Task Scenario_Set_BeforeTargets()
    {
        await Given("a target builder", () => Package.Define("Test"))
            .When("setting BeforeTargets", b => b
                .Targets(t => t.Target<TTarget>(tgt => tgt
                    .BeforeTargets("Build")
                    .Message("Before build")))
                .Build())
            .Then("target has BeforeTargets attribute", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                return target.BeforeTargets == "Build";
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Set_AfterTargets()
    {
        await Given("a target builder", () => Package.Define("Test"))
            .When("setting AfterTargets", b => b
                .Targets(t => t.Target<TTarget>(tgt => tgt
                    .AfterTargets("CoreBuild")
                    .Message("After build")))
                .Build())
            .Then("target has AfterTargets attribute", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                return target.AfterTargets == "CoreBuild";
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Set_DependsOnTargets()
    {
        await Given("a target builder", () => Package.Define("Test"))
            .When("setting DependsOnTargets", b => b
                .Targets(t => t.Target<TTarget>(tgt => tgt
                    .DependsOnTargets("Restore;Compile")
                    .Message("Dependent")))
                .Build())
            .Then("target has DependsOnTargets attribute", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                return target.DependsOnTargets == "Restore;Compile";
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Combine_orchestration_attributes()
    {
        await Given("a target builder", () => Package.Define("Test"))
            .When("setting multiple orchestration attributes", b => b
                .Targets(t => t.Target<TTarget>(tgt => tgt
                    .BeforeTargets("Build")
                    .AfterTargets("PreBuild")
                    .DependsOnTargets("Restore")
                    .Message("Orchestrated")))
                .Build())
            .Then("all orchestration attributes are set", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                return target.BeforeTargets == "Build" &&
                       target.AfterTargets == "PreBuild" &&
                       target.DependsOnTargets == "Restore";
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Set_Inputs_and_Outputs()
    {
        await Given("a target builder", () => Package.Define("Test"))
            .When("setting Inputs and Outputs", b => b
                .Targets(t => t.Target<TTarget>(tgt => tgt
                    .Inputs("@(SourceFiles)")
                    .Outputs("@(TargetFiles)")
                    .Message("Incremental")))
                .Build())
            .Then("target has Inputs and Outputs", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                return target.Inputs == "@(SourceFiles)" &&
                       target.Outputs == "@(TargetFiles)";
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Add_target_condition()
    {
        await Given("a target builder", () => Package.Define("Test"))
            .When("setting a condition on target", b => b
                .Targets(t => t.Target<TTarget>(tgt => tgt
                    .Condition("'$(Configuration)' == 'Release'")
                    .Message("Conditional")))
                .Build())
            .Then("target has Condition", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                return target.Condition == "'$(Configuration)' == 'Release'";
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Use_strongly_typed_target_orchestration()
    {
        await Given("target names", () => (new TBeforeTarget(), new TAfterTarget(), new TDependsTarget()))
            .When("using strongly-typed orchestration", targets => Package.Define("Test")
                .Targets(t => t.Target<TTarget>(tgt => tgt
                    .BeforeTargets(targets.Item1)
                    .AfterTargets(targets.Item2)
                    .DependsOnTargets(targets.Item3)
                    .Message("Typed")))
                .Build())
            .Then("orchestration uses correct names", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                return target.BeforeTargets == "TBeforeTarget" &&
                       target.AfterTargets == "TAfterTarget" &&
                       target.DependsOnTargets == "TDependsTarget";
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Set_target_label()
    {
        await Given("a target builder", () => Package.Define("Test"))
            .When("setting Label attribute", b => b
                .Targets(t => t.Target<TTarget>(tgt => tgt
                    .Label("CustomLabel")
                    .Message("Labeled")))
                .Build())
            .Then("target has Label", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                return target.Label == "CustomLabel";
            })
            .AssertPassed();
    }

    private readonly struct TTarget : IMsBuildTargetName { public string Name => "TTarget"; }
    private readonly struct TBeforeTarget : IMsBuildTargetName { public string Name => "TBeforeTarget"; }
    private readonly struct TAfterTarget : IMsBuildTargetName { public string Name => "TAfterTarget"; }
    private readonly struct TDependsTarget : IMsBuildTargetName { public string Name => "TDependsTarget"; }
}
