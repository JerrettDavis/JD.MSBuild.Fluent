# JD.MSBuild.Fluent Documentation

Welcome to the comprehensive documentation for **JD.MSBuild.Fluent**, a strongly-typed fluent DSL for authoring MSBuild `.props`, `.targets`, and SDK assets with the ergonomics of modern C#.

## What is JD.MSBuild.Fluent?

JD.MSBuild.Fluent is a library that transforms MSBuild package authoring from error-prone XML manipulation into type-safe, refactorable C# code. Define your MSBuild packages using intuitive fluent APIs, then emit them into the exact NuGet folder layout (`build/`, `buildTransitive/`, `Sdk/`) expected by MSBuild and NuGet.

### Key Features

- **Strongly-Typed DSL**: Author MSBuild constructs with IntelliSense, compile-time checking, and refactoring support
- **Fluent API**: Chain method calls naturally to build complex package definitions
- **100% Standard MSBuild**: Generates canonical, deterministic MSBuild XML that works everywhere
- **Intermediate Representation**: Composable IR layer separates authoring from rendering
- **Deterministic Output**: Canonical ordering of properties, items, and task parameters for meaningful diffs
- **Multi-Target Support**: Build packages targeting multiple frameworks or platforms
- **SDK-Style Packages**: Full support for MSBuild SDK-style project imports
- **Type-Safety Options**: Optional strongly-typed property, target, and item names

## Quick Example

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

var package = Package.Define("MyCompany.Build")
    .Description("Custom build tasks for MyCompany projects")
    .Props(p => p
        .Property("MyCompanyBuildEnabled", "true")
        .Property("MyCompanyVersion", "2.0.0"))
    .Targets(t => t
        .Target("MyCompany_PreBuild", target => target
            .BeforeTargets("Build")
            .Condition("'$(MyCompanyBuildEnabled)' == 'true'")
            .Message("MyCompany Build v$(MyCompanyVersion)", "High")))
    .Pack(o => o.BuildTransitive = true)
    .Build();
```

This generates clean MSBuild XML ready for packaging:

```xml
<Project>
  <PropertyGroup>
    <MyCompanyBuildEnabled>true</MyCompanyBuildEnabled>
    <MyCompanyVersion>2.0.0</MyCompanyVersion>
  </PropertyGroup>
