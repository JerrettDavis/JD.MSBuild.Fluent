# Item Metadata

Item metadata provides rich context and configuration for MSBuild items. This guide covers metadata definition, well-known metadata, custom metadata, and advanced patterns.

## Metadata Basics

### What Is Item Metadata?

Metadata is **key-value data attached to items**. Each item in a collection can have different metadata values.

```xml
<ItemGroup>
  <Content Include="image.png">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Visible>false</Visible>
  </Content>
</ItemGroup>
```

**Access metadata:** `%(MetadataName)` or `%(ItemType.MetadataName)`

## Defining Metadata

### Element Metadata

Metadata as child XML elements:

```csharp
.Props(p => p
    .ItemGroup(null, group =>
    {
        group.Include("Content", "assets/*.png", item => item
            .Meta("CopyToOutputDirectory", "PreserveNewest")
            .Meta("Visible", "false")
            .Meta("Pack", "true"));
    }))
```

**Generated XML:**

```xml
<ItemGroup>
  <Content Include="assets/*.png">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Visible>false</Visible>
    <Pack>true</Pack>
  </Content>
</ItemGroup>
```

### Attribute Metadata

Metadata as XML attributes (more concise):

```csharp
.ItemGroup(null, group =>
{
    group.Include("None", "README.md", item => item
        .MetaAttribute("Visible", "false")
        .MetaAttribute("Pack", "true"));
})
```

**Generated XML:**

```xml
<ItemGroup>
  <None Include="README.md" Visible="false" Pack="true" />
</ItemGroup>
```

### Mixed Metadata

Combine elements and attributes:

```csharp
.ItemGroup(null, group =>
{
    group.Include("Content", "config.json", item => item
        .MetaAttribute("Visible", "false")      // Attribute
        .Meta("CopyToOutputDirectory", "Always") // Element
        .Meta("PackagePath", "config/"));       // Element
})
```

**Generated XML:**

```xml
<ItemGroup>
  <Content Include="config.json" Visible="false">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    <PackagePath>config/</PackagePath>
  </Content>
</ItemGroup>
```

## Well-Known Metadata

### Automatic Metadata

MSBuild provides automatic metadata for all items based on file paths:

| Metadata | Description | Example Value |
|----------|-------------|---------------|
| `%(FullPath)` | Absolute file path | `C:\Project\src\File.cs` |
| `%(RootDir)` | Root directory | `C:\` |
| `%(Filename)` | Name without extension | `File` |
| `%(Extension)` | File extension | `.cs` |
| `%(RelativeDir)` | Relative directory from item spec | `src\` |
| `%(Directory)` | Directory containing the file | `C:\Project\src\` |
| `%(RecursiveDir)` | Recursive portion from wildcard | `sub\folder\` |
| `%(Identity)` | Full item specification | `src\File.cs` |
| `%(ModifiedTime)` | Last modification time | `2024-01-15 10:30:00` |
| `%(CreatedTime)` | Creation time | `2024-01-01 09:00:00` |
| `%(AccessedTime)` | Last access time | `2024-01-15 14:20:00` |

### Using Automatic Metadata

Reference automatic metadata in other metadata:

```csharp
.ItemGroup(null, group =>
{
    group.Include("Content", "assets/**/*.png", item => item
        .Meta("Link", "Content/%(RecursiveDir)%(Filename)%(Extension)")
        .Meta("TargetPath", "$(OutputPath)/assets/%(RecursiveDir)%(Filename)%(Extension)"));
})
```

**For file `assets/icons/app.png`:**

```
%(RecursiveDir) = icons\
%(Filename) = app
%(Extension) = .png
Link = Content/icons\app.png
TargetPath = bin/Debug/assets/icons\app.png
```

### RecursiveDir Metadata

Especially useful for maintaining folder structure:

```csharp
.ItemGroup(null, group =>
{
    group.Include("Content", "$(MSBuildThisFileDirectory)../../content/**/*.*", item => item
        .Meta("Pack", "true")
        .Meta("PackagePath", "contentFiles/any/any/%(RecursiveDir)%(Filename)%(Extension)")
        .Meta("CopyToOutputDirectory", "PreserveNewest"));
})
```

**Result:** Files maintain their relative folder structure in the package.

## Common Metadata Properties

### CopyToOutputDirectory

Controls file copying to output:

```csharp
.ItemGroup(null, group =>
{
    group.Include("None", "config.json", item => item
        .Meta("CopyToOutputDirectory", "Always"));          // Always copy
    
    group.Include("None", "template.txt", item => item
        .Meta("CopyToOutputDirectory", "PreserveNewest"));  // Copy if newer
    
    group.Include("None", "build-only.json", item => item
        .Meta("CopyToOutputDirectory", "Never"));           // Don't copy
})
```

**Values:**
- `Always` - Copy every build
- `PreserveNewest` - Copy only if source is newer
- `Never` - Don't copy

### Pack and PackagePath

Control NuGet packaging:

```csharp
.ItemGroup(null, group =>
{
    group.Include("None", "README.md", item => item
        .Meta("Pack", "true")
        .Meta("PackagePath", "docs/"));
    
    group.Include("None", "LICENSE.txt", item => item
        .Meta("Pack", "true")
        .Meta("PackagePath", ""));  // Root of package
    
    group.Include("None", "internal-notes.txt", item => item
        .Meta("Pack", "false"));  // Don't include in package
})
```

### Visible

Controls Solution Explorer visibility:

```csharp
.ItemGroup(null, group =>
{
    group.Include("None", ".editorconfig", item => item
        .Meta("Visible", "true"));   // Show in Solution Explorer
    
    group.Include("None", "obj/**/*.*", item => item
        .Meta("Visible", "false"));  // Hide from Solution Explorer
})
```

### Link

Creates virtual paths in Solution Explorer:

```csharp
.ItemGroup(null, group =>
{
    group.Include("Compile", "$(MSBuildThisFileDirectory)../../shared/Utilities.cs", item => item
        .Meta("Link", "Shared/Utilities.cs"));  // Appears under Shared/ folder
})
```

### DependentUpon

Creates parent-child relationship:

```csharp
.ItemGroup(null, group =>
{
    group.Include("Compile", "MainForm.Designer.cs", item => item
        .Meta("DependentUpon", "MainForm.cs")
        .Meta("AutoGen", "True"));
})
```

## Custom Metadata

### Define Custom Metadata

Add domain-specific metadata:

```csharp
.ItemGroup(null, group =>
{
    group.Include("MyAsset", "assets/logo.png", item => item
        .Meta("AssetType", "Logo")
        .Meta("AssetCategory", "Branding")
        .Meta("RequiresProcessing", "true")
        .Meta("ProcessingTool", "ImageOptimizer"));
    
    group.Include("MyAsset", "assets/icon.png", item => item
        .Meta("AssetType", "Icon")
        .Meta("AssetCategory", "UI")
        .Meta("RequiresProcessing", "false"));
})
```

### Use Custom Metadata in Targets

Access custom metadata in targets:

```csharp
.Targets(t => t
    .Target("ProcessAssets", target => target
        .Message("Processing asset: %(MyAsset.Identity)", "Normal")
        .Message("  Type: %(MyAsset.AssetType)", "Normal")
        .Message("  Category: %(MyAsset.AssetCategory)", "Normal")
        
        .Task("Exec", task =>
        {
            task.Param("Command", "%(MyAsset.ProcessingTool) %(MyAsset.FullPath)");
            task.Param("Condition", "'%(MyAsset.RequiresProcessing)' == 'true'");
        })))
