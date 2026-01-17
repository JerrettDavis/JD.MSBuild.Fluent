# Best Practices

Patterns and recommendations for authoring robust, maintainable MSBuild packages with JD.MSBuild.Fluent.

## Overview

This guide covers best practices for package organization, property and target design, error handling, testing, and multi-target framework support.

## Package Organization

### Separation of Props and Targets

Keep evaluation-time logic (properties, items) separate from execution-time logic (targets, tasks).

**Props** (`.props` files):
- Default property values
- Item includes/excludes
- Imports of other props files
- Platform/configuration-specific settings
- No targets or task invocations

**Targets** (`.targets` files):
- Target definitions
- UsingTask declarations
- Task invocations
- Build orchestration
- No property/item definitions at project level (use PropertyGroup/ItemGroup inside targets)

```csharp
// ✅ Good - clear separation
Package.Define("Contoso.Build")
    .Props(props =>
    {
        // Evaluation-time: properties and items
        props.Property("ContosoEnabled", "true");
        props.Item<None>(MsBuildItemOperation.Include, "README.md");
    })
    .Targets(targets =>
    {
        // Execution-time: targets and tasks
        targets.Target("Contoso_Build", target =>
        {
            target.Message("Building...");
        });
    })
    .Build();
```

### Build vs BuildTransitive

**Direct Consumers Only** (`build/`):
- Use for package-specific settings that shouldn't propagate
- SDK resolver hooks
- Package-internal tooling paths

**Transitive Propagation** (`buildTransitive/`):
- Use for settings that should flow through dependency graph
- Shared defaults (e.g., LangVersion, Nullable)
- Common item definitions

```csharp
// ✅ Good - buildTransitive for shared defaults
Package.Define("Contoso.Standards")
    .BuildTransitiveProps(props =>
    {
        props.Property("LangVersion", "latest");
        props.Property("Nullable", "enable");
        props.Property("TreatWarningsAsErrors", "true");
    })
    .Pack(o => o.BuildTransitive = true)
    .Build();

// ✅ Good - build/ for package-internal paths
Package.Define("Contoso.Tools")
    .BuildProps(props =>
    {
        props.Property("ContosoToolsPath", 
            "$(MSBuildThisFileDirectory)../../tools");
    })
    .Build();
```

### Logical Grouping

Group related properties and items in named groups with labels:

```csharp
// ✅ Good - labeled groups
props.PropertyGroup(null, group =>
{
    group.Comment("Output directories");
    group.Property("ContosoOutputPath", "$(MSBuildProjectDirectory)/output");
    group.Property("ContosoIntermediatePath", "$(MSBuildProjectDirectory)/obj/contoso");
}, label: "Output Paths");

props.PropertyGroup(null, group =>
{
    group.Comment("Feature flags");
    group.Property("ContosoEnableFeatureA", "true");
    group.Property("ContosoEnableFeatureB", "false");
}, label: "Feature Flags");
```

## Property Design

### Naming Conventions

Use consistent, hierarchical property names:

```csharp
// ✅ Good - namespaced with package prefix
props.Property("ContosoEnabled", "true");
props.Property("ContosoVersion", "1.0.0");
props.Property("ContosoOutputPath", "$(OutputPath)/contoso");
props.Property("ContosoOptimize", "false");

// ❌ Avoid - generic names that might conflict
props.Property("Enabled", "true");
props.Property("Version", "1.0.0");
props.Property("MyPath", "...");
```

### Boolean Properties

Use `true`/`false` for boolean properties:

```csharp
// ✅ Good
props.Property("ContosoEnabled", "true");
target.Condition("'$(ContosoEnabled)' == 'true'");

// ❌ Avoid - non-standard values
props.Property("ContosoEnabled", "yes");
props.Property("ContosoEnabled", "1");
```

### Provide Defaults with Override Points

Always provide sensible defaults that can be overridden:

