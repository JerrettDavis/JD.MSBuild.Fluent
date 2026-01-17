# Migration from XML to Fluent API

Migrate existing MSBuild `.props` and `.targets` files to JD.MSBuild.Fluent's type-safe fluent API. This guide provides side-by-side comparisons and migration strategies.

## Overview

JD.MSBuild.Fluent generates standard MSBuild XML from a fluent C# API. Existing XML can be systematically converted to fluent definitions while maintaining identical behavior.

## Migration Strategy

### 1. Assess Current Assets

Inventory your existing MSBuild files:
- `build/*.props` and `build/*.targets`
- `buildTransitive/*.props` and `buildTransitive/*.targets`
- `Sdk/*/Sdk.props` and `Sdk/*/Sdk.targets`

### 2. Create Package Definition Project

Create a new .NET project for your package definition:

```bash
dotnet new classlib -n MyPackage.Definition
dotnet add package JD.MSBuild.Fluent
```

### 3. Convert Incrementally

Convert one file at a time, starting with the simplest (usually `.props` files).

### 4. Validate Output

Generate MSBuild files and compare with originals:

```bash
jdmsbuild generate \
    --assembly bin/Release/net8.0/MyPackage.Definition.dll \
    --type MyPackage.Definition.Factory \
    --method Create \
    --output artifacts/generated

# Compare
diff artifacts/generated/build/MyPackage.props original/build/MyPackage.props
```

### 5. Test in Real Projects

Reference the generated package in test projects to ensure behavior is unchanged.

## Basic Conversions

### Properties

**XML**:
```xml
<Project>
  <PropertyGroup>
    <MyProperty>Value</MyProperty>
    <AnotherProperty>$(SomeOtherProperty)</AnotherProperty>
  </PropertyGroup>
</Project>
```

**Fluent API**:
```csharp
Package.Define("MyPackage")
    .Props(props =>
    {
        props.Property("MyProperty", "Value");
        props.Property("AnotherProperty", "$(SomeOtherProperty)");
    })
    .Build();
```

### Conditional Properties

**XML**:
```xml
<Project>
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <Optimize>true</Optimize>
    <DebugSymbols>false</DebugSymbols>
  </PropertyGroup>
</Project>
```

**Fluent API**:
```csharp
props.PropertyGroup("'$(Configuration)' == 'Release'", group =>
{
    group.Property("Optimize", "true");
    group.Property("DebugSymbols", "false");
});
```

### Items

**XML**:
```xml
<Project>
  <ItemGroup>
    <Compile Include="*.cs" />
    <None Include="README.md">
      <Pack>true</Pack>
      <PackagePath>content/</PackagePath>
    </None>
  </ItemGroup>
</Project>
```

**Fluent API**:
```csharp
using JD.MSBuild.Fluent.IR;

props.Item("Compile", MsBuildItemOperation.Include, "*.cs");
props.Item("None", MsBuildItemOperation.Include, "README.md", item =>
{
    item.Meta("Pack", "true");
    item.Meta("PackagePath", "content/");
});
```

### Item Remove and Update

**XML**:
```xml
<Project>
  <ItemGroup>
    <Compile Remove="Generated/**/*.cs" />
    <Compile Update="*.Designer.cs">
      <AutoGen>true</AutoGen>
    </Compile>
  </ItemGroup>
</Project>
```

**Fluent API**:
```csharp
props.ItemGroup(null, group =>
{
    group.Remove("Compile", "Generated/**/*.cs");
    group.Update("Compile", "*.Designer.cs", item =>
    {
        item.Meta("AutoGen", "true");
    });
});
```

### Imports

**XML**:
```xml
<Project>
  <Import Project="$(MSBuildExtensionsPath)\Custom.props" 
          Condition="Exists('$(MSBuildExtensionsPath)\Custom.props')" />
  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
</Project>
```

