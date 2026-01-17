# Intermediate Representation (IR)

The Intermediate Representation (IR) layer is the core data model of JD.MSBuild.Fluent. Understanding the IR is essential for advanced usage, custom rendering, and extending the framework.

## Overview

The IR layer provides an **in-memory, language-agnostic representation** of MSBuild constructs. It sits between the high-level fluent API and the low-level XML renderer:

```
Fluent API → IR (Data Structures) → XML Renderer → MSBuild XML
```

### Design Principles

The IR layer follows these principles:

1. **Immutability**: IR structures use init-only properties where possible
2. **Composability**: IR elements can be combined and nested freely
3. **Language Agnostic**: No MSBuild-specific logic, just data structures
4. **Explicit**: All MSBuild concepts have corresponding IR types
5. **Validatable**: IR can be validated before rendering

## Core IR Types

### MsBuildProject

The root container for all MSBuild constructs:

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

**Usage:**

```csharp
using JD.MSBuild.Fluent.IR;

var project = new MsBuildProject
{
    Label = "My Custom Project"
};

project.PropertyGroups.Add(new MsBuildPropertyGroup
{
    Properties = 
    {
        new MsBuildProperty { Name = "MyProp", Value = "MyValue" }
    }
});
```

### Elements Collection

The `Elements` collection maintains **insertion order** for rendering. When you add a property group, item group, import, or target, it's added to both:

1. The strongly-typed collection (`PropertyGroups`, `ItemGroups`, etc.)
2. The ordered `Elements` collection

This dual-tracking enables:
- Type-specific access via `project.PropertyGroups`
- Deterministic rendering order via `project.Elements`

## Property Structures

### MsBuildPropertyGroup

Represents a `<PropertyGroup>` element:

```csharp
public sealed class MsBuildPropertyGroup : IMsBuildProjectElement
{
    public string? Condition { get; set; }
    public string? Label { get; set; }
    public List<MsBuildProperty> Properties { get; }
    public List<IMsBuildPropertyGroupEntry> Entries { get; }
}
```

**Example:**

```csharp
var propertyGroup = new MsBuildPropertyGroup
{
    Condition = "'$(Configuration)' == 'Release'",
    Label = "Release Configuration"
};

propertyGroup.Properties.Add(new MsBuildProperty
{
    Name = "Optimize",
    Value = "true",
    Condition = "'$(Platform)' == 'AnyCPU'"
});

project.PropertyGroups.Add(propertyGroup);
project.Elements.Add(propertyGroup);
```

**Renders to:**

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'" Label="Release Configuration">
  <Optimize Condition="'$(Platform)' == 'AnyCPU'">true</Optimize>
</PropertyGroup>
```

### MsBuildProperty

Represents a single property:

```csharp
public sealed class MsBuildProperty : IMsBuildPropertyGroupEntry
{
    public required string Name { get; init; }
    public required string Value { get; init; }
    public string? Condition { get; init; }
}
```

**Required vs Optional:**
- `Name` and `Value` are **required** using C# 11's `required` keyword
- `Condition` is **optional**

## Item Structures

### MsBuildItemGroup

Represents an `<ItemGroup>` element:

```csharp
public sealed class MsBuildItemGroup : IMsBuildProjectElement
{
    public string? Condition { get; set; }
    public string? Label { get; set; }
    public List<MsBuildItem> Items { get; }
    public List<IMsBuildItemGroupEntry> Entries { get; }
}
```

### MsBuildItem

Represents an item operation (Include, Remove, Update):

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

**Example:**

```csharp
var item = new MsBuildItem
{
    ItemType = "Compile",
    Operation = MsBuildItemOperation.Include,
    Spec = "src/**/*.cs",
    Exclude = "src/**/*.g.cs"
};

item.Metadata["Link"] = "%(Filename)%(Extension)";
item.Metadata["CopyToOutputDirectory"] = "PreserveNewest";

var itemGroup = new MsBuildItemGroup();
itemGroup.Items.Add(item);
itemGroup.Entries.Add(item);

