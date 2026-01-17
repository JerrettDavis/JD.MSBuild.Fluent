# Built-in MSBuild Tasks

MSBuild provides a comprehensive set of built-in tasks for common build operations. This reference covers the most frequently used tasks and their usage with JD.MSBuild.Fluent.

## Task Invocation

### Generic Task Invocation

Use the `.Task()` method to invoke any MSBuild task:

```csharp
.Target("MyTarget", target => target
    .Task("TaskName", task =>
    {
        task.Param("Parameter1", "value1");
        task.Param("Parameter2", "value2");
    }))
```

### Convenience Methods

JD.MSBuild.Fluent provides convenience methods for common tasks:

```csharp
.Target("MyTarget", target => target
    .Message("Hello, MSBuild!", "High")
    .Warning("'$(Condition)' == 'true'", "This is a warning")
    .Error("'$(Condition)' == 'true'", "This is an error")
    .Exec("npm run build", "$(MSBuildProjectDirectory)/client"))
```

## File and Directory Tasks

### Copy

Copy files from source to destination:

```csharp
.Task("Copy", task =>
{
    task.Param("SourceFiles", "@(Content)");
    task.Param("DestinationFolder", "$(OutputPath)/content");
    task.Param("SkipUnchangedFiles", "true");
    task.Param("OverwriteReadOnlyFiles", "true");
})
```

**Common parameters:**

| Parameter | Description | Example |
|-----------|-------------|---------|
| `SourceFiles` | Files to copy | `@(Content)` |
| `DestinationFiles` | Destination file paths | `@(Content->'$(OutputPath)/%(Filename)%(Extension)')` |
| `DestinationFolder` | Destination directory | `$(OutputPath)/assets` |
| `SkipUnchangedFiles` | Skip if destination is newer | `true` |
| `OverwriteReadOnlyFiles` | Overwrite read-only | `false` |
| `Retries` | Number of retry attempts | `3` |
| `RetryDelayMilliseconds` | Delay between retries | `1000` |

**Capture copied files:**

```csharp
.Task("Copy", task =>
{
    task.Param("SourceFiles", "@(Content)");
    task.Param("DestinationFolder", "$(OutputPath)/content");
    task.Output("CopiedFiles", "CopiedContentFiles");  // Item output
})
```

### Delete

Delete files:

```csharp
.Task("Delete", task =>
{
    task.Param("Files", "@(FilesToDelete);$(TempFile)");
})
```

**Parameters:**

- `Files`: Files to delete (semicolon-separated or item list)
- `TreatErrorsAsWarnings`: Continue on errors

### Move

Move files:

```csharp
.Task("Move", task =>
{
    task.Param("SourceFiles", "@(TemporaryFiles)");
    task.Param("DestinationFolder", "$(ArchiveFolder)");
    task.Param("OverwriteReadOnlyFiles", "true");
})
```

**Parameters:** Similar to Copy task

### MakeDir

Create directories:

```csharp
.Task("MakeDir", task =>
{
    task.Param("Directories", "$(OutputPath);$(IntermediateOutputPath);$(CustomPath)");
})
```

**Output:**

```csharp
.Task("MakeDir", task =>
{
    task.Param("Directories", "$(CustomPath)");
    task.Output("DirectoriesCreated", "CreatedDirs");  // Capture created dirs
})
```

### RemoveDir

Remove directories:

```csharp
.Task("RemoveDir", task =>
{
    task.Param("Directories", "$(CustomOutputPath);$(TempPath)");
})
```

**Parameters:**

- `Directories`: Directories to remove (semicolon-separated)

## File Content Tasks

### WriteLinesToFile

Write lines to a text file:

```csharp
.Task("WriteLinesToFile", task =>
{
    task.Param("File", "$(OutputPath)/buildinfo.txt");
    task.Param("Lines", "Build Date: $([System.DateTime]::UtcNow);Version: $(Version)");
    task.Param("Overwrite", "true");
    task.Param("Encoding", "UTF-8");
})
```

**Parameters:**

| Parameter | Description | Example |
|-----------|-------------|---------|
| `File` | Output file path | `$(OutputPath)/data.txt` |
| `Lines` | Lines to write (semicolon-separated or item) | `Line1;Line2;Line3` or `@(Lines)` |
| `Overwrite` | Overwrite existing file | `true` or `false` |
| `Encoding` | Text encoding | `UTF-8`, `ASCII`, `Unicode` |
| `WriteOnlyWhenDifferent` | Skip if content unchanged | `true` |

**Multi-line content:**

```csharp
.Task("WriteLinesToFile", task =>
{
    task.Param("File", "$(OutputPath)/manifest.json");
    task.Param("Lines", @"{
  ""version"": ""$(Version)"",
  ""timestamp"": ""$([System.DateTime]::UtcNow.ToString('o'))""
}");
    task.Param("Overwrite", "true");
})
```

