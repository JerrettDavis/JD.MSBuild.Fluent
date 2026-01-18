# XML-to-Fluent Migration Guide

Learn how to migrate existing MSBuild XML files to the fluent API using the scaffolding feature.

## Overview

The `jdmsbuild scaffold` command automatically converts MSBuild XML files (`.props` and `.targets`) into fluent API C# code. This helps you:

- **Migrate existing packages** to the fluent DSL
- **Learn the fluent API** by seeing XML translated to C#
- **Get a head start** on new package definitions

## Basic Usage

```bash
jdmsbuild scaffold --xml path/to/MyPackage.targets --output DefinitionFactory.cs --package-id MyCompany.MyPackage
```

### Options

- `--xml`: Path to MSBuild XML file (`.props` or `.targets`) - **Required**
- `--output`: Output C# file path (default: `DefinitionFactory.cs`)
- `--package-id`: Package ID for the definition (default: derived from filename)
- `--class-name`: Factory class name (default: `DefinitionFactory`)

## Supported Elements

The scaffolder handles most MSBuild constructs:

### Properties and Items
- ✅ `PropertyGroup` (with conditions)
- ✅ `ItemGroup` (Include/Exclude/Remove/Update)
- ✅ Item metadata
- ✅ `Choose/When/Otherwise` blocks

### Targets
- ✅ `Target` definitions with orchestration (`BeforeTargets`, `AfterTargets`, `DependsOnTargets`)
- ✅ `Inputs` and `Outputs` for incremental builds
- ✅ PropertyGroup and ItemGroup inside targets

### Tasks
- ✅ `UsingTask` declarations
- ✅ Built-in tasks: `Message`, `Exec`, `Error`, `Warning`
- ✅ Custom task invocations with parameters
- ✅ Task outputs mapped to properties and items

### Other
- ✅ `Import` statements
- ✅ Conditions on all elements
- ✅ Strongly-typed name struct generation (commented)

## Example Migrations

### Simple Properties

**XML:**
```xml
<Project>
  <PropertyGroup>
    <MyPackageEnabled>true</MyPackageEnabled>
    <MyPackageVersion>1.0.0</MyPackageVersion>
  </PropertyGroup>
  
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <MyPackageOptimized>true</MyPackageOptimized>
  </PropertyGroup>
</Project>
```

**Generated Fluent:**
```csharp
public static class DefinitionFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("MyPackage")
            .Props(p =>
            {
                p.PropertyGroup(null, group =>
                {
                    group.Property("MyPackageEnabled", "true");
                    group.Property("MyPackageVersion", "1.0.0");
                });
                p.Property("MyPackageOptimized", "true", "'$(Configuration)' == 'Release'");
            })
            .Build();
    }
}
```

### Targets with Tasks

**XML:**
```xml
<Project>
  <Target Name="MyPackage_Build" BeforeTargets="Build" Condition="'$(MyPackageEnabled)' == 'true'">
    <Message Text="Running MyPackage build" Importance="High" />
    
    <PropertyGroup>
      <_TempDir>$(MSBuildProjectDirectory)\obj\mypackage</_TempDir>
    </PropertyGroup>
    
    <MakeDir Directories="$(_TempDir)" />
    <Exec Command="dotnet --version" WorkingDirectory="$(MSBuildProjectDirectory)" />
  </Target>
</Project>
```

**Generated Fluent:**
```csharp
public static class DefinitionFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("MyPackage")
            .Targets(t =>
            {
                t.Target("MyPackage_Build", target =>
                {
                    target.BeforeTargets("Build");
                    target.Condition("'$(MyPackageEnabled)' == 'true'");
                    target.Message("Running MyPackage build", "High");
                    target.PropertyGroup(null, group =>
                    {
                        group.Property("_TempDir", "$(MSBuildProjectDirectory)\\obj\\mypackage");
                    });
                    target.Task("MakeDir", task =>
                    {
                        task.Param("Directories", "$(_TempDir)");
                    });
                    target.Exec("dotnet --version", "$(MSBuildProjectDirectory)");
                });
            })
            .Build();
    }
}
```

### Choose/When/Otherwise

**XML:**
```xml
<Project>
  <Choose>
    <When Condition="$([MSBuild]::IsOSPlatform('Windows'))">
      <PropertyGroup>
        <Platform>Windows</Platform>
        <Extension>.exe</Extension>
      </PropertyGroup>
    </When>
    <Otherwise>
      <PropertyGroup>
        <Platform>Unix</Platform>
        <Extension></Extension>
      </PropertyGroup>
    </Otherwise>
  </Choose>
</Project>
```