project.ItemGroups.Add(itemGroup);
project.Elements.Add(itemGroup);
```

**Renders to:**

```xml
<ItemGroup>
  <Compile Include="src/**/*.cs" Exclude="src/**/*.g.cs">
    <Link>%(Filename)%(Extension)</Link>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Compile>
</ItemGroup>
```

### Metadata: Elements vs Attributes

Items support metadata in two forms:

1. **Element metadata** (`Metadata` dictionary): Rendered as child elements
2. **Attribute metadata** (`MetadataAttributes` dictionary): Rendered as XML attributes

```csharp
item.Metadata["Visible"] = "false";  // <Visible>false</Visible>
item.MetadataAttributes["Pack"] = "true";  // Pack="true"
```

**Renders to:**

```xml
<None Include="README.md" Pack="true">
  <Visible>false</Visible>
</None>
```

**When to use each:**
- **Attributes**: Simple values, less verbose
- **Elements**: Complex values, multi-line content, property references

## Target Structures

### MsBuildTarget

Represents a `<Target>` element:

```csharp
public sealed class MsBuildTarget : IMsBuildProjectElement
{
    public required string Name { get; init; }
    public string? Condition { get; init; }
    public string? DependsOnTargets { get; init; }
    public string? BeforeTargets { get; init; }
    public string? AfterTargets { get; init; }
    public string? Inputs { get; init; }
    public string? Outputs { get; init; }
    public string? Returns { get; init; }
    public List<IMsBuildTargetElement> Elements { get; }
}
```

**Example:**

```csharp
var target = new MsBuildTarget
{
    Name = "MyCustomTarget",
    Condition = "'$(RunCustomTarget)' == 'true'",
    BeforeTargets = "Build",
    Inputs = "@(Compile)",
    Outputs = "$(IntermediateOutputPath)custom.txt"
};

target.Elements.Add(new MsBuildMessage
{
    Text = "Running custom target",
    Importance = "High"
});

target.Elements.Add(new MsBuildTask
{
    Name = "WriteLinesToFile",
    Parameters = new Dictionary<string, string>
    {
        ["File"] = "$(IntermediateOutputPath)custom.txt",
        ["Lines"] = "Custom content"
    }
});

project.Targets.Add(target);
project.Elements.Add(target);
```

## Task Structures

### MsBuildTask

Represents a task invocation:

```csharp
public sealed class MsBuildTask : IMsBuildTargetElement
{
    public required string Name { get; init; }
    public string? Condition { get; init; }
    public Dictionary<string, string> Parameters { get; }
    public List<MsBuildTaskOutput> Outputs { get; }
}
```

**Example:**

```csharp
var task = new MsBuildTask
{
    Name = "Copy",
    Condition = "'$(CopyEnabled)' == 'true'"
};

task.Parameters["SourceFiles"] = "@(Content)";
task.Parameters["DestinationFolder"] = "$(OutputPath)content";
task.Parameters["SkipUnchangedFiles"] = "true";

// Capture output
task.Outputs.Add(new MsBuildTaskOutput
{
    TaskParameter = "CopiedFiles",
    ItemName = "CopiedContentFiles"
});

target.Elements.Add(task);
```

**Renders to:**

```xml
<Copy SourceFiles="@(Content)" 
      DestinationFolder="$(OutputPath)content" 
      SkipUnchangedFiles="true"
      Condition="'$(CopyEnabled)' == 'true'">
  <Output TaskParameter="CopiedFiles" ItemName="CopiedContentFiles" />
</Copy>
```

### Built-in Task Elements

JD.MSBuild.Fluent provides dedicated IR types for common MSBuild tasks:

```csharp
public sealed class MsBuildMessage : IMsBuildTargetElement
{
    public required string Text { get; init; }
    public string? Importance { get; init; }
    public string? Condition { get; init; }
}

public sealed class MsBuildError : IMsBuildTargetElement
{
    public required string Text { get; init; }
    public string? Condition { get; init; }
    public string? Code { get; init; }
}

public sealed class MsBuildWarning : IMsBuildTargetElement
{
    public required string Text { get; init; }
    public string? Condition { get; init; }
    public string? Code { get; init; }
}