```csharp
// ✅ Good - default with override capability
props.PropertyGroup(null, group =>
{
    group.Property("ContosoOutputPath", "$(OutputPath)/contoso");
    group.Property("ContosoOptimize", "false");
    group.Property("ContosoOptimize", "true", 
        condition: "'$(Configuration)' == 'Release'");
});

// Projects can override:
// <PropertyGroup>
//   <ContosoOutputPath>custom/path</ContosoOutputPath>
// </PropertyGroup>
```

### Respect Existing Properties

Check if properties are already set before assigning defaults:

```csharp
// ✅ Good - respects user overrides
props.PropertyGroup("'$(ContosoOutputPath)' == ''", group =>
{
    group.Property("ContosoOutputPath", "$(OutputPath)/contoso");
});

// ❌ Avoid - unconditional override
props.Property("ContosoOutputPath", "$(OutputPath)/contoso");
```

### Computed Properties

Document property dependencies and computation order:

```csharp
props.PropertyGroup(null, group =>
{
    group.Comment("Base paths");
    group.Property("ContosoBasePath", "$(MSBuildThisFileDirectory)../../");
    
    group.Comment("Computed paths (depend on ContosoBasePath)");
    group.Property("ContosoToolsPath", "$(ContosoBasePath)tools");
    group.Property("ContosoTasksPath", "$(ContosoBasePath)tasks");
}, label: "Path Configuration");
```

## Target Design

### Naming Conventions

Use descriptive, hierarchical target names prefixed with your package name:

```csharp
// ✅ Good
"Contoso_PreBuild"
"Contoso_GenerateCode"
"Contoso_Build_Client"
"Contoso_Build_Server"
"Contoso_PostBuild_Package"

// ❌ Avoid
"MyTarget"
"DoStuff"
"Target1"
"CustomBuild"  // No package prefix
```

### Target Granularity

Create small, focused targets that do one thing well:

```csharp
// ✅ Good - composable targets
targets
    .Target("Contoso_ValidateInputs", target =>
    {
        target.Error("InputPath is required", 
            condition: "'$(InputPath)' == ''");
    })
    .Target("Contoso_PrepareOutputDirectory", target =>
    {
        target.DependsOnTargets("Contoso_ValidateInputs");
        target.Task("MakeDir", t => t.Param("Directories", "$(OutputPath)"));
    })
    .Target("Contoso_ProcessFiles", target =>
    {
        target.DependsOnTargets("Contoso_PrepareOutputDirectory");
        target.Task("Copy", t => { /* ... */ });
    });

// ❌ Avoid - monolithic target
targets.Target("Contoso_DoEverything", target =>
{
    // 100 lines of validation, preparation, and processing
});
```

### Incremental Builds

Always specify Inputs and Outputs for targets that transform files:

```csharp
// ✅ Good - incremental
targets.Target("Contoso_GenerateCode", target =>
{
    target.Inputs("@(SourceTemplate)");
    target.Outputs("$(IntermediateOutputPath)%(SourceTemplate.Filename).g.cs");
    
    target.Task("MyCodeGen", task => { /* ... */ });
});

// ❌ Avoid - runs every time
targets.Target("Contoso_GenerateCode", target =>
{
    // No Inputs/Outputs - always executes
    target.Task("MyCodeGen", task => { /* ... */ });
});
```

### Error Handling

Validate prerequisites and provide clear, actionable error messages:

```csharp
// ✅ Good - clear error with code and guidance
target.Error(
    "ContosoApiKey property is required for deployment. " +
    "Set it in your project file or via MSBuild parameter: /p:ContosoApiKey=YOUR_KEY",
    code: "CONTOSO001",
    condition: "'$(ContosoApiKey)' == '' AND '$(ContosoDeployEnabled)' == 'true'");

// ❌ Avoid - vague error
target.Error("Missing property", condition: "'$(ContosoApiKey)' == ''");
```

### Logging

Use appropriate message importance levels:

```csharp
// High - key milestones visible at normal verbosity
target.Message("Contoso build completed successfully", importance: "High");

// Normal - progress updates
target.Message("Processing 42 files", importance: "Normal");

// Low - diagnostic details (only visible at detailed/diagnostic verbosity)
target.Message("Processing file: example.cs", importance: "Low");
```

