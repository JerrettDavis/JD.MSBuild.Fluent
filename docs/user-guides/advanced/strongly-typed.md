# Strongly-Typed Helpers

JD.MSBuild.Fluent provides strongly-typed helpers for property names, item types, target names, and metadata, enabling compile-time safety and better refactoring support.

## Why Use Strongly-Typed Names?

### Benefits

- **IntelliSense**: Autocomplete for names
- **Compile-Time Safety**: Catch typos before runtime
- **Refactoring**: Rename across your codebase
- **Discoverability**: Find all usages easily
- **Documentation**: Types serve as documentation

### Trade-offs

- **Additional Code**: Must define types for names
- **Verbosity**: Slightly more verbose than strings
- **Learning Curve**: Requires understanding of the pattern

## Defining Strongly-Typed Names

### Property Names

Implement `IMsBuildPropertyName`:

```csharp
using JD.MSBuild.Fluent.Typed;

public readonly struct MyPackageEnabled : IMsBuildPropertyName
{
    public string Name => "MyPackageEnabled";
}

public readonly struct MyPackageVersion : IMsBuildPropertyName
{
    public string Name => "MyPackageVersion";
}

public readonly struct MyPackageOutputPath : IMsBuildPropertyName
{
    public string Name => "MyPackageOutputPath";
}
```

**Usage:**

```csharp
.Props(p => p
    .Property<MyPackageEnabled>("true")
    .Property<MyPackageVersion>("2.0.0")
    .Property<MyPackageOutputPath>("$(OutputPath)/mypackage"))
```

### Item Type Names

Implement `IMsBuildItemTypeName`:

```csharp
public readonly struct MyPackageAsset : IMsBuildItemTypeName
{
    public string Name => "MyPackageAsset";
}

public readonly struct MyPackageConfiguration : IMsBuildItemTypeName
{
    public string Name => "MyPackageConfiguration";
}
```

**Usage:**

```csharp
.Props(p => p
    .Item<MyPackageAsset>(MsBuildItemOperation.Include, "assets/**/*.*")
    .Item<MyPackageConfiguration>(MsBuildItemOperation.Include, "config/*.json"))
```

### Target Names

Implement `IMsBuildTargetName`:

```csharp
public readonly struct PreBuildValidation : IMsBuildTargetName
{
    public string Name => "MyPackage_PreBuildValidation";
}

public readonly struct ProcessAssets : IMsBuildTargetName
{
    public string Name => "MyPackage_ProcessAssets";
}
```

**Usage:**

```csharp
.Targets(t => t
    .Target<PreBuildValidation>(target => target
        .BeforeTargets("Build")
        .Message("Validating...")))
```

### Metadata Names

Implement `IMsBuildMetadataName`:

```csharp
public readonly struct AssetType : IMsBuildMetadataName
{
    public string Name => "AssetType";
}

public readonly struct RequiresProcessing : IMsBuildMetadataName
{
    public string Name => "RequiresProcessing";
}
```

**Usage:**

```csharp
.Item<MyPackageAsset>(MsBuildItemOperation.Include, "asset.png", item => item
    .Meta<AssetType>("Image")
    .Meta<RequiresProcessing>("true"))
```

## Built-In Expressions

### MsBuildExpr Helper

The `MsBuildExpr` class provides helpers for building common MSBuild expressions:

```csharp
using JD.MSBuild.Fluent.Typed;

// Property reference: $(PropertyName)
string propRef = MsBuildExpr.Prop<MyPackageVersion>();
// Result: "$(MyPackageVersion)"

// Item reference: @(ItemType)
string itemRef = MsBuildExpr.Item<MyPackageAsset>();
// Result: "@(MyPackageAsset)"

// Empty check: '$(Prop)' == ''
string isEmpty = MsBuildExpr.IsEmpty<MyPackageEnabled>();
// Result: "'$(MyPackageEnabled)'==''"

// Empty check with spaces: '$(Prop)' == ''
string isEmptySpaced = MsBuildExpr.IsEmptyWithSpace<MyPackageEnabled>();
// Result: "'$(MyPackageEnabled)' == ''"

// Not empty: '$(Prop)' != ''
string notEmpty = MsBuildExpr.NotEmpty<MyPackageEnabled>();
// Result: "'$(MyPackageEnabled)'!=''"

// Is true: '$(Prop)' == 'true'
string isTrue = MsBuildExpr.IsTrue<MyPackageEnabled>();
// Result: "'$(MyPackageEnabled)' == 'true'"

// Is not true: '$(Prop)' != 'true'
string isNotTrue = MsBuildExpr.IsNotTrue<MyPackageEnabled>();
// Result: "'$(MyPackageEnabled)' != 'true'"
```