```

## Metadata Conditions

### Conditional Metadata

Set metadata only when conditions are met:

```csharp
.ItemGroup(null, group =>
{
    group.Include("Content", "appsettings.json", item =>
    {
        item.Meta("CopyToOutputDirectory", "Always");
        // Note: Individual metadata conditions not directly supported in fluent API
        // Use conditional item groups instead
    });
})
```

**Alternative: Conditional Item Groups**

```csharp
.ItemGroup("'$(Configuration)' == 'Debug'", group =>
{
    group.Update("Content", "appsettings.json", item => item
        .Meta("CopyToOutputDirectory", "Always"));
})

.ItemGroup("'$(Configuration)' == 'Release'", group =>
{
    group.Update("Content", "appsettings.json", item => item
        .Meta("CopyToOutputDirectory", "Never"));
})
```

## Metadata Transformations

### Basic Transformation

Transform items using metadata:

```csharp
.Props(p => p
    .Property("ContentPaths", "@(Content->'%(FullPath)')"))
```

**Result:** Semicolon-separated list of full paths.

### Transformation with Custom Separator

```csharp
.Property("ContentList", "@(Content->'%(Filename)', ',')")
```

**Result:** Comma-separated list of filenames.

### Multi-Metadata Transformation

Combine multiple metadata values:

```csharp
.Property("AssetInfo", "@(MyAsset->'%(Filename): %(AssetType) in %(AssetCategory)')")
```

**Result:** `logo: Logo in Branding;icon: Icon in UI`

### Conditional Transformation

Transform only items matching a condition:

```csharp
.Property("ProcessableAssets", 
    "@(MyAsset->'%(Identity)'->WithMetadataValue('RequiresProcessing', 'true'))")
```

## Metadata Batching

### Batching in Targets

Execute targets once per unique metadata value:

```csharp
.Targets(t => t
    .Target("ProcessByCategory", target => target
        // Batching syntax: %(ItemType.MetadataName)
        .Message("Processing category: %(MyAsset.AssetCategory)", "High")
        .Task("MakeDir", task =>
        {
            task.Param("Directories", "$(OutputPath)/assets/%(MyAsset.AssetCategory)");
        })
        .Task("Copy", task =>
        {
            task.Param("SourceFiles", "@(MyAsset)");
            task.Param("DestinationFolder", "$(OutputPath)/assets/%(MyAsset.AssetCategory)");
        })))
