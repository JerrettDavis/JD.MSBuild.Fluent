# Working with MSBuild Items

MSBuild items represent **collections of files and data** used throughout the build process. This guide covers item definition, manipulation, and advanced patterns with JD.MSBuild.Fluent.

## Item Basics

### What Are Items?

Items are **typed collections** with metadata. Common uses:

- **File lists**: Source files (`@(Compile)`), content files (`@(Content)`)
- **References**: Package references, project references
- **Custom data**: Build configuration data, deployment targets

### Item Syntax

**Reference:** `@(ItemType)`

```xml
<ItemGroup>
  <Compile Include="src/**/*.cs" />
  <Content Include="assets/**/*.png" />
</ItemGroup>
<!-- @(Compile) = list of all .cs files -->
<!-- @(Content) = list of all .png files -->
```

## Item Operations

### Include Operation

Add items to a collection:

```csharp
.Props(p => p
    .Item("Compile", MsBuildItemOperation.Include, "src/**/*.cs")
    .Item("Content", MsBuildItemOperation.Include, "assets/*.png")
    .Item("None", MsBuildItemOperation.Include, "README.md"))
```

**Generated XML:**

```xml
<ItemGroup>
  <Compile Include="src/**/*.cs" />
  <Content Include="assets/*.png" />
  <None Include="README.md" />
</ItemGroup>
```

### Remove Operation

Remove items from a collection:

```csharp
.Props(p => p
    .Item("Compile", MsBuildItemOperation.Remove, "src/**/*.g.cs")
    .Item("Content", MsBuildItemOperation.Remove, "assets/temp/**"))
```

**Generated XML:**

```xml
<ItemGroup>
  <Compile Remove="src/**/*.g.cs" />
  <Content Remove="assets/temp/**" />
</ItemGroup>
```

**Use case:** Remove auto-generated files or temporary content from build.

### Update Operation

Modify metadata on existing items without changing the collection:

```csharp
.Props(p => p
    .Item("Compile", MsBuildItemOperation.Update, "**/*.Designer.cs", item => item
        .Meta("AutoGen", "True")
        .Meta("DependentUpon", "%(Filename)")))
```

**Generated XML:**

```xml
<ItemGroup>
  <Compile Update="**/*.Designer.cs">
    <AutoGen>True</AutoGen>
    <DependentUpon>%(Filename)</DependentUpon>
  </Compile>
</ItemGroup>
```

**Use case:** Update metadata on items already in the project without re-including them.

## Item Groups

### Simple Item Group

Group related items together:

```csharp
.Props(p => p
    .ItemGroup(null, group =>
    {
        group.Include("Compile", "src/CoreModule.cs");
        group.Include("Compile", "src/UtilityModule.cs");
        group.Include("Compile", "src/DataModule.cs");
    }))
```

**Generated XML:**

```xml
<ItemGroup>
  <Compile Include="src/CoreModule.cs" />
  <Compile Include="src/UtilityModule.cs" />
  <Compile Include="src/DataModule.cs" />
</ItemGroup>
```

### Conditional Item Group

Add items only when a condition is met:

```csharp
.Props(p => p
    .ItemGroup("'$(IncludeTests)' == 'true'", group =>
    {
        group.Include("Compile", "test/**/*.cs");
        group.Include("None", "test/**/*.json");
    }))
```

**Generated XML:**

```xml
<ItemGroup Condition="'$(IncludeTests)' == 'true'">
  <Compile Include="test/**/*.cs" />
  <None Include="test/**/*.json" />
</ItemGroup>
```

### Labeled Item Groups

Add labels for organization:

```csharp
.Props(p => p
    .ItemGroup(null, group =>
    {
        group.Include("Compile", "src/**/*.cs");
    }, label: "Source Files")
    
    .ItemGroup(null, group =>
    {
        group.Include("EmbeddedResource", "resources/**/*.resx");
    }, label: "Resources"))
```

**Generated XML:**

```xml
<ItemGroup Label="Source Files">
  <Compile Include="src/**/*.cs" />
</ItemGroup>
<ItemGroup Label="Resources">
  <EmbeddedResource Include="resources/**/*.resx" />
</ItemGroup>
```

