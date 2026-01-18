using System.Reflection;
using System.Xml.Linq;
using FluentAssertions;
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Cli;
using JD.MSBuild.Fluent.Render;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace JD.MSBuild.Fluent.RealWorldTests;

/// <summary>
/// Real-world integration tests using JD.Efcpt.Build as validation.
/// Tests the full round-trip: XML -> Fluent Code -> Compile -> Generate XML -> Compare
/// 
/// NOTE: These tests require JD.Efcpt.Build to be cloned in the parent directory.
/// They will be skipped if the repository is not available (e.g., in CI).
/// </summary>
public class EfcptBuildRoundTripTests
{
    private readonly string _efcptBuildPath;
    private readonly string _tempDir;
    private readonly bool _isAvailable;

    public EfcptBuildRoundTripTests()
    {
        // Find JD.Efcpt.Build repository
        _efcptBuildPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", "..", "..", "..", "..", "..", "JD.Efcpt.Build");
        _efcptBuildPath = Path.GetFullPath(_efcptBuildPath);
        
        // Check if repository is available
        _isAvailable = Directory.Exists(_efcptBuildPath) && 
                      Directory.Exists(Path.Combine(_efcptBuildPath, "src", "JD.Efcpt.Build"));
        
        _tempDir = Path.Combine(Path.GetTempPath(), $"jdmsbuild-integration-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact(Skip = "Requires JD.Efcpt.Build repository - runs locally only")]
    public void RoundTrip_EfcptBuild_BuildTransitiveTargets_ProducesSameXml()
    {
        // Skip if repository not available
        if (!_isAvailable)
        {
            // This will never execute due to Skip attribute, but keeping for clarity
            return;
        }
        // Arrange - Get original XML
        var originalXmlPath = Path.Combine(_efcptBuildPath, "src", "JD.Efcpt.Build", "buildTransitive", "JD.Efcpt.Build.targets");
        
        if (!File.Exists(originalXmlPath))
        {
            throw new FileNotFoundException($"JD.Efcpt.Build not found at: {originalXmlPath}. Clone it to parent directory.");
        }

        var originalXml = XDocument.Load(originalXmlPath);

        // Act - Scaffold to fluent code
        var scaffolder = new XmlToFluentScaffolder();
        var fluentCode = scaffolder.Scaffold(originalXmlPath, "JD.Efcpt.Build", "BuildTransitiveTargetsFactory");

        // Save for inspection
        var fluentCodePath = Path.Combine(_tempDir, "BuildTransitiveTargetsFactory.cs");
        File.WriteAllText(fluentCodePath, fluentCode);

        // Compile the fluent code
        var packageDef = CompileAndExecuteFactory(fluentCodePath, "JDEfcptBuild.BuildTransitiveTargetsFactory", "Create");

        // Generate XML back
        var renderer = new MsBuildXmlRenderer();
        var generatedTargetsXml = renderer.RenderToString(packageDef.GetBuildTargets());
        var generatedXml = XDocument.Parse(generatedTargetsXml);

        // Save for comparison
        var generatedXmlPath = Path.Combine(_tempDir, "Generated.targets");
        generatedXml.Save(generatedXmlPath);

        // Assert - Compare key elements (semantic comparison, not text)
        AssertXmlSemanticallyEquivalent(originalXml, generatedXml);
    }

    [Fact(Skip = "Requires JD.Efcpt.Build repository - runs locally only")]
    public void RoundTrip_EfcptBuild_BuildTransitiveProps_ProducesSameXml()
    {
        // Skip if repository not available
        if (!_isAvailable)
        {
            // This will never execute due to Skip attribute, but keeping for clarity
            return;
        }
        // Arrange
        var originalXmlPath = Path.Combine(_efcptBuildPath, "src", "JD.Efcpt.Build", "buildTransitive", "JD.Efcpt.Build.props");
        
        if (!File.Exists(originalXmlPath))
        {
            throw new FileNotFoundException($"JD.Efcpt.Build not found at: {originalXmlPath}");
        }

        var originalXml = XDocument.Load(originalXmlPath);

        // Act
        var scaffolder = new XmlToFluentScaffolder();
        var fluentCode = scaffolder.Scaffold(originalXmlPath, "JD.Efcpt.Build", "BuildTransitivePropsFactory");

        var fluentCodePath = Path.Combine(_tempDir, "BuildTransitivePropsFactory.cs");
        File.WriteAllText(fluentCodePath, fluentCode);

        var packageDef = CompileAndExecuteFactory(fluentCodePath, "JDEfcptBuild.BuildTransitivePropsFactory", "Create");

        var renderer = new MsBuildXmlRenderer();
        var generatedPropsXml = renderer.RenderToString(packageDef.GetBuildProps());
        var generatedXml = XDocument.Parse(generatedPropsXml);

        var generatedXmlPath = Path.Combine(_tempDir, "Generated.props");
        generatedXml.Save(generatedXmlPath);

        // Assert
        AssertXmlSemanticallyEquivalent(originalXml, generatedXml);
    }

    private PackageDefinition CompileAndExecuteFactory(string csFilePath, string typeName, string methodName)
    {
        var code = File.ReadAllText(csFilePath);

        // Compile using Roslyn
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            $"DynamicAssembly_{Guid.NewGuid():N}",
            new[] { syntaxTree },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(PackageDefinition).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Linq").Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location)
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var emitResult = compilation.Emit(ms);

        if (!emitResult.Success)
        {
            var errors = string.Join("\n", emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString()));
            throw new InvalidOperationException($"Compilation failed:\n{errors}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());

        var type = assembly.GetType(typeName);
        type.Should().NotBeNull($"Type {typeName} should exist in compiled assembly");

        var method = type!.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        method.Should().NotBeNull($"Method {methodName} should exist");

        var result = method!.Invoke(null, null);
        result.Should().NotBeNull().And.BeOfType<PackageDefinition>();

        return (PackageDefinition)result!;
    }

    private void AssertXmlSemanticallyEquivalent(XDocument expected, XDocument actual)
    {
        var expectedRoot = expected.Root!;
        var actualRoot = actual.Root!;

        // Detect namespace (MSBuild may or may not use default namespace)
        XNamespace expectedNs = expectedRoot.Name.Namespace;
        XNamespace actualNs = actualRoot.Name.Namespace;

        // Compare root element
        expectedRoot.Name.LocalName.Should().Be(actualRoot.Name.LocalName);

        // Compare PropertyGroups
        var expectedPropGroups = expectedRoot.Elements(expectedNs + "PropertyGroup").ToList();
        var actualPropGroups = actualRoot.Elements(actualNs + "PropertyGroup").ToList();

        // We should have similar structure (exact count might differ due to grouping)
        actualPropGroups.Should().NotBeEmpty("Generated XML should have property groups");

        // Collect all properties (may have duplicates with different conditions)
        var expectedProps = expectedPropGroups
            .SelectMany(g => g.Elements())
            .Select(e => new { Name = e.Name.LocalName, Value = e.Value, Condition = e.Attribute("Condition")?.Value })
            .ToList();

        var actualProps = actualPropGroups
            .SelectMany(g => g.Elements())
            .Select(e => new { Name = e.Name.LocalName, Value = e.Value, Condition = e.Attribute("Condition")?.Value })
            .ToList();

        // Group by property name for comparison
        var expectedPropsByName = expectedProps.GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.ToList());
        var actualPropsByName = actualProps.GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.ToList());

        // All expected properties should exist in generated
        foreach (var (propName, expectedValues) in expectedPropsByName)
        {
            actualPropsByName.Should().ContainKey(propName, $"Property '{propName}' should exist in generated XML");
            
            var actualValues = actualPropsByName[propName];
            expectedValues.Count.Should().Be(actualValues.Count, $"Property '{propName}' should have same number of declarations");

            // Match each expected property declaration with an actual one (order may differ)
            foreach (var expectedProp in expectedValues)
            {
                actualValues.Should().Contain(
                    a => a.Value == expectedProp.Value && a.Condition == expectedProp.Condition,
                    $"Property '{propName}' with value '{expectedProp.Value}' and condition '{expectedProp.Condition}' should exist");
            }
        }

        // Compare Targets
        var expectedTargets = expectedRoot.Elements(expectedNs + "Target").ToList();
        var actualTargets = actualRoot.Elements(actualNs + "Target").ToList();

        expectedTargets.Count.Should().Be(actualTargets.Count, "Should have same number of targets");

        foreach (var expectedTarget in expectedTargets)
        {
            var targetName = expectedTarget.Attribute("Name")?.Value;
            targetName.Should().NotBeNullOrEmpty();

            var actualTarget = actualTargets.FirstOrDefault(t => t.Attribute("Name")?.Value == targetName);
            actualTarget.Should().NotBeNull($"Target '{targetName}' should exist in generated XML");

            // Compare target attributes
            CompareAttributes(expectedTarget, actualTarget!, targetName!);
        }

        // Compare UsingTasks
        var expectedUsingTasks = expectedRoot.Elements(expectedNs + "UsingTask").ToList();
        var actualUsingTasks = actualRoot.Elements(actualNs + "UsingTask").ToList();

        expectedUsingTasks.Count.Should().Be(actualUsingTasks.Count, "Should have same number of UsingTask declarations");

        foreach (var expectedTask in expectedUsingTasks)
        {
            var taskName = expectedTask.Attribute("TaskName")?.Value;
            var actualTask = actualUsingTasks.FirstOrDefault(t => t.Attribute("TaskName")?.Value == taskName);
            actualTask.Should().NotBeNull($"UsingTask '{taskName}' should exist");
        }
    }

    private void CompareAttributes(XElement expected, XElement actual, string elementName)
    {
        foreach (var attr in expected.Attributes())
        {
            if (attr.Name.LocalName == "xmlns") continue; // Skip namespace

            var actualAttr = actual.Attribute(attr.Name);
            actualAttr.Should().NotBeNull($"Attribute '{attr.Name}' on '{elementName}' should exist");
            actualAttr!.Value.Should().Be(attr.Value, $"Attribute '{attr.Name}' on '{elementName}' should match");
        }
    }
}
