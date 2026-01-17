# Import Statements

Import statements bring external MSBuild files into your project, enabling modularization and reuse. This guide covers import patterns, SDK imports, and best practices.

## Import Basics

### What Are Imports?

Imports allow you to:

- Include external `.props` or `.targets` files
- Modularize MSBuild logic across multiple files
- Reuse shared configurations
- Load SDK props and targets

### Simple Import

```csharp
.Props(p => p
    .Import("$(MSBuildThisFileDirectory)Common.props"))
```

**Generated XML:**

```xml
<Import Project="$(MSBuildThisFileDirectory)Common.props" />
```

### Conditional Import

```csharp
.Props(p => p
    .Import("$(MSBuildThisFileDirectory)Optional.props", 
        condition: "Exists('$(MSBuildThisFileDirectory)Optional.props')"))
```

**Generated XML:**

```xml
<Import Project="$(MSBuildThisFileDirectory)Optional.props" 
        Condition="Exists('$(MSBuildThisFileDirectory)Optional.props')" />
```

## Import Paths

### Relative to Current File

```csharp
// Same directory
.Import("$(MSBuildThisFileDirectory)Common.props")

// Parent directory
.Import("$(MSBuildThisFileDirectory)../Shared.props")

// Sibling directory
.Import("$(MSBuildThisFileDirectory)../SharedProps/Common.props")
```

### Relative to Project

```csharp
// Project root
.Import("$(MSBuildProjectDirectory)/Custom.props")

// Project subdirectory
.Import("$(MSBuildProjectDirectory)/build/Custom.props")
```

### Package-Relative Paths

```csharp
// Navigate up from build/ to package root, then into folder
.Import("$(MSBuildThisFileDirectory)../../shared/Common.props")

// From buildTransitive/ to tools/
.Import("$(MSBuildThisFileDirectory)../../tools/TaskDefinitions.props")
```

## SDK Imports

### What Are SDK Imports?

SDK imports load props and targets from MSBuild SDKs:

```csharp
.Props(p => p
    .Import("Sdk.props", sdk: "Microsoft.NET.Sdk"))
```

**Generated XML:**

```xml
<Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
```

### Common SDK Imports

```csharp
// .NET SDK
.Import("Sdk.props", sdk: "Microsoft.NET.Sdk")
.Import("Sdk.targets", sdk: "Microsoft.NET.Sdk")

// ASP.NET Core SDK
.Import("Sdk.props", sdk: "Microsoft.NET.Sdk.Web")

// Custom SDK
.Import("Sdk.props", sdk: "MyCompany.CustomSDK")
```

## Pattern: Modular Configuration

### Split Configuration Across Files

**Main props:**

```csharp
.Props(p => p
    .Import("$(MSBuildThisFileDirectory)Properties.props")
    .Import("$(MSBuildThisFileDirectory)Items.props")
    .Import("$(MSBuildThisFileDirectory)References.props"))
```

**Properties.props:**

```csharp
Package.Define("MyPackage")
    .BuildProps(p => p
        .Property("MyPackageVersion", "1.0.0")
        .Property("MyPackageEnabled", "true"))
    .Build();
```

**Items.props:**

```csharp
Package.Define("MyPackage")
    .BuildProps(p => p
        .ItemGroup(null, group =>
        {
            group.Include("MyPackageAsset", "$(MSBuildThisFileDirectory)../../assets/**/*.*");
        }))
    .Build();
```

### Multi-File Package

**Structure:**

```
MyPackage/
├── build/
│   ├── MyPackage.props             # Main entry point
│   ├── MyPackage.targets           # Main entry point
│   ├── Properties.props            # Property definitions
│   ├── Items.props                 # Item definitions
│   ├── Targets.Common.targets      # Common targets
│   └── Targets.Platform.targets    # Platform-specific targets
```

**Main props imports:**

```csharp
.Props(p => p
    .Comment("Main props entry point")
    .Import("$(MSBuildThisFileDirectory)Properties.props")
    .Import("$(MSBuildThisFileDirectory)Items.props"))
```

**Main targets imports:**

