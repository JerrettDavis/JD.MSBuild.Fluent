# Architecture

Understanding JD.MSBuild.Fluent's architecture helps you author effective MSBuild packages and leverage the framework's design.

## Overview

JD.MSBuild.Fluent uses a layered architecture that separates concerns:

```
┌─────────────────────────────────────────┐
│         Authoring Layer                 │
│  (Package.Define(), Fluent Builders)    │
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│    Intermediate Representation (IR)     │
│     (MsBuildProject, MsBuildTarget)     │
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│         Rendering Layer                 │
│      (MsBuildXmlRenderer)               │
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│         Packaging Layer                 │
│     (MsBuildPackageEmitter)             │
└──────────────────┬──────────────────────┘
                   │
                   ▼
         MSBuild XML Files in NuGet Layout
```

## Intermediate Representation (IR)

The IR layer provides an in-memory, language-agnostic representation of MSBuild projects.

### Core IR Types

**MsBuildProject** - Represents a complete MSBuild project file (`.props` or `.targets`):

```csharp
public sealed class MsBuildProject
{
    public List<IMsBuildProjectElement> Elements { get; }
    public List<MsBuildPropertyGroup> PropertyGroups { get; }
    public List<MsBuildItemGroup> ItemGroups { get; }
    public List<MsBuildImport> Imports { get; }
    public List<MsBuildUsingTask> UsingTasks { get; }
    public List<MsBuildTarget> Targets { get; }
    public List<MsBuildChoose> Chooses { get; }
    public string? Label { get; set; }
}
```

The `Elements` list maintains document order for all project-level constructs. The typed collections (`PropertyGroups`, `ItemGroups`, etc.) provide convenient access.

**MsBuildPropertyGroup** - Represents a `<PropertyGroup>` element:

```csharp
public sealed class MsBuildPropertyGroup : IMsBuildProjectElement
{
    public string? Condition { get; set; }
    public string? Label { get; set; }
    public List<MsBuildProperty> Properties { get; }
    public List<IMsBuildPropertyGroupEntry> Entries { get; }
}
```

**MsBuildProperty** - Represents a single property:

```csharp
public sealed class MsBuildProperty : IMsBuildPropertyGroupEntry
{
    public required string Name { get; init; }
    public required string Value { get; init; }
    public string? Condition { get; init; }
}
```

**MsBuildItemGroup** - Represents an `<ItemGroup>` element:

```csharp
public sealed class MsBuildItemGroup : IMsBuildProjectElement
{
    public string? Condition { get; set; }
    public string? Label { get; set; }
    public List<MsBuildItem> Items { get; }
    public List<IMsBuildItemGroupEntry> Entries { get; }
}
```

**MsBuildItem** - Represents an item with Include, Remove, or Update:

```csharp
public sealed class MsBuildItem : IMsBuildItemGroupEntry
{
    public required string ItemType { get; init; }
    public required MsBuildItemOperation Operation { get; init; }
    public required string Spec { get; init; }
    public string? Exclude { get; set; }
    public string? Condition { get; init; }
    public Dictionary<string, string> Metadata { get; }
    public Dictionary<string, string> MetadataAttributes { get; }
}

public enum MsBuildItemOperation
{
    Include,
    Remove,
    Update
}
```

**MsBuildTarget** - Represents a `<Target>` element:

```csharp
public sealed class MsBuildTarget : IMsBuildProjectElement
{
    public required string Name { get; init; }
    public string? Condition { get; set; }
    public string? BeforeTargets { get; set; }
    public string? AfterTargets { get; set; }
    public string? DependsOnTargets { get; set; }
    public string? Inputs { get; set; }
    public string? Outputs { get; set; }
    public string? Label { get; set; }
    public List<MsBuildTargetElement> Elements { get; }
}
```

**MsBuildTargetElement** - Base class for target contents (tasks, property groups, item groups):

```csharp
public abstract class MsBuildTargetElement { }

// Task invocations
public sealed class MsBuildTaskStep : MsBuildStep
{
    public required string TaskName { get; init; }
    public Dictionary<string, string> Parameters { get; }
    public List<MsBuildTaskOutput> Outputs { get; }
}

// Convenience steps
public sealed class MsBuildMessageStep : MsBuildStep { /* ... */ }
public sealed class MsBuildExecStep : MsBuildStep { /* ... */ }
public sealed class MsBuildErrorStep : MsBuildStep { /* ... */ }
public sealed class MsBuildWarningStep : MsBuildStep { /* ... */ }
```

**MsBuildChoose** - Represents conditional logic:

```csharp
public sealed class MsBuildChoose : IMsBuildProjectElement
{
    public List<MsBuildWhen> Whens { get; }
    public MsBuildOtherwise? Otherwise { get; set; }
}

public sealed class MsBuildWhen
{
    public required string Condition { get; init; }
    public List<MsBuildPropertyGroup> PropertyGroups { get; }
    public List<MsBuildItemGroup> ItemGroups { get; }
}
```

### Why an IR Layer?

The IR layer provides several benefits:

1. **Separation of Concerns**: Authoring logic is decoupled from XML rendering
2. **Multiple Renderers**: Support different output formats (XML, JSON, etc.) by implementing new renderers
3. **Transformations**: Modify or analyze the IR before rendering
4. **Testing**: Validate the IR structure without parsing XML
5. **Immutability**: IR types use `init` properties where possible for safety

### Working Directly with IR

While the fluent builders are the recommended API, you can construct IR directly:

```csharp
using JD.MSBuild.Fluent.IR;

var project = new MsBuildProject
{
    Label = "Manual IR Construction"
};

var propertyGroup = new MsBuildPropertyGroup();
propertyGroup.Properties.Add(new MsBuildProperty 
{ 
    Name = "MyProperty", 
    Value = "MyValue" 
});

project.PropertyGroups.Add(propertyGroup);
project.Elements.Add(propertyGroup);
```

**Note**: Maintain both the typed collection (`PropertyGroups`) and document-order list (`Elements`). The renderer uses `Elements` for output order.

## Fluent Builders

The builder layer provides a type-safe, chainable API for constructing IR.

### Builder Hierarchy

```
Package (static entry point)
  └─ PackageBuilder
       ├─ PropsBuilder (for .props files)
       │    ├─ PropsGroupBuilder (for PropertyGroup)
       │    ├─ ItemGroupBuilder (for ItemGroup)
       │    ├─ ItemBuilder (for individual items)
       │    └─ ChooseBuilder (for Choose/When/Otherwise)
       └─ TargetsBuilder (for .targets files)
            ├─ TargetBuilder (for Target)
            └─ TaskInvocationBuilder (for task parameters/outputs)
```

### Immutable Builder Pattern

Builders return `this` from all methods, enabling fluent chaining:

```csharp
props
    .Property("A", "1")
    .Property("B", "2")
    .Item<Compile>(MsBuildItemOperation.Include, "*.cs");
```

Builders accumulate changes in the underlying IR object. When you call `.Build()` on the `PackageBuilder`, you receive the final `PackageDefinition` containing all configured IR projects.

### Builder Design Principles

1. **Contextual APIs**: Each builder exposes only relevant methods (e.g., `PropsBuilder` has no `Target()` method)
2. **Smart Defaults**: Builders create groups automatically when needed
3. **Strongly-Typed Overloads**: Provide generic overloads for compile-time validation
4. **Condition-First**: Conditions are explicit parameters, not hidden in method chains

Example of smart defaults:

```csharp
props
    .Property("A", "1")  // Creates PropertyGroup automatically
    .Property("B", "2"); // Adds to the same PropertyGroup
```

To force a new group:

```csharp
props
    .PropertyGroup(null, g => g.Property("A", "1"))
    .PropertyGroup(null, g => g.Property("B", "2"));
```

## Rendering Layer

The rendering layer converts IR to canonical MSBuild XML.

### MsBuildXmlRenderer

The `MsBuildXmlRenderer` class handles XML generation:

```csharp
using JD.MSBuild.Fluent.Render;
using System.Xml.Linq;

var project = /* MsBuildProject instance */;
var renderer = new MsBuildXmlRenderer();
XDocument xml = renderer.Render(project);

// Write to file
xml.Save("MyPackage.props");
```

### Deterministic Rendering

The renderer produces canonical output to ensure consistent diffs:

**Property Ordering**: Properties within a group are sorted alphabetically by name.

**Item Ordering**: Items are rendered in declaration order (preserving author intent).

**Metadata Ordering**: Item metadata is sorted alphabetically by key.

**Task Parameters**: Task parameters are sorted alphabetically.

**Whitespace**: Consistent indentation (2 spaces) and line endings.

This canonicalization eliminates spurious diffs caused by insertion order or dictionary iteration.

### Example: Before and After Canonicalization

**Before** (non-deterministic):

```xml
<PropertyGroup>
  <ZProperty>z</ZProperty>
  <AProperty>a</AProperty>
  <MProperty>m</MProperty>
</PropertyGroup>
```

