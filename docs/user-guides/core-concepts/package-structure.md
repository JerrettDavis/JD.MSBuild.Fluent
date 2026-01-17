# NuGet MSBuild Package Structure

Understanding the NuGet package folder structure is essential for authoring effective MSBuild packages. This guide explains how JD.MSBuild.Fluent maps your definitions to the correct package layout.

## NuGet Package Anatomy

A NuGet package with MSBuild integration follows this structure:

```
MyPackage.1.0.0.nupkg
├── build/                      # Direct consumer integration
│   ├── MyPackage.props         # Evaluation-time (early)
│   └── MyPackage.targets       # Execution-time (late)
├── buildTransitive/            # Transitive consumer integration
│   ├── MyPackage.props         # Flows through dependencies
│   └── MyPackage.targets       # Flows through dependencies
├── Sdk/                        # SDK-style project support
│   └── Sdk/
│       ├── Sdk.props           # SDK evaluation-time
│       └── Sdk.targets         # SDK execution-time
├── tools/                      # Executables, scripts
│   ├── MyTool.exe
│   └── install.ps1
├── lib/                        # Assemblies for runtime
│   ├── net6.0/
│   │   └── MyPackage.dll
│   └── netstandard2.0/
│       └── MyPackage.dll
├── analyzers/                  # Roslyn analyzers
│   └── dotnet/
│       └── cs/
│           └── MyAnalyzer.dll
├── contentFiles/               # Content files
│   └── any/
│       └── README.md
├── [Content_Types].xml         # Package metadata
├── _rels/                      # Package relationships
└── package/                    # NuGet metadata
    ├── services/
    └── MyPackage.nuspec
```

## MSBuild Integration Folders

### build/ Folder

The `build/` folder contains MSBuild files automatically imported by **direct consumers** only.

**Files:**
- `{PackageId}.props` - Imported **early** in evaluation phase
- `{PackageId}.targets` - Imported **late** in execution phase

**When to use:**
- Properties that should only affect direct consumers
- Targets that shouldn't propagate transitively
- Settings specific to the immediate consuming project

**Import order:**

```
1. Project opening tag
2. NuGet imports build/{PackageId}.props ← EARLY
3. Project content (properties, items, imports)
4. NuGet imports build/{PackageId}.targets ← LATE
5. Project closing tag
```

**Example scenario:**

```
Solution:
├── App (consumes Library)
└── Library (consumes MyPackage)

With build/ only:
- Library imports MyPackage's build/ files
- App does NOT import MyPackage's files
```

### buildTransitive/ Folder

The `buildTransitive/` folder contains MSBuild files imported by **all consumers**, including transitive dependencies.

**Files:**
- `{PackageId}.props` - Transitive early import
- `{PackageId}.targets` - Transitive late import

**When to use:**
- Properties that must flow through dependency chains
- Analyzers that should run on all consuming projects
- Conventions that apply to the entire solution

**Example scenario:**

```
Solution:
├── App (consumes Library)
└── Library (consumes MyPackage)

With buildTransitive/:
- Library imports MyPackage's buildTransitive/ files
- App ALSO imports MyPackage's buildTransitive/ files
```

**Use case:**
```csharp
.Pack(options =>
{
    options.BuildTransitive = true;  // Enable transitive imports
})
```

### Sdk/ Folder

The `Sdk/` folder supports SDK-style project references:

```xml
<Project Sdk="MyPackage">
  <!-- Project content -->
</Project>
```

**Files:**
- `Sdk/{PackageId}/Sdk.props` - Early SDK import
- `Sdk/{PackageId}/Sdk.targets` - Late SDK import

**When to use:**
- Creating MSBuild SDKs (like `Microsoft.NET.Sdk`)
- Opinionated project templates
- Convention-driven project systems

**Enable SDK emission:**

```csharp
.Pack(options =>
{
    options.EmitSdk = true;
})
```

**SDK Import Order:**

```xml
<!-- Conceptually equivalent to: -->
<Import Project="Sdk.props" Sdk="MyPackage" />
<!-- Project content -->
<Import Project="Sdk.targets" Sdk="MyPackage" />
```

## Mapping Definition to Package Structure

### Default Mapping

JD.MSBuild.Fluent maps your definition to package structure:

```csharp
var definition = Package.Define("MyPackage")
    .Props(p => p.Property("Prop1", "Value1"))       // → build/MyPackage.props
    .Targets(t => t.Target("Target1", ...))          // → build/MyPackage.targets
    .Pack(o => o.BuildTransitive = false)
    .Build();
```

**Generated files:**

```
build/
├── MyPackage.props       (from .Props())
└── MyPackage.targets     (from .Targets())
```