**Fluent API**:
```csharp
props.Import("$(MSBuildExtensionsPath)\\Custom.props",
    condition: "Exists('$(MSBuildExtensionsPath)\\Custom.props')");
props.Import("Sdk.props", sdk: "Microsoft.NET.Sdk");
```

## Advanced Conversions

### Choose/When/Otherwise

**XML**:
```xml
<Project>
  <Choose>
    <When Condition="'$(TargetFramework)' == 'net8.0'">
      <PropertyGroup>
        <UseLatestFeatures>true</UseLatestFeatures>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="NewFeaturePackage" Version="2.0.0" />
      </ItemGroup>
    </When>
    <When Condition="'$(TargetFramework)' == 'net6.0'">
      <PropertyGroup>
        <UseLatestFeatures>false</UseLatestFeatures>
      </PropertyGroup>
    </When>
    <Otherwise>
      <PropertyGroup>
        <UseLatestFeatures>false</UseLatestFeatures>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="PolyfillPackage" Version="1.0.0" />
      </ItemGroup>
    </Otherwise>
  </Choose>
</Project>
```

**Fluent API**:
```csharp
props.Choose(choose =>
{
    choose.When("'$(TargetFramework)' == 'net8.0'", whenProps =>
    {
        whenProps.Property("UseLatestFeatures", "true");
        whenProps.ItemGroup(null, group =>
        {
            group.Include("PackageReference", "NewFeaturePackage", item =>
            {
                item.Meta("Version", "2.0.0");
            });
        });
    });
    
    choose.When("'$(TargetFramework)' == 'net6.0'", whenProps =>
    {
        whenProps.Property("UseLatestFeatures", "false");
    });
    
    choose.Otherwise(otherwiseProps =>
    {
        otherwiseProps.Property("UseLatestFeatures", "false");
        otherwiseProps.ItemGroup(null, group =>
        {
            group.Include("PackageReference", "PolyfillPackage", item =>
            {
                item.Meta("Version", "1.0.0");
            });
        });
    });
});
```

### Targets

**XML**:
```xml
<Project>
  <Target Name="MyTarget" 
          BeforeTargets="Build" 
          Condition="'$(RunMyTarget)' == 'true'"
          Inputs="@(SourceFiles)"
          Outputs="$(IntermediateOutputPath)marker.txt">
    <Message Text="Running MyTarget" Importance="High" />
    <MakeDir Directories="$(IntermediateOutputPath)" />
  </Target>
</Project>
```

**Fluent API**:
```csharp
targets.Target("MyTarget", target =>
{
    target.BeforeTargets("Build");
    target.Condition("'$(RunMyTarget)' == 'true'");
    target.Inputs("@(SourceFiles)");
    target.Outputs("$(IntermediateOutputPath)marker.txt");
    
    target.Message("Running MyTarget", importance: "High");
    target.Task("MakeDir", task =>
    {
        task.Param("Directories", "$(IntermediateOutputPath)");
    });
});
```

### UsingTask

**XML**:
```xml
<Project>
  <UsingTask TaskName="MyCompany.Build.Tasks.CustomTask" 
             AssemblyFile="$(MSBuildThisFileDirectory)../tasks/net472/MyTasks.dll"
             Condition="'$(MSBuildRuntimeType)' != 'Core'" />
  
  <UsingTask TaskName="MyCompany.Build.Tasks.CustomTask" 
             AssemblyFile="$(MSBuildThisFileDirectory)../tasks/netstandard2.0/MyTasks.dll"
             Condition="'$(MSBuildRuntimeType)' == 'Core'" />
</Project>
```

**Fluent API**:
```csharp
targets.UsingTask(
    taskName: "MyCompany.Build.Tasks.CustomTask",
    assemblyFile: "$(MSBuildThisFileDirectory)../tasks/net472/MyTasks.dll",
    condition: "'$(MSBuildRuntimeType)' != 'Core'");

targets.UsingTask(
    taskName: "MyCompany.Build.Tasks.CustomTask",
    assemblyFile: "$(MSBuildThisFileDirectory)../tasks/netstandard2.0/MyTasks.dll",
    condition: "'$(MSBuildRuntimeType)' == 'Core'");
```

