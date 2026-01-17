# Working with MSBuild Properties

Properties are the fundamental building blocks of MSBuild evaluation. This guide covers property definition, manipulation, and advanced patterns using JD.MSBuild.Fluent.

## Property Basics

### What Are Properties?

MSBuild properties are **name-value pairs** evaluated during the project evaluation phase. They:

- Store configuration values (e.g., `$(Configuration)`, `$(Platform)`)
- Reference file paths (e.g., `$(OutputPath)`, `$(MSBuildProjectDirectory)`)
- Control build behavior (e.g., `$(TreatWarningsAsErrors)`)
- Pass data between targets

### Property Syntax

**Reference:** `$(PropertyName)`

```xml
<PropertyGroup>
  <MyProperty>MyValue</MyProperty>
  <ReferencingProperty>$(MyProperty) extended</ReferencingProperty>
</PropertyGroup>
<!-- $(ReferencingProperty) = "MyValue extended" -->
```

## Defining Properties

### Simple Property

Define a property with a fixed value:

```csharp
.Props(p => p
    .Property("MyCompanyVersion", "2.0.0")
    .Property("MyCompanyEnabled", "true")
    .Property("OutputBasePath", "bin/output"))
```

**Generated XML:**

```xml
<PropertyGroup>
  <MyCompanyVersion>2.0.0</MyCompanyVersion>
  <MyCompanyEnabled>true</MyCompanyEnabled>
  <OutputBasePath>bin/output</OutputBasePath>
</PropertyGroup>
```

### Property with Condition

Define a property only when a condition is true:

```csharp
.Props(p => p
    .Property("EnableOptimization", "true", "'$(Configuration)' == 'Release'")
    .Property("DebugSymbols", "full", "'$(Configuration)' == 'Debug'"))
```

**Generated XML:**

```xml
<PropertyGroup>
  <EnableOptimization Condition="'$(Configuration)' == 'Release'">true</EnableOptimization>
  <DebugSymbols Condition="'$(Configuration)' == 'Debug'">full</DebugSymbols>
</PropertyGroup>
```

### Property Group with Condition

Group related properties under a single condition:

```csharp
.Props(p => p
    .PropertyGroup("'$(Configuration)' == 'Release'", group =>
    {
        group.Property("Optimize", "true");
        group.Property("DebugType", "none");
        group.Property("DebugSymbols", "false");
    }))
```

**Generated XML:**

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <Optimize>true</Optimize>
  <DebugType>none</DebugType>
  <DebugSymbols>false</DebugSymbols>
</PropertyGroup>
```

### Labeled Property Groups

Add labels for documentation and organization:

```csharp
.Props(p => p
    .PropertyGroup(null, group =>
    {
        group.Property("CompanyName", "MyCompany");
        group.Property("CompanyEmail", "support@mycompany.com");
    }, label: "Company Information")
    
    .PropertyGroup(null, group =>
    {
        group.Property("TargetFramework", "net8.0");
        group.Property("LangVersion", "latest");
    }, label: "Compiler Settings"))
```

**Generated XML:**

```xml
<PropertyGroup Label="Company Information">
  <CompanyName>MyCompany</CompanyName>
  <CompanyEmail>support@mycompany.com</CompanyEmail>
</PropertyGroup>
<PropertyGroup Label="Compiler Settings">
  <TargetFramework>net8.0</TargetFramework>
  <LangVersion>latest</LangVersion>
</PropertyGroup>
```

## Property References

### Referencing Other Properties

Properties can reference previously defined properties:

```csharp
.Props(p => p
    .Property("RootPath", "$(MSBuildProjectDirectory)")
    .Property("SourcePath", "$(RootPath)/src")
    .Property("TestPath", "$(RootPath)/test")
    .Property("OutputPath", "$(RootPath)/bin/$(Configuration)"))