## Conditional Logic

### Use Choose for Multi-Way Branching

```csharp
// ✅ Good - clear multi-way branch
props.Choose(choose =>
{
    choose.When("'$(TargetFramework)' == 'net8.0'", w => 
        w.Property("UseModernFeatures", "true"));
    choose.When("'$(TargetFramework)' == 'net6.0'", w => 
        w.Property("UseModernFeatures", "false"));
    choose.Otherwise(o => 
        o.Property("UseModernFeatures", "false"));
});

// ❌ Less clear - multiple conditional groups
props.PropertyGroup("'$(TargetFramework)' == 'net8.0'", g => 
    g.Property("UseModernFeatures", "true"));
props.PropertyGroup("'$(TargetFramework)' == 'net6.0'", g => 
    g.Property("UseModernFeatures", "false"));
props.PropertyGroup(
    "'$(TargetFramework)' != 'net8.0' AND '$(TargetFramework)' != 'net6.0'", 
    g => g.Property("UseModernFeatures", "false"));
```

### Platform Detection

Use MSBuild functions for platform detection:

```csharp
// ✅ Good - MSBuild IsOSPlatform
props.Choose(choose =>
{
    choose.When("$([MSBuild]::IsOSPlatform('Windows'))", w =>
        w.Property("NativeLib", "contoso.dll"));
    choose.When("$([MSBuild]::IsOSPlatform('Linux'))", w =>
        w.Property("NativeLib", "libcontoso.so"));
    choose.Otherwise(o =>
        w.Property("NativeLib", "libcontoso.dylib"));
});

// ❌ Avoid - brittle string checks
props.PropertyGroup("'$(OS)' == 'Windows_NT'", g => { /* ... */ });
```

### Feature Flags

Provide feature flags with sensible defaults:

```csharp
// ✅ Good - opt-in with clear naming
props.Property("ContosoEnableAdvancedOptimizations", "false");
props.Property("ContosoEnableDetailedLogging", "false");

target.Condition("'$(ContosoEnableAdvancedOptimizations)' == 'true'");

// Users opt in explicitly:
// <PropertyGroup>
//   <ContosoEnableAdvancedOptimizations>true</ContosoEnableAdvancedOptimizations>
// </PropertyGroup>
```

## Multi-Target Framework Support

### Task Assembly Resolution

Support both .NET Framework and .NET Core/5+:

```csharp
// ✅ Good - supports both runtimes
props.Choose(choose =>
{
    choose.When("'$(MSBuildRuntimeType)' == 'Core'", whenProps =>
    {
        whenProps.Property("ContosoTasksAssembly",
            "$(MSBuildThisFileDirectory)../tasks/netstandard2.0/Contoso.Tasks.dll");
    });
    choose.Otherwise(otherwiseProps =>
    {
        otherwiseProps.Property("ContosoTasksAssembly",
            "$(MSBuildThisFileDirectory)../tasks/net472/Contoso.Tasks.dll");
    });
});

targets.UsingTask("Contoso.Tasks.MyTask", "$(ContosoTasksAssembly)");
```

### NuGet Package Layout

Structure your package to support multiple TFMs:

```
YourPackage.nupkg
├── build/
│   ├── YourPackage.props
│   └── YourPackage.targets
├── tasks/
│   ├── net472/
│   │   └── YourPackage.Tasks.dll
│   └── netstandard2.0/
│       └── YourPackage.Tasks.dll
└── lib/
    └── netstandard2.0/
        └── YourPackage.dll
```

## Strongly-Typed Names

### Define Reusable Types

Create strongly-typed name types for properties, items, and targets you reference frequently:

