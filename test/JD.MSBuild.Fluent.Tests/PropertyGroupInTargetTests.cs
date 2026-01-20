using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.Render;
using Xunit;
using Xunit.Abstractions;

namespace JD.MSBuild.Fluent.Tests;

public class PropertyGroupInTargetTests
{
    private readonly ITestOutputHelper _output;

    public PropertyGroupInTargetTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void PropertyGroup_WithCondition_InsideTarget_EmitsConditionAttribute()
    {
        // Arrange
        var project = Package.Define("TestPackage")
            .Targets(t =>
            {
                t.Target("TestTarget", target =>
                {
                    target.PropertyGroup("'$(MyProp)' == ''", group =>
                    {
                        group.Property("MyProp", "DefaultValue");
                    });
                });
            })
            .Build();

        // Act
        var renderer = new MsBuildXmlRenderer();
        var xml = renderer.RenderToString(project.Targets);
        _output.WriteLine("Generated XML:");
        _output.WriteLine(xml);

        // Assert
        Assert.Contains("<PropertyGroup Condition=\"'$(MyProp)' == ''\">", xml);
        Assert.Contains("<MyProp>DefaultValue</MyProp>", xml);
    }

    [Fact]
    public void PropertyGroup_WithoutCondition_InsideTarget_EmitsWithoutConditionAttribute()
    {
        // Arrange
        var project = Package.Define("TestPackage")
            .Targets(t =>
            {
                t.Target("TestTarget", target =>
                {
                    target.PropertyGroup(null, group =>
                    {
                        group.Property("MyProp", "Value");
                    });
                });
            })
            .Build();

        // Act
        var renderer = new MsBuildXmlRenderer();
        var xml = renderer.RenderToString(project.Targets);
        _output.WriteLine("Generated XML:");
        _output.WriteLine(xml);

        // Assert
        Assert.Contains("<PropertyGroup>", xml);
        Assert.DoesNotContain("<PropertyGroup Condition=", xml);
        Assert.Contains("<MyProp>Value</MyProp>", xml);
    }
}
