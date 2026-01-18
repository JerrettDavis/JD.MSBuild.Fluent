# Quick Start

Get up and running with JD.MSBuild.Fluent in 5 minutes. This guide walks you through creating a complete MSBuild package definition using the fluent API.

## Prerequisites

- .NET SDK 8.0 or later
- Basic understanding of MSBuild properties, targets, and tasks
- Familiarity with C# and fluent APIs

## Installation

Add the JD.MSBuild.Fluent package to your build definitions project:

```bash
dotnet add package JD.MSBuild.Fluent
```

## Your First Package Definition

Create a factory class that defines your MSBuild package:

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace MyCompany.Build;

public static class MyBuildPackageFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("MyCompany.Build")
            .Description("Custom MSBuild package for MyCompany projects")
            .Props(p => p
                .Property("MyCompanyBuildEnabled", "true")
                .Property("MyCompanyVersion", "1.0.0"))
            .Targets(t => t
                .Target("MyCompany_ShowInfo", target => target
                    .BeforeTargets("Build")
                    .Condition("'$(MyCompanyBuildEnabled)' == 'true'")
                    .Message("Building with MyCompany.Build v$(MyCompanyVersion)", "High")))
            .Pack(options =>
            {
                options.BuildTransitive = true;
                options.EmitSdk = false;
            })
            .Build();
    }
}
```

## Understanding the Structure

### Package.Define()

`Package.Define(string id)` is the entry point for defining a package. The ID should match your NuGet package ID.

### Props()

The `Props()` method configures properties and items that are evaluated during the MSBuild evaluation phase. Use this for:
- Default property values
- Item includes/excludes
- Imports
- Choose/When conditionals

Properties defined here appear in `build/{PackageId}.props` and optionally `buildTransitive/{PackageId}.props`.

### Targets()

The `Targets()` method configures targets and tasks that execute during the build. Use this for:
- Target definitions
- UsingTask declarations
- Task invocations
- Build orchestration

Targets defined here appear in `build/{PackageId}.targets` and optionally `buildTransitive/{PackageId}.targets`.

### Pack()

The `Pack()` method controls packaging options:

```csharp
.Pack(options =>
{
    options.BuildTransitive = true;  // Emit buildTransitive/ assets
    options.EmitSdk = true;          // Emit Sdk/ folder for SDK-style references
    options.BuildAssetBasename = null; // Override default filename (uses package ID)
})
```

## Generating MSBuild Assets

### Automatic Build Integration (Recommended)

**MSBuild automatically generates assets during build** - no manual steps required!

When you build your project, JD.MSBuild.Fluent automatically:
1. Loads your factory class
2. Invokes the `Create()` method
3. Generates MSBuild assets
4. Includes them in your package

Configuration (all optional):

```xml
<PropertyGroup>
  <!-- Enable/disable generation (default: true) -->
  <JDMSBuildFluentGenerateEnabled>true</JDMSBuildFluentGenerateEnabled>
  
  <!-- Specify factory type (default: auto-detects {RootNamespace}.DefinitionFactory) -->
  <JDMSBuildFluentDefinitionType>MyCompany.Build.MyBuildPackageFactory</JDMSBuildFluentDefinitionType>
  
  <!-- Factory method name (default: Create) -->
  <JDMSBuildFluentFactoryMethod>Create</JDMSBuildFluentFactoryMethod>
  
  <!-- Output directory (default: obj/msbuild) -->
  <JDMSBuildFluentOutputDir>$(MSBuildProjectDirectory)\msbuild</JDMSBuildFluentOutputDir>
</PropertyGroup>
```

Generated files are automatically included in your NuGet package.

### Manual Generation with CLI (Optional)

For manual control or debugging, use the CLI tool:

```bash
# Generate from a compiled assembly
jdmsbuild generate \
    --assembly bin/Release/net8.0/MyCompany.Build.dll \
    --type MyCompany.Build.MyBuildPackageFactory \
    --method Create \
    --output artifacts/msbuild

# Or use the built-in example
jdmsbuild generate --example --output artifacts/msbuild
```

### Programmatically

```csharp
using JD.MSBuild.Fluent.Packaging;

var definition = MyBuildPackageFactory.Create();
var emitter = new MsBuildPackageEmitter();
emitter.Emit(definition, outputDirectory: "artifacts/msbuild");
```

This generates:
```
artifacts/msbuild/
├── build/
│   ├── MyCompany.Build.props
│   └── MyCompany.Build.targets
└── buildTransitive/
    ├── MyCompany.Build.props
    └── MyCompany.Build.targets