```csharp
// ✅ Good - reusable typed names
using JD.MSBuild.Fluent.Typed;

public readonly struct ContosoEnabled : IMsBuildPropertyName
{
    public string Name => "ContosoEnabled";
}

public readonly struct ContosoOutputPath : IMsBuildPropertyName
{
    public string Name => "ContosoOutputPath";
}

public readonly struct ContosoBuildTarget : IMsBuildTargetName
{
    public string Name => "Contoso_Build";
}

// Usage across multiple files
props.Property<ContosoEnabled>("true");
target.Condition(IsTrue<ContosoEnabled>());
target.BeforeTargets(new ContosoBuildTarget());
```

### Expression Helpers

Use expression helpers for common patterns:

```csharp
using static JD.MSBuild.Fluent.Typed.MsBuildExpr;

// ✅ Good - readable and type-safe
target.Condition(IsTrue<ContosoEnabled>());
props.Property("Computed", Prop<ContosoOutputPath>());
task.Param("Files", Item<Compile>());

// ❌ Less clear - raw strings
target.Condition("'$(ContosoEnabled)' == 'true'");
props.Property("Computed", "$(ContosoOutputPath)");
task.Param("Files", "@(Compile)");
```

## Code Organization

### Extract Helper Methods

Create helper methods for reusable configuration:

```csharp
// ✅ Good - composable helpers
public static class ContosoConfiguration
{
    public static void AddDefaultProperties(PropsBuilder props, string version)
    {
        props.Property("ContosoEnabled", "true");
        props.Property("ContosoVersion", version);
        props.Property("ContosoOutputPath", "$(OutputPath)/contoso");
    }

    public static void AddPlatformProperties(PropsBuilder props)
    {
        props.Choose(choose =>
        {
            choose.When("$([MSBuild]::IsOSPlatform('Windows'))", w =>
                w.Property("ContosoNativeLib", "contoso.dll"));
            choose.When("$([MSBuild]::IsOSPlatform('Linux'))", w =>
                w.Property("ContosoNativeLib", "libcontoso.so"));
            choose.Otherwise(o =>
                o.Property("ContosoNativeLib", "libcontoso.dylib"));
        });
    }

    public static void AddBuildTargets(TargetsBuilder targets)
    {
        targets.Target("Contoso_Build", target =>
        {
            target.BeforeTargets("Build");
            target.Message("Building with Contoso");
        });
    }
}

// Usage
Package.Define("Contoso.Build")
    .Props(props =>
    {
        ContosoConfiguration.AddDefaultProperties(props, "2.0.0");
        ContosoConfiguration.AddPlatformProperties(props);
    })
    .Targets(ContosoConfiguration.AddBuildTargets)
    .Build();
```

### Extension Methods

Create extension methods for domain-specific patterns:

```csharp
public static class ContosoExtensions
{
    public static PropsBuilder WithContosoDefaults(
        this PropsBuilder props, 
        string version)
    {
        ContosoConfiguration.AddDefaultProperties(props, version);
        ContosoConfiguration.AddPlatformProperties(props);
        return props;
    }

    public static TargetsBuilder WithContosoBuild(this TargetsBuilder targets)
    {
        ContosoConfiguration.AddBuildTargets(targets);
        return targets;
    }
}

// Usage
Package.Define("MyApp")
    .Props(p => p.WithContosoDefaults("2.0.0"))
    .Targets(t => t.WithContosoBuild())
    .Build();
```

## Testing

### Unit Test Package Definitions

```csharp
using Xunit;
using JD.MSBuild.Fluent.Packaging;

public class PackageDefinitionTests
{
    [Fact]
    public void Package_has_expected_properties()
    {
        var definition = MyPackageFactory.Create();

        Assert.Equal("MyPackage", definition.Id);
        Assert.NotNull(definition.Description);
    }

    [Fact]
    public void Props_contains_required_properties()
    {
        var definition = MyPackageFactory.Create();
        var properties = definition.Props.PropertyGroups
            .SelectMany(g => g.Properties)
            .ToDictionary(p => p.Name);

        Assert.True(properties.ContainsKey("MyPackageEnabled"));
        Assert.Equal("true", properties["MyPackageEnabled"].Value);
    }

    [Fact]
    public void Targets_contains_build_target()
    {
        var definition = MyPackageFactory.Create();
        var targets = definition.Targets.Targets
            .ToDictionary(t => t.Name);

        Assert.True(targets.ContainsKey("MyPackage_Build"));
        Assert.Equal("Build", targets["MyPackage_Build"].BeforeTargets);
    }
}
```

