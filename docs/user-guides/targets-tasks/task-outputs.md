# Task Outputs

Task outputs enable data flow between targets by capturing task results and making them available as properties or items. This guide covers output capturing, usage patterns, and advanced scenarios.

## Output Basics

### What Are Task Outputs?

Task outputs are **values returned by tasks** that can be:

- Stored in properties
- Added to item collections
- Used by subsequent tasks/targets

### Syntax

```csharp
.Task("TaskName", task =>
{
    task.Param("InputParam", "value");
    task.Output("OutputParameter", "PropertyOrItemName");
})
```

**Specify output type:**

```csharp
task.Output("OutputParam", "PropertyName");  // Property output
task.Output("OutputParam", "ItemName");      // Item output
```

## Capturing Property Outputs

### Simple Property Output

Capture task output to a property:

```csharp
.Target("GetCommitHash", target => target
    .Task("Exec", task =>
    {
        task.Param("Command", "git rev-parse HEAD");
        task.Param("ConsoleToMSBuild", "true");
        task.Output("ConsoleOutput", "GitCommitHash");  // → Property
    })
    .Message("Commit: $(GitCommitHash)", "High"))
```

**Generated XML:**

```xml
<Target Name="GetCommitHash">
  <Exec Command="git rev-parse HEAD" ConsoleToMSBuild="true">
    <Output TaskParameter="ConsoleOutput" PropertyName="GitCommitHash" />
  </Exec>
  <Message Text="Commit: $(GitCommitHash)" Importance="High" />
</Target>
```

### Multiple Outputs

Capture multiple outputs from a single task:

```csharp
.Task("Copy", task =>
{
    task.Param("SourceFiles", "@(Content)");
    task.Param("DestinationFolder", "$(OutputPath)/content");
    task.Output("CopiedFiles", "ContentFilesCopied");     // Item
    task.Output("DestinationFiles", "ContentDestPaths");  // Item
})
```

## Capturing Item Outputs

### Simple Item Output

Capture to an item collection:

```csharp
.Target("CopyAssets", target => target
    .Task("Copy", task =>
    {
        task.Param("SourceFiles", "@(Asset)");
        task.Param("DestinationFolder", "$(OutputPath)/assets");
        task.Output("CopiedFiles", "CopiedAssetFiles");  // → Item list
    })
    .Message("Copied @(CopiedAssetFiles->Count()) files", "High"))
```

### Item Outputs with Transforms

Use captured items in transformations:

```csharp
.Target("ProcessAndReport", target => target
    .Task("Copy", task =>
    {
        task.Param("SourceFiles", "@(Source)");
        task.Param("DestinationFolder", "$(OutputPath)");
        task.Output("CopiedFiles", "Copied");
    })
    
    // Transform captured items
    .PropertyGroup(null, group =>
    {
        group.Property("CopiedFileNames", "@(Copied->'%(Filename)%(Extension)')");
        group.Property("CopiedFullPaths", "@(Copied->'%(FullPath)')");
    })
    
    .Message("Copied files: $(CopiedFileNames)", "Normal")
    .Message("Full paths: $(CopiedFullPaths)", "Low"))
```

## Common Task Outputs

### Copy Task

**Outputs:**

- `CopiedFiles`: Successfully copied files
- `DestinationFiles`: Destination paths

```csharp
.Task("Copy", task =>
{
    task.Param("SourceFiles", "@(Content)");
    task.Param("DestinationFolder", "$(OutputPath)/content");
    task.Output("CopiedFiles", "ContentCopied");
    task.Output("DestinationFiles", "ContentDestinations");
})
```

### Exec Task

**Outputs:**

- `ConsoleOutput`: Captured stdout (requires `ConsoleToMSBuild="true"`)
- `ExitCode`: Process exit code

```csharp
.Task("Exec", task =>
{
    task.Param("Command", "npm --version");
    task.Param("ConsoleToMSBuild", "true");
    task.Output("ConsoleOutput", "NpmVersion");
    task.Output("ExitCode", "NpmExitCode");
})
```

### GetFileHash Task

**Outputs:**

- `Items`: Items with hash metadata

```csharp
.Task("GetFileHash", task =>
{
    task.Param("Files", "@(AssemblyFile)");
    task.Param("Algorithm", "SHA256");
    task.Output("Items", "HashedFiles");
})

// Use hash metadata
.Message("Hash: %(HashedFiles.FileHash)", "Normal")
```

### ReadLinesFromFile Task

**Outputs:**

- `Lines`: File lines as items

```csharp
.Task("ReadLinesFromFile", task =>
{
    task.Param("File", "version.txt");
    task.Output("Lines", "VersionLines");
})

.PropertyGroup(null, group =>
{
    group.Property("Version", "@(VersionLines)");  // First line
})
```