**After** (canonical):

```xml
<PropertyGroup>
  <AProperty>a</AProperty>
  <MProperty>m</MProperty>
  <ZProperty>z</ZProperty>
</PropertyGroup>
```

### Rendering Options

While the renderer has no public configuration, the IR supports rich constructs:

- **Comments**: `MsBuildComment` elements preserve documentation
- **Labels**: The `Label` attribute provides human-readable annotations
- **Conditions**: All major elements support MSBuild conditions

## Packaging Layer

The packaging layer emits rendered XML into the NuGet folder structure.

### PackageDefinition

`PackageDefinition` is the root container for all MSBuild assets:

```csharp
public sealed class PackageDefinition
{
    public required string Id { get; init; }
    public string? Description { get; set; }

    // Primary projects (used when specific build/sdk projects aren't set)
    public MsBuildProject Props { get; }
    public MsBuildProject Targets { get; }

    // Specific locations
    public MsBuildProject? BuildProps { get; set; }
    public MsBuildProject? BuildTargets { get; set; }
    public MsBuildProject? BuildTransitiveProps { get; set; }
    public MsBuildProject? BuildTransitiveTargets { get; set; }
    public MsBuildProject? SdkProps { get; set; }
    public MsBuildProject? SdkTargets { get; set; }

    public PackagePackagingOptions Packaging { get; }
}
```

### Fallback Logic

If you don't explicitly configure specific projects, the emitter uses fallback:

```csharp
public MsBuildProject GetBuildProps() => BuildProps ?? Props;
public MsBuildProject GetBuildTargets() => BuildTargets ?? Targets;
```

This means:

```csharp
Package.Define("MyPackage")
    .Props(p => p.Property("A", "1"))
    .Targets(t => t.Target("T", tgt => { /* ... */ }))
    .Build();
```

Results in:
- `build/MyPackage.props` contains properties
- `build/MyPackage.targets` contains targets
- No buildTransitive or Sdk assets (unless packaging options enable them)

### PackagePackagingOptions

```csharp
public sealed class PackagePackagingOptions
{
    // Emit buildTransitive assets in addition to build assets
    public bool BuildTransitive { get; set; }

    // Emit Sdk/{PackageId}/Sdk.props and Sdk/{PackageId}/Sdk.targets
    public bool EmitSdk { get; set; }

    // Override the base filename (default: package ID)
    public string? BuildAssetBasename { get; set; }
}
```

### MsBuildPackageEmitter

The `MsBuildPackageEmitter` orchestrates the entire packaging process:

```csharp
public void Emit(PackageDefinition definition, string outputDirectory)
{
    // Creates:
    // - outputDirectory/build/{Id}.props
    // - outputDirectory/build/{Id}.targets
    // - (optional) outputDirectory/buildTransitive/{Id}.props
    // - (optional) outputDirectory/buildTransitive/{Id}.targets
    // - (optional) outputDirectory/Sdk/{Id}/Sdk.props
    // - (optional) outputDirectory/Sdk/{Id}/Sdk.targets
}
```

**Usage**:

```csharp
using JD.MSBuild.Fluent.Packaging;

var definition = Package.Define("Contoso.Build")
    .Props(p => p.Property("ContosoEnabled", "true"))
    .Targets(t => t.Target("Contoso_Hello", tgt => tgt.Message("Hello")))
    .Pack(o => { o.BuildTransitive = true; o.EmitSdk = false; })
    .Build();

var emitter = new MsBuildPackageEmitter();
emitter.Emit(definition, "artifacts/nuget");
```

## Typed Helpers

JD.MSBuild.Fluent provides strongly-typed helpers to catch errors at compile time.

### Name Interfaces

```csharp
namespace JD.MSBuild.Fluent.Typed;

public interface IMsBuildPropertyName { string Name { get; } }
public interface IMsBuildItemTypeName { string Name { get; } }
public interface IMsBuildTargetName { string Name { get; } }
public interface IMsBuildMetadataName { string Name { get; } }
public interface IMsBuildTaskName { string Name { get; } }
public interface IMsBuildTaskParameterName { string Name { get; } }
```

### Defining Typed Names

```csharp
using JD.MSBuild.Fluent.Typed;

public readonly struct Configuration : IMsBuildPropertyName
{
    public string Name => "Configuration";
}

public readonly struct Compile : IMsBuildItemTypeName
{
    public string Name => "Compile";
}

public readonly struct Build : IMsBuildTargetName
{
    public string Name => "Build";
}
```

### Using Typed Names