public sealed class MsBuildExec : IMsBuildTargetElement
{
    public required string Command { get; init; }
    public string? WorkingDirectory { get; init; }
    public string? Condition { get; init; }
}
```

**Usage:**

```csharp
target.Elements.Add(new MsBuildMessage
{
    Text = "Build starting...",
    Importance = "High"
});

target.Elements.Add(new MsBuildExec
{
    Command = "npm run build",
    WorkingDirectory = "$(MSBuildProjectDirectory)/client"
});

target.Elements.Add(new MsBuildError
{
    Text = "Build failed!",
    Condition = "'$(BuildFailed)' == 'true'",
    Code = "BUILD001"
});
```

## Import Structures

### MsBuildImport

Represents an `<Import>` element:

```csharp
public sealed class MsBuildImport : IMsBuildProjectElement
{
    public required string Project { get; init; }
    public string? Sdk { get; init; }
    public string? Condition { get; init; }
}
```

**Example:**

```csharp
// Regular import
var import = new MsBuildImport
{
    Project = "$(MSBuildThisFileDirectory)Common.props",
    Condition = "Exists('$(MSBuildThisFileDirectory)Common.props')"
};

project.Imports.Add(import);
project.Elements.Add(import);

// SDK import
var sdkImport = new MsBuildImport
{
    Project = "Sdk.props",
    Sdk = "Microsoft.NET.Sdk"
};

project.Imports.Add(sdkImport);
project.Elements.Add(sdkImport);
```

**Renders to:**

```xml
<Import Project="$(MSBuildThisFileDirectory)Common.props" 
        Condition="Exists('$(MSBuildThisFileDirectory)Common.props')" />
<Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
```

## Choose/When/Otherwise Structures

### MsBuildChoose

Represents conditional branching:

```csharp
public sealed class MsBuildChoose : IMsBuildProjectElement
{
    public List<MsBuildWhen> Whens { get; }
    public MsBuildOtherwise? Otherwise { get; set; }
}

public sealed class MsBuildWhen
{
    public required string Condition { get; init; }
    public MsBuildProject Project { get; } = new();
}

public sealed class MsBuildOtherwise
{
    public MsBuildProject Project { get; } = new();
}
```

**Example:**

```csharp
var choose = new MsBuildChoose();

var when1 = new MsBuildWhen
{
    Condition = "'$(Platform)' == 'x64'"
};
when1.Project.PropertyGroups.Add(new MsBuildPropertyGroup
{
    Properties =
    {
        new MsBuildProperty { Name = "PlatformTarget", Value = "x64" }
    }
});
choose.Whens.Add(when1);

var when2 = new MsBuildWhen
{
    Condition = "'$(Platform)' == 'x86'"
};
when2.Project.PropertyGroups.Add(new MsBuildPropertyGroup
{
    Properties =
    {
        new MsBuildProperty { Name = "PlatformTarget", Value = "x86" }
    }
});
choose.Whens.Add(when2);

choose.Otherwise = new MsBuildOtherwise();
choose.Otherwise.Project.PropertyGroups.Add(new MsBuildPropertyGroup
{
    Properties =
    {
        new MsBuildProperty { Name = "PlatformTarget", Value = "AnyCPU" }
    }
});

project.Chooses.Add(choose);
project.Elements.Add(choose);
```

## UsingTask Structures

### MsBuildUsingTask

Declares custom tasks:

```csharp
public sealed class MsBuildUsingTask : IMsBuildProjectElement
{
    public required string TaskName { get; init; }
    public string? AssemblyFile { get; init; }
    public string? AssemblyName { get; init; }
    public string? TaskFactory { get; init; }
    public string? Condition { get; init; }
}
```

**Example:**

```csharp
var usingTask = new MsBuildUsingTask
{
    TaskName = "MyCompany.CustomTask",
    AssemblyFile = "$(MSBuildThisFileDirectory)../../tools/MyCompany.Tasks.dll",
    Condition = "Exists('$(MSBuildThisFileDirectory)../../tools/MyCompany.Tasks.dll')"
};