```csharp
.Targets(t => t
    .Comment("Main targets entry point")
    .Import("$(MSBuildThisFileDirectory)Targets.Common.targets")
    .Import("$(MSBuildThisFileDirectory)Targets.Platform.targets", 
        condition: "$([MSBuild]::IsOSPlatform('Windows'))"))
```

## Pattern: Conditional Imports

### Import Based on Property

```csharp
.Props(p => p
    // Always import base
    .Import("$(MSBuildThisFileDirectory)Base.props")
    
    // Import debug settings in debug mode
    .Import("$(MSBuildThisFileDirectory)Debug.props", 
        "'$(Configuration)' == 'Debug'")
    
    // Import release settings in release mode
    .Import("$(MSBuildThisFileDirectory)Release.props", 
        "'$(Configuration)' == 'Release'"))
```

### Import Based on File Existence

```csharp
.Props(p => p
    // Import if file exists
    .Import("$(MSBuildProjectDirectory)/Custom.props", 
        "Exists('$(MSBuildProjectDirectory)/Custom.props')")
    
    // Import user-specific overrides if present
    .Import("$(MSBuildProjectDirectory)/User.props", 
        "Exists('$(MSBuildProjectDirectory)/User.props')"))
```

### Import Based on Framework

```csharp
.Props(p => p
    .Import("$(MSBuildThisFileDirectory)Net6.props", 
        "'$(TargetFramework)' == 'net6.0'")
    
    .Import("$(MSBuildThisFileDirectory)Net8.props", 
        "'$(TargetFramework)' == 'net8.0'")
    
    .Import("$(MSBuildThisFileDirectory)NetStandard.props", 
        "$(TargetFramework.StartsWith('netstandard'))"))
```

## Pattern: Layered Configuration

### Base + Override Pattern

```csharp
.Props(p => p
    // Layer 1: Defaults
    .Import("$(MSBuildThisFileDirectory)Defaults.props")
    
    // Layer 2: Package-specific
    .Import("$(MSBuildThisFileDirectory)Package.props")
    
    // Layer 3: User overrides (optional)
    .Import("$(MSBuildProjectDirectory)/MyPackage.User.props", 
        "Exists('$(MSBuildProjectDirectory)/MyPackage.User.props')"))
```

**Defaults.props sets base values:**

```csharp
.Property("MyProp", "default", "'$(MyProp)' == ''")
```

**User.props can override:**

```csharp
.Property("MyProp", "user-override")
```

## Pattern: Platform-Specific Imports

### OS-Specific Files

```csharp
.Props(p => p
    // Common props
    .Import("$(MSBuildThisFileDirectory)Common.props")
    
    // Windows-specific
    .Import("$(MSBuildThisFileDirectory)Windows.props", 
        "$([MSBuild]::IsOSPlatform('Windows'))")
    
    // Linux-specific
    .Import("$(MSBuildThisFileDirectory)Linux.props", 
        "$([MSBuild]::IsOSPlatform('Linux'))")
    
    // macOS-specific
    .Import("$(MSBuildThisFileDirectory)MacOS.props", 
        "$([MSBuild]::IsOSPlatform('OSX'))"))
```

## Pattern: SDK Wrapping

### Wrap SDK with Custom Logic

```csharp
.Props(p => p
    // Before SDK import
    .Property("_MyPackageBeforeSDK", "true")
    
    // Import SDK props
    .Import("Sdk.props", sdk: "Microsoft.NET.Sdk")
    
    // After SDK import - can use or override SDK properties
    .PropertyGroup("'$(TargetFramework)' == ''", group =>
    {
        group.Property("TargetFramework", "net8.0");  // Default if not set by SDK
    }))

.Targets(t => t
    // Custom targets before SDK targets
    .Target("MyPackage_BeforeBuild", target => target
        .BeforeTargets("Build")
        .Message("Custom pre-build logic"))
    
    // Import SDK targets
    .Import("Sdk.targets", sdk: "Microsoft.NET.Sdk")
    
    // Custom targets after SDK targets
    .Target("MyPackage_AfterBuild", target => target
        .AfterTargets("Build")
        .Message("Custom post-build logic")))
```

## Import Order Matters

### Evaluation Order

Imports are evaluated **in order**:

```csharp
.Props(p => p
    .Property("Value", "1")               // Value = "1"
    .Import("File1.props")                // May change Value
    .Property("Value", "$(Value)_ext")    // Extends Value
    .Import("File2.props"))               // May change Value again
```

### Early vs Late Imports

**Early imports (props):**

```csharp
// Import before defining properties - imported props are defaults
.Props(p => p
    .Import("Defaults.props")
    .Property("MyProp", "override", "'$(MyProp)' != ''"))  // Override if set by import
```

**Late imports (props):**

```csharp
// Import after defining properties - imported props override
.Props(p => p
    .Property("MyProp", "default")
    .Import("Overrides.props"))  // Can override MyProp
```

## Circular Import Protection

### Avoid Circular Imports

```csharp
// ✗ Circular import
// File A imports File B
// File B imports File A
// = Infinite loop (MSBuild detects and errors)
```

**Solution: Use a common base file:**

```
Common.props  ← Both import this
├── A.props
└── B.props
```

### Guard with Properties

```csharp
// In imported file
.Props(p => p
    .PropertyGroup("'$(MyFileImported)' != 'true'", group =>
    {
        group.Property("MyFileImported", "true");
        // File content here
    }))
```

## Debugging Imports

### Log Import Resolution

```csharp
.Props(p => p
    .Import("$(MSBuildThisFileDirectory)Custom.props", 
        "Exists('$(MSBuildThisFileDirectory)Custom.props')")
    
    .Message("Custom.props imported: $([System.IO.File]::Exists('$(MSBuildThisFileDirectory)Custom.props'))", "High"))
```

### MSBuild Preprocessor

See all imports resolved:

```bash
dotnet msbuild -pp:preprocessed.xml
```

Opens `preprocessed.xml` showing all imports inlined.

### Verbosity

View import details:

```bash
dotnet build -v:detailed | grep "Import"
```

## Best Practices

### DO: Use Conditional Imports

```csharp
// ✓ Safe - won't error if missing
.Import("Optional.props", "Exists('Optional.props')")

// ✗ Errors if file missing
.Import("Optional.props")
```

### DO: Use Relative Paths

```csharp
// ✓ Portable
.Import("$(MSBuildThisFileDirectory)Common.props")

// ✗ Not portable
.Import("C:/MyPackage/Common.props")
```

### DO: Document Import Dependencies

```csharp
.Props(p => p
    .Comment("Import order matters:")
    .Comment("1. Defaults - sets base values")
    .Comment("2. Configuration - configuration-specific overrides")
    .Comment("3. User - optional user overrides")
    
    .Import("$(MSBuildThisFileDirectory)Defaults.props")
    .Import("$(MSBuildThisFileDirectory)Configuration.props")
    .Import("$(MSBuildProjectDirectory)/User.props", 
        "Exists('$(MSBuildProjectDirectory)/User.props')"))
```

### DON'T: Import the Same File Multiple Times

```csharp
// ✗ Redundant
.Import("Common.props")
.Import("Other.props")
.Import("Common.props")  // ✗ Already imported
```

### DON'T: Create Deep Import Chains

```csharp
// ✗ Hard to follow
// A imports B imports C imports D imports E
// = Hard to debug

// ✓ Flat structure
// Main imports A, B, C, D, E
```

## Summary

| Type | Syntax | Use For |
|------|--------|---------|
| File import | `.Import("path/to/file.props")` | External files |
| Conditional import | `.Import("file.props", "condition")` | Optional files |
| SDK import | `.Import("Sdk.props", sdk: "SDKName")` | MSBuild SDKs |

**Path properties:**

- `$(MSBuildThisFileDirectory)` - Directory of current file
- `$(MSBuildProjectDirectory)` - Project root directory
- `$(MSBuildExtensionsPath)` - MSBuild extensions path

## Next Steps

- [Package Structure](../core-concepts/package-structure.md) - File layout
- [Properties](../properties-items/properties.md) - Property definitions
- [Conditional Logic](../properties-items/conditionals.md) - Conditions
- [MSBuild Imports (Microsoft Docs)](https://learn.microsoft.com/en-us/visualstudio/msbuild/import-element-msbuild)