### ReadLinesFromFile

Read lines from a file:

```csharp
.Task("ReadLinesFromFile", task =>
{
    task.Param("File", "$(MSBuildProjectDirectory)/version.txt");
    task.Output("Lines", "VersionLines");  // Capture as item
})
```

**Then use the output:**

```csharp
.PropertyGroup(null, group =>
{
    group.Property("Version", "@(VersionLines)");  // First line
})
```

### Touch

Update file timestamps:

```csharp
.Task("Touch", task =>
{
    task.Param("Files", "$(IntermediateOutputPath)/build.stamp");
    task.Param("AlwaysCreate", "true");  // Create if doesn't exist
})
```

## Execution Tasks

### Exec

Execute external commands:

```csharp
.Exec("npm run build", "$(MSBuildProjectDirectory)/client")
```

Or with Task method:

```csharp
.Task("Exec", task =>
{
    task.Param("Command", "dotnet tool restore");
    task.Param("WorkingDirectory", "$(MSBuildProjectDirectory)");
    task.Param("IgnoreExitCode", "false");
    task.Param("ConsoleToMSBuild", "true");  // Capture output
})
```

**Parameters:**

| Parameter | Description | Example |
|-----------|-------------|---------|
| `Command` | Command to execute | `npm install` |
| `WorkingDirectory` | Working directory | `$(MSBuildProjectDirectory)` |
| `IgnoreExitCode` | Continue on non-zero exit | `false` |
| `ConsoleToMSBuild` | Capture stdout/stderr | `true` |
| `IgnoreStandardErrorWarningFormat` | Don't parse stderr for errors | `false` |
| `Timeout` | Timeout in milliseconds | `60000` |

**Capture output:**

```csharp
.Task("Exec", task =>
{
    task.Param("Command", "git rev-parse HEAD");
    task.Param("ConsoleToMSBuild", "true");
    task.Output("ConsoleOutput", "GitCommitHash");
})
```

## Logging Tasks

### Message

Log messages:

```csharp
.Message("This is a message", "High")
```

Or:

```csharp
.Task("Message", task =>
{
    task.Param("Text", "Build succeeded for $(Configuration)");
    task.Param("Importance", "High");
})
```

**Importance levels:**

- `High`: Always shown
- `Normal`: Shown by default
- `Low`: Shown only with detailed verbosity

### Warning

Log warnings:

```csharp
.Warning("'$(Deprecated)' == 'true'", "This feature is deprecated")
```

Or:

```csharp
.Task("Warning", task =>
{
    task.Param("Text", "Configuration $(Configuration) is not recommended");
    task.Param("Condition", "'$(Configuration)' == 'Unusual'");
    task.Param("Code", "WARN001");
})
```

### Error

Log errors (stops build):

```csharp
.Error("'$(Required)' == ''", "Required property is missing")
```

Or:

```csharp
.Task("Error", task =>
{
    task.Param("Text", "TargetFramework must be specified");
    task.Param("Condition", "'$(TargetFramework)' == ''");
    task.Param("Code", "ERR001");
})
```

## Compression Tasks

### Zip (MSBuild 15.8+)

Create ZIP archives:

```csharp
.Task("ZipDirectory", task =>
{
    task.Param("SourceDirectory", "$(PublishDir)");
    task.Param("DestinationFile", "$(OutputPath)/package.zip");
    task.Param("Overwrite", "true");
})
```

**Parameters:**

- `SourceDirectory`: Directory to zip
- `DestinationFile`: Output ZIP path
- `Overwrite`: Overwrite existing file

### Unzip (MSBuild 15.8+)

Extract ZIP archives:

```csharp
.Task("Unzip", task =>
{
    task.Param("SourceFiles", "$(DownloadPath)/archive.zip");
    task.Param("DestinationFolder", "$(ExtractPath)");
    task.Param("OverwriteReadOnlyFiles", "true");
})
```

## Download Tasks

### DownloadFile (MSBuild 16.0+)

Download files from URLs:

```csharp
.Task("DownloadFile", task =>
{
    task.Param("SourceUrl", "https://example.com/asset.zip");
    task.Param("DestinationFolder", "$(IntermediateOutputPath)/downloads");
    task.Param("SkipUnchangedFiles", "true");
})
```

**Parameters:**

- `SourceUrl`: URL to download from
- `DestinationFolder`: Where to save
- `DestinationFileName`: Override filename
- `SkipUnchangedFiles`: Skip if already downloaded
- `Retries`: Number of retry attempts

## Hash Tasks

### GetFileHash (MSBuild 16.10+)

Compute file hashes:

```csharp
.Task("GetFileHash", task =>
{
    task.Param("Files", "@(AssemblyFile)");
    task.Param("Algorithm", "SHA256");
    task.Param("HashEncoding", "hex");
    task.Output("Items", "FileHashes");
})
```

