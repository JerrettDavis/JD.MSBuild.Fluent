using System.Reflection;
using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using JD.MSBuild.Fluent.Cli;
using JD.MSBuild.Fluent.Packaging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace JD.MSBuild.Fluent.IntegrationTests;

/// <summary>
/// Integration tests for XML-to-Fluent scaffolding round-trip conversion.
/// Tests that XML -> Fluent -> XML produces functionally equivalent output.
/// </summary>
public class ScaffoldingRoundTripTests
{
    private readonly string _tempDir;

    public ScaffoldingRoundTripTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"jdmsbuild-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void RoundTrip_SimplePropsFile_ProducesEquivalentXml()
    {
        // Arrange - Create original XML
        var originalXml = @"<Project>
  <PropertyGroup>
    <TestProperty>TestValue</TestProperty>
    <TestVersion>1.0.0</TestVersion>
  </PropertyGroup>
  
  <PropertyGroup Condition=""'$(Configuration)' == 'Release'"">
    <TestOptimize>true</TestOptimize>
  </PropertyGroup>
</Project>";

        var xmlPath = Path.Combine(_tempDir, "Original.props");
        File.WriteAllText(xmlPath, originalXml);

        // Act - Convert XML to Fluent
        var scaffolder = new XmlToFluentScaffolder();
        var fluentCode = scaffolder.Scaffold(xmlPath, "TestPackage", "TestFactory");

        var csPath = Path.Combine(_tempDir, "TestFactory.cs");
        File.WriteAllText(csPath, fluentCode);

        // Compile and execute the fluent code
        var definition = CompileAndExecuteFactory(csPath, "TestPackage.TestFactory", "Create");

        // Generate XML back from fluent definition
        var outputDir = Path.Combine(_tempDir, "output");
        var emitter = new MsBuildPackageEmitter();
        emitter.Emit(definition, outputDir);

        var generatedXmlPath = Path.Combine(outputDir, "build", "TestPackage.props");

        // Assert - Compare original and generated XML semantically
        File.Exists(generatedXmlPath).Should().BeTrue("generated XML should exist");

        var generatedDoc = XDocument.Load(generatedXmlPath);

        // Compare property values
        AssertPropertyEquals(generatedDoc, "TestProperty", "TestValue");
        AssertPropertyEquals(generatedDoc, "TestVersion", "1.0.0");
        AssertConditionalPropertyEquals(generatedDoc, "TestOptimize", "true", "Configuration");
    }

    [Fact]
    public void RoundTrip_SimpleTargetsFile_ProducesEquivalentXml()
    {
        // Arrange - Create original XML
        var originalXml = @"<Project>
  <Target Name=""TestTarget"" BeforeTargets=""Build"" Condition=""'$(TestEnabled)' == 'true'"">
    <Message Text=""Running test target"" Importance=""High"" />
    <Exec Command=""echo Hello"" />
  </Target>
</Project>";

        var xmlPath = Path.Combine(_tempDir, "Original.targets");
        File.WriteAllText(xmlPath, originalXml);

        // Act - Convert XML to Fluent
        var scaffolder = new XmlToFluentScaffolder();
        var fluentCode = scaffolder.Scaffold(xmlPath, "TestPackage", "TestFactory");

        var csPath = Path.Combine(_tempDir, "TestFactory.cs");
        File.WriteAllText(csPath, fluentCode);

        // Compile and execute the fluent code
        var definition = CompileAndExecuteFactory(csPath, "TestPackage.TestFactory", "Create");

        // Generate XML back from fluent definition
        var outputDir = Path.Combine(_tempDir, "output");
        var emitter = new MsBuildPackageEmitter();
        emitter.Emit(definition, outputDir);

        var generatedXmlPath = Path.Combine(outputDir, "build", "TestPackage.targets");

        // Assert
        File.Exists(generatedXmlPath).Should().BeTrue("generated XML should exist");

        var generatedDoc = XDocument.Load(generatedXmlPath);
        var target = generatedDoc.Root!.Elements("Target").FirstOrDefault(t => t.Attribute("Name")?.Value == "TestTarget");

        target.Should().NotBeNull("TestTarget should exist");
        target!.Attribute("BeforeTargets")?.Value.Should().Be("Build");
        target.Attribute("Condition")?.Value.Should().Contain("TestEnabled");

        var message = target.Elements("Message").FirstOrDefault();
        message.Should().NotBeNull("Message task should exist");
        message!.Attribute("Text")?.Value.Should().Be("Running test target");
        message.Attribute("Importance")?.Value.Should().Be("High");

        var exec = target.Elements("Exec").FirstOrDefault();
        exec.Should().NotBeNull("Exec task should exist");
        exec!.Attribute("Command")?.Value.Should().Be("echo Hello");
    }