### Custom Task Invocation

**XML**:
```xml
<Project>
  <Target Name="ProcessFiles">
    <CustomTask InputFile="$(InputFile)" 
                OutputFile="$(OutputFile)"
                Verbose="true">
      <Output TaskParameter="ProcessedCount" PropertyName="FileCount" />
      <Output TaskParameter="GeneratedFiles" ItemName="GeneratedFiles" />
    </CustomTask>
    <Message Text="Processed $(FileCount) files" />
  </Target>
</Project>
```

**Fluent API**:
```csharp
targets.Target("ProcessFiles", target =>
{
    target.Task("CustomTask", task =>
    {
        task.Param("InputFile", "$(InputFile)");
        task.Param("OutputFile", "$(OutputFile)");
        task.Param("Verbose", "true");
        task.OutputProperty("ProcessedCount", "FileCount");
        task.OutputItem("GeneratedFiles", "GeneratedFiles");
    });
    
    target.Message("Processed $(FileCount) files");
});
```

### PropertyGroup and ItemGroup in Targets

**XML**:
```xml
<Project>
  <Target Name="SetDynamicProperties">
    <PropertyGroup>
      <Timestamp>$([System.DateTime]::Now.ToString('yyyyMMdd-HHmmss'))</Timestamp>
    </PropertyGroup>
    <ItemGroup>
      <FilesToCopy Include="$(SourceDir)/**/*.dll" />
    </ItemGroup>
    <Message Text="Timestamp: $(Timestamp)" />
  </Target>
</Project>
```

**Fluent API**:
```csharp
targets.Target("SetDynamicProperties", target =>
{
    target.PropertyGroup(null, group =>
    {
        group.Property("Timestamp", "$([System.DateTime]::Now.ToString('yyyyMMdd-HHmmss'))");
    });
    
    target.ItemGroup(null, group =>
    {
        group.Include("FilesToCopy", "$(SourceDir)/**/*.dll");
    });
    
    target.Message("Timestamp: $(Timestamp)");
});
```

## Complete Example Migration

### Original XML

**build/Contoso.Build.props**:
```xml
<Project>
  <!-- Default properties -->
  <PropertyGroup>
    <ContosoEnabled>true</ContosoEnabled>
    <ContosoVersion>2.0.0</ContosoVersion>
  </PropertyGroup>

  <!-- Configuration-specific properties -->
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <ContosoOptimize>true</ContosoOptimize>
  </PropertyGroup>

  <!-- Platform-specific native libraries -->
  <Choose>
    <When Condition="$([MSBuild]::IsOSPlatform('Windows'))">
      <PropertyGroup>
        <ContosoNativeLib>contoso-win.dll</ContosoNativeLib>
      </PropertyGroup>
    </When>
    <When Condition="$([MSBuild]::IsOSPlatform('Linux'))">
      <PropertyGroup>
        <ContosoNativeLib>libcontoso-linux.so</ContosoNativeLib>
      </PropertyGroup>
    </When>
    <Otherwise>
      <PropertyGroup>
        <ContosoNativeLib>libcontoso-mac.dylib</ContosoNativeLib>
      </PropertyGroup>
    </Otherwise>
  </Choose>

  <!-- Task assembly path -->
  <PropertyGroup>
    <ContosoTasksAssembly Condition="'$(MSBuildRuntimeType)' == 'Core'">$(MSBuildThisFileDirectory)../tasks/netstandard2.0/Contoso.Build.Tasks.dll</ContosoTasksAssembly>
    <ContosoTasksAssembly Condition="'$(MSBuildRuntimeType)' != 'Core'">$(MSBuildThisFileDirectory)../tasks/net472/Contoso.Build.Tasks.dll</ContosoTasksAssembly>
  </PropertyGroup>
</Project>
```