project.UsingTasks.Add(usingTask);
project.Elements.Add(usingTask);
```

## Comments

### MsBuildComment

Add comments to any collection:

```csharp
public sealed class MsBuildComment : 
    IMsBuildProjectElement, 
    IMsBuildPropertyGroupEntry, 
    IMsBuildItemGroupEntry
{
    public required string Text { get; init; }
}
```

**Example:**

```csharp
// Project-level comment
project.Elements.Add(new MsBuildComment
{
    Text = "This section contains default properties"
});

// Property group comment
var propertyGroup = new MsBuildPropertyGroup();
propertyGroup.Entries.Add(new MsBuildComment
{
    Text = "Version properties"
});
propertyGroup.Entries.Add(new MsBuildProperty
{
    Name = "Version",
    Value = "1.0.0"
});
```

## Working Directly with IR

While the fluent API is recommended, you can work directly with IR:

### Creating IR Manually

```csharp
using JD.MSBuild.Fluent.IR;
using JD.MSBuild.Fluent.Render;

var project = new MsBuildProject
{
    Label = "Manual IR Example"
};

// Add property group
var pg = new MsBuildPropertyGroup();
pg.Properties.Add(new MsBuildProperty
{
    Name = "MyProperty",
    Value = "MyValue"
});
project.PropertyGroups.Add(pg);
project.Elements.Add(pg);

// Add target
var target = new MsBuildTarget
{
    Name = "MyTarget",
    BeforeTargets = "Build"
};
target.Elements.Add(new MsBuildMessage
{
    Text = "Hello from MyTarget"
});
project.Targets.Add(target);
project.Elements.Add(target);

// Render to XML
var renderer = new MsBuildXmlRenderer();
string xml = renderer.Render(project);
Console.WriteLine(xml);
```

### Manipulating Fluent-Generated IR

Access IR from fluent definitions:

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

var definition = Package.Define("MyPackage")
    .Props(p => p.Property("Prop1", "Value1"))
    .Build();

// Access underlying IR
var propsProject = definition.Props;
Console.WriteLine($"Properties: {propsProject.PropertyGroups.Count}");

// Modify IR directly
propsProject.PropertyGroups[0].Properties.Add(new MsBuildProperty
{
    Name = "Prop2",
    Value = "Value2"
});
```

## IR Validation

The validation system operates on IR:

```csharp
using JD.MSBuild.Fluent.Validation;

var validator = new MsBuildValidator();
var results = validator.Validate(project);

foreach (var result in results)
{
    Console.WriteLine($"{result.Severity}: {result.Message}");
}
```

## Advanced: Custom Renderers

Implement custom renderers by consuming IR:

```csharp
public class JsonRenderer
{
    public string Render(MsBuildProject project)
    {
        var obj = new
        {
            label = project.Label,
            propertyGroups = project.PropertyGroups.Select(pg => new
            {
                condition = pg.Condition,
                properties = pg.Properties.Select(p => new
                {
                    name = p.Name,
                    value = p.Value
                })
            }),
            targets = project.Targets.Select(t => new
            {
                name = t.Name,
                beforeTargets = t.BeforeTargets
            })
        };

        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
```

## Performance Considerations

### Memory Efficiency

- IR structures use **collection initializers** and **init-only properties**
- Avoid holding references to IR after rendering
- Use `List<T>` for collections (resizable, good for small sizes)

### Rendering Performance

- Rendering is **single-threaded** and **deterministic**
- Large projects (1000+ targets) render in milliseconds
- Canonical ordering adds ~10% overhead

## Summary

The IR layer provides:

- **Clear abstractions** for MSBuild constructs
- **Type safety** with required properties
- **Flexibility** for direct manipulation
- **Extensibility** for custom renderers
- **Foundation** for the fluent API

Understanding IR enables:
- Advanced package authoring
- Custom rendering scenarios
- Debugging complex definitions
- Extending JD.MSBuild.Fluent

## Next Steps

- [Package Structure](package-structure.md) - How IR maps to NuGet layout
- [Working with Properties](../properties-items/properties.md) - Property patterns
- [Target Orchestration](../targets-tasks/orchestration.md) - Target dependencies
- [XML Rendering](../../api/JD.MSBuild.Fluent.Render.MsBuildXmlRenderer.html) - API reference