## Item Include Patterns

### Glob Patterns

Use wildcards to match multiple files:

```csharp
.Props(p => p
    .Item("Compile", MsBuildItemOperation.Include, "src/**/*.cs")           // All .cs files recursively
    .Item("Content", MsBuildItemOperation.Include, "assets/*.png")          // .png files in assets/
    .Item("None", MsBuildItemOperation.Include, "docs/**/*.md")             // All .md files in docs/
    .Item("EmbeddedResource", MsBuildItemOperation.Include, "**/*.resx"))  // All .resx files everywhere
```

**Glob syntax:**
- `*` - Match any characters except directory separator
- `**` - Match any characters including directory separator (recursive)
- `?` - Match single character
- `[abc]` - Match any of the characters a, b, or c

### Exclude Patterns

Exclude files from includes:

```csharp
.Props(p => p
    .Item("Compile", MsBuildItemOperation.Include, "src/**/*.cs", exclude: "src/**/*.g.cs")
    .Item("Content", MsBuildItemOperation.Include, "assets/**/*.*", exclude: "assets/temp/**/*.*"))
```

**Generated XML:**

```xml
<ItemGroup>
  <Compile Include="src/**/*.cs" Exclude="src/**/*.g.cs" />
  <Content Include="assets/**/*.*" Exclude="assets/temp/**/*.*" />
</ItemGroup>
```

### Individual Conditional Items

Add individual items with conditions:

```csharp
.Props(p => p
    .Item("Content", MsBuildItemOperation.Include, "appsettings.Development.json", 
        condition: "'$(Configuration)' == 'Debug'")
    .Item("Content", MsBuildItemOperation.Include, "appsettings.Production.json",
        condition: "'$(Configuration)' == 'Release'"))
```

**Generated XML:**

```xml
<ItemGroup>
  <Content Include="appsettings.Development.json" Condition="'$(Configuration)' == 'Debug'" />
  <Content Include="appsettings.Production.json" Condition="'$(Configuration)' == 'Release'" />
</ItemGroup>
```

## Item Metadata

### What Is Metadata?

Metadata provides additional information about items:

```csharp
.Props(p => p
    .ItemGroup(null, group =>
    {
        group.Include("None", "README.md", item => item
            .Meta("CopyToOutputDirectory", "PreserveNewest")
            .Meta("Visible", "true")
            .Meta("Pack", "true")
            .Meta("PackagePath", "docs/"));
    }))
```

**Generated XML:**

```xml
<ItemGroup>
  <None Include="README.md">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Visible>true</Visible>
    <Pack>true</Pack>
    <PackagePath>docs/</PackagePath>
  </None>
</ItemGroup>
```

### Common Metadata

| Metadata | Description | Values |
|----------|-------------|--------|
| `CopyToOutputDirectory` | Copy file to output | `Always`, `PreserveNewest`, `Never` |
| `Pack` | Include in NuGet package | `true`, `false` |
| `PackagePath` | Path within package | `content/`, `tools/`, etc. |
| `Visible` | Show in Solution Explorer | `true`, `false` |
| `Link` | Virtual path in project | `Properties/AssemblyInfo.cs` |
| `DependentUpon` | Parent file dependency | `MainForm.cs` |

### Well-Known Metadata

MSBuild provides automatic metadata for all items:

```csharp
.Props(p => p
    .ItemGroup(null, group =>
    {
        group.Include("Content", "src/data/config.json", item => item
            .Meta("Link", "%(RecursiveDir)%(Filename)%(Extension)")
            .Meta("TargetPath", "config/%(Filename)%(Extension)"));
    }))
```

**Well-known metadata:**