**build/Contoso.Build.targets**:
```xml
<Project>
  <!-- Declare custom task -->
  <UsingTask TaskName="Contoso.Build.Tasks.ProcessFiles" 
             AssemblyFile="$(ContosoTasksAssembly)" />

  <!-- Pre-build target -->
  <Target Name="Contoso_PreBuild" 
          BeforeTargets="Build"
          Condition="'$(ContosoEnabled)' == 'true'">
    <Message Text="Contoso SDK v$(ContosoVersion) - Pre-build" Importance="High" />
    <MakeDir Directories="$(IntermediateOutputPath)contoso" />
  </Target>

  <!-- Process files target -->
  <Target Name="Contoso_ProcessFiles"
          BeforeTargets="Build"
          Condition="'$(ContosoEnabled)' == 'true'"
          DependsOnTargets="Contoso_PreBuild"
          Inputs="@(ContosoInputFiles)"
          Outputs="$(IntermediateOutputPath)contoso/processed.txt">
    
    <Contoso.Build.Tasks.ProcessFiles 
        InputDirectory="$(ContosoInputDir)"
        OutputDirectory="$(IntermediateOutputPath)contoso"
        Optimize="$(ContosoOptimize)">
      <Output TaskParameter="ProcessedCount" PropertyName="ContosoProcessedCount" />
    </Contoso.Build.Tasks.ProcessFiles>

    <Message Text="Processed $(ContosoProcessedCount) files" Importance="High" />
  </Target>
</Project>
```

### Migrated Fluent API

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.IR;

namespace Contoso.Build;

public static class PackageFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("Contoso.Build")
            .Description("Contoso build package")
            .Props(ConfigureProps)
            .Targets(ConfigureTargets)
            .Pack(options =>
            {
                options.BuildTransitive = true;
                options.EmitSdk = false;
            })
            .Build();
    }

    private static void ConfigureProps(PropsBuilder props)
    {
        // Default properties
        props.Property("ContosoEnabled", "true");
        props.Property("ContosoVersion", "2.0.0");

        // Configuration-specific properties
        props.PropertyGroup("'$(Configuration)' == 'Release'", group =>
        {
            group.Property("ContosoOptimize", "true");
        });

        // Platform-specific native libraries
        props.Choose(choose =>
        {
            choose.When("$([MSBuild]::IsOSPlatform('Windows'))", whenProps =>
            {
                whenProps.Property("ContosoNativeLib", "contoso-win.dll");
            });
            choose.When("$([MSBuild]::IsOSPlatform('Linux'))", whenProps =>
            {
                whenProps.Property("ContosoNativeLib", "libcontoso-linux.so");
            });
            choose.Otherwise(otherwiseProps =>
            {
                otherwiseProps.Property("ContosoNativeLib", "libcontoso-mac.dylib");
            });
        });

        // Task assembly path
        props.Choose(choose =>
        {
            choose.When("'$(MSBuildRuntimeType)' == 'Core'", whenProps =>
            {
                whenProps.Property("ContosoTasksAssembly",
                    "$(MSBuildThisFileDirectory)../tasks/netstandard2.0/Contoso.Build.Tasks.dll");
            });
            choose.Otherwise(otherwiseProps =>
            {
                otherwiseProps.Property("ContosoTasksAssembly",
                    "$(MSBuildThisFileDirectory)../tasks/net472/Contoso.Build.Tasks.dll");
            });
        });
    }

    private static void ConfigureTargets(TargetsBuilder targets)
    {
        // Declare custom task
        targets.UsingTask(
            taskName: "Contoso.Build.Tasks.ProcessFiles",
            assemblyFile: "$(ContosoTasksAssembly)");

        // Pre-build target
        targets.Target("Contoso_PreBuild", target =>
        {
            target.BeforeTargets("Build");
            target.Condition("'$(ContosoEnabled)' == 'true'");

            target.Message("Contoso SDK v$(ContosoVersion) - Pre-build", importance: "High");
            target.Task("MakeDir", task =>
            {
                task.Param("Directories", "$(IntermediateOutputPath)contoso");
            });
        });

        // Process files target
        targets.Target("Contoso_ProcessFiles", target =>
        {
            target.BeforeTargets("Build");
            target.Condition("'$(ContosoEnabled)' == 'true'");
            target.DependsOnTargets("Contoso_PreBuild");
            target.Inputs("@(ContosoInputFiles)");
            target.Outputs("$(IntermediateOutputPath)contoso/processed.txt");

            target.Task("Contoso.Build.Tasks.ProcessFiles", task =>
            {
                task.Param("InputDirectory", "$(ContosoInputDir)");
                task.Param("OutputDirectory", "$(IntermediateOutputPath)contoso");
                task.Param("Optimize", "$(ContosoOptimize)");
                task.OutputProperty("ProcessedCount", "ContosoProcessedCount");
            });

            target.Message("Processed $(ContosoProcessedCount) files", importance: "High");
        });
    }
}
```

## Refactoring Opportunities

After migration, refactor to take advantage of C# language features.

### Extract Helper Methods

```csharp
private static void ConfigureCommonProps(PropsBuilder props)
{
    props.Property("Company", "Contoso");
    props.Property("Copyright", "© 2024 Contoso");
}