### Combining Conditions

```csharp
// AND condition
string andCondition = MsBuildExpr.And(
    MsBuildExpr.IsTrue<MyPackageEnabled>(),
    MsBuildExpr.NotEmpty<MyPackageVersion>()
);
// Result: "'$(MyPackageEnabled)' == 'true' and '$(MyPackageVersion)'!=''"

// OR condition
string orCondition = MsBuildExpr.Or(
    "'$(Configuration)' == 'Debug'",
    "'$(Configuration)' == 'Release'"
);
// Result: "'$(Configuration)' == 'Debug' or '$(Configuration)' == 'Release'"
```

## Comprehensive Example

### Define Types

```csharp
using JD.MSBuild.Fluent.Typed;

namespace MyCompany.Build.Names;

// Properties
public readonly struct Enabled : IMsBuildPropertyName
{
    public string Name => "MyCompanyEnabled";
}

public readonly struct Version : IMsBuildPropertyName
{
    public string Name => "MyCompanyVersion";
}

public readonly struct OutputPath : IMsBuildPropertyName
{
    public string Name => "MyCompanyOutputPath";
}

// Items
public readonly struct Asset : IMsBuildItemTypeName
{
    public string Name => "MyCompanyAsset";
}

public readonly struct Configuration : IMsBuildItemTypeName
{
    public string Name => "MyCompanyConfiguration";
}

// Targets
public readonly struct ValidateTarget : IMsBuildTargetName
{
    public string Name => "MyCompany_Validate";
}

public readonly struct BuildTarget : IMsBuildTargetName
{
    public string Name => "MyCompany_Build";
}

// Metadata
public readonly struct AssetCategory : IMsBuildMetadataName
{
    public string Name => "AssetCategory";
}

public readonly struct ProcessingRequired : IMsBuildMetadataName
{
    public string Name => "ProcessingRequired";
}
```

### Use Types in Definition

```csharp
using MyCompany.Build.Names;
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.Typed;

public static class PackageFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("MyCompany.Build")
            .Props(ConfigureProps)
            .Targets(ConfigureTargets)
            .Build();
    }

    private static void ConfigureProps(PropsBuilder props)
    {
        // Strongly-typed properties
        props.Property<Enabled>("true", MsBuildExpr.IsEmpty<Enabled>());
        props.Property<Version>("2.0.0", MsBuildExpr.IsEmpty<Version>());
        props.Property<OutputPath>("$(OutputPath)/mycompany");

        // Strongly-typed items
        props.ItemGroup(null, group =>
        {
            group.Include<Asset>("assets/**/*.*", item => item
                .Meta<AssetCategory>("General")
                .Meta<ProcessingRequired>("false"));

            group.Include<Configuration>("config/*.json", item => item
                .Meta("CopyToOutputDirectory", "PreserveNewest"));
        });
    }

    private static void ConfigureTargets(TargetsBuilder targets)
    {
        // Strongly-typed target with typed property references
        targets.Target<ValidateTarget>(target => target
            .BeforeTargets("Build")
            .Condition(MsBuildExpr.IsTrue<Enabled>())
            .Message($"MyCompany.Build {MsBuildExpr.Prop<Version>()}", "High")
            .Error(MsBuildExpr.IsEmpty<OutputPath>(), "OutputPath must be set"));

        // Another strongly-typed target
        targets.Target<BuildTarget>(target => target
            .DependsOnTargets(new ValidateTarget().Name)
            .Condition(MsBuildExpr.IsTrue<Enabled>())
            .Task("Copy", task =>
            {
                task.Param("SourceFiles", MsBuildExpr.Item<Asset>());
                task.Param("DestinationFolder", MsBuildExpr.Prop<OutputPath>());
            }));
    }
}
```

## Organizing Strongly-Typed Names

### Recommended Structure

```
MyCompany.Build.Definitions/
├── Names/
│   ├── Properties.cs      // All IMsBuildPropertyName types
│   ├── Items.cs           // All IMsBuildItemTypeName types
│   ├── Targets.cs         // All IMsBuildTargetName types
│   └── Metadata.cs        // All IMsBuildMetadataName types
└── PackageFactory.cs
```

**Properties.cs:**

```csharp
namespace MyCompany.Build.Names;

public readonly struct Enabled : IMsBuildPropertyName
{
    public string Name => "MyCompanyEnabled";
}

public readonly struct Version : IMsBuildPropertyName
{
    public string Name => "MyCompanyVersion";
}

// ... more properties
```

**Items.cs:**