| Metadata | Description | Example |
|----------|-------------|---------|
| `%(FullPath)` | Full file path | `C:\Project\src\File.cs` |
| `%(RootDir)` | Root directory | `C:\` |
| `%(Filename)` | Name without extension | `File` |
| `%(Extension)` | File extension | `.cs` |
| `%(RelativeDir)` | Relative directory | `src\` |
| `%(Directory)` | Full directory | `C:\Project\src\` |
| `%(RecursiveDir)` | Recursive wildcard portion | `subdir\` |
| `%(Identity)` | Full item specification | `src\File.cs` |
| `%(ModifiedTime)` | Last modified timestamp | `2024-01-15 10:30:00` |
| `%(CreatedTime)` | Creation timestamp | `2024-01-01 09:00:00` |
| `%(AccessedTime)` | Last access timestamp | `2024-01-15 14:20:00` |

[Full metadata reference](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-well-known-item-metadata)

### Metadata as Attributes

Some metadata can be specified as attributes for brevity:

```csharp
.Props(p => p
    .ItemGroup(null, group =>
    {
        group.Include("None", "README.md", item => item
            .Meta("CopyToOutputDirectory", "PreserveNewest")  // As element
            .MetaAttribute("Visible", "false"));              // As attribute
    }))
```

**Generated XML:**

```xml
<ItemGroup>
  <None Include="README.md" Visible="false">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

**When to use attributes:**
- Simple string values
- Reduced verbosity
- Compatibility with existing projects

**When to use elements:**
- Complex expressions
- Property references
- Multi-line values

## Item Transformations

### Basic Transform

Transform item lists using metadata:

```csharp
.Props(p => p
    .Item("Compile", MsBuildItemOperation.Include, "src/**/*.cs")
    .Property("AllSourceFileNames", "@(Compile->'%(Filename)')"))
```

**Generated XML:**

```xml
<ItemGroup>
  <Compile Include="src/**/*.cs" />
</ItemGroup>
<PropertyGroup>
  <AllSourceFileNames>@(Compile->'%(Filename)')</AllSourceFileNames>
</PropertyGroup>
```

**Result:** Semicolon-separated list of filenames without extensions.

### Transform with Separator

Specify a custom separator:

```csharp
.Property("CommaDelimitedFiles", "@(Compile->='%(Filename)', ',')")
```

### Transform with Paths

Common path transformations:

```csharp
.Props(p => p
    .Property("SourceFullPaths", "@(Compile->'%(FullPath)')")
    .Property("SourceFileNames", "@(Compile->'%(Filename)%(Extension)')")
    .Property("SourceDirectories", "@(Compile->'%(RelativeDir)')"))
```

## Common Item Types

### Well-Known Item Types

MSBuild recognizes these item types:

| Item Type | Description | Auto-Included |
|-----------|-------------|---------------|
| `Compile` | C# source files | Yes (SDK projects) |
| `Content` | Content files | Yes (SDK projects) |
| `None` | Non-compiled files | No |
| `EmbeddedResource` | Embedded resources (.resx) | Yes (SDK projects) |
| `Reference` | Assembly references | No |
| `PackageReference` | NuGet package references | No |
| `ProjectReference` | Project-to-project references | No |
| `Analyzer` | Roslyn analyzers | No |

### Custom Item Types

Define custom item types for your package:

```csharp
.Props(p => p
    .ItemGroup(null, group =>
    {
        group.Include("MyPackageAsset", "$(MSBuildThisFileDirectory)../../assets/**/*.*", item => item
            .Meta("CopyToOutputDirectory", "PreserveNewest")
            .Meta("AssetType", "MyPackage"));

        group.Include("MyPackageConfiguration", "$(MSBuildThisFileDirectory)../../config/*.json", item => item
            .Meta("CopyToOutputDirectory", "Always"));
    }))
```

**Use in targets:**

```csharp
.Targets(t => t
    .Target("ProcessMyPackageAssets", target => target
        .Message("Processing @(MyPackageAsset->Count()) assets", "High")
        .Task("Copy", task =>
        {
            task.Param("SourceFiles", "@(MyPackageAsset)");
            task.Param("DestinationFolder", "$(OutputPath)/mypackage");
        })))
```

## Item References in Targets

Items are frequently used in targets for file operations:

```csharp
.Targets(t => t
    .Target("CopyAssets", target => target
        .Task("Copy", task =>
        {
            task.Param("SourceFiles", "@(Content)");
            task.Param("DestinationFolder", "$(OutputPath)/content");
        })
        
        .Task("Message", task =>
        {
            task.Param("Text", "Copied @(Content->Count()) files");
            task.Param("Importance", "High");
        })))
```

## Advanced Item Patterns

### Pattern: Package Content Files

Include files from your package into consumer projects:

```csharp
.Props(p => p
    .ItemGroup(null, group =>
    {
        group.Include("Content", "$(MSBuildThisFileDirectory)../../contentFiles/**/*.*", item => item
            .Meta("Pack", "true")
            .Meta("PackagePath", "contentFiles/any/any/%(RecursiveDir)%(Filename)%(Extension)")
            .Meta("CopyToOutputDirectory", "PreserveNewest"));
    }))
```

### Pattern: Analyzer References

Reference Roslyn analyzers:

```csharp
.Props(p => p
    .ItemGroup(null, group =>
    {
        group.Include("Analyzer", "$(MSBuildThisFileDirectory)../../analyzers/dotnet/cs/MyAnalyzer.dll", item => item
            .Meta("Visible", "false"));
    }))
```

### Pattern: Conditional Item Inclusion

Include different items based on target framework:

```csharp
.Props(p => p
    .ItemGroup("'$(TargetFramework)' == 'net6.0'", group =>
    {
        group.Include("Reference", "$(MSBuildThisFileDirectory)../../lib/net6.0/MyLibrary.dll");
    })
    
    .ItemGroup("'$(TargetFramework)' == 'net8.0'", group =>
    {
        group.Include("Reference", "$(MSBuildThisFileDirectory)../../lib/net8.0/MyLibrary.dll");
    }))
```

### Pattern: Item Filtering

Create derived item lists with filters:

```csharp
.Targets(t => t
    .Target("FilterItems", target => target
        .ItemGroup(null, group =>
        {
            // Create filtered list
            group.Include("CsFiles", "@(Compile)", item => item
                .Meta("Condition", "'%(Extension)' == '.cs'"));
            
            group.Include("GeneratedFiles", "@(Compile)", item => item
                .Meta("Condition", "%(Filename.EndsWith('.g'))"));
        })))
```

## Debugging Items

### Display Item Contents

Use Message tasks to inspect items:

```csharp
.Targets(t => t
    .Target("DebugItems", target => target
        .Message("Compile items: @(Compile)", "High")
        .Message("Count: @(Compile->Count())", "High")
        .Message("Filenames: @(Compile->'%(Filename)')", "High")))
```

Run with: `dotnet build -t:DebugItems`

### List Items with Metadata

```csharp
.Targets(t => t
    .Target("ListItemDetails", target => target
        .Message("%(Compile.Identity) - FullPath: %(Compile.FullPath)", "High")))
```

## Summary

| Operation | Fluent API | XML Output |
|-----------|-----------|------------|
| Include | `.Item("Type", Include, "spec")` | `<Type Include="spec" />` |
| Remove | `.Item("Type", Remove, "spec")` | `<Type Remove="spec" />` |
| Update | `.Item("Type", Update, "spec", config)` | `<Type Update="spec">...</Type>` |
| With metadata | `.Item(..., item => item.Meta("K", "V"))` | `<Type ...><K>V</K></Type>` |
| Item group | `.ItemGroup(condition, group => {...})` | `<ItemGroup Condition="...">...</ItemGroup>` |

## Next Steps

- [Item Metadata](metadata.md) - Deep dive into metadata handling
- [Conditional Logic](conditionals.md) - Conditional item inclusion
- [Target Orchestration](../targets-tasks/orchestration.md) - Use items in targets
- [Built-in Tasks](../targets-tasks/builtin-tasks.md) - Tasks that operate on items

## Additional Resources

- [MSBuild Items (Microsoft Docs)](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-items)
- [MSBuild Item Metadata](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-well-known-item-metadata)
- [MSBuild Item Functions](https://learn.microsoft.com/en-us/visualstudio/msbuild/item-functions)
