# Fluent Builders

Master the fluent builder API for authoring MSBuild packages with JD.MSBuild.Fluent. This guide covers all builder methods, patterns, and advanced techniques.

## Overview

JD.MSBuild.Fluent's builder API follows the fluent pattern: methods return `this` to enable chaining. Each builder is specialized for a specific MSBuild context (props, targets, property groups, etc.).

## PackageBuilder

The `PackageBuilder` is the root builder for defining packages.

### Entry Point: Package.Define()

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

PackageBuilder builder = Package.Define("MyPackage.Id");
```

### Configuration Methods

#### Description()

Sets the package description (metadata only):

```csharp
Package.Define("Contoso.Build")
    .Description("MSBuild package for Contoso projects")
```

#### Props()

Configures the primary props project (properties, items, imports):

```csharp
Package.Define("Contoso.Build")
    .Props(props =>
    {
        props.Property("ContosoEnabled", "true");
        props.Item<Compile>(MsBuildItemOperation.Include, "*.cs");
    })
```

#### Targets()

Configures the primary targets project (targets, UsingTask declarations):

```csharp
Package.Define("Contoso.Build")
    .Targets(targets =>
    {
        targets.Target("Contoso_Build", target =>
        {
            target.Message("Building with Contoso");
        });
    })
```

#### BuildProps() / BuildTargets()

Configures specific `build/` assets (overrides `Props()` and `Targets()` for `build/` location):

```csharp
Package.Define("Contoso.Build")
    .Props(p => p.Property("GlobalProp", "1"))
    .BuildProps(p => p.Property("BuildOnlyProp", "2"))
    .Targets(t => t.Target("GlobalTarget", tgt => { }))
    .BuildTargets(t => t.Target("BuildOnlyTarget", tgt => { }))
```

When `BuildProps()` is set, the `build/{Id}.props` file uses it instead of `Props()`.

#### BuildTransitiveProps() / BuildTransitiveTargets()

Configures `buildTransitive/` assets:

```csharp
Package.Define("Contoso.Build")
    .BuildTransitiveProps(p => p.Property("TransitiveProp", "1"))
    .BuildTransitiveTargets(t => t.Target("TransitiveTarget", tgt => { }))
```

#### SdkProps() / SdkTargets()

Configures SDK assets (`Sdk/{Id}/Sdk.props` and `Sdk/{Id}/Sdk.targets`):

```csharp
Package.Define("Contoso.SDK")
    .SdkProps(p => p.Property("ContosoSdkEnabled", "true"))
    .SdkTargets(t => t.Target("Contoso_SdkInit", tgt => { }))
    .Pack(o => o.EmitSdk = true)
```

**Important**: Set `PackagePackagingOptions.EmitSdk = true` to generate SDK assets.

#### Pack()

Configures packaging options:

```csharp
Package.Define("Contoso.Build")
    .Pack(options =>
    {
        options.BuildTransitive = true;
        options.EmitSdk = false;
        options.BuildAssetBasename = "Custom.Basename";
    })
```

#### Build()

Returns the final `PackageDefinition`:

```csharp
PackageDefinition definition = Package.Define("Contoso.Build")
    .Props(p => { /* ... */ })
    .Targets(t => { /* ... */ })
    .Build();
```

## PropsBuilder

The `PropsBuilder` configures `.props` files (properties, items, imports, Choose/When).

### Properties

#### Property()

Adds a property to the last property group, or creates a new group if none exists:

```csharp
props.Property("MyProperty", "MyValue");
props.Property("MyProperty", "MyValue", condition: "'$(Debug)' == 'true'");
```

**Strongly-typed overloads**:

```csharp
// Generic overload
props.Property<Configuration>("Debug");