### Validate Generated XML

```csharp
[Fact]
public void Generated_XML_is_well_formed()
{
    var definition = MyPackageFactory.Create();
    var renderer = new MsBuildXmlRenderer();

    var propsXml = renderer.Render(definition.Props);
    var targetsXml = renderer.Render(definition.Targets);

    // Should not throw
    Assert.NotNull(propsXml);
    Assert.NotNull(targetsXml);
}

[Fact]
public void Generated_props_matches_expected_structure()
{
    var definition = MyPackageFactory.Create();
    var renderer = new MsBuildXmlRenderer();
    var xml = renderer.Render(definition.Props);

    // Validate structure
    Assert.NotEmpty(xml.Descendants("PropertyGroup"));
    Assert.Contains(xml.Descendants("PropertyGroup")
        .SelectMany(g => g.Elements()),
        e => e.Name.LocalName == "MyPackageEnabled");
}
```

### Integration Tests

Test the package in real projects:

```csharp
[Fact]
public void Package_works_in_real_project()
{
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(tempDir);

    try
    {
        // Generate package
        var definition = MyPackageFactory.Create();
        new MsBuildPackageEmitter().Emit(definition, Path.Combine(tempDir, "package"));

        // Create test project
        var projectFile = Path.Combine(tempDir, "test", "test.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(projectFile));
        File.WriteAllText(projectFile, @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <Import Project=""../package/build/MyPackage.props"" />
  <Import Project=""../package/build/MyPackage.targets"" />
</Project>");

        // Build project
        var result = RunMSBuild(projectFile);
        Assert.True(result.Success);
    }
    finally
    {
        Directory.Delete(tempDir, recursive: true);
    }
}
```

## Documentation

### XML Comments

Document your package definition:

```csharp
/// <summary>
/// Creates the Contoso.Build package definition.
/// </summary>
/// <param name="version">The package version.</param>
/// <param name="enableAdvancedFeatures">
/// If true, includes advanced optimization features.
/// </param>
/// <returns>A configured package definition ready for emission.</returns>
public static PackageDefinition Create(
    string version = "2.0.0",
    bool enableAdvancedFeatures = false)
{
    // Implementation
}
```

### Inline Comments

Add comments in the fluent API for complex logic:

```csharp
props.Choose(choose =>
{
    // Windows: Use native DLL from Windows runtime
    choose.When("$([MSBuild]::IsOSPlatform('Windows'))", w =>
        w.Property("ContosoNativeLib", "contoso.dll"));

    // Linux: Use shared object from Linux runtime
    choose.When("$([MSBuild]::IsOSPlatform('Linux'))", w =>
        w.Property("ContosoNativeLib", "libcontoso.so"));

    // macOS and other platforms: Use dylib
    choose.Otherwise(o =>
        o.Property("ContosoNativeLib", "libcontoso.dylib"));
});
```

### README.md

Include a README in your package project explaining usage:

```markdown
# Contoso.Build

MSBuild package for Contoso projects.

## Properties

| Property | Default | Description |
|----------|---------|-------------|
| `ContosoEnabled` | `true` | Enables Contoso build features |
| `ContosoVersion` | `2.0.0` | Package version |
| `ContosoOutputPath` | `$(OutputPath)/contoso` | Output directory |

## Targets

- `Contoso_PreBuild` - Runs before Build target
- `Contoso_Build` - Main Contoso build logic
- `Contoso_PostBuild` - Runs after Build target

## Usage

```xml
<PropertyGroup>
  <ContosoEnabled>true</ContosoEnabled>
  <ContosoOptimize>true</ContosoOptimize>
</PropertyGroup>
```
```

## Performance