**Parameters:**

- `Files`: Files to hash
- `Algorithm`: `SHA256`, `SHA384`, `SHA512`, `MD5`
- `HashEncoding`: `hex`, `base64`

## Assembly Tasks

### GetAssemblyIdentity

Get assembly information:

```csharp
.Task("GetAssemblyIdentity", task =>
{
    task.Param("AssemblyFiles", "@(ReferencePath)");
    task.Output("Assemblies", "AssemblyIdentities");
})
```

**Output includes:**

- `%(FullPath)`
- `%(Name)`
- `%(Version)`
- `%(PublicKeyToken)`
- `%(Culture)`

### AssignTargetPath

Assign target paths to items:

```csharp
.Task("AssignTargetPath", task =>
{
    task.Param("Files", "@(Content)");
    task.Param("RootFolder", "$(MSBuildProjectDirectory)");
    task.Output("AssignedFiles", "ContentWithTargetPath");
})
```

## Pattern: Common Task Combinations

### Copy and Log

```csharp
.Target("CopyAssets", target => target
    .Task("Copy", task =>
    {
        task.Param("SourceFiles", "@(Asset)");
        task.Param("DestinationFolder", "$(OutputPath)/assets");
        task.Param("SkipUnchangedFiles", "true");
        task.Output("CopiedFiles", "CopiedAssets");
    })
    .Message("Copied @(CopiedAssets->Count()) assets", "High"))
```

### Create Directory and Write File

```csharp
.Target("GenerateManifest", target => target
    .Task("MakeDir", task =>
    {
        task.Param("Directories", "$(OutputPath)/config");
    })
    .Task("WriteLinesToFile", task =>
    {
        task.Param("File", "$(OutputPath)/config/manifest.json");
        task.Param("Lines", @"{ ""version"": ""$(Version)"" }");
        task.Param("Overwrite", "true");
    }))
```

### Execute Command and Capture Output

```csharp
.Target("GetGitCommit", target => target
    .Task("Exec", task =>
    {
        task.Param("Command", "git rev-parse HEAD");
        task.Param("ConsoleToMSBuild", "true");
        task.Param("IgnoreExitCode", "true");
        task.Output("ConsoleOutput", "GitCommit");
    })
    .PropertyGroup(null, group =>
    {
        group.Property("SourceRevisionId", "@(GitCommit)", "'@(GitCommit)' != ''");
    })
    .Message("Git commit: $(SourceRevisionId)", "Normal"))
```

## Best Practices

### DO: Use SkipUnchangedFiles

```csharp
// ✓ Efficient
.Task("Copy", task =>
{
    task.Param("SourceFiles", "@(Content)");
    task.Param("DestinationFolder", "$(OutputPath)/content");
    task.Param("SkipUnchangedFiles", "true");  // Skip if up-to-date
})
```

### DO: Capture Task Outputs

```csharp
// ✓ Capture for use later
.Task("Copy", task =>
{
    task.Param("SourceFiles", "@(Content)");
    task.Param("DestinationFolder", "$(OutputPath)/content");
    task.Output("CopiedFiles", "ContentCopied");
})
```

### DO: Add Retries for Network Operations

```csharp
// ✓ Resilient
.Task("DownloadFile", task =>
{
    task.Param("SourceUrl", "https://example.com/file.zip");
    task.Param("DestinationFolder", "$(DownloadPath)");
    task.Param("Retries", "3");
    task.Param("RetryDelayMilliseconds", "1000");
})
```

### DON'T: Hardcode Paths

```csharp
// ✗ Hardcoded
.Task("Copy", task =>
{
    task.Param("SourceFiles", "@(Content)");
    task.Param("DestinationFolder", "C:\\MyProject\\output");  // ✗
})

// ✓ Use properties
.Task("Copy", task =>
{
    task.Param("SourceFiles", "@(Content)");
    task.Param("DestinationFolder", "$(OutputPath)/content");  // ✓
})
```

## Summary

Most frequently used tasks:

| Task | Purpose | Common Use |
|------|---------|------------|
| `Copy` | Copy files | Deploy assets |
| `MakeDir` | Create directories | Setup output structure |
| `WriteLinesToFile` | Write text file | Generate manifests |
| `Exec` | Run command | Build tools, npm, git |
| `Message` | Log message | Build progress |
| `Delete` | Remove files | Cleanup |
| `RemoveDir` | Remove directories | Cleanup |
| `DownloadFile` | Download from URL | Fetch dependencies |

## Next Steps

- [Task Outputs](task-outputs.md) - Capturing and using task results
- [UsingTask](../advanced/usingtask.md) - Declaring custom tasks
- [Target Orchestration](orchestration.md) - Combining tasks in targets
- [MSBuild Task Reference (Microsoft Docs)](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-task-reference)
