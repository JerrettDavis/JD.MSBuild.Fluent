using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.IR;
using JD.MSBuild.Fluent.Typed;
using TinyBDD.Xunit;
using Xunit.Abstractions;
using static JD.MSBuild.Fluent.Typed.MsBuildExpr;

namespace JD.MSBuild.Fluent.Tests;

public sealed class BddFluentApiTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public async Task DefineBasicPackage()
    {
        await Given("a package ID", CreatePackageId)
            .When("defining with Package.Define", DefinePackageFromId)
            .Then("package ID should be set", VerifyPackageId)
            .AssertPassed();
    }

    [Fact]
    public async Task AddPropertiesToPackage()
    {
        await Given("a package builder", CreateTestPackageBuilder)
            .When("adding properties", AddProperty)
            .Then("property exists in build props", VerifyPropertyExists)
            .AssertPassed();
    }

    [Fact]
    public async Task AddItemsToPackage()
    {
        await Given("a package builder", CreateTestPackageBuilder)
            .When("adding items", AddNoneItem)
            .Then("item exists in build props", VerifyItemExists)
            .AssertPassed();
    }

    [Fact]
    public async Task AddTargetsWithMessageTask()
    {
        await Given("a package builder", CreateTestPackageBuilder)
            .When("adding target with Message", AddMessageTarget)
            .Then("target exists in build targets", VerifyTargetExists)
            .AssertPassed();
    }

    [Fact]
    public async Task ChainPropsAndTargets()
    {
        await Given("package definition", CreateTestPackageBuilder)
            .When("chaining Props and Targets", ChainPropsAndTargetsBuilder)
            .Then("has both props and targets", VerifyBothPropsAndTargets)
            .AssertPassed();
    }

    [Fact]
    public async Task UseMsBuildExprConditionals()
    {
        await Given("condition helper", CreateCondition)
            .When("applied to property", ApplyConditionToProperty)
            .Then("condition contains property name", VerifyConditionContainsProperty)
            .AssertPassed();
    }

    [Fact]
    public async Task ConfigurePackagingOptions()
    {
        await Given("package builder", CreateTestPackageBuilder)
            .When("setting PackagingOptions", ConfigurePackaging)
            .Then("options are set", VerifyPackagingOptions)
            .AssertPassed();
    }

    #region Helper Methods - Given

    private static string CreatePackageId() => "MyPackage";
    private static PackageBuilder CreateTestPackageBuilder() => Package.Define("Test");
    private static string CreateCondition() => IsTrue<TProp>();

    #endregion

    #region Helper Methods - When

    private static PackageDefinition DefinePackageFromId(string id) => 
        Package.Define(id).Build();

    private static PackageDefinition AddProperty(PackageBuilder builder) => 
        builder.Props(p => p.Property<TProp>("Val")).Build();

    private static PackageDefinition AddNoneItem(PackageBuilder builder) =>
        builder.Props(p => p.Item<MsBuildItemTypes.None>(MsBuildItemOperation.Include, "file.txt")).Build();

    private static PackageDefinition AddMessageTarget(PackageBuilder builder) =>
        builder.Targets(t => t.Target<TTarget>(tgt => tgt.Message("Hi"))).Build();

    private static PackageDefinition ChainPropsAndTargetsBuilder(PackageBuilder builder) =>
        builder
            .Props(p => p.Property<TProp>("V"))
            .Targets(t => t.Target<TTarget>(tgt => tgt.Message("M")))
            .Build();

    private static PackageDefinition ApplyConditionToProperty(string condition) =>
        Package.Define("Test")
            .Props(p => p.Property<TProp>("V", condition))
            .Build();

    private static PackageDefinition ConfigurePackaging(PackageBuilder builder) =>
        builder
            .Pack(o => { o.BuildTransitive = true; o.EmitSdk = true; })
            .Build();

    #endregion

    #region Helper Methods - Then

    private static bool VerifyPackageId(PackageDefinition def)
    {
        Assert.Equal("MyPackage", def.Id);
        return true;
    }

    private static bool VerifyPropertyExists(PackageDefinition def)
    {
        var props = def.GetBuildProps();
        var hasProperty = props.PropertyGroups.Any(pg => 
            pg.Properties.Any(p => p.Name == "TProp"));
        
        Assert.True(hasProperty, "the TProp property should exist");
        return true;
    }

    private static bool VerifyItemExists(PackageDefinition def)
    {
        var props = def.GetBuildProps();
        var hasItem = props.ItemGroups.Any(ig => 
            ig.Items.Any(i => i.Spec == "file.txt"));
        
        Assert.True(hasItem, "the file.txt item should exist");
        return true;
    }

    private static bool VerifyTargetExists(PackageDefinition def)
    {
        var targets = def.GetBuildTargets();
        var hasTarget = targets.Targets.Any(t => t.Name == "TTarget");
        
        Assert.True(hasTarget, "the TTarget should exist");
        return true;
    }

    private static bool VerifyBothPropsAndTargets(PackageDefinition def)
    {
        var hasProps = def.GetBuildProps().PropertyGroups.Any();
        var hasTargets = def.GetBuildTargets().Targets.Any();
        
        Assert.True(hasProps, "properties should exist");
        Assert.True(hasTargets, "targets should exist");
        
        return hasProps && hasTargets;
    }

    private static bool VerifyConditionContainsProperty(PackageDefinition def)
    {
        var prop = def.GetBuildProps().PropertyGroups.First().Properties.First();
        var containsProperty = prop.Condition?.Contains("TProp") == true;
        
        Assert.True(containsProperty, "condition should reference TProp");
        return true;
    }

    private static bool VerifyPackagingOptions(PackageDefinition def)
    {
        Assert.True(def.Packaging.BuildTransitive);
        Assert.True(def.Packaging.EmitSdk);
        return true;
    }

    #endregion

    private readonly struct TProp : IMsBuildPropertyName { public string Name => "TProp"; }
    private readonly struct TTarget : IMsBuildTargetName { public string Name => "TTarget"; }
}