private static void ConfigurePlatformProps(PropsBuilder props)
{
    props.Choose(choose =>
    {
        choose.When("$([MSBuild]::IsOSPlatform('Windows'))", w =>
            w.Property("NativeLib", "lib-win.dll"));
        choose.When("$([MSBuild]::IsOSPlatform('Linux'))", w =>
            w.Property("NativeLib", "lib-linux.so"));
        choose.Otherwise(o =>
            o.Property("NativeLib", "lib-mac.dylib"));
    });
}

// Compose
Package.Define("Contoso.Build")
    .Props(p =>
    {
        ConfigureCommonProps(p);
        ConfigurePlatformProps(p);
    })
    .Build();
```

### Use Strongly-Typed Names

```csharp
using JD.MSBuild.Fluent.Typed;

public readonly struct ContosoEnabled : IMsBuildPropertyName
{
    public string Name => "ContosoEnabled";
}

public readonly struct ContosoVersion : IMsBuildPropertyName
{
    public string Name => "ContosoVersion";
}

// Usage
props.Property<ContosoEnabled>("true");
props.Property<ContosoVersion>("2.0.0");

target.Condition("'$(ContosoEnabled)' == 'true'");
// Or with expression helpers:
using static JD.MSBuild.Fluent.Typed.MsBuildExpr;
target.Condition(IsTrue<ContosoEnabled>());
```

### Create Extension Methods

```csharp
public static class ContosoExtensions
{
    public static PropsBuilder AddContosoDefaults(
        this PropsBuilder props, 
        string version)
    {
        return props
            .Property<ContosoEnabled>("true")
            .Property<ContosoVersion>(version)
            .PropertyGroup("'$(Configuration)' == 'Release'", g =>
                g.Property("ContosoOptimize", "true"));
    }
}

// Usage
Package.Define("MyPackage")
    .Props(p => p.AddContosoDefaults("2.0.0"))
    .Build();
```

### Parameterize Configuration

```csharp
public static PackageDefinition Create(
    string version,
    bool enableAdvancedFeatures)
{
    var builder = Package.Define("Contoso.Build")
        .Props(p => p.Property("ContosoVersion", version));

    if (enableAdvancedFeatures)
    {
        builder.Props(p => p.Property("ContosoAdvanced", "true"));
        builder.Targets(t => t.Target("Contoso_Advanced", tgt => { /* ... */ }));
    }

    return builder.Build();
}
```

## Common Migration Challenges

### XML Comments

**XML**:
```xml
<!-- This is a comment -->
<PropertyGroup>
  <MyProperty>Value</MyProperty>