### Minimize Condition Evaluation

Cache expensive computations in properties:

```csharp
// ✅ Good - compute once
props.Property("IsWindowsPlatform", 
    "$([MSBuild]::IsOSPlatform('Windows'))");

props.PropertyGroup("'$(IsWindowsPlatform)' == 'true'", g => { /* ... */ });
target.Condition("'$(IsWindowsPlatform)' == 'true'");

// ❌ Avoid - evaluate multiple times
props.PropertyGroup("$([MSBuild]::IsOSPlatform('Windows'))", g => { /* ... */ });
target.Condition("$([MSBuild]::IsOSPlatform('Windows'))");
```

### Avoid Excessive Targets

Don't create too many targets - combine related steps:

```csharp
// ✅ Good - balanced granularity
targets
    .Target("Contoso_Prepare", t => { /* validation, setup */ })
    .Target("Contoso_Build", t => { /* main build logic */ })
    .Target("Contoso_Finalize", t => { /* cleanup, reporting */ });

// ❌ Avoid - excessive targets
targets
    .Target("Contoso_ValidateProperty1", t => { /* ... */ })
    .Target("Contoso_ValidateProperty2", t => { /* ... */ })
    .Target("Contoso_ValidateProperty3", t => { /* ... */ })
    .Target("Contoso_CreateDir1", t => { /* ... */ })
    .Target("Contoso_CreateDir2", t => { /* ... */ });
    // 20+ more tiny targets...
```

## Security

### Don't Hardcode Secrets

Never hardcode API keys, passwords, or tokens:

```csharp
// ❌ NEVER - hardcoded secret
props.Property("ContosoApiKey", "secret-key-12345");

// ✅ Good - require user to set
props.PropertyGroup("'$(ContosoApiKey)' == ''", g =>
{
    g.Property("ContosoApiKey", "");
});

target.Error(
    "ContosoApiKey is required. Set it via: /p:ContosoApiKey=YOUR_KEY",
    code: "CONTOSO001",
    condition: "'$(ContosoApiKey)' == '' AND '$(ContosoDeployEnabled)' == 'true'");
```

### Validate User Input

Validate properties from users:

```csharp
// ✅ Good - validate paths exist
target.Error(
    "ContosoInputPath '$(ContosoInputPath)' does not exist",
    code: "CONTOSO002",
    condition: "'$(ContosoInputPath)' != '' AND !Exists('$(ContosoInputPath)')");

// ✅ Good - validate enum values
target.Error(
    "ContosoMode must be 'Debug' or 'Release', got '$(ContosoMode)'",
    code: "CONTOSO003",
    condition: "'$(ContosoMode)' != 'Debug' AND '$(ContosoMode)' != 'Release'");
```

## Versioning

### Semantic Versioning

Use semantic versioning for your packages:

```csharp
props.Property("ContosoPackageVersion", "2.1.0");
props.Property("ContosoMajorVersion", "2");
props.Property("ContosoMinorVersion", "1");
props.Property("ContosoPatchVersion", "0");
```

### Breaking Changes

Document breaking changes and provide migration paths in upgrade guides.

## Summary

Following these best practices ensures your MSBuild packages are:
- **Maintainable**: Well-organized, documented code
- **Robust**: Error handling and validation
- **Performant**: Efficient condition evaluation and incremental builds
- **Testable**: Unit and integration tests
- **Secure**: No hardcoded secrets, validated inputs
- **Compatible**: Multi-TFM support

## Next Steps

- [Quick Start](../getting-started/quick-start.md) - Get started with JD.MSBuild.Fluent
- [Architecture](../core-concepts/architecture.md) - Understand the framework design
- [Fluent Builders](../core-concepts/builders.md) - Master the builder API
- [Migration Guide](../migration/from-xml.md) - Convert existing XML to fluent API

## Related Topics

- [Targets](../targets-tasks/targets.md) - Target orchestration patterns
- [Custom Tasks](../targets-tasks/custom-tasks.md) - Task declaration and invocation