// Instance overload
props.Property(new Configuration(), "Debug");
```

#### PropertyGroup()

Creates an explicit property group with a condition and label:

```csharp
props.PropertyGroup("'$(Configuration)' == 'Release'", group =>
{
    group.Property("Optimize", "true");
    group.Property("DebugSymbols", "false");
}, label: "Release Configuration");
```

**PropsGroupBuilder methods**:
- `Property(string name, string value, string? condition = null)`
- `Property<TProperty>(string value, string? condition = null)`
- `Comment(string text)`

### Items

#### Item()

Adds an item to the last item group, or creates a new group if none exists:

```csharp
// Include
props.Item("Compile", MsBuildItemOperation.Include, "*.cs");

// Include with metadata
props.Item("None", MsBuildItemOperation.Include, "README.md", item =>
{
    item.Meta("Pack", "true");
    item.Meta("PackagePath", "content/");
}, condition: "'$(IncludeReadme)' == 'true'");

// Remove
props.Item("Compile", MsBuildItemOperation.Remove, "Generated/*.cs");

// Update
props.Item("Compile", MsBuildItemOperation.Update, "*.cs", item =>
{
    item.Meta("AutoGen", "true");
});
```

**Strongly-typed overloads**:

```csharp
// Generic overload
props.Item<Compile>(MsBuildItemOperation.Include, "**/*.cs", item =>
{
    item.Meta<AutoGen>("true");
});

// Instance overload
props.Item(new Compile(), MsBuildItemOperation.Include, "**/*.cs");
```

**ItemBuilder methods**:
- `Meta(string name, string value)` - Adds metadata as child element
- `MetaAttribute(string name, string value)` - Adds metadata as attribute
- `Exclude(string spec)` - Sets the Exclude attribute

#### ItemGroup()

Creates an explicit item group:

```csharp
props.ItemGroup("'$(EnableFeature)' == 'true'", group =>
{
    group.Include<Compile>("Feature/*.cs");
    group.Remove<Compile>("Legacy/*.cs");
    group.Update<Compile>("Generated/*.cs", item =>
    {
        item.Meta("Visible", "false");
    });
}, label: "Feature Items");
```

**ItemGroupBuilder methods**:
- `Include(string itemType, string spec, Action<ItemBuilder>? configure = null, string? condition = null, string? exclude = null)`
- `Include<TItem>(string spec, ...)`
- `Remove(string itemType, string spec, string? condition = null)`
- `Remove<TItem>(string spec, string? condition = null)`
- `Update(string itemType, string spec, Action<ItemBuilder> configure, string? condition = null)`
- `Update<TItem>(string spec, Action<ItemBuilder> configure, string? condition = null)`
- `Comment(string text)`

### Imports

#### Import()

Adds an `<Import>` element:

```csharp
// Basic import
props.Import("$(MSBuildToolsPath)\\Microsoft.CSharp.targets");

// Conditional import
props.Import("Custom.props", 
    condition: "Exists('Custom.props')");

// SDK import
props.Import("Sdk.props", 
    sdk: "Microsoft.NET.Sdk");
```

### Choose/When/Otherwise

#### Choose()

Adds conditional logic with `<Choose>`, `<When>`, and `<Otherwise>`:

```csharp
props.Choose(choose =>
{
    choose.When("'$(TargetFramework)' == 'net8.0'", whenProps =>
    {
        whenProps.Property("UseMinimals", "true");
    });
    
    choose.When("'$(TargetFramework)' == 'net6.0'", whenProps =>
    {
        whenProps.Property("UseMinimals", "false");
    });
    
    choose.Otherwise(otherwiseProps =>
    {
        otherwiseProps.Property("UseMinimals", "false");
    });
});
```

**Complex example with items**:

```csharp
props.Choose(choose =>
{
    choose.When("$([MSBuild]::IsOSPlatform('Windows'))", whenProps =>
    {
        whenProps.Property("NativeLib", "lib-win.dll");
        whenProps.ItemGroup(null, group =>
        {
            group.Include<None>("runtimes/win-x64/native/*.dll");
        });
    });
    
    choose.When("$([MSBuild]::IsOSPlatform('Linux'))", whenProps =>
    {
        whenProps.Property("NativeLib", "lib-linux.so");
        whenProps.ItemGroup(null, group =>
        {
            group.Include<None>("runtimes/linux-x64/native/*.so");
        });
    });
    
    choose.Otherwise(otherwiseProps =>
    {
        otherwiseProps.Property("NativeLib", "lib-osx.dylib");
    });
});
```

### Comments

#### Comment()

Adds a project-level comment:

```csharp
props.Comment("=================================");
props.Comment(" Default Properties");
props.Comment("=================================");
props.Property("DefaultProp", "1");
```

## TargetsBuilder

The `TargetsBuilder` configures `.targets` files (targets, UsingTask, imports).

### UsingTask

#### UsingTask()

Declares a custom task:

```csharp
// From assembly file
targets.UsingTask("MyTask", 
    assemblyFile: "$(MSBuildThisFileDirectory)../tasks/net472/MyTasks.dll");