```

**Generated XML:**

```xml
<PropertyGroup>
  <RootPath>$(MSBuildProjectDirectory)</RootPath>
  <SourcePath>$(RootPath)/src</SourcePath>
  <TestPath>$(RootPath)/test</TestPath>
  <OutputPath>$(RootPath)/bin/$(Configuration)</OutputPath>
</PropertyGroup>
```

### Well-Known MSBuild Properties

Reference built-in MSBuild properties:

```csharp
.Props(p => p
    .Property("CustomOutputPath", "$(MSBuildProjectDirectory)/output")
    .Property("IntermediatePath", "$(BaseIntermediateOutputPath)/custom")
    .Property("PackageFolder", "$(MSBuildThisFileDirectory)../../packages"))
```

**Common MSBuild Properties:**

| Property | Description |
|----------|-------------|
| `$(MSBuildProjectDirectory)` | Directory containing the project file |
| `$(MSBuildThisFileDirectory)` | Directory containing the current props/targets file |
| `$(Configuration)` | Build configuration (Debug, Release) |
| `$(Platform)` | Target platform (AnyCPU, x64, x86) |
| `$(TargetFramework)` | Target framework moniker (net8.0, netstandard2.0) |
| `$(OutputPath)` | Output directory for binaries |
| `$(BaseIntermediateOutputPath)` | Intermediate output path (obj/) |

[Full list of MSBuild properties](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-reserved-and-well-known-properties)

## Conditional Properties

### Using Property Conditions

Set default values that can be overridden:

```csharp
.Props(p => p
    .Property("MyPackageEnabled", "true", "'$(MyPackageEnabled)' == ''")
    .Property("MyPackageLogLevel", "Normal", "'$(MyPackageLogLevel)' == ''"))
```

**Generated XML:**

```xml
<PropertyGroup>
  <MyPackageEnabled Condition="'$(MyPackageEnabled)' == ''">true</MyPackageEnabled>
  <MyPackageLogLevel Condition="'$(MyPackageLogLevel)' == ''">Normal</MyPackageLogLevel>
</PropertyGroup>
```

**Behavior:**
- If property not already set → Use default value
- If property already set → Keep existing value

### Configuration-Specific Properties

Different values per configuration:

```csharp
.Props(p => p
    // Base value
    .Property("MyPackageOptimize", "false")
    
    // Override for Release
    .PropertyGroup("'$(Configuration)' == 'Release'", group =>
    {
        group.Property("MyPackageOptimize", "true");
    }))
```

### Platform-Specific Properties

Different values per platform:

```csharp
.Props(p => p
    .PropertyGroup("'$(Platform)' == 'x64'", group =>
    {
        group.Property("NativeBinary", "native-x64.dll");
    })
    
    .PropertyGroup("'$(Platform)' == 'x86'", group =>
    {
        group.Property("NativeBinary", "native-x86.dll");
    })
    
    .PropertyGroup("'$(Platform)' == 'ARM64'", group =>
    {
        group.Property("NativeBinary", "native-arm64.dll");
    }))
```

## Advanced Property Patterns

### Computed Properties

Use MSBuild functions to compute property values:

```csharp
.Props(p => p
    .Property("BuildTimestamp", "$([System.DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))")
    .Property("IsWindows", "$([MSBuild]::IsOSPlatform('Windows'))")
    .Property("ProjectFileName", "$([System.IO.Path]::GetFileName('$(MSBuildProjectFile)'))")
    .Property("Year", "$([System.DateTime]::Now.Year)"))
```

**Generated XML:**

```xml
<PropertyGroup>
  <BuildTimestamp>$([System.DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))</BuildTimestamp>
  <IsWindows>$([MSBuild]::IsOSPlatform('Windows'))</IsWindows>
  <ProjectFileName>$([System.IO.Path]::GetFileName('$(MSBuildProjectFile)'))</ProjectFileName>
  <Year>$([System.DateTime]::Now.Year)</Year>