### With BuildTransitive

```csharp
.Pack(o => o.BuildTransitive = true)
```

**Generated files:**

```
build/
├── MyPackage.props
└── MyPackage.targets
buildTransitive/
├── MyPackage.props       (copy of build/MyPackage.props)
└── MyPackage.targets     (copy of build/MyPackage.targets)
```

### With EmitSdk

```csharp
.Pack(o => o.EmitSdk = true)
```

**Generated files:**

```
build/
├── MyPackage.props
└── MyPackage.targets
Sdk/
└── Sdk/
    ├── Sdk.props         (from .Props() or .SdkProps())
    └── Sdk.targets       (from .Targets() or .SdkTargets())
```

## Advanced: Separate Build and BuildTransitive Content

Define different content for build/ and buildTransitive/:

```csharp
var definition = Package.Define("MyPackage")
    // build/ folder content
    .BuildProps(p => p
        .Property("DirectConsumerOnly", "true"))
    .BuildTargets(t => t
        .Target("DirectTarget", target => target
            .Message("Direct consumer target")))
    
    // buildTransitive/ folder content (different!)
    .BuildTransitiveProps(p => p
        .Property("TransitiveProp", "value"))
    .BuildTransitiveTargets(t => t
        .Target("TransitiveTarget", target => target
            .Message("Transitive target")))
    
    .Build();
```

**Generated files:**

```
build/
├── MyPackage.props       (DirectConsumerOnly property)
└── MyPackage.targets     (DirectTarget)
buildTransitive/
├── MyPackage.props       (TransitiveProp property)
└── MyPackage.targets     (TransitiveTarget)
```

### When to Separate

**Use separate build/ and buildTransitive/ when:**
- Direct consumers need different settings than transitive consumers
- Applying analyzers only to direct consumers
- Different target orchestration for different consumer levels

**Example: Analyzer Package**

```csharp
.BuildProps(p => p
    .ItemGroup(null, g => g
        .Include("Analyzer", "$(MSBuildThisFileDirectory)../../analyzers/dotnet/cs/MyAnalyzer.dll", item => item
            .Meta("Visible", "false"))))

// Don't propagate analyzer to transitive consumers
.Pack(o => o.BuildTransitive = false)
```

## Advanced: SDK-Specific Content

Define different content for SDK imports:

```csharp
var definition = Package.Define("MySDK")
    // Regular build/ content
    .Props(p => p.Property("RegularProp", "value"))
    .Targets(t => t.Target("RegularTarget", ...))
    
    // SDK-specific content
    .SdkProps(p => p
        .Property("SdkProp", "sdkValue")
        .Import("$(MSBuildThisFileDirectory)../build/$(MSBuildThisFile)"))  // Import regular props
    .SdkTargets(t => t
        .Target("SdkTarget", target => target
            .Message("SDK-specific target"))
        .Import("$(MSBuildThisFileDirectory)../build/$(MSBuildThisFile)"))  // Import regular targets
    
    .Pack(o => o.EmitSdk = true)
    .Build();
```

**Generated structure:**

```
build/
├── MySDK.props           (RegularProp)
└── MySDK.targets         (RegularTarget)
Sdk/
└── Sdk/
    ├── Sdk.props         (SdkProp + imports build/MySDK.props)
    └── Sdk.targets       (SdkTarget + imports build/MySDK.targets)
```

**This pattern enables:**
- SDK projects: `<Project Sdk="MySDK">` → Uses Sdk/ folder
- Regular projects: `<PackageReference Include="MySDK">` → Uses build/ folder

## props vs targets Files

### MyPackage.props

**Import timing:** Early in project evaluation

**Use for:**
- Property definitions
- Default property values
- Item includes/excludes (evaluation-time items)
- Imports of other props files
- Choose/When conditionals affecting evaluation

**Example:**

```csharp
.Props(p => p
    .Property("MyPackageVersion", "1.0.0")
    .Property("MyPackageEnabled", "true")
    .ItemGroup(null, g => g
        .Include("MyPackageContent", "$(MSBuildThisFileDirectory)../../content/**/*.*")))
```

### MyPackage.targets

**Import timing:** Late in project execution

**Use for:**
- Target definitions
- UsingTask declarations
- Task invocations within targets
- Imports of other targets files
- Build orchestration logic

**Example:**

```csharp
.Targets(t => t
    .Target("MyPackage_Build", target => target
        .BeforeTargets("Build")
        .Message("Running MyPackage build")))
```

### Why the Split?

MSBuild has two distinct phases:

1. **Evaluation**: Properties and items are processed, conditions evaluated
2. **Execution**: Targets run, tasks execute