    [Fact]
    public void RoundTrip_ComplexFile_WithChooseAndItems_ProducesEquivalentXml()
    {
        // Arrange - Create original XML with Choose/When/Otherwise and ItemGroups
        var originalXml = @"<Project>
  <PropertyGroup>
    <PackageEnabled>true</PackageEnabled>
  </PropertyGroup>
  
  <Choose>
    <When Condition=""$([MSBuild]::IsOSPlatform('Windows'))"">
      <PropertyGroup>
        <Platform>Windows</Platform>
      </PropertyGroup>
    </When>
    <Otherwise>
      <PropertyGroup>
        <Platform>Unix</Platform>
      </PropertyGroup>
    </Otherwise>
  </Choose>
  
  <ItemGroup>
    <None Include=""README.md"">
      <Pack>true</Pack>
      <PackagePath>docs/</PackagePath>
    </None>
    <Content Include=""assets/**/*.json"" Exclude=""assets/**/*.tmp"" />
  </ItemGroup>
</Project>";

        var xmlPath = Path.Combine(_tempDir, "Complex.props");
        File.WriteAllText(xmlPath, originalXml);

        // Act - Convert XML to Fluent and back
        var scaffolder = new XmlToFluentScaffolder();
        var fluentCode = scaffolder.Scaffold(xmlPath, "ComplexPackage", "ComplexFactory");

        var csPath = Path.Combine(_tempDir, "ComplexFactory.cs");
        File.WriteAllText(csPath, fluentCode);

        var definition = CompileAndExecuteFactory(csPath, "ComplexPackage.ComplexFactory", "Create");

        var outputDir = Path.Combine(_tempDir, "complex-output");
        var emitter = new MsBuildPackageEmitter();
        emitter.Emit(definition, outputDir);

        var generatedXmlPath = Path.Combine(outputDir, "build", "ComplexPackage.props");

        // Assert
        File.Exists(generatedXmlPath).Should().BeTrue("generated XML should exist");

        var generatedDoc = XDocument.Load(generatedXmlPath);

        // Check property
        AssertPropertyEquals(generatedDoc, "PackageEnabled", "true");

        // Check Choose structure exists
        var choose = generatedDoc.Root!.Elements("Choose").FirstOrDefault();
        choose.Should().NotBeNull("Choose element should exist");

        var when = choose!.Elements("When").FirstOrDefault();
        when.Should().NotBeNull("When element should exist");
        when!.Attribute("Condition")?.Value.Should().Contain("IsOSPlatform");

        // Check items
        var itemGroups = generatedDoc.Root!.Elements("ItemGroup");
        itemGroups.Should().NotBeEmpty("ItemGroup elements should exist");

        var noneItem = itemGroups.SelectMany(ig => ig.Elements("None")).FirstOrDefault();
        noneItem.Should().NotBeNull("None item should exist");
        noneItem!.Attribute("Include")?.Value.Should().Be("README.md");

        var packMeta = noneItem.Elements("Pack").FirstOrDefault();
        packMeta.Should().NotBeNull("Pack metadata should exist");
        packMeta!.Value.Should().Be("true");
    }