</Project>
```

## Why Use JD.MSBuild.Fluent?

### Traditional XML Authoring Challenges

Authoring MSBuild packages manually involves:

- **No IntelliSense**: Easy to mistype property names, target names, or task parameters
- **No Refactoring**: Renaming requires manual find-and-replace across multiple files
- **Verbose Syntax**: XML is repetitive and hard to scan
- **No Reusability**: Difficult to extract common patterns into reusable functions
- **Merge Conflicts**: XML diffs are noisy and hard to resolve
- **Late Errors**: Typos and structural errors only appear at build time

### The JD.MSBuild.Fluent Approach

JD.MSBuild.Fluent treats MSBuild authoring as a first-class C# development experience:

- **Full IntelliSense**: Discover available methods and properties as you type
- **Compile-Time Safety**: Catch structural errors before generating any XML
- **Easy Refactoring**: Use IDE refactoring tools to rename properties, targets, and methods
- **DRY Principles**: Extract helper methods, share configurations, compose definitions
- **Clean Diffs**: Generated XML has canonical ordering for meaningful version control
- **Early Validation**: Validation rules run during build, before MSBuild sees the output

## Architecture Overview

JD.MSBuild.Fluent has a layered architecture:

```
┌─────────────────────────────────────┐
│     Fluent API (Builders)           │  ← You work here
│  Package.Define(...).Props().Build()│
├─────────────────────────────────────┤
│   Intermediate Representation (IR)  │  ← Composable data structures
│    MsBuildProject, MsBuildTarget    │
├─────────────────────────────────────┤
│         XML Renderer                │  ← Deterministic serialization
│     MsBuildXmlRenderer              │
├─────────────────────────────────────┤
│      Package Emitter                │  ← NuGet folder layout
│   build/, buildTransitive/, Sdk/    │
└─────────────────────────────────────┘
```

### Layers Explained

1. **Fluent API**: High-level builders (`Package`, `PropsBuilder`, `TargetsBuilder`) for natural C# authoring
2. **IR Layer**: Immutable data structures (`MsBuildProject`, `MsBuildTarget`, `MsBuildProperty`) representing MSBuild constructs
3. **Renderer**: Converts IR to canonical MSBuild XML with deterministic ordering
4. **Emitter**: Organizes rendered XML into NuGet package folder structure

## Getting Started

<div class="embeddedContent">
    <a href="user-guides/getting-started/installation.html" class="xref">Installation Guide</a>
</div>

<div class="embeddedContent">
    <a href="user-guides/getting-started/first-package.html" class="xref">Create Your First Package</a>
</div>

<div class="embeddedContent">
    <a href="user-guides/getting-started/quick-start.html" class="xref">Quick Start</a>
</div>

## Core Concepts

<div class="embeddedContent">
    <a href="user-guides/core-concepts/architecture.html" class="xref">Architecture & Design</a>
</div>

<div class="embeddedContent">
    <a href="user-guides/core-concepts/ir.html" class="xref">Intermediate Representation (IR)</a>
</div>

<div class="embeddedContent">
    <a href="user-guides/core-concepts/package-structure.html" class="xref">Package Structure</a>
</div>

## User Guides

### Properties, Items & Metadata
- [Working with Properties](user-guides/properties-items/properties.md)
- [Working with Items](user-guides/properties-items/items.md)
- [Item Metadata](user-guides/properties-items/metadata.md)
- [Conditional Logic](user-guides/properties-items/conditionals.md)

### Targets & Tasks
- [Target Orchestration](user-guides/targets-tasks/orchestration.md)
- [Built-in Tasks Reference](user-guides/targets-tasks/builtin-tasks.md)
- [Task Outputs](user-guides/targets-tasks/task-outputs.md)

### Advanced Topics
- [UsingTask Declarations](user-guides/advanced/usingtask.md)
- [Multi-Target Framework Patterns](user-guides/advanced/multi-tfm.md)
- [Choose/When/Otherwise](user-guides/advanced/choose.md)
- [Import Statements](user-guides/advanced/imports.md)
- [Strongly-Typed Helpers](user-guides/advanced/strongly-typed.md)

## CLI Reference

<div class="embeddedContent">
    <a href="user-guides/cli/index.html" class="xref">Command-Line Interface</a>
</div>

## API Reference

Browse the complete API documentation:

<div class="embeddedContent">
    <a href="api/index.html" class="xref">API Reference</a>
</div>

## Troubleshooting

<div class="embeddedContent">
    <a href="user-guides/troubleshooting/index.html" class="xref">Troubleshooting Guide</a>
</div>

## Samples

Explore working examples in the repository:

- **MinimalSdkPackage**: A complete end-to-end SDK-style package definition
- **ContosoSDK**: Comprehensive example demonstrating advanced patterns

## Contributing

Contributions are welcome! See the repository's CONTRIBUTING.md for guidelines on:

- Reporting bugs
- Proposing features
- Submitting pull requests
- Code style and conventions

## License

JD.MSBuild.Fluent is licensed under the MIT License. See LICENSE in the repository root for details.

## Additional Resources

- [MSBuild Concepts (Microsoft Docs)](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-concepts)
- [NuGet Package Authoring](https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package-msbuild)
- [MSBuild SDK Resolver](https://learn.microsoft.com/en-us/visualstudio/msbuild/how-to-use-project-sdk)
- [MSBuild Reserved and Well-Known Properties](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-reserved-and-well-known-properties)