// From assembly name
targets.UsingTask("MyTask", 
    assemblyFile: null, 
    assemblyName: "MyTasks, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

// With condition
targets.UsingTask("MyTask", 
    assemblyFile: "$(MSBuildThisFileDirectory)../tasks/netstandard2.0/MyTasks.dll",
    condition: "'$(MSBuildRuntimeType)' == 'Core'");

// With TaskFactory
targets.UsingTask("InlineTask", 
    assemblyFile: null, 
    taskFactory: "RoslynCodeTaskFactory",
    assemblyName: "$(MSBuildToolsPath)\\Microsoft.Build.Tasks.Core.dll");
```

**Strongly-typed overloads**:

```csharp
using JD.MSBuild.Fluent.Typed;

// From CLR type
targets.UsingTask<MyCustomTask>(
    assemblyFile: "$(MSBuildThisFileDirectory)../tasks/net8.0/MyTasks.dll");

// From task reference
var taskRef = MsBuildTaskReference.FromType<MyCustomTask>(
    nameStyle: MsBuildTaskNameStyle.FullName);
targets.UsingTask(taskRef);
```

### Targets

#### Target()

Defines a build target:

```csharp
targets.Target("MyTarget", target =>
{
    target.BeforeTargets("Build");
    target.Condition("'$(MyTargetEnabled)' == 'true'");
    target.Message("Running MyTarget");
});
```

**Strongly-typed overload**:

```csharp
targets.Target<MyTarget>(target =>
{
    // Configuration
});
```

See [Targets](../targets-tasks/targets.md) for comprehensive target configuration.

### Imports and Comments

The `TargetsBuilder` also supports `Import()` and `Comment()` with the same syntax as `PropsBuilder`.

### Project-Level PropertyGroup and ItemGroup

You can add property groups and item groups at the project level in targets files:

```csharp
targets.PropertyGroup(null, group =>
{
    group.Property("TargetsProp", "value");
});

targets.ItemGroup(null, group =>
{
    group.Include<TargetItem>("items/*.txt");
});
```

**Note**: PropertyGroups and ItemGroups in targets files are evaluated during the evaluation phase, not execution phase. Place execution-time logic inside targets.

## TargetBuilder

The `TargetBuilder` configures individual targets.

### Target Attributes

#### Condition()

Sets the target condition:

```csharp
target.Condition("'$(Configuration)' == 'Release' AND '$(Optimize)' == 'true'");
```

#### BeforeTargets() / AfterTargets()

Orchestrates target execution order:

```csharp
// String syntax
target.BeforeTargets("Build");
target.AfterTargets("CoreCompile");

// Multiple targets
target.BeforeTargets("Build;Test;Pack");

// Strongly-typed syntax
target.BeforeTargets(new MsBuildTargets.Build(), new MsBuildTargets.Test());
target.AfterTargets(new MsBuildTargets.CoreCompile());
```

#### DependsOnTargets()

Specifies dependencies:

```csharp
// String syntax
target.DependsOnTargets("Restore;Build");

// Strongly-typed syntax
target.DependsOnTargets(new Restore(), new Build());
```

#### Inputs() / Outputs()

Enables incremental builds:

```csharp
target.Inputs("@(Compile)");
target.Outputs("$(IntermediateOutputPath)%(Compile.Filename).dll");
```

#### Label()

Adds a human-readable label:

```csharp
target.Label("Custom Build Step");
```

### Task Invocations

#### Message()

Logs a message:

```csharp
target.Message("Build started", importance: "High");
target.Message("Debug info", importance: "Low", condition: "'$(Debug)' == 'true'");
```

#### Exec()

Executes a command:

```csharp
target.Exec("dotnet --version");
target.Exec("npm install", workingDirectory: "$(MSBuildProjectDirectory)/client");
target.Exec("echo Done", condition: "'$(Verbose)' == 'true'");
```

#### Error() / Warning()

Reports errors or warnings:

```csharp
target.Error("Missing required property: $(RequiredProp)", 
    code: "CONTOSO001", 
    condition: "'$(RequiredProp)' == ''");

target.Warning("Deprecated feature used", 
    code: "CONTOSO002");
```

#### Task()

Invokes arbitrary tasks:

```csharp
// Built-in task
target.Task("MakeDir", task =>
{
    task.Param("Directories", "$(OutputPath)");
});

// Custom task with outputs
target.Task("MyCustomTask", task =>
{
    task.Param("InputFile", "$(InputFile)");
    task.Param("OutputFile", "$(OutputFile)");
    task.OutputProperty("ResultCount", "ResultCount");
    task.OutputItem("GeneratedFiles", "GeneratedFiles");
});

// Strongly-typed task
target.Task<WriteLinesToFile>(task =>
{
    task.Param("File", "$(OutputFile)");
    task.Param("Lines", "@(LinesToWrite)");
    task.Param("Overwrite", "true");
});
```

See [Custom Tasks](../targets-tasks/custom-tasks.md) for details on task parameters and outputs.

### PropertyGroup and ItemGroup Inside Targets

You can create property groups and item groups inside targets for dynamic evaluation:

```csharp
target.PropertyGroup(null, group =>
{
    group.Property("ComputedValue", "$([System.DateTime]::Now.Ticks)");
});

target.ItemGroup(null, group =>
{
    group.Include<FilesToCopy>("$(SourceDir)/**/*.txt");
});
```

### Comments

```csharp
target.Comment("Step 1: Prepare");
target.Exec("prepare.cmd");
target.Comment("Step 2: Build");
target.Exec("build.cmd");
```

## TaskInvocationBuilder

The `TaskInvocationBuilder` configures task parameters and outputs.

### Parameters

#### Param()

Sets a task parameter:

```csharp
task.Param("File", "$(OutputFile)");
task.Param("Lines", "@(LinesToWrite)");
task.Param("Overwrite", "true");
```

**Strongly-typed overload**:

```csharp
task.Param<FileParameter>("$(OutputFile)");
```

### Outputs

#### OutputProperty()

Maps a task output to a property:

```csharp
task.OutputProperty("Result", "MyResultProperty");
task.OutputProperty("Result", "MyResultProperty", condition: "'$(CaptureResult)' == 'true'");
```

**Strongly-typed overload**:

```csharp
task.OutputProperty<ResultParameter, ResultProperty>();
task.OutputProperty(new ResultParameter(), new ResultProperty());
```

#### OutputItem()

Maps a task output to an item:

```csharp
task.OutputItem("GeneratedFiles", "GeneratedFiles");
task.OutputItem("GeneratedFiles", "GeneratedFiles", condition: "'$(CaptureFiles)' == 'true'");
```

**Strongly-typed overload**:

```csharp
task.OutputItem<GeneratedFilesParameter, GeneratedFilesItem>();
```

## Strongly-Typed Names

Strongly-typed names provide compile-time safety and IntelliSense support.

### Defining Typed Names

```csharp
using JD.MSBuild.Fluent.Typed;

// Property name
public readonly struct Configuration : IMsBuildPropertyName
{
    public string Name => "Configuration";
}

// Item type
public readonly struct Compile : IMsBuildItemTypeName
{
    public string Name => "Compile";
}

// Target name
public readonly struct Build : IMsBuildTargetName
{
    public string Name => "Build";
}

// Metadata name
public readonly struct Link : IMsBuildMetadataName
{
    public string Name => "Link";
}

// Task name
public readonly struct WriteLinesToFile : IMsBuildTaskName
{
    public string Name => "WriteLinesToFile";
}

// Task parameter name
public readonly struct FileParameter : IMsBuildTaskParameterName
{
    public string Name => "File";
}
```

### Using Typed Names

```csharp
// Properties
props.Property<Configuration>("Debug");

// Items
props.Item<Compile>(MsBuildItemOperation.Include, "*.cs", item =>
{
    item.Meta<Link>("%(Filename)%(Extension)");
});

// Targets
targets.Target<Build>(target =>
{
    target.Message("Building...");
});

// Tasks
target.Task<WriteLinesToFile>(task =>
{
    task.Param<FileParameter>("output.txt");
});
```

### Wrapper Structs

For ad-hoc usage without defining custom types:

```csharp
props.Property(new MsBuildPropertyName("MyProperty"), "value");
props.Item(new MsBuildItemTypeName("MyItem"), MsBuildItemOperation.Include, "spec");
targets.Target(new MsBuildTargetName("MyTarget"), target => { });
```

## Expression Helpers

The `MsBuildExpr` static class provides helpers for building MSBuild expressions.

### Property References

```csharp
using static JD.MSBuild.Fluent.Typed.MsBuildExpr;

// Prop<T>() => "$(PropertyName)"
string expr = Prop<Configuration>();  // "$(Configuration)"

// Usage
props.Property("MyDerivedProp", Prop<Configuration>());
```

### Item References

```csharp
// Item<T>() => "@(ItemType)"
string items = Item<Compile>();  // "@(Compile)"

// Usage
target.Task("Copy", task =>
{
    task.Param("SourceFiles", Item<Compile>());
});
```

### Conditions

```csharp
// IsTrue<T>() => "'$(Property)' == 'true'"
string cond = IsTrue<EnableFeature>();

// Eq<T>(value) => "'$(Property)' == 'value'"
string cond2 = Eq<Configuration>("Release");

// Usage
target.Condition(IsTrue<EnableFeature>());
props.PropertyGroup(Eq<Configuration>("Release"), group => { });
```

### Custom Expressions

```csharp
// Build complex expressions
string expr = $"{Prop<MSBuildRuntimeType>()} == 'Core' AND {IsTrue<EnableFeature>()}";
```

## Advanced Patterns

### Builder Extension Methods

Create domain-specific extensions:

```csharp
public static class ContosoExtensions
{
    public static PropsBuilder AddContosoProperties(
        this PropsBuilder props, 
        string version)
    {
        return props
            .Property("ContosoEnabled", "true")
            .Property("ContosoVersion", version)
            .Property("ContosoToolsPath", 
                "$(MSBuildThisFileDirectory)../../tools");
    }

    public static TargetsBuilder AddContosoTargets(this TargetsBuilder targets)
    {
        return targets
            .Target("Contoso_PreBuild", t => t
                .BeforeTargets("Build")
                .Message("Contoso Pre-Build v$(ContosoVersion)"))
            .Target("Contoso_PostBuild", t => t
                .AfterTargets("Build")
                .Message("Contoso Post-Build Complete"));
    }
}

// Usage
Package.Define("MyPackage")
    .Props(p => p.AddContosoProperties("2.0.0"))
    .Targets(t => t.AddContosoTargets())
    .Build();
```

### Shared Configuration

Extract shared configuration into helper methods:

```csharp
private static void ConfigureCommonProps(PropsBuilder props)
{
    props.Property("Company", "Contoso");
    props.Property("Copyright", "© 2024 Contoso Corporation");
}

private static void ConfigureDebugProps(PropsBuilder props)
{
    ConfigureCommonProps(props);
    props.Property("Optimize", "false");
    props.Property("DebugSymbols", "true");
}

// Usage
Package.Define("Contoso.Build")
    .Props(ConfigureDebugProps)
    .Build();
```

### Conditional Builder Logic

Use C# conditionals to drive MSBuild configuration:

```csharp
public static PackageDefinition Create(bool enableAdvancedFeatures)
{
    var builder = Package.Define("Contoso.Build")
        .Props(p => p.Property("ContosoEnabled", "true"));

    if (enableAdvancedFeatures)
    {
        builder.Props(p => p.Property("ContosoAdvanced", "true"));
        builder.Targets(t => t.Target("Contoso_Advanced", target =>
        {
            target.Message("Advanced features enabled");
        }));
    }

    return builder.Build();
}
```

### Multi-Target Framework UsingTask

Declare tasks for multiple target frameworks:

```csharp
targets.UsingTask("MyTask", 
    assemblyFile: "$(MSBuildThisFileDirectory)../tasks/net472/MyTasks.dll",
    condition: "'$(MSBuildRuntimeType)' != 'Core'");

targets.UsingTask("MyTask", 
    assemblyFile: "$(MSBuildThisFileDirectory)../tasks/netstandard2.0/MyTasks.dll",
    condition: "'$(MSBuildRuntimeType)' == 'Core'");
```

## Best Practices

### Use Strongly-Typed Names

Define reusable typed names for properties, items, and targets you reference frequently:

```csharp
// ✅ Good - compile-time safety
props.Property<Configuration>("Release");

// ❌ Avoid - typos not caught until runtime
props.Property("Configuraton", "Release");  // Typo!
```

### Group Related Properties and Items

```csharp
// ✅ Good - logical grouping
props.PropertyGroup(null, group =>
{
    group.Comment("Output settings");
    group.Property("OutputPath", "bin/");
    group.Property("IntermediateOutputPath", "obj/");
});

// ❌ Less clear
props.Comment("Output settings");
props.Property("OutputPath", "bin/");
props.Property("IntermediateOutputPath", "obj/");
```

### Use Extension Methods for Reusability

Extract common patterns into extension methods rather than copy-pasting configuration.

### Explicit Groups for Conditions

When adding conditional groups, use explicit `PropertyGroup()` or `ItemGroup()` calls:

```csharp
// ✅ Good - explicit conditional group
props.PropertyGroup("'$(Configuration)' == 'Release'", group =>
{
    group.Property("Optimize", "true");
});

// ❌ Avoid - implicit group with condition on each property
props.Property("Optimize", "true", condition: "'$(Configuration)' == 'Release'");
```

### Prefer Choose for Multi-Way Branching

Use `Choose` for multiple mutually exclusive branches:

```csharp
// ✅ Good - clear intent
props.Choose(choose =>
{
    choose.When("'$(OS)' == 'Windows_NT'", w => w.Property("Shell", "cmd"));
    choose.When("'$(OS)' == 'Unix'", w => w.Property("Shell", "bash"));
    choose.Otherwise(o => o.Property("Shell", "sh"));
});

// ❌ Less clear - multiple conditional groups
props.PropertyGroup("'$(OS)' == 'Windows_NT'", g => g.Property("Shell", "cmd"));
props.PropertyGroup("'$(OS)' == 'Unix'", g => g.Property("Shell", "bash"));
props.PropertyGroup("'$(OS)' != 'Windows_NT' AND '$(OS)' != 'Unix'", g => g.Property("Shell", "sh"));
```

## Next Steps

- [Targets](../targets-tasks/targets.md) - Deep dive into target orchestration
- [Custom Tasks](../targets-tasks/custom-tasks.md) - UsingTask and task invocation patterns
- [Best Practices](../best-practices/index.md) - Patterns for robust packages
- [Migration Guide](../migration/from-xml.md) - Convert XML to fluent API

## Related Topics

- [Architecture](architecture.md) - Understanding the IR and rendering layers
- [Quick Start](../getting-started/quick-start.md) - Get started quickly