</PropertyGroup>
```

**Fluent API**:
```csharp
props.Comment("This is a comment");
props.Property("MyProperty", "Value");
```

### Metadata as Attributes vs Elements

**XML** (attribute):
```xml
<None Include="file.txt" Pack="true" PackagePath="content/" />
```

**XML** (element):
```xml
<None Include="file.txt">
  <Pack>true</Pack>
  <PackagePath>content/</PackagePath>
</None>
```

**Fluent API**:
```csharp
// Element-style (default)
props.Item("None", MsBuildItemOperation.Include, "file.txt", item =>
{
    item.Meta("Pack", "true");
    item.Meta("PackagePath", "content/");
});

// Attribute-style
props.Item("None", MsBuildItemOperation.Include, "file.txt", item =>
{
    item.MetaAttribute("Pack", "true");
    item.MetaAttribute("PackagePath", "content/");
});
```

### Line Continuations

XML allows splitting long values across lines. In C#, use string concatenation or interpolation:

```csharp
props.Property("LongValue",
    "Part1" +
    "Part2" +
    "Part3");

// Or with formatting
props.Property("FormattedValue",
    $"{prefix}" +
    $"/middle" +
    $"/{suffix}");
```

### Escaping

MSBuild uses `%xx` for escaping special characters. In C#, use verbatim strings or escape sequences:

```csharp
// Verbatim string for paths
props.Property("Path", @"C:\Program Files\MyApp");

// MSBuild escaping remains the same
props.Property("EscapedSemicolon", "Value1%3BValue2");
```

## Validation After Migration

### Generate and Compare

```bash
# Generate MSBuild files
jdmsbuild generate --assembly bin/Release/net8.0/MyPackage.dll \
    --type MyPackage.Factory --method Create --output generated

# Normalize line endings and compare
dos2unix original/build/MyPackage.props generated/build/MyPackage.props
diff original/build/MyPackage.props generated/build/MyPackage.props
```

### Test in Real Projects

1. Create a test project
2. Reference the original package
3. Build and note behavior
4. Replace with generated package
5. Build and verify identical behavior

### Unit Tests

```csharp
using Xunit;
using JD.MSBuild.Fluent.Packaging;

public class MigrationValidationTests
{
    [Fact]
    public void Generated_package_has_expected_structure()
    {
        var definition = MyPackage.Factory.Create();
        
        Assert.NotEmpty(definition.Props.PropertyGroups);
        Assert.NotEmpty(definition.Targets.Targets);
    }

    [Fact]
    public void Generated_props_contains_expected_properties()
    {
        var definition = MyPackage.Factory.Create();
        var props = definition.Props.PropertyGroups
            .SelectMany(g => g.Properties)
            .ToDictionary(p => p.Name, p => p.Value);

        Assert.Equal("true", props["MyProperty"]);
        Assert.Equal("$(SomeOther)", props["DerivedProperty"]);
    }
}
```

## Best Practices

### Migrate Incrementally

Don't convert everything at once. Migrate file-by-file and validate each step.

### Preserve Comments

Maintain XML comments as code comments or fluent `Comment()` calls for documentation.

### Test Thoroughly

Run builds with both original and migrated packages to ensure identical behavior.

### Use Strongly-Typed Names

After migration, introduce typed names for compile-time safety.

### Extract Reusable Patterns

Identify repeated patterns and extract them into helper methods or extensions.

## Next Steps

- [Best Practices](../best-practices/index.md) - Patterns for robust packages
- [Architecture](../core-concepts/architecture.md) - Understanding the IR layer
- [Fluent Builders](../core-concepts/builders.md) - Complete API reference

## Related Topics

- [Quick Start](../getting-started/quick-start.md) - Getting started guide
- [Targets](../targets-tasks/targets.md) - Working with targets
- [Custom Tasks](../targets-tasks/custom-tasks.md) - UsingTask and task invocation