```csharp
// Generic overloads
props.Property<Configuration>("Debug");
props.Item<Compile>(MsBuildItemOperation.Include, "Program.cs");
targets.Target<Build>(tgt => { /* ... */ });

// Or with instances
props.Property(new Configuration(), "Debug");
targets.Target(new Build(), tgt => { /* ... */ });
```

### MsBuildExpr Helpers

For building MSBuild expressions and conditions:

```csharp
using static JD.MSBuild.Fluent.Typed.MsBuildExpr;

// Property reference
string expr = Prop<Configuration>();  // "$(Configuration)"

// Item reference
string items = Item<Compile>();  // "@(Compile)"

// Condition helpers
string cond = IsTrue<MyProperty>();  // "'$(MyProperty)' == 'true'"
string cond2 = Eq<Configuration>("Release");  // "'$(Configuration)' == 'Release'"
```

## Validation

The framework includes validation to catch common errors.

### MsBuildValidator

```csharp
using JD.MSBuild.Fluent.Validation;

var project = /* ... */;
var validator = new MsBuildValidator();
var results = validator.Validate(project);

if (!results.IsValid)
{
    foreach (var error in results.Errors)
    {
        Console.WriteLine($"{error.Severity}: {error.Message}");
    }
}
```

Common validations:
- Target names are not empty
- Property names are valid identifiers
- Item types follow MSBuild conventions
- Condition syntax is well-formed

## Parsing

The framework can parse existing MSBuild XML into IR.

### MsBuildXmlParser

```csharp
using JD.MSBuild.Fluent.Parse;
using System.Xml.Linq;

var xml = XDocument.Load("MyPackage.props");
var parser = new MsBuildXmlParser();
MsBuildProject project = parser.Parse(xml);

// Inspect or modify the IR
foreach (var prop in project.PropertyGroups.SelectMany(g => g.Properties))
{
    Console.WriteLine($"{prop.Name} = {prop.Value}");
}

// Re-render
var renderer = new MsBuildXmlRenderer();
XDocument modified = renderer.Render(project);
```

**Use Cases**:
- Migrate existing XML to fluent API
- Analyze MSBuild files programmatically
- Transform MSBuild projects

## Extension Points

The architecture supports extensibility:

### Custom Renderers

Implement a custom renderer for different output formats:

```csharp
public interface IMsBuildRenderer
{
    void Render(MsBuildProject project, Stream output);
}

public class JsonRenderer : IMsBuildRenderer
{
    public void Render(MsBuildProject project, Stream output)
    {
        // Serialize IR to JSON
    }
}
```

### Custom Builders

Create domain-specific builders that wrap the core API:

```csharp
public static class ContosoBuilderExtensions
{
    public static PropsBuilder AddContosoDefaults(this PropsBuilder props)
    {
        return props
            .Property("ContosoEnabled", "true")
            .Property("ContosoVersion", "1.0.0");
    }

    public static TargetsBuilder AddContosoTargets(this TargetsBuilder targets)
    {
        return targets
            .Target("Contoso_PreBuild", tgt => tgt
                .BeforeTargets("Build")
                .Message("Contoso Pre-Build"));
    }
}

// Usage
Package.Define("MyPackage")
    .Props(p => p.AddContosoDefaults())
    .Targets(t => t.AddContosoTargets())
    .Build();
```

### IR Transformations

Apply transformations to the IR:

```csharp
public static class MsBuildProjectExtensions
{
    public static void AddComments(this MsBuildProject project, string comment)
    {
        var commentElement = new MsBuildComment { Text = comment };
        project.Elements.Insert(0, commentElement);
    }

    public static void RemoveEmptyPropertyGroups(this MsBuildProject project)
    {
        var emptyGroups = project.PropertyGroups
            .Where(g => g.Properties.Count == 0)
            .ToList();

        foreach (var group in emptyGroups)
        {
            project.PropertyGroups.Remove(group);
            project.Elements.Remove(group);
        }
    }
}
```

## Summary

JD.MSBuild.Fluent's architecture provides:

1. **Clear Separation**: IR, Builders, Renderer, and Packaging are independent layers
2. **Type Safety**: Strongly-typed names and generic overloads catch errors early
3. **Determinism**: Canonical rendering eliminates non-semantic diffs
4. **Extensibility**: Custom renderers, builders, and transformations are supported
5. **Testability**: IR can be validated without running MSBuild

Next, explore the [Fluent Builders](builders.md) guide for detailed API coverage, or see [Targets](../targets-tasks/targets.md) for working with build orchestration.