</PropertyGroup>
```

### String Manipulation

Transform property values with MSBuild functions:

```csharp
.Props(p => p
    .Property("PackageIdLower", "$(PackageId.ToLower())")
    .Property("PackageIdUpper", "$(PackageId.ToUpper())")
    .Property("PackageIdTrimmed", "$(PackageId.Trim())")
    .Property("VersionMajor", "$(Version.Split('.')[0])"))
```

### File System Checks

Check file existence before setting properties:

```csharp
.Props(p => p
    .Property("CustomPropsFile", "$(MSBuildProjectDirectory)/Custom.props", 
        "Exists('$(MSBuildProjectDirectory)/Custom.props')")
    
    .Property("HasGitFolder", "true", 
        "Exists('$(MSBuildProjectDirectory)/.git')"))
```

### Multi-Line Property Values

Properties can contain multi-line values:

```csharp
.Props(p => p
    .Property("DefineConstants", @"$(DefineConstants);
DEBUG_EXTENDED;
LOGGING_ENABLED;
CUSTOM_FEATURE"))
```

## Property Naming Conventions

### Best Practices

1. **Prefix with package name**: `MyPackage` → `MyPackageVersion`
2. **Use PascalCase**: `MyPackageEnabled`, not `my_package_enabled`
3. **Be descriptive**: `GenerateDocumentation`, not `GenDocs`
4. **Boolean naming**: Use `Enabled`/`Disabled` suffix or `true`/`false` values
5. **Avoid reserved names**: Don't override MSBuild built-ins without reason

### Examples

```csharp
// ✓ Good
.Property("MyPackageVersion", "1.0.0")
.Property("MyPackageEnabled", "true")
.Property("MyPackageOutputPath", "$(OutputPath)/mypackage")
.Property("MyPackageGenerateManifest", "false")

// ✗ Bad
.Property("version", "1.0.0")           // Not specific, conflicts possible
.Property("enabled", "1")               // Not boolean-like
.Property("out", "path")                // Too short, unclear
.Property("OutputPath", "custom")       // Overrides MSBuild built-in
```

## Property Evaluation Order

### Understanding Evaluation

Properties are evaluated **sequentially** during the evaluation phase:

```csharp
.Props(p => p
    .Property("A", "1")
    .Property("B", "$(A)2")     // B = "12"
    .Property("C", "$(A)$(B)")  // C = "112"
    .Property("A", "3")         // A is now "3"
    .Property("D", "$(A)$(B)")) // D = "312" (A changed, B didn't)
```

**Key insight:** Later properties see the latest values of earlier properties.

### Import Order Matters

Properties from imports are evaluated when the import is encountered:

```csharp
.Props(p => p
    .Property("BeforeImport", "value1")
    .Import("Other.props")  // May override BeforeImport
    .Property("AfterImport", "value2"))
```

## Common Property Patterns

### Pattern: Feature Flags

Enable/disable features with properties:

```csharp
.Props(p => p
    .Property("MyPackageEnabled", "true", "'$(MyPackageEnabled)' == ''")
    .Property("MyPackageFeatureA", "true", "'$(MyPackageFeatureA)' == ''")
    .Property("MyPackageFeatureB", "false", "'$(MyPackageFeatureB)' == ''"))

.Targets(t => t
    .Target("Feature_A", target => target
        .Condition("'$(MyPackageEnabled)' == 'true' AND '$(MyPackageFeatureA)' == 'true'")
        .Message("Feature A enabled")))
```

### Pattern: Version Properties

Semantic versioning properties:

```csharp
.Props(p => p
    .Property("MyPackageVersionMajor", "2")
    .Property("MyPackageVersionMinor", "1")
    .Property("MyPackageVersionPatch", "0")
    .Property("MyPackageVersionPrerelease", "beta.1", "'$(MyPackageVersionPrerelease)' == ''")
    .Property("MyPackageVersion", "$(MyPackageVersionMajor).$(MyPackageVersionMinor).$(MyPackageVersionPatch)")
    .Property("MyPackageFullVersion", "$(MyPackageVersion)-$(MyPackageVersionPrerelease)", 
        "'$(MyPackageVersionPrerelease)' != ''"))
```

### Pattern: Path Normalization

Normalize paths consistently:

```csharp
.Props(p => p
    .Property("MyPackageRoot", "$(MSBuildThisFileDirectory.TrimEnd('\\'))")
    .Property("MyPackageToolsPath", "$(MyPackageRoot)/tools")
    .Property("MyPackageContentPath", "$(MyPackageRoot)/content"))
```

### Pattern: Fallback Properties

Chain of fallbacks for property values:

```csharp
.Props(p => p
    // Try environment variable first
    .Property("MyPackageApiKey", "$(MY_PACKAGE_API_KEY)", "'$(MY_PACKAGE_API_KEY)' != ''")
    
    // Fall back to user-specific file
    .Property("MyPackageApiKey", "$([System.IO.File]::ReadAllText('$(UserProfile)/.mypackage/apikey.txt').Trim())", 
        "'$(MyPackageApiKey)' == '' AND Exists('$(UserProfile)/.mypackage/apikey.txt')")
    
    // Fall back to default (empty)
    .Property("MyPackageApiKey", "", "'$(MyPackageApiKey)' == ''"))
```

## Integration with Targets

Properties defined in props files are available in targets:

```csharp
.Props(p => p
    .Property("MyPackageEnabled", "true")
    .Property("MyPackageVersion", "2.0.0"))

.Targets(t => t
    .Target("ShowVersion", target => target
        .Condition("'$(MyPackageEnabled)' == 'true'")
        .Message("MyPackage version: $(MyPackageVersion)", "High")))
```

Properties can also be set within targets (runtime evaluation):

```csharp
.Targets(t => t
    .Target("ComputeVersion", target => target
        .PropertyGroup(null, group =>
        {
            group.Property("ComputedVersion", "$([System.IO.File]::ReadAllText('version.txt'))");
        })))
```

## Debugging Properties

### Display Property Values

Use Message tasks to inspect properties:

```csharp
.Targets(t => t
    .Target("DebugProperties", target => target
        .Message("MyPackageEnabled: $(MyPackageEnabled)", "High")
        .Message("MyPackageVersion: $(MyPackageVersion)", "High")
        .Message("Configuration: $(Configuration)", "High")))
```

Run with: `dotnet build -t:DebugProperties`

### MSBuild Verbosity

View all property evaluations with detailed verbosity:

```bash
dotnet build -v:detailed | grep "Property"
```

## Summary

| Concept | Fluent API | XML Output |
|---------|-----------|------------|
| Simple property | `.Property("Name", "Value")` | `<Name>Value</Name>` |
| Conditional property | `.Property("Name", "Value", "condition")` | `<Name Condition="...">Value</Name>` |
| Property group | `.PropertyGroup(condition, group => {...})` | `<PropertyGroup Condition="...">...</PropertyGroup>` |
| Labeled group | `.PropertyGroup(null, group => {...}, "Label")` | `<PropertyGroup Label="...">...</PropertyGroup>` |

## Next Steps

- [Working with Items](items.md) - Define item collections
- [Conditional Logic](conditionals.md) - Advanced conditionals with Choose/When
- [Target Orchestration](../targets-tasks/orchestration.md) - Use properties in targets
- [Strongly-Typed Properties](../advanced/strongly-typed.md) - Type-safe property names

## Additional Resources

- [MSBuild Properties (Microsoft Docs)](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-properties)
- [MSBuild Property Functions](https://learn.microsoft.com/en-us/visualstudio/msbuild/property-functions)
- [MSBuild Reserved Properties](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-reserved-and-well-known-properties)
