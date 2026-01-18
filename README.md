# JD.MSBuild.Fluent

**A strongly-typed, fluent DSL for authoring MSBuild packages in C#**

[![NuGet](https://img.shields.io/nuget/v/JD.MSBuild.Fluent.svg)](https://www.nuget.org/packages/JD.MSBuild.Fluent/) 
[![License](https://img.shields.io/github/license/jerrettdavis/JD.MSBuild.Fluent.svg)](LICENSE) 
[![CI](https://github.com/JerrettDavis/JD.MSBuild.Fluent/actions/workflows/ci.yml/badge.svg)](https://github.com/JerrettDavis/JD.MSBuild.Fluent/actions/workflows/ci.yml) 
[![codecov](https://codecov.io/gh/JerrettDavis/JD.MSBuild.Fluent/branch/main/graph/badge.svg)](https://codecov.io/gh/JerrettDavis/JD.MSBuild.Fluent) 
[![Documentation](https://img.shields.io/badge/docs-online-blue)](https://jerrettdavis.github.io/JD.MSBuild.Fluent/)

Author MSBuild `.props`, `.targets`, and SDK assets using a strongly-typed fluent API in C#, then automatically generate **100% standard MSBuild XML** during build. No more hand-editing XML - write refactorable, testable, type-safe C# code instead.

## 📚 Documentation

**[View Complete Documentation](https://jerrettdavis.github.io/JD.MSBuild.Fluent/)**

- [Introduction](https://jerrettdavis.github.io/JD.MSBuild.Fluent/articles/introduction.html) - Project overview and core concepts
- [Getting Started](https://jerrettdavis.github.io/JD.MSBuild.Fluent/articles/getting-started.html) - Installation and quick start guide
- [User Guides](https://jerrettdavis.github.io/JD.MSBuild.Fluent/user-guides/overview.html) - Detailed tutorials and patterns
- [API Reference](https://jerrettdavis.github.io/JD.MSBuild.Fluent/api/) - Complete API documentation
- [Migration Guide](https://jerrettdavis.github.io/JD.MSBuild.Fluent/user-guides/migration/xml-to-fluent.html) - Convert existing XML to fluent API

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
- ✅ `buildTransitive/MySdk.props` and `.targets` (if enabled)
- ✅ `Sdk/MySdk/Sdk.props` and `Sdk.targets` (if SDK enabled)

**No CLI required!** Just build and pack:

```bash
dotnet build
dotnet pack
```

### Optional: Configure generation

```xml
<PropertyGroup>
  <!-- Enable/disable generation (default: true) -->
  <JDMSBuildFluentGenerateEnabled>true</JDMSBuildFluentGenerateEnabled>
  
  <!-- Specify factory type (default: auto-detect) -->
  <JDMSBuildFluentDefinitionType>MySdk.DefinitionFactory</JDMSBuildFluentDefinitionType>
  
  <!-- Output directory (default: obj/msbuild) -->
  <JDMSBuildFluentOutputDir>$(MSBuildProjectDirectory)\msbuild</JDMSBuildFluentOutputDir>
</PropertyGroup>
```

## 🔄 Migrate from XML

Convert existing MSBuild XML files to fluent API:

```bash
# Install CLI tool
dotnet tool install -g JD.MSBuild.Fluent.Cli

# Scaffold from existing XML
jdmsbuild scaffold --xml MyPackage.targets --output DefinitionFactory.cs --package-id MyCompany.MyPackage
```

**Before (XML)**:
```xml
<Project>
  <PropertyGroup>
    <MyPackageVersion>1.0.0</MyPackageVersion>
  </PropertyGroup>
  <Target Name="Hello" BeforeTargets="Build">
    <Message Text="Hello from MyPackage v$(MyPackageVersion)!" Importance="High" />
  </Target>
</Project>
```

**After (Fluent C#)**:
```csharp
public static PackageDefinition Create()
{
    return Package.Define("MyPackage")
        .Targets(t =>
        {
            t.PropertyGroup(null, group =>
            {
                group.Property("MyPackageVersion", "1.0.0");
            });
            t.Target("Hello", target =>
            {
                target.BeforeTargets("Build");
                target.Message("Hello from MyPackage v$(MyPackageVersion)!", "High");
            });
        })
        .Build();
}
```

Now you can refactor, test, and maintain your MSBuild logic like regular C# code!

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

## 📦 CLI Tool (Optional)

The CLI is optional for advanced scenarios. Most users don't need it since generation happens automatically during build.

```bash
# Install globally
dotnet tool install -g JD.MSBuild.Fluent.Cli

# Generate assets manually
jdmsbuild generate --assembly path/to/MySdk.dll --type MySdk.DefinitionFactory --output msbuild

# Generate built-in example
jdmsbuild generate --example --output artifacts/msbuild

# Scaffold from XML
jdmsbuild scaffold --xml MyPackage.targets --output DefinitionFactory.cs
```

## 📁 Output Layout

Generated files follow standard NuGet conventions:

```
build/
  MySdk.props          ← Applied to direct consumers
  MySdk.targets        ← Applied to direct consumers
buildTransitive/       ← (optional)
  MySdk.props          ← Applied transitively
  MySdk.targets        ← Applied transitively
Sdk/                   ← (optional)
  MySdk/
    Sdk.props          ← SDK-style project support
    Sdk.targets        ← SDK-style project support
```

## 🧪 Samples

- [`samples/MinimalSdkPackage`](samples/MinimalSdkPackage) - Complete end-to-end example
- Integration tests validate against [JD.Efcpt.Build](https://github.com/JerrettDavis/JD.Efcpt.Build) (real-world production package)

## 🔍 Deterministic Output

The XML renderer produces **deterministic, canonical output**:
- Consistent property ordering
- Consistent item metadata ordering
- Consistent task parameter ordering
- Meaningful diffs across versions

## 🤝 Contributing

Contributions welcome! This project follows standard GitHub flow:
1. Fork the repository
2. Create a feature branch
3. Make your changes with tests
4. Submit a pull request

## 📄 License

[MIT License](LICENSE)

## 🔗 Related Projects

- [JD.Efcpt.Build](https://github.com/JerrettDavis/JD.Efcpt.Build) - EF Core Power Tools build integration (uses JD.MSBuild.Fluent)
- [JD.MSBuild.Containers](https://github.com/JerrettDavis/JD.MSBuild.Containers) - Docker container build integration