### MakeDir Task

**Outputs:**

- `DirectoriesCreated`: Created directories

```csharp
.Task("MakeDir", task =>
{
    task.Param("Directories", "$(CustomPath1);$(CustomPath2)");
    task.Output("DirectoriesCreated", "CreatedDirs");
})

.Message("Created: @(CreatedDirs)", "Normal")
```

## Advanced Patterns

### Pattern: Conditional Processing Based on Output

```csharp
.Target("CopyIfNeeded", target => target
    .Task("Copy", task =>
    {
        task.Param("SourceFiles", "@(Content)");
        task.Param("DestinationFolder", "$(OutputPath)/content");
        task.Param("SkipUnchangedFiles", "true");
        task.Output("CopiedFiles", "ActuallyCopied");
    })
    
    // Only run if files were copied
    .PropertyGroup(null, group =>
    {
        group.Property("AnythingCopied", "true", "@(ActuallyCopied->Count()) > 0");
    })
    
    .Task("Exec", task =>
    {
        task.Param("Command", "post-copy-script.bat");
        task.Param("Condition", "'$(AnythingCopied)' == 'true'");
    }))
```

### Pattern: Chain Tasks with Outputs

```csharp
.Target("ProcessFiles", target => target
    // Step 1: Copy files
    .Task("Copy", task =>
    {
        task.Param("SourceFiles", "@(SourceFile)");
        task.Param("DestinationFolder", "$(IntermediateOutputPath)");
        task.Output("CopiedFiles", "FilesToProcess");
    })
    
    // Step 2: Process copied files
    .Task("Exec", task =>
    {
        task.Param("Command", "processor.exe %(FilesToProcess.FullPath)");
    })
    
    // Step 3: Copy processed files to final location
    .Task("Copy", task =>
    {
        task.Param("SourceFiles", "@(FilesToProcess)");
        task.Param("DestinationFolder", "$(OutputPath)");
        task.Output("CopiedFiles", "FinalFiles");
    })
    
    .Message("Processed @(FinalFiles->Count()) files", "High"))
```

### Pattern: Aggregate Outputs Across Batches

```csharp
.Target("ProcessByCategory", target => target
    // Process each category (batching on metadata)
    .Task("Exec", task =>
    {
        task.Param("Command", "process-category.exe %(Asset.Category)");
        task.Output("ConsoleOutput", "ProcessedCategories");
    })
    
    // Aggregate all processed categories
    .PropertyGroup(null, group =>
    {
        group.Property("AllCategories", "@(ProcessedCategories)", "'@(ProcessedCategories)' != ''");
    })
    
    .Message("Processed categories: $(AllCategories)", "High"))
```

### Pattern: Error Handling with Outputs

```csharp
.Target("SafeExecute", target => target
    .Task("Exec", task =>
    {
        task.Param("Command", "risky-command.exe");
        task.Param("IgnoreExitCode", "true");
        task.Param("ConsoleToMSBuild", "true");
        task.Output("ExitCode", "CommandExitCode");
        task.Output("ConsoleOutput", "CommandOutput");
    })
    
    .Warning("'$(CommandExitCode)' != '0'", "Command failed with exit code $(CommandExitCode)")
    .Message("Output: $(CommandOutput)", "Normal", "'$(CommandOutput)' != ''"))
```

### Pattern: Build Version from Git

```csharp
.Target("ComputeVersion", target => target
    // Get commit count
    .Task("Exec", task =>
    {
        task.Param("Command", "git rev-list --count HEAD");
        task.Param("ConsoleToMSBuild", "true");
        task.Output("ConsoleOutput", "CommitCount");
    })
    
    // Get commit hash
    .Task("Exec", task =>
    {
        task.Param("Command", "git rev-parse --short HEAD");
        task.Param("ConsoleToMSBuild", "true");
        task.Output("ConsoleOutput", "CommitHash");
    })
    
    // Compute version
    .PropertyGroup(null, group =>
    {
        group.Property("VersionMajor", "1");
        group.Property("VersionMinor", "0");
        group.Property("VersionBuild", "@(CommitCount)");
        group.Property("VersionRevision", "0");
        group.Property("Version", "$(VersionMajor).$(VersionMinor).$(VersionBuild).$(VersionRevision)");
        group.Property("InformationalVersion", "$(Version)+$(CommitHash)");
    })
    
    .Message("Computed version: $(InformationalVersion)", "High"))
```

## Using Outputs in Subsequent Targets

### Target Returns

Return items from a target for use by dependents:

```csharp
.Target("ProduceData", target => target
    .Returns("@(ProducedData)")
    
    .ItemGroup(null, group =>
    {
        group.Include("ProducedData", "data1.txt", item => item.Meta("Type", "Config"));
        group.Include("ProducedData", "data2.txt", item => item.Meta("Type", "Assets"));
    }))

.Target("ConsumeData", target => target
    .DependsOnTargets("ProduceData")
    
    .Message("Data items: @(ProducedData)", "High")
    .Message("Config items: @(ProducedData->WithMetadataValue('Type', 'Config'))", "Normal"))
```

### CallTarget with Outputs

Call another target and capture outputs:

```csharp
.Target("ComputeHash", target => target
    .Returns("@(HashedFile)")
    
    .Task("GetFileHash", task =>
    {
        task.Param("Files", "@(InputFile)");
        task.Output("Items", "HashedFile");
    }))

.Target("UseHash", target => target
    .Task("CallTarget", task =>
    {
        task.Param("Targets", "ComputeHash");
        task.Output("TargetOutputs", "ComputedHashes");
    })
    
    .Message("Hashes: @(ComputedHashes)", "High"))
```

## Debugging Outputs

### Display Output Values

```csharp
.Target("DebugOutputs", target => target
    .Task("Copy", task =>
    {
        task.Param("SourceFiles", "@(Content)");
        task.Param("DestinationFolder", "$(OutputPath)");
        task.Output("CopiedFiles", "Copied");
    })
    
    // Debug outputs
    .Message("Output count: @(Copied->Count())", "High")
    .Message("Output items: @(Copied)", "High")
    .Message("Output names: @(Copied->'%(Filename)%(Extension)')", "High"))
```

### Verify Output Types

```csharp
.Target("VerifyOutputs", target => target
    .Task("SomeTask", task =>
    {
        task.Output("SomeOutput", "CapturedValue");
    })
    
    // Verify property
    .Message("Property value: $(CapturedValue)", "High", "'$(CapturedValue)' != ''")
    .Warning("'$(CapturedValue)' == ''", "Output was not captured")
    
    // Verify item
    .Message("Item count: @(CapturedValue->Count())", "High", "@(CapturedValue->Count()) > 0")
    .Warning("@(CapturedValue->Count()) == 0", "No items captured"))
```

## Best Practices

### DO: Capture Outputs for Reuse

```csharp
// ✓ Capture for later use
.Task("Copy", task =>
{
    task.Param("SourceFiles", "@(Content)");
    task.Param("DestinationFolder", "$(OutputPath)");
    task.Output("CopiedFiles", "ContentCopied");
})
```

### DO: Use Descriptive Output Names

```csharp
// ✓ Clear
task.Output("CopiedFiles", "CopiedContentFiles");
task.Output("ConsoleOutput", "GitCommitHash");

// ✗ Unclear
task.Output("CopiedFiles", "Output1");
task.Output("ConsoleOutput", "Data");
```

### DO: Check Output Existence Before Use

```csharp
// ✓ Safe
.Message("Count: @(Output->Count())", "High", "@(Output->Count()) > 0")

// ✗ May produce confusing message if empty
.Message("Count: @(Output->Count())", "High")
```

### DON'T: Overwrite Outputs Unintentionally

```csharp
// ✗ Second output overwrites first
.Task("Task1", task =>
{
    task.Output("Result", "MyOutput");
})
.Task("Task2", task =>
{
    task.Output("Result", "MyOutput");  // ✗ Overwrites
})

// ✓ Use different output names
.Task("Task1", task =>
{
    task.Output("Result", "MyOutput1");
})
.Task("Task2", task =>
{
    task.Output("Result", "MyOutput2");
})
```

## Summary

| Output Type | Syntax | Use For |
|-------------|--------|---------|
| Property | `task.Output("Param", "PropName")` | Single values, strings |
| Item | `task.Output("Param", "ItemName")` | Collections, file lists |
| Target return | `.Returns("@(Item)")` | Cross-target data flow |

**Common task outputs:**

| Task | Output Parameter | Description |
|------|------------------|-------------|
| `Copy` | `CopiedFiles` | Files successfully copied |
| `Copy` | `DestinationFiles` | Destination file paths |
| `Exec` | `ConsoleOutput` | Captured stdout |
| `Exec` | `ExitCode` | Process exit code |
| `GetFileHash` | `Items` | Items with hash metadata |
| `ReadLinesFromFile` | `Lines` | File lines as items |
| `MakeDir` | `DirectoriesCreated` | Created directories |

## Next Steps

- [Built-in Tasks](builtin-tasks.md) - Task reference
- [Target Orchestration](orchestration.md) - Target dependencies
- [UsingTask](../advanced/usingtask.md) - Custom tasks with outputs
- [MSBuild Target Outputs (Microsoft Docs)](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-targets#target-outputs)
