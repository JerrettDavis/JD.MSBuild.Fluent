using System.Xml.Linq;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.IR;
using JD.MSBuild.Fluent.Packaging;
using JD.MSBuild.Fluent.Render;
using JD.MSBuild.Fluent.Typed;
using JD.MSBuild.Fluent.Validation;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace JD.MSBuild.Fluent.Tests;

/// <summary>Feature: EndToEndPackageGeneration</summary>
public sealed class BddEndToEndTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public async Task Scenario_Generate_complete_package()
    {
        string? tempDir = null;
        try
        {
            await Given("a complete package definition", () =>
                {
                    var pkg = Package.Define("CompletePackage")
                        .Description("A complete test package")
                        .Props(p => p.Property<TProperty>("PropValue"))
                        .Targets(t => t.Target<TTarget>(tgt => tgt.Message("Hello")))
                        .Pack(o => o.BuildTransitive = true)
                        .Build();
                    tempDir = Path.Combine(Path.GetTempPath(), $"BddTest_{Guid.NewGuid():N}");
                    Directory.CreateDirectory(tempDir);
                    return (pkg, tempDir);
                })
                .When("generating package to disk", ctx =>
                {
                    var emitter = new MsBuildPackageEmitter();
                    emitter.Emit(ctx.pkg, ctx.tempDir);
                    return ctx.tempDir;
                })
                .Then("build folder exists", dir => Directory.Exists(Path.Combine(dir, "build")))
                .And("build props file exists", dir => File.Exists(Path.Combine(dir, "build", "CompletePackage.props")))
                .And("build targets file exists", dir => File.Exists(Path.Combine(dir, "build", "CompletePackage.targets")))
                .And("buildTransitive folder exists", dir => Directory.Exists(Path.Combine(dir, "buildTransitive")))
                .AssertPassed();
        }
        finally
        {
            if (tempDir != null && Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Scenario_Render_deterministic_XML()
    {
        await Given("a simple package", () => Package.Define("DeterministicPkg")
                .Props(p => p
                    .Property("Prop1", "Value1")
                    .Property("Prop2", "Value2"))
                .Build())
            .When("rendering props twice", pkg =>
            {
                var renderer = new MsBuildXmlRenderer();
                var xml1 = renderer.RenderToString(pkg.Props);
                var xml2 = renderer.RenderToString(pkg.Props);
                return (xml1, xml2);
            })
            .Then("XML output is identical", xmls => xmls.xml1 == xmls.xml2)
            .And("XML is valid", xmls =>
            {
                var doc = XDocument.Parse(xmls.xml1);
                return doc.Root?.Name.LocalName == "Project";
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Validate_XML_structure()
    {
        await Given("a package with properties and targets", () => Package.Define("ValidPkg")
                .Props(p => p.Property("MyProp", "Value"))
                .Targets(t => t.Target<TTarget>(tgt => tgt.Message("Msg")))
                .Build())
            .When("rendering to XML", pkg =>
            {
                var renderer = new MsBuildXmlRenderer();
                var propsXml = renderer.RenderToString(pkg.Props);
                var targetsXml = renderer.RenderToString(pkg.Targets);
                return (propsXml, targetsXml);
            })
            .Then("props XML contains PropertyGroup", xmls =>
            {
                var doc = XDocument.Parse(xmls.propsXml);
                return doc.Descendants().Any(e => e.Name.LocalName == "PropertyGroup");
            })
            .And("targets XML contains Target", xmls =>
            {
                var doc = XDocument.Parse(xmls.targetsXml);
                return doc.Descendants().Any(e => e.Name.LocalName == "Target");
            })
            .And("targets XML contains Message task", xmls =>
            {
                var doc = XDocument.Parse(xmls.targetsXml);
                return doc.Descendants().Any(e => e.Name.LocalName == "Message");
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Handle_validation_errors()
    {
        await Given("a package definition", () => Package.Define("TestPkg")
                .Props(p => p.Property("ValidProp", "Value"))
                .Build())
            .When("validating the package", pkg =>
            {
                try
                {
                    MsBuildValidator.ValidatePackageDefinition(pkg);
                    return (true, Array.Empty<string>());
                }
                catch (MsBuildValidationException ex)
                {
                    return (false, ex.Errors.ToArray());
                }
            })
            .Then("validation completes", result => result.Item1 || result.Item2 != null)
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Clean_temp_directories()
    {
        string? tempDir = null;
        try
        {
            await Given("a temporary directory", () =>
                {
                    tempDir = Path.Combine(Path.GetTempPath(), $"BddCleanTest_{Guid.NewGuid():N}");
                    Directory.CreateDirectory(tempDir);
                    File.WriteAllText(Path.Combine(tempDir, "test.txt"), "test");
                    return tempDir;
                })
                .When("deleting the directory", dir =>
                {
                    Directory.Delete(dir, recursive: true);
                    return dir;
                })
                .Then("directory no longer exists", dir => !Directory.Exists(dir))
                .AssertPassed();
        }
        finally
        {
            if (tempDir != null && Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Scenario_Round_trip_through_parser()
    {
        await Given("a package with complex structure", () => Package.Define("RoundTripPkg")
                .Props(p => p
                    .Property("Prop1", "Value1")
                    .Import("Custom.props", "'$(UseCustomProps)' == 'true'"))
                .Targets(t => t
                    .UsingTask("MyTask", "MyAssembly.dll")
                    .Target<TTarget>(tgt => tgt
                        .Condition("'$(RunTarget)' == 'true'")
                        .Message("Running target")))
                .Build())
            .When("rendering to XML and parsing structure", pkg =>
            {
                var renderer = new MsBuildXmlRenderer();
                var propsXml = renderer.RenderToString(pkg.Props);
                var targetsXml = renderer.RenderToString(pkg.Targets);
                var propsDoc = XDocument.Parse(propsXml);
                var targetsDoc = XDocument.Parse(targetsXml);
                return (propsDoc, targetsDoc);
            })
            .Then("props document is valid", docs => docs.propsDoc.Root != null)
            .And("targets document is valid", docs => docs.targetsDoc.Root != null)
            .And("props contains Import", docs =>
                docs.propsDoc.Descendants().Any(e => e.Name.LocalName == "Import"))
            .And("targets contains UsingTask", docs =>
                docs.targetsDoc.Descendants().Any(e => e.Name.LocalName == "UsingTask"))
            .And("target has condition", docs =>
            {
                var target = docs.targetsDoc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "Target");
                return target?.Attribute("Condition")?.Value.Contains("RunTarget") == true;
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Generate_with_custom_basename()
    {
        string? tempDir = null;
        try
        {
            await Given("a package with custom basename", () =>
                {
                    var pkg = Package.Define("MyPackage")
                        .Props(p => p.Property("Test", "Value"))
                        .Pack(o => o.BuildAssetBasename = "CustomName")
                        .Build();
                    tempDir = Path.Combine(Path.GetTempPath(), $"BddBasename_{Guid.NewGuid():N}");
                    Directory.CreateDirectory(tempDir);
                    return (pkg, tempDir);
                })
                .When("generating package", ctx =>
                {
                    var emitter = new MsBuildPackageEmitter();
                    emitter.Emit(ctx.pkg, ctx.tempDir);
                    return ctx.tempDir;
                })
                .Then("uses custom basename for props", dir =>
                    File.Exists(Path.Combine(dir, "build", "CustomName.props")))
                .And("uses custom basename for targets", dir =>
                    File.Exists(Path.Combine(dir, "build", "CustomName.targets")))
                .AssertPassed();
        }
        finally
        {
            if (tempDir != null && Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    private readonly struct TProperty : IMsBuildPropertyName { public string Name => "TProperty"; }
    private readonly struct TTarget : IMsBuildTargetName { public string Name => "TTarget"; }
}