    [Fact]
    public void RoundTrip_UsingTaskAndCustomTask_ProducesEquivalentXml()
    {
        // Arrange - Create XML with UsingTask and custom task invocation
        var originalXml = @"<Project>
  <UsingTask TaskName=""MyCustomTask"" AssemblyFile=""$(MSBuildThisFileDirectory)\..\tasks\MyTask.dll"" />
  
  <Target Name=""RunCustomTask"" AfterTargets=""Build"">
    <PropertyGroup>
      <TempPath>$(MSBuildProjectDirectory)\obj\temp</TempPath>
    </PropertyGroup>
    
    <MyCustomTask InputFiles=""@(Compile)"" OutputPath=""$(TempPath)"">
      <Output TaskParameter=""GeneratedFiles"" ItemName=""CustomGenerated"" />
    </MyCustomTask>
  </Target>
</Project>";

        var xmlPath = Path.Combine(_tempDir, "CustomTask.targets");
        File.WriteAllText(xmlPath, originalXml);

        // Act
        var scaffolder = new XmlToFluentScaffolder();
        var fluentCode = scaffolder.Scaffold(xmlPath, "CustomTaskPackage", "CustomTaskFactory");

        var csPath = Path.Combine(_tempDir, "CustomTaskFactory.cs");
        File.WriteAllText(csPath, fluentCode);

        var definition = CompileAndExecuteFactory(csPath, "CustomTaskPackage.CustomTaskFactory", "Create");

        var outputDir = Path.Combine(_tempDir, "customtask-output");
        var emitter = new MsBuildPackageEmitter();
        emitter.Emit(definition, outputDir);

        var generatedXmlPath = Path.Combine(outputDir, "build", "CustomTaskPackage.targets");

        // Assert
        File.Exists(generatedXmlPath).Should().BeTrue("generated XML should exist");

        var generatedDoc = XDocument.Load(generatedXmlPath);

        // Check UsingTask
        var usingTask = generatedDoc.Root!.Elements("UsingTask").FirstOrDefault(ut => ut.Attribute("TaskName")?.Value == "MyCustomTask");
        usingTask.Should().NotBeNull("UsingTask should exist");
        usingTask!.Attribute("AssemblyFile")?.Value.Should().Contain("MyTask.dll");

        // Check Target with custom task
        var target = generatedDoc.Root!.Elements("Target").FirstOrDefault(t => t.Attribute("Name")?.Value == "RunCustomTask");
        target.Should().NotBeNull("RunCustomTask target should exist");

        var customTask = target!.Elements("MyCustomTask").FirstOrDefault();
        customTask.Should().NotBeNull("MyCustomTask invocation should exist");
        customTask!.Attribute("InputFiles")?.Value.Should().Be("@(Compile)");
        customTask.Attribute("OutputPath")?.Value.Should().Be("$(TempPath)");

        // Check Output element
        var output = customTask.Elements("Output").FirstOrDefault();
        output.Should().NotBeNull("Output element should exist");
        output!.Attribute("TaskParameter")?.Value.Should().Be("GeneratedFiles");
        output.Attribute("ItemName")?.Value.Should().Be("CustomGenerated");
    }

    private PackageDefinition CompileAndExecuteFactory(string csFilePath, string typeName, string methodName)
    {
        // Load the generated C# code and compile it in-memory
        var code = File.ReadAllText(csFilePath);

        // Get all necessary assembly references
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(PackageDefinition).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Linq").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location)
        };

        // Add reference to mscorlib for .NET Framework compatibility
        try
        {
            references.Add(MetadataReference.CreateFromFile(Assembly.Load("mscorlib").Location));
        }
        catch
        {
            // mscorlib not available in .NET Core/5+, that's okay
        }

        // Use Roslyn to compile the code
        var compilation = CSharpCompilation.Create(
            $"DynamicAssembly_{Guid.NewGuid():N}",
            new[] { CSharpSyntaxTree.ParseText(code) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var failures = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
            var errorMessages = string.Join("\n", failures.Select(f => $"{f.Id}: {f.GetMessage()} at {f.Location.GetLineSpan()}"));
            
            // Write the code to a temp file for debugging
            var debugPath = Path.Combine(_tempDir, "failed-compilation.cs");
            File.WriteAllText(debugPath, code);
            
            throw new InvalidOperationException($"Compilation failed. Code written to: {debugPath}\n\nErrors:\n{errorMessages}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());

        var type = assembly.GetType(typeName) ?? throw new InvalidOperationException($"Type '{typeName}' not found in assembly. Available types: {string.Join(", ", assembly.GetTypes().Select(t => t.FullName))}");
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static) 
            ?? throw new InvalidOperationException($"Method '{methodName}' not found on type '{typeName}'. Available methods: {string.Join(", ", type.GetMethods(BindingFlags.Public | BindingFlags.Static).Select(m => m.Name))}");

        var definition = method.Invoke(null, null) as PackageDefinition
            ?? throw new InvalidOperationException("Factory method did not return PackageDefinition");

        return definition;
    }

    private void AssertPropertyEquals(XDocument generatedDoc, string propertyName, string expectedValue)
    {
        var generatedProp = generatedDoc.Root!
            .Elements("PropertyGroup")
            .SelectMany(pg => pg.Elements(propertyName))
            .FirstOrDefault();

        generatedProp.Should().NotBeNull($"Property '{propertyName}' should exist in generated XML");
        generatedProp!.Value.Should().Be(expectedValue, $"Property '{propertyName}' should have correct value");
    }

    private void AssertConditionalPropertyEquals(XDocument generatedDoc, 
        string propertyName, string expectedValue, string conditionContains)
    {
        var generatedPropGroup = generatedDoc.Root!
            .Elements("PropertyGroup")
            .FirstOrDefault(pg => pg.Attribute("Condition")?.Value.Contains(conditionContains) == true);

        generatedPropGroup.Should().NotBeNull($"Conditional PropertyGroup with '{conditionContains}' should exist");

        var generatedProp = generatedPropGroup!.Elements(propertyName).FirstOrDefault();
        generatedProp.Should().NotBeNull($"Property '{propertyName}' should exist in conditional group");
        generatedProp!.Value.Should().Be(expectedValue, $"Property '{propertyName}' should have correct value");
    }
}