Splitting props and targets ensures:
- Properties are available when targets evaluate conditions
- Targets can reference properties from evaluation
- Correct dependency ordering

## Filename Conventions

### Required Naming

NuGet auto-imports files matching your package ID:

```
build/{PackageId}.props       ✓ Auto-imported
build/{PackageId}.targets     ✓ Auto-imported
build/Custom.props            ✗ NOT auto-imported
build/MyPackage.Custom.props  ✗ NOT auto-imported
```

**Override naming:**

```csharp
.Pack(o => o.BuildAssetBasename = "Custom")
```

**Generates:**

```
build/Custom.props
build/Custom.targets
```

**When to override:**
- Legacy compatibility
- Shared assets across multiple package IDs
- Custom import patterns

### Multi-File Packages

Import additional files from your main files:

**Main file (MyPackage.props):**

```csharp
.Props(p => p
    .Import("$(MSBuildThisFileDirectory)MyPackage.Common.props")
    .Import("$(MSBuildThisFileDirectory)MyPackage.$(Configuration).props", 
        condition: "Exists('$(MSBuildThisFileDirectory)MyPackage.$(Configuration).props')"))
```

**Package additional files manually:**

```xml
<ItemGroup>
  <None Include="build/MyPackage.Common.props" Pack="true" PackagePath="build" />
  <None Include="build/MyPackage.Debug.props" Pack="true" PackagePath="build" />
  <None Include="build/MyPackage.Release.props" Pack="true" PackagePath="build" />
</ItemGroup>
```

## Testing Package Structure

### Inspect Generated Structure

```bash
jdmsbuild generate --assembly MyPackage.dll --type Factory --method Create --output ./output

ls output/
# build/
# buildTransitive/
# Sdk/
```

### Verify NuGet Package

```bash
dotnet pack
unzip -l bin/Debug/MyPackage.1.0.0.nupkg | grep build
# build/MyPackage.props
# build/MyPackage.targets
# buildTransitive/MyPackage.props
# buildTransitive/MyPackage.targets
```

### Test Imports

Create a test project:

```bash
dotnet new console
dotnet add package MyPackage
dotnet build -v:detailed | grep "Importing"
# Importing build/MyPackage.props
# Importing build/MyPackage.targets
```

## Common Patterns

### Pattern: Analyzer Package

```csharp
Package.Define("MyAnalyzer")
    .Props(p => p
        .ItemGroup(null, g => g
            .Include("Analyzer", "$(MSBuildThisFileDirectory)../../analyzers/dotnet/cs/MyAnalyzer.dll", item => item
                .Meta("Visible", "false"))))
    .Pack(o => o.BuildTransitive = false)  // Analyzers only for direct consumers
    .Build();
```

### Pattern: SDK Package

```csharp
Package.Define("MySDK")
    .SdkProps(p => p
        .Property("TargetFramework", "net8.0", "'$(TargetFramework)' == ''")
        .Property("LangVersion", "latest")
        .Property("Nullable", "enable"))
    .SdkTargets(t => t
        .Target("MySdkBuild", target => target
            .AfterTargets("Build")
            .Message("Built with MySDK")))
    .Pack(o => o.EmitSdk = true)
    .Build();
```

### Pattern: Multi-Target Framework

```csharp
Package.Define("MyMultiTargetPackage")
    .Props(p => p
        .Choose(choose =>
        {
            choose.When("'$(TargetFramework)' == 'net6.0'", whenProps =>
            {
                whenProps.Property("UseNet6Features", "true");
            });
            choose.When("'$(TargetFramework)' == 'net8.0'", whenProps =>
            {
                whenProps.Property("UseNet8Features", "true");
            });
        }))
    .Build();
```

## Summary

| Folder | Import Scope | Timing | Use For |
|--------|--------------|--------|---------|
| `build/` | Direct consumers only | Auto-imported | Direct integration, non-transitive settings |
| `buildTransitive/` | All consumers (transitive) | Auto-imported | Settings that flow through dependencies |
| `Sdk/` | SDK-style projects | SDK resolver | SDKs and opinionated templates |
| `tools/` | Manual | Not imported | Executables, scripts, utilities |

## Next Steps

- [Working with Properties](../properties-items/properties.md) - Define properties in props files
- [Target Orchestration](../targets-tasks/orchestration.md) - Define targets in targets files
- [Import Statements](../advanced/imports.md) - Import additional files
- [Multi-Target Framework](../advanced/multi-tfm.md) - Handle multiple frameworks

## Additional Resources

- [NuGet MSBuild props and targets](https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package-msbuild)
- [MSBuild SDKs](https://learn.microsoft.com/en-us/visualstudio/msbuild/how-to-use-project-sdk)