```

## Complete Working Example

Here's a more comprehensive example demonstrating common patterns:

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.IR;

namespace ContosoSDK;

public static class PackageFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("Contoso.SDK")
            .Description("Contoso SDK for building modern applications")
            .Props(ConfigureProps)
            .Targets(ConfigureTargets)
            .Pack(options =>
            {
                options.BuildTransitive = true;
                options.EmitSdk = true;
            })
            .Build();
    }

    private static void ConfigureProps(PropsBuilder props)
    {
        // Default properties
        props.Property("ContosoSDKEnabled", "true");
        props.Property("ContosoSDKVersion", "2.0.0");
        props.Property("ContosoOutputPath", "$(MSBuildProjectDirectory)/bin/contoso");

        // Conditional property group
        props.PropertyGroup("'$(Configuration)' == 'Release'", group =>
        {
            group.Property("ContosoOptimize", "true");
            group.Property("ContosoDebugSymbols", "false");
        });

        // Choose/When for platform-specific settings
        props.Choose(choose =>
        {
            choose.When("$([MSBuild]::IsOSPlatform('Windows'))", whenProps =>
            {
                whenProps.Property("ContosoNativeLibrary", "contoso-win.dll");
            });
            choose.When("$([MSBuild]::IsOSPlatform('Linux'))", whenProps =>
            {
                whenProps.Property("ContosoNativeLibrary", "libcontoso-linux.so");
            });
            choose.Otherwise(otherwiseProps =>
            {
                otherwiseProps.Property("ContosoNativeLibrary", "libcontoso-mac.dylib");
            });
        });

        // Item includes
        props.ItemGroup(null, group =>
        {
            group.Include("None", "$(MSBuildThisFileDirectory)../../tools/**/*.*", item =>
            {
                item.Meta("Visible", "false");
                item.Meta("Pack", "true");
                item.Meta("PackagePath", "tools/%(RecursiveDir)%(Filename)%(Extension)");
            });
        });
    }

    private static void ConfigureTargets(TargetsBuilder targets)
    {
        // Define a target that runs before Build
        targets.Target("Contoso_PreBuild", target =>
        {
            target.BeforeTargets("Build");
            target.Condition("'$(ContosoSDKEnabled)' == 'true'");
            
            // Show info message
            target.Message("Contoso SDK v$(ContosoSDKVersion) - Pre-build started", "High");
            
            // Create output directory
            target.Task("MakeDir", task =>
            {
                task.Param("Directories", "$(ContosoOutputPath)");
            });
            
            // Run validation
            target.Exec("dotnet --version", "$(MSBuildProjectDirectory)");
        });

        // Define a target with outputs for incremental builds
        targets.Target("Contoso_GenerateAssets", target =>
        {
            target.DependsOnTargets("Contoso_PreBuild");
            target.Inputs("@(Compile)");
            target.Outputs("$(ContosoOutputPath)/manifest.json");
            
            target.Message("Generating Contoso assets...", "Normal");
            
            // Conditional task execution
            target.PropertyGroup("'$(ContosoOptimize)' == 'true'", group =>
            {
                group.Property("ContosoFlags", "$(ContosoFlags) --optimize");
            });
            
            target.Task("WriteLinesToFile", task =>
            {
                task.Param("File", "$(ContosoOutputPath)/manifest.json");
                task.Param("Lines", "{ \"version\": \"$(ContosoSDKVersion)\" }");
                task.Param("Overwrite", "true");
            });
        });

        // Post-build target
        targets.Target("Contoso_PostBuild", target =>
        {
            target.AfterTargets("Build");
            target.Condition("'$(ContosoSDKEnabled)' == 'true'");
            
            target.Message("Contoso SDK - Post-build complete", "High");
        });
    }
}
```

## Generated XML Output

The above definition generates clean, canonical MSBuild XML:

**build/Contoso.SDK.props:**
```xml
<Project>
  <!-- Generated by JD.MSBuild.Fluent -->
  <PropertyGroup>
    <ContosoSDKEnabled>true</ContosoSDKEnabled>
    <ContosoSDKVersion>2.0.0</ContosoSDKVersion>
    <ContosoOutputPath>$(MSBuildProjectDirectory)/bin/contoso</ContosoOutputPath>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <ContosoOptimize>true</ContosoOptimize>
    <ContosoDebugSymbols>false</ContosoDebugSymbols>
  </PropertyGroup>
  <Choose>
    <When Condition="$([MSBuild]::IsOSPlatform('Windows'))">
      <PropertyGroup>
        <ContosoNativeLibrary>contoso-win.dll</ContosoNativeLibrary>
      </PropertyGroup>
    </When>
    <When Condition="$([MSBuild]::IsOSPlatform('Linux'))">
      <PropertyGroup>
        <ContosoNativeLibrary>libcontoso-linux.so</ContosoNativeLibrary>
      </PropertyGroup>
    </When>
    <Otherwise>
      <PropertyGroup>
        <ContosoNativeLibrary>libcontoso-mac.dylib</ContosoNativeLibrary>
      </PropertyGroup>
    </Otherwise>
  </Choose>
  <ItemGroup>
    <None Include="$(MSBuildThisFileDirectory)../../tools/**/*.*">
      <Visible>false</Visible>
      <Pack>true</Pack>
      <PackagePath>tools/%(RecursiveDir)%(Filename)%(Extension)</PackagePath>
    </None>
  </ItemGroup>
</Project>
```

## Strongly-Typed Names

For better type safety, define strongly-typed names using the provided interfaces:

```csharp
using JD.MSBuild.Fluent.Typed;

// Property names
public readonly struct ContosoEnabled : IMsBuildPropertyName
{
    public string Name => "ContosoEnabled";
}

// Target names
public readonly struct PreBuildTarget : IMsBuildTargetName
{
    public string Name => "Contoso_PreBuild";
}

// Item types
public readonly struct ContosoAsset : IMsBuildItemTypeName
{
    public string Name => "ContosoAsset";
}

// Usage
props.Property<ContosoEnabled>("true");
targets.Target<PreBuildTarget>(target => { /* ... */ });
props.Item<ContosoAsset>(MsBuildItemOperation.Include, "assets/**/*.json");
```

## Testing Your Definition

Validate your generated MSBuild projects in unit tests:

```csharp
using JD.MSBuild.Fluent.Packaging;
using Xunit;

public class PackageDefinitionTests
{
    [Fact]
    public void Definition_generates_expected_assets()
    {
        var definition = ContosoSDK.PackageFactory.Create();
        var outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        try
        {
            new MsBuildPackageEmitter().Emit(definition, outputDir);
            
            Assert.True(File.Exists(Path.Combine(outputDir, "build", "Contoso.SDK.props")));
            Assert.True(File.Exists(Path.Combine(outputDir, "build", "Contoso.SDK.targets")));
            Assert.True(File.Exists(Path.Combine(outputDir, "buildTransitive", "Contoso.SDK.props")));
        }
        finally
        {
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
```

## Next Steps

Now that you've created your first package definition, explore:

- [Architecture](../core-concepts/architecture.md) - Understand the framework's design
- [Fluent Builders](../core-concepts/builders.md) - Master all builder methods and patterns
- [Working with Targets](../targets-tasks/targets.md) - Advanced target orchestration
- [Custom Tasks](../targets-tasks/custom-tasks.md) - UsingTask and custom task invocation
- [Best Practices](../best-practices/index.md) - Patterns for robust packages

## Common Pitfalls

### Props vs Targets

**Don't** put targets in props or properties in targets files:

```csharp
// ❌ Wrong - targets in props
.Props(p => p./* This will fail - props don't have Target() method */)

// ✅ Correct
.Props(p => p.Property("MyProperty", "value"))
.Targets(t => t.Target("MyTarget", target => { /* ... */ }))
```

### Condition Syntax

MSBuild conditions use single quotes and `$()` syntax:

```csharp
// ✅ Correct
.Condition("'$(Configuration)' == 'Release'")

// ❌ Wrong - double quotes inside
.Condition("\"$(Configuration)\" == \"Release\"")
```

### Build vs BuildTransitive

- **build/**: Only affects direct consumers
- **buildTransitive/**: Propagates through dependency chain

Set `BuildTransitive = true` if your settings should flow transitively:

```csharp
.Pack(options => { options.BuildTransitive = true; })
```

## Troubleshooting

### Generated files are not in the expected location

Check the output path specified in `Emit()` or the CLI `--output` argument.

### Properties not being set

Ensure you're using `Props()` for properties and `Targets()` for targets. Check that conditions are correctly formatted.

### Package not being imported

Verify the package ID matches your NuGet package ID exactly. MSBuild imports `build/{PackageId}.props` and `build/{PackageId}.targets` automatically.

## Additional Resources

- [MSBuild Concepts](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-concepts)
- [NuGet Package Authoring](https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package-msbuild)
- [MSBuild SDK Resolver](https://learn.microsoft.com/en-us/visualstudio/msbuild/how-to-use-project-sdk)
