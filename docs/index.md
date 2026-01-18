# JD.MSBuild.Fluent

**A strongly-typed, fluent DSL for authoring MSBuild packages in C#**

[![NuGet](https://img.shields.io/nuget/v/JD.MSBuild.Fluent.svg)](https://www.nuget.org/packages/JD.MSBuild.Fluent/) 
[![License](https://img.shields.io/github/license/jerrettdavis/JD.MSBuild.Fluent.svg)](https://github.com/JerrettDavis/JD.MSBuild.Fluent/blob/main/LICENSE) 
[![CI](https://github.com/JerrettDavis/JD.MSBuild.Fluent/actions/workflows/ci.yml/badge.svg)](https://github.com/JerrettDavis/JD.MSBuild.Fluent/actions/workflows/ci.yml) 
[![codecov](https://codecov.io/gh/JerrettDavis/JD.MSBuild.Fluent/branch/main/graph/badge.svg)](https://codecov.io/gh/JerrettDavis/JD.MSBuild.Fluent)

Author MSBuild `.props`, `.targets`, and SDK assets using a strongly-typed fluent API in C#, then automatically generate **100% standard MSBuild XML** during build. No more hand-editing XML - write refactorable, testable, type-safe C# code instead.

## ✨ Features

- 🎯 **Strongly-typed fluent API** - IntelliSense, refactoring, compile-time validation
- 🔄 **Automatic build integration** - Generate MSBuild assets during `dotnet build`, no CLI required
- 📦 **Full NuGet layout support** - `build/`, `buildTransitive/`, and `Sdk/` folders
- 🔧 **XML scaffolding** - Convert existing XML to fluent code with `jdmsbuild scaffold`
- ✅ **Production-tested** - Validated against real-world MSBuild packages
- 📝 **Deterministic output** - Consistent XML generation for meaningful diffs

## Quick Start

### 1. Install the package

```xml
<PackageReference Include="JD.MSBuild.Fluent" Version="*" />
```

### 2. Define your MSBuild assets in C#

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace MySdk;

public static class DefinitionFactory
{
  public static PackageDefinition Create() => Package.Define("MySdk")
    .Props(p => p
      .Property("MySdkEnabled", "true")
      .Property("MySdkVersion", "1.0.0"))
    .Targets(t => t
      .Target("MySdk_Hello", target => target
        .BeforeTargets("Build")
        .Condition("'$(MySdkEnabled)' == 'true'")
        .Message("Hello from MySdk v$(MySdkVersion)!", "High")))
    .Pack(o => { 
      o.BuildTransitive = true; 
      o.EmitSdk = true; 
    })
    .Build();
}
```

### 3. Build your project

MSBuild assets are **automatically generated during build** and packaged correctly:

- ✅ `build/MySdk.props`
- ✅ `build/MySdk.targets`
- ✅ `buildTransitive/MySdk.props` and `.targets`
- ✅ `Sdk/MySdk/Sdk.props` and `Sdk.targets`

**No CLI required!** Just build and pack:

```bash
dotnet build
dotnet pack
```

## 🎯 Why JD.MSBuild.Fluent?

### Problem: Hand-editing MSBuild XML is painful
- ❌ No IntelliSense or type safety
- ❌ No refactoring support
- ❌ Hard to test and validate
- ❌ Copy-paste leads to duplication
- ❌ Difficult to review diffs

### Solution: Write C# instead
- ✅ **Strongly-typed API** with full IntelliSense
- ✅ **Refactoring support** - rename, extract, move
- ✅ **Unit testable** - validate logic before publishing
- ✅ **DRY principle** - reuse patterns across targets
- ✅ **Better diffs** - meaningful C# changes instead of XML noise
- ✅ **Automatic generation** - integrated into build pipeline

## 📚 Documentation

### Getting Started
- [Introduction](articles/introduction.md) - Project overview and core concepts
- [Getting Started Guide](articles/getting-started.md) - Installation and your first package
- [Examples](articles/examples.md) - Real-world usage examples

### User Guides
- [Overview](user-guides/overview.md) - Guide to all user documentation
- [Basic Concepts](user-guides/basic-concepts/fluent-api-overview.md) - Understanding the fluent API
- [MSBuild Integration](user-guides/build-integration/automatic-generation.md) - How build integration works
- [Migration from XML](user-guides/migration/xml-to-fluent.md) - Convert existing XML to fluent
- [Advanced Topics](user-guides/advanced/custom-tasks.md) - Custom tasks, conditions, and more

### Reference
- [API Reference](api/index.md) - Complete API documentation
- [CLI Reference](user-guides/cli/overview.md) - Command-line tool documentation

## 🔄 Migrate from XML

Convert existing MSBuild XML files to fluent API:

```bash
# Install CLI tool
dotnet tool install -g JD.MSBuild.Fluent.Cli

# Scaffold from existing XML
jdmsbuild scaffold --xml MyPackage.targets --output DefinitionFactory.cs --package-id MyCompany.MyPackage
```

See the [Migration Guide](user-guides/migration/xml-to-fluent.md) for complete details.

## 🧪 Samples

- [Minimal SDK Package](https://github.com/JerrettDavis/JD.MSBuild.Fluent/tree/main/samples/MinimalSdkPackage) - Complete end-to-end example
- Integration tests validate against [JD.Efcpt.Build](https://github.com/JerrettDavis/JD.Efcpt.Build) (real-world production package)

## 🏗️ Architecture Overview

JD.MSBuild.Fluent has a clean, layered architecture:

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

1. **Fluent API**: High-level builders for natural C# authoring
2. **IR Layer**: Immutable data structures representing MSBuild constructs
3. **Renderer**: Converts IR to canonical MSBuild XML with deterministic ordering
4. **Emitter**: Organizes rendered XML into NuGet package folder structure

## 🤝 Contributing

Contributions welcome! See our [GitHub repository](https://github.com/JerrettDavis/JD.MSBuild.Fluent) to get started.

## 📄 License

[MIT License](https://github.com/JerrettDavis/JD.MSBuild.Fluent/blob/main/LICENSE)

## 🔗 Related Projects

- [JD.Efcpt.Build](https://github.com/JerrettDavis/JD.Efcpt.Build) - EF Core Power Tools build integration
- [JD.MSBuild.Containers](https://github.com/JerrettDavis/JD.MSBuild.Containers) - Docker container build integration