**Generated Fluent:**
```csharp
public static class DefinitionFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("MyPackage")
            .Props(p =>
            {
                p.Choose(choose =>
                {
                    choose.When("$([MSBuild]::IsOSPlatform('Windows'))", whenProps =>
                    {
                        whenProps.Property("Platform", "Windows");
                        whenProps.Property("Extension", ".exe");
                    });
                    choose.Otherwise(otherwiseProps =>
                    {
                        otherwiseProps.Property("Platform", "Unix");
                        otherwiseProps.Property("Extension", "");
                    });
                });
            })
            .Build();
    }
}
```

### Custom Tasks with UsingTask

**XML:**
```xml
<Project>
  <UsingTask TaskName="MyCustomTask" AssemblyFile="$(MSBuildThisFileDirectory)\..\tasks\MyTask.dll" />
  
  <Target Name="RunCustom" AfterTargets="Build">
    <MyCustomTask InputFiles="@(Compile)" OutputPath="$(OutDir)">
      <Output TaskParameter="GeneratedFiles" ItemName="CustomGenerated" />
    </MyCustomTask>
  </Target>
</Project>
```

**Generated Fluent:**
```csharp
public static class DefinitionFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("MyPackage")
            .Targets(t =>
            {
                t.UsingTask("MyCustomTask", "$(MSBuildThisFileDirectory)\\..\\tasks\\MyTask.dll");
                t.Target("RunCustom", target =>
                {
                    target.AfterTargets("Build");
                    target.Task("MyCustomTask", task =>
                    {
                        task.Param("InputFiles", "@(Compile)");
                        task.Param("OutputPath", "$(OutDir)");
                        task.OutputItem("GeneratedFiles", "CustomGenerated");
                    });
                });
            })
            .Build();
    }
}
```

## Post-Scaffolding Steps

After scaffolding, you can improve the generated code:

1. **Add `using` directives** for strongly-typed names
2. **Uncomment and use** generated struct definitions
3. **Extract common logic** into helper methods
4. **Add documentation** to explain intent
5. **Refactor conditions** using constants
6. **Group related targets** into separate methods

### Example Refactoring

**Generated:**
```csharp
public static class DefinitionFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("MyPackage")
            .Props(p =>
            {
                p.Property("MyPackageEnabled", "true");
                p.Property("MyPackageVersion", "1.0.0");
            })
            .Build();
    }
}
```

**Refactored:**
```csharp
using JD.MSBuild.Fluent.Typed;

public static class DefinitionFactory
{
    private const string DefaultVersion = "1.0.0";
    
    public static PackageDefinition Create()
    {
        return Package.Define("MyPackage")
            .Description("My custom MSBuild package")
            .Props(ConfigureProperties)
            .Targets(ConfigureTargets)
            .Pack(o => o.BuildTransitive = true)
            .Build();
    }
    
    private static void ConfigureProperties(PropsBuilder props)
    {
        props.Property<MyPackageEnabled>("true");
        props.Property<MyPackageVersion>(DefaultVersion);
    }
    
    private static void ConfigureTargets(TargetsBuilder targets)
    {
        // ... target definitions
    }
}

// Strongly-typed property names
public readonly struct MyPackageEnabled : IMsBuildPropertyName
{
    public string Name => "MyPackageEnabled";
}

public readonly struct MyPackageVersion : IMsBuildPropertyName
{
    public string Name => "MyPackageVersion";
}
```

## Tips and Best Practices

### Review Generated Code
- The scaffolder does its best, but **always review** the output
- Some complex constructs may need manual adjustment
- Comments indicate generated strongly-typed names

### Start Simple
- Begin with simple `.props` files
- Move to `.targets` once comfortable
- Tackle complex packages last

### Iterative Migration
- Scaffold one file at a time
- Test each conversion
- Gradually combine into a complete package

### Preserve Intent
- XML can be terse - add comments explaining **why**
- Use descriptive variable names
- Extract magic strings into constants

### Testing
- Keep the original XML for comparison
- Generate from fluent code and diff with original
- Test the package in a real project

## Limitations

The scaffolder handles most scenarios but has some limitations:

- **Complex expressions**: Very complex MSBuild expressions may need manual review
- **Rare constructs**: Some rarely-used MSBuild features might not scaffold perfectly
- **Comments**: XML comments are not preserved (add them back manually)
- **Formatting**: Generated code follows a standard format (refactor as preferred)

## Next Steps

After scaffolding:

1. Review the [Fluent Builders guide](core-concepts/builders.md)
2. Learn about [target orchestration](targets-tasks/targets.md)
3. Explore [best practices](best-practices/index.md)
4. Check out [samples](../samples/) for inspiration

## Getting Help

- Check the [API documentation](../api/) for method signatures
- Look at [samples](../samples/) for working examples
- File an issue if scaffolding produces unexpected results