```csharp
namespace MyCompany.Build.Names;

public readonly struct Asset : IMsBuildItemTypeName
{
    public string Name => "MyCompanyAsset";
}

public readonly struct Configuration : IMsBuildItemTypeName
{
    public string Name => "MyCompanyConfiguration";
}

// ... more item types
```

## Mixed String and Typed Approaches

You can mix string-based and typed approaches:

```csharp
.Props(p => p
    // Typed
    .Property<MyPackageEnabled>("true")
    
    // String-based
    .Property("MyPackageInternalFlag", "false")
    
    // Typed with condition using typed expression
    .Property<MyPackageVersion>("2.0.0", MsBuildExpr.IsEmpty<MyPackageVersion>()))
```

**When to use each:**

- **Typed**: Public API properties, frequently referenced
- **String**: Internal properties, one-off values

## Strongly-Typed Task Names

Define task names for consistency:

```csharp
public readonly struct CopyTask : IMsBuildTaskName
{
    public string Name => "Copy";
}

public readonly struct MakeDirTask : IMsBuildTaskName
{
    public string Name => "MakeDir";
}
```

**Usage:**

```csharp
.Task(new CopyTask().Name, task =>
{
    task.Param("SourceFiles", "@(Content)");
    task.Param("DestinationFolder", "$(OutputPath)");
})
```

**Note:** Task name typing is less common than property/item/target typing, since MSBuild task names are well-known and rarely change.

## Benefits in Large Codebases

### Find All References

With strongly-typed names, find all usages of a property:

```csharp
// Right-click on "MyPackageEnabled" → Find All References
public readonly struct MyPackageEnabled : IMsBuildPropertyName
{
    public string Name => "MyPackageEnabled";
}
```

Results show every place `MyPackageEnabled` is used.

### Safe Renaming

Rename a property across your entire codebase:

```csharp
// Rename "MyPackageEnabled" to "MyPackageIsEnabled"
public readonly struct MyPackageIsEnabled : IMsBuildPropertyName
{
    public string Name => "MyPackageIsEnabled";  // Update name
}
```

IDE refactoring renames all usages of the type.

### Intellisense Documentation

Add XML documentation:

```csharp
/// <summary>
/// Enables or disables MyCompany.Build integration.
/// Default is true.
/// </summary>
public readonly struct Enabled : IMsBuildPropertyName
{
    public string Name => "MyCompanyEnabled";
}
```

Documentation appears in IntelliSense when using the type.

## Best Practices

### DO: Use Descriptive Type Names

```csharp
// ✓ Clear
public readonly struct BuildEnabled : IMsBuildPropertyName
{
    public string Name => "MyPackageBuildEnabled";
}

// ✗ Unclear
public readonly struct BE : IMsBuildPropertyName
{
    public string Name => "MyPackageBuildEnabled";
}
```

### DO: Group Related Types

```csharp
// Group by feature
namespace MyCompany.Build.Names.Compilation;
namespace MyCompany.Build.Names.Packaging;
namespace MyCompany.Build.Names.Testing;
```

### DO: Prefix Property Names

```csharp
public readonly struct Enabled : IMsBuildPropertyName
{
    public string Name => "MyCompage_Enabled";  // Prefixed
}
```

### DON'T: Use Typed Names for Everything

```csharp
// ✗ Overkill for one-off internal properties
public readonly struct InternalTempFlag : IMsBuildPropertyName
{
    public string Name => "_MyPackageInternalTempFlag";
}

// ✓ Just use a string
.Property("_MyPackageInternalTempFlag", "temporary")
```

### DON'T: Make Names Too Generic

```csharp
// ✗ Too generic
public readonly struct Version : IMsBuildPropertyName
{
    public string Name => "Version";  // Conflicts with MSBuild's Version
}

// ✓ Specific
public readonly struct MyPackageVersion : IMsBuildPropertyName
{
    public string Name => "MyPackageVersion";
}
```

## Summary

| Type | Interface | Use For |
|------|-----------|---------|
| Property names | `IMsBuildPropertyName` | `$(PropertyName)` references |
| Item type names | `IMsBuildItemTypeName` | `@(ItemType)` references |
| Target names | `IMsBuildTargetName` | Target definitions |
| Metadata names | `IMsBuildMetadataName` | Item metadata keys |
| Task names | `IMsBuildTaskName` | Task invocations |

## Next Steps

- [First Package](../getting-started/first-package.md) - Apply typed names in a complete example
- [Working with Properties](../properties-items/properties.md) - Property patterns
- [Target Orchestration](../targets-tasks/orchestration.md) - Target patterns
- [MsBuildExpr API](../../api/JD.MSBuild.Fluent.Typed.MsBuildExpr.html) - Expression helper reference