```

**Behavior:** Executes once per unique `AssetCategory` value.

## Advanced Metadata Patterns

### Pattern: Analyzer Metadata

Configure Roslyn analyzers:

```csharp
.ItemGroup(null, group =>
{
    group.Include("Analyzer", "$(MSBuildThisFileDirectory)../../analyzers/MyAnalyzer.dll", item => item
        .Meta("Visible", "false")
        .Meta("AnalyzerLanguage", "C#")
        .Meta("AnalyzerSeverity", "Warning"));
})
```

### Pattern: Multi-Target Framework Assets

Include different assets per target framework:

```csharp
.ItemGroup("'$(TargetFramework)' == 'net6.0'", group =>
{
    group.Include("Reference", "$(MSBuildThisFileDirectory)../../lib/net6.0/MyLib.dll", item => item
        .Meta("Private", "false")
        .Meta("HintPath", "$(MSBuildThisFileDirectory)../../lib/net6.0/MyLib.dll"));
})

.ItemGroup("'$(TargetFramework)' == 'net8.0'", group =>
{
    group.Include("Reference", "$(MSBuildThisFileDirectory)../../lib/net8.0/MyLib.dll", item => item
        .Meta("Private", "false")
        .Meta("HintPath", "$(MSBuildThisFileDirectory)../../lib/net8.0/MyLib.dll"));
})
```

### Pattern: Content with Preprocessing

Mark content for build-time processing:

```csharp
.ItemGroup(null, group =>
{
    group.Include("MyContent", "templates/**/*.template", item => item
        .Meta("PreprocessTemplate", "true")
        .Meta("OutputExtension", ".cs")
        .Meta("Generator", "TextTemplatingFileGenerator"));
})

.Targets(t => t
    .Target("PreprocessContent", target => target
        .Task("Exec", task =>
        {
            task.Param("Command", "dotnet template-tool %(MyContent.FullPath) --output %(MyContent.Directory)%(MyContent.Filename)%(MyContent.OutputExtension)");
            task.Param("Condition", "'%(MyContent.PreprocessTemplate)' == 'true'");
        })))
```

### Pattern: Deployment Metadata

Track deployment configuration:

```csharp
.ItemGroup(null, group =>
{
    group.Include("DeploymentAsset", "deploy/**/*.*", item => item
        .Meta("DeploymentTarget", "Production")
        .Meta("DeploymentRegion", "US-East")
        .Meta("RequiresEncryption", "true")
        .Meta("Priority", "High"));
})
```

## Debugging Metadata

### Display Metadata Values

```csharp
.Targets(t => t
    .Target("ShowMetadata", target => target
        .Message("Item: %(Content.Identity)", "High")
        .Message("  FullPath: %(Content.FullPath)", "High")
        .Message("  Filename: %(Content.Filename)", "High")
        .Message("  Extension: %(Content.Extension)", "High")
        .Message("  CopyToOutput: %(Content.CopyToOutputDirectory)", "High")))
```

### List All Metadata

```csharp
.Targets(t => t
    .Target("ListAllMetadata", target => target
        .Message("@(Content->'%(Identity): %(FullPath), Copy=%(CopyToOutputDirectory)')", "High")))
```

## Performance Considerations

### Metadata Evaluation Cost

- Automatic metadata (e.g., `%(FullPath)`) requires file system access
- Minimize metadata evaluation in hot paths
- Use batching to process items efficiently

### Lazy Evaluation

Metadata is evaluated **lazily** when referenced:

```csharp
// FullPath not evaluated until target runs
.Target("ShowPath", target => target
    .Message("Path: %(Content.FullPath)", "High"))
```

## Summary

| Concept | Fluent API | XML Output |
|---------|-----------|------------|
| Element metadata | `.Meta("Key", "Value")` | `<Key>Value</Key>` |
| Attribute metadata | `.MetaAttribute("Key", "Value")` | `Key="Value"` |
| Well-known metadata | N/A (automatic) | `%(Filename)`, `%(FullPath)` |
| Custom metadata | `.Meta("Custom", "Value")` | `<Custom>Value</Custom>` |
| Metadata reference | `"%(MetadataName)"` | `%(MetadataName)` |

## Next Steps

- [Conditional Logic](conditionals.md) - Conditional metadata and items
- [Target Orchestration](../targets-tasks/orchestration.md) - Use metadata in targets
- [Built-in Tasks](../targets-tasks/builtin-tasks.md) - Tasks that use metadata
- [Multi-Target Framework](../advanced/multi-tfm.md) - Framework-specific metadata

## Additional Resources

- [MSBuild Item Metadata](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-well-known-item-metadata)
- [MSBuild Batching](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-batching)
- [Item Functions](https://learn.microsoft.com/en-us/visualstudio/msbuild/item-functions)
