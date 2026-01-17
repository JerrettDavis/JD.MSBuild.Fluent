# Working with Targets

Targets are the core execution units in MSBuild. This guide covers defining targets, orchestrating execution order, implementing incremental builds, and integrating tasks.

## Overview

MSBuild targets define sequences of tasks that execute during the build process. Targets can depend on other targets, execute conditionally, and run before or after specific points in the build lifecycle.

In JD.MSBuild.Fluent, you define targets using the `TargetsBuilder` and `TargetBuilder` classes.

## Defining Targets

### Basic Target Definition

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

Package.Define("Contoso.Build")
    .Targets(targets =>
    {
        targets.Target("Contoso_Hello", target =>
        {
            target.Message("Hello from Contoso!");
        });
    })
    .Build();
```

### Strongly-Typed Target Names

Define typed target names for compile-time safety:

```csharp
using JD.MSBuild.Fluent.Typed;

public readonly struct ContosoHello : IMsBuildTargetName
{
    public string Name => "Contoso_Hello";
}

// Usage
targets.Target<ContosoHello>(target =>
{
    target.Message("Hello from Contoso!");
});
```

## Target Orchestration

MSBuild provides three mechanisms for ordering target execution:

1. **BeforeTargets** - Run before specified targets
2. **AfterTargets** - Run after specified targets
3. **DependsOnTargets** - Ensure specified targets run first

### BeforeTargets

Execute a target before another target:

```csharp
targets.Target("Contoso_PreBuild", target =>
{
    target.BeforeTargets("Build");
    target.Message("Running before Build");
});
```

**Multiple targets**:

```csharp
target.BeforeTargets("Build;Test;Pack");
```

**Strongly-typed syntax**:

```csharp
using JD.MSBuild.Fluent.Typed;

public readonly struct Build : IMsBuildTargetName
{
    public string Name => "Build";
}

public readonly struct Test : IMsBuildTargetName
{
    public string Name => "Test";
}

target.BeforeTargets(new Build(), new Test());
```

### AfterTargets

Execute a target after another target:

```csharp
targets.Target("Contoso_PostBuild", target =>
{
    target.AfterTargets("Build");
    target.Message("Running after Build");
});
```

### DependsOnTargets

Declare dependencies that must execute before this target:

```csharp
targets.Target("Contoso_Package", target =>
{
    target.DependsOnTargets("Contoso_Build;Contoso_Test");
    target.Message("Packaging after build and test");
});
```

**Strongly-typed syntax**:

```csharp
target.DependsOnTargets(new ContosoBuild(), new ContosoTest());
```

### Execution Order Example

```csharp
targets
    .Target("Contoso_Init", target =>
    {
        target.BeforeTargets("Build");
        target.Message("Step 1: Initialize");
    })
    .Target("Contoso_Prepare", target =>
    {
        target.DependsOnTargets("Contoso_Init");
        target.BeforeTargets("Build");
        target.Message("Step 2: Prepare");
    })
    .Target("Contoso_Validate", target =>
    {
        target.AfterTargets("Build");
        target.Message("Step 3: Validate");
    })
    .Target("Contoso_Finalize", target =>
    {
        target.DependsOnTargets("Contoso_Validate");
        target.AfterTargets("Build");
        target.Message("Step 4: Finalize");
    });
```

**Execution flow**:
```
Contoso_Init (BeforeTargets Build)
  → Contoso_Prepare (DependsOn Init, BeforeTargets Build)
    → Build
      → Contoso_Validate (AfterTargets Build)
        → Contoso_Finalize (DependsOn Validate, AfterTargets Build)
```

## Conditional Execution

### Target-Level Conditions

Execute a target only when a condition is met:

```csharp
targets.Target("Contoso_ReleaseOnly", target =>
{
    target.Condition("'$(Configuration)' == 'Release'");
    target.Message("This only runs in Release configuration");
});
```

**Complex conditions**:

```csharp
target.Condition("'$(Configuration)' == 'Release' AND '$(Platform)' == 'x64'");
```

**Using expression helpers**:

```csharp
using static JD.MSBuild.Fluent.Typed.MsBuildExpr;

public readonly struct EnableOptimization : IMsBuildPropertyName
{
    public string Name => "EnableOptimization";
}

target.Condition(IsTrue<EnableOptimization>());
```

### Task-Level Conditions

Execute individual tasks conditionally:

```csharp
targets.Target("Contoso_ConditionalSteps", target =>
{
    target.Message("This always runs");
    
    target.Message("This only runs in Debug", 
        condition: "'$(Configuration)' == 'Debug'");
    
    target.Message("This only runs in Release", 
        condition: "'$(Configuration)' == 'Release'");
});
```

## Incremental Builds

Incremental builds skip targets when inputs haven't changed since outputs were produced.

### Inputs and Outputs

```csharp
targets.Target("Contoso_GenerateCode", target =>
{
    target.Inputs("@(SourceTemplate)");
    target.Outputs("$(IntermediateOutputPath)%(SourceTemplate.Filename).g.cs");
    
    target.Message("Generating code from @(SourceTemplate)");
    
    target.Task("MyCodeGenerator", task =>
    {
        task.Param("InputFile", "%(SourceTemplate.Identity)");
        task.Param("OutputFile", "$(IntermediateOutputPath)%(SourceTemplate.Filename).g.cs");
    });
});
```

**How it works**:
- MSBuild compares timestamps of `Inputs` vs `Outputs`
- If all outputs are newer than all inputs, the target is skipped
- Use item metadata with `%(...)` for batching

### Batching Example

Process each input file separately:

```csharp
targets.Target("Contoso_ProcessFiles", target =>
{
    target.Inputs("%(FileToProcess.Identity)");
    target.Outputs("$(OutputPath)%(FileToProcess.Filename).processed");
    
    // This target executes once per unique FileToProcess item
    target.Message("Processing %(FileToProcess.Identity)");
    
    target.Task("Copy", task =>
    {
        task.Param("SourceFiles", "%(FileToProcess.Identity)");
        task.Param("DestinationFiles", "$(OutputPath)%(FileToProcess.Filename).processed");
    });
});
```

### Force Rebuild

Omit `Inputs` and `Outputs` to run the target every time:

```csharp
targets.Target("Contoso_AlwaysRun", target =>
{
    // No Inputs/Outputs - always executes
    target.Message("This runs on every build");
});
```

## Task Invocations

Targets contain task invocations that perform the actual work.

### Built-In Tasks

#### Message

Log informational messages:

```csharp
target.Message("Build started", importance: "High");
target.Message("Verbose details", importance: "Low");
target.Message("Normal message", importance: "Normal");
```

**Importance levels**:
- `High` - Always shown
- `Normal` - Default verbosity
- `Low` - Verbose/diagnostic logging

#### Exec

Execute external commands:

```csharp
target.Exec("dotnet --version");
target.Exec("npm install", workingDirectory: "$(ProjectDir)/client");
target.Exec("git rev-parse HEAD", workingDirectory: "$(MSBuildProjectDirectory)");
```

#### Error and Warning

Report build errors or warnings:

```csharp
target.Error("Missing required property: MyProperty", 
    code: "CONTOSO001",
    condition: "'$(MyProperty)' == ''");

target.Warning("Deprecated feature used", 
    code: "CONTOSO002",
    condition: "'$(UseDeprecatedFeature)' == 'true'");
```

### Common Built-In Tasks

#### MakeDir

Create directories:

```csharp
target.Task("MakeDir", task =>
{
    task.Param("Directories", "$(OutputPath);$(IntermediateOutputPath)");
});
```

#### Copy

Copy files:

```csharp
target.Task("Copy", task =>
{
    task.Param("SourceFiles", "@(ContentFiles)");
    task.Param("DestinationFolder", "$(OutputPath)");
    task.Param("SkipUnchangedFiles", "true");
});
```

#### Delete

Delete files or directories:

```csharp
target.Task("Delete", task =>
{
    task.Param("Files", "@(FilesToDelete)");
});

target.Task("RemoveDir", task =>
{
    task.Param("Directories", "$(IntermediateOutputPath)");
});
```

#### WriteLinesToFile

Write text to a file:

```csharp
target.Task("WriteLinesToFile", task =>
{
    task.Param("File", "$(OutputPath)manifest.txt");
    task.Param("Lines", "Version: $(Version)");
    task.Param("Overwrite", "true");
    task.Param("Encoding", "UTF-8");
});
```

#### ReadLinesFromFile

Read lines from a file into an item:

```csharp
target.Task("ReadLinesFromFile", task =>
{
    task.Param("File", "dependencies.txt");
    task.OutputItem("Lines", "DependencyList");
});
```

### Custom Tasks

See [Custom Tasks](custom-tasks.md) for details on invoking custom MSBuild tasks.

## Dynamic Properties and Items

### PropertyGroup Inside Targets

Set properties dynamically during execution:

```csharp
targets.Target("Contoso_SetDynamicProps", target =>
{
    target.PropertyGroup(null, group =>
    {
        group.Property("BuildTimestamp", "$([System.DateTime]::Now.ToString('yyyyMMdd-HHmmss'))");
        group.Property("GitCommit", "$(GitCommit)");
    });
    
    target.Message("Built at $(BuildTimestamp) from commit $(GitCommit)");
});
```

**Conditional property groups**:

```csharp
target.PropertyGroup("'$(Configuration)' == 'Release'", group =>
{
    group.Property("PublishUrl", "https://production.example.com");
});

target.PropertyGroup("'$(Configuration)' == 'Debug'", group =>
{
    group.Property("PublishUrl", "https://staging.example.com");
});
```

### ItemGroup Inside Targets

Manipulate items during execution:

```csharp
targets.Target("Contoso_PrepareFiles", target =>
{
    // Add items
    target.ItemGroup(null, group =>
    {
        group.Include<ContentFiles>("$(SourceDir)/**/*.config");
    });
    
    // Remove items
    target.ItemGroup(null, group =>
    {
        group.Remove<ContentFiles>("**/appsettings.Development.json", 
            condition: "'$(Configuration)' == 'Release'");
    });
    
    // Update metadata
    target.ItemGroup(null, group =>
    {
        group.Update<ContentFiles>("**/*.json", item =>
        {
            item.Meta("CopyToOutputDirectory", "PreserveNewest");
        });
    });
});
```

## Complete Target Examples

### Pre-Build Validation

```csharp
targets.Target("Contoso_ValidateEnvironment", target =>
{
    target.BeforeTargets("Build");
    target.Label("Validate build environment");
    
    // Check required properties
    target.Error("ApiKey property is required", 
        code: "CONTOSO001",
        condition: "'$(ApiKey)' == ''");
    
    target.Error("OutputPath must be set", 
        code: "CONTOSO002",
        condition: "'$(OutputPath)' == ''");
    
    // Validate tools
    target.Exec("dotnet --version");
    target.Exec("node --version", 
        condition: "'$(BuildClientApp)' == 'true'");
    
    target.Message("Environment validation passed", importance: "High");
});
```

### Code Generation

```csharp
targets.Target("Contoso_GenerateVersionInfo", target =>
{
    target.BeforeTargets("CoreCompile");
    target.Inputs("$(MSBuildProjectFile)");
    target.Outputs("$(IntermediateOutputPath)VersionInfo.g.cs");
    
    // Compute version properties
    target.PropertyGroup(null, group =>
    {
        group.Property("BuildNumber", "$([System.DateTime]::Now.ToString('yyyyMMdd'))");
        group.Property("GitCommit", "$(GitCommit)");
        group.Comment("Generated version info");
    });
    
    // Generate C# file
    target.Task("WriteLinesToFile", task =>
    {
        task.Param("File", "$(IntermediateOutputPath)VersionInfo.g.cs");
        task.Param("Lines", 
            "using System;^" +
            "namespace Generated^" +
            "{^" +
            "    public static class VersionInfo^" +
            "    {^" +
            "        public const string Version = \"$(Version)\";^" +
            "        public const string BuildNumber = \"$(BuildNumber)\";^" +
            "        public const string GitCommit = \"$(GitCommit)\";^" +
            "    }^" +
            "}");
        task.Param("Overwrite", "true");
    });
    
    // Add generated file to compilation
    target.ItemGroup(null, group =>
    {
        group.Include<Compile>("$(IntermediateOutputPath)VersionInfo.g.cs");
    });
    
    target.Message("Generated VersionInfo.g.cs", importance: "High");
});
```

### Asset Packaging

```csharp
targets.Target("Contoso_PackageAssets", target =>
{
    target.AfterTargets("Build");
    target.Condition("'$(PackageAssets)' == 'true'");
    target.Label("Package application assets");
    
    // Create output directory
    target.Task("MakeDir", task =>
    {
        task.Param("Directories", "$(PackageOutputPath)");
    });
    
    // Copy runtime dependencies
    target.Task("Copy", task =>
    {
        task.Param("SourceFiles", "@(RuntimeDependency)");
        task.Param("DestinationFolder", "$(PackageOutputPath)/lib");
        task.Param("SkipUnchangedFiles", "true");
    });
    
    // Copy content files
    target.Task("Copy", task =>
    {
        task.Param("SourceFiles", "@(ContentFiles)");
        task.Param("DestinationFiles", 
            "@(ContentFiles->'$(PackageOutputPath)/content/%(RecursiveDir)%(Filename)%(Extension)')");
    });
    
    // Generate manifest
    target.Task("WriteLinesToFile", task =>
    {
        task.Param("File", "$(PackageOutputPath)/manifest.json");
        task.Param("Lines", 
            "{^" +
            "  \"name\": \"$(PackageId)\",^" +
            "  \"version\": \"$(Version)\",^" +
            "  \"files\": [@(RuntimeDependency->'\"%(Filename)%(Extension)\"', ', ')]^" +
            "}");
        task.Param("Overwrite", "true");
    });
    
    target.Message("Assets packaged to $(PackageOutputPath)", importance: "High");
});
```

### Multi-Step Build

```csharp
targets
    .Target("Contoso_RestoreDeps", target =>
    {
        target.BeforeTargets("Build");
        target.Message("Restoring dependencies...", importance: "High");
        target.Exec("npm install", workingDirectory: "$(ClientAppPath)");
    })
    .Target("Contoso_BuildClient", target =>
    {
        target.DependsOnTargets("Contoso_RestoreDeps");
        target.BeforeTargets("Build");
        target.Inputs("$(ClientAppPath)/**/*.ts;$(ClientAppPath)/**/*.tsx");
        target.Outputs("$(ClientAppPath)/dist/bundle.js");
        
        target.Message("Building client application...", importance: "High");
        target.Exec("npm run build", workingDirectory: "$(ClientAppPath)");
    })
    .Target("Contoso_CopyClientAssets", target =>
    {
        target.DependsOnTargets("Contoso_BuildClient");
        target.AfterTargets("Build");
        
        target.Task("Copy", task =>
        {
            task.Param("SourceFiles", "@(ClientAsset)");
            task.Param("DestinationFolder", "$(OutputPath)/wwwroot");
        });
        
        target.Message("Client assets copied", importance: "High");
    });
```

## Target Labels

Add human-readable labels for documentation:

```csharp
targets.Target("Contoso_ComplexProcess", target =>
{
    target.Label("Complex multi-step build process");
    target.BeforeTargets("Build");
    
    // Target implementation
});
```

Labels appear in the XML as:

```xml
<Target Name="Contoso_ComplexProcess" 
        Label="Complex multi-step build process" 
        BeforeTargets="Build">
  <!-- ... -->
</Target>
```

## Comments

Add comments to document target logic:

```csharp
targets.Target("Contoso_Deploy", target =>
{
    target.Comment("========================================");
    target.Comment(" Deployment Steps");
    target.Comment("========================================");
    
    target.Comment("Step 1: Prepare deployment package");
    target.Exec("prepare-deploy.cmd");
    
    target.Comment("Step 2: Upload to server");
    target.Exec("upload-deploy.cmd");
    
    target.Comment("Step 3: Activate deployment");
    target.Exec("activate-deploy.cmd");
});
```

## Best Practices

### Naming Conventions

Use clear, hierarchical target names:

```csharp
// ✅ Good
"Contoso_PreBuild"
"Contoso_Build_Client"
"Contoso_Build_Server"
"Contoso_PostBuild_Package"

// ❌ Avoid
"MyTarget"
"DoStuff"
"Target1"
```

### Granular Targets

Break complex processes into smaller, composable targets:

```csharp
// ✅ Good - composable
targets
    .Target("Contoso_ValidateInputs", target => { /* ... */ })
    .Target("Contoso_PrepareEnvironment", target => 
    {
        target.DependsOnTargets("Contoso_ValidateInputs");
        // ...
    })
    .Target("Contoso_ExecuteBuild", target => 
    {
        target.DependsOnTargets("Contoso_PrepareEnvironment");
        // ...
    });

// ❌ Avoid - monolithic
targets.Target("Contoso_DoEverything", target =>
{
    // 100 lines of task invocations
});
```

### Use Incremental Builds

Always specify `Inputs` and `Outputs` for targets that transform files:

```csharp
// ✅ Good - incremental
target.Inputs("@(SourceFiles)");
target.Outputs("$(OutputPath)%(SourceFiles.Filename).out");

// ❌ Avoid - runs every time
// (no Inputs/Outputs)
```

### Conditional Execution

Use conditions to avoid unnecessary work:

```csharp
// ✅ Good
target.Condition("'$(EnableFeature)' == 'true'");

// ✅ Also good for expensive checks
target.Condition("Exists('$(ConfigFile)')");
```

### Error Handling

Validate prerequisites and provide clear error messages:

```csharp
// ✅ Good
target.Error(
    "ApiKey property is required for deployment. Set it in your project file or via /p:ApiKey=YOUR_KEY",
    code: "CONTOSO001",
    condition: "'$(ApiKey)' == '' AND '$(Deploy)' == 'true'");

// ❌ Avoid - unclear error
target.Error("Missing property", condition: "'$(ApiKey)' == ''");
```

### Logging

Use appropriate message importance:

```csharp
// High - key milestones
target.Message("Build completed successfully", importance: "High");

// Normal - progress updates
target.Message("Compiling 10 files", importance: "Normal");

// Low - diagnostic details
target.Message("Processing file: example.cs", importance: "Low");
```

## Troubleshooting

### Target Not Executing

**Check conditions**: Ensure target and task conditions evaluate to true.

```csharp
// Add diagnostic output
target.Message("Target executing: condition is true");
```

**Check orchestration**: Verify `BeforeTargets`, `AfterTargets`, and `DependsOnTargets` reference valid targets.

### Incremental Build Not Working

**Check Inputs/Outputs syntax**: Ensure paths are correct and use `%(...)` for batching.

```csharp
// ✅ Correct
target.Outputs("$(IntermediateOutputPath)%(Compile.Filename).obj");

// ❌ Wrong - missing directory separator
target.Outputs("$(IntermediateOutputPath)%(Compile.Filename).obj");
```

**Force rebuild**: Delete intermediate output to test incremental logic.

### Target Runs Too Often

**Missing Inputs/Outputs**: Add them for incremental builds.

**Wrong BeforeTargets/AfterTargets**: Check orchestration attributes.

## Next Steps

- [Custom Tasks](custom-tasks.md) - Learn to declare and invoke custom tasks
- [Best Practices](../best-practices/index.md) - Patterns for robust targets
- [Migration Guide](../migration/from-xml.md) - Convert XML targets to fluent API

## Related Topics

- [Fluent Builders](../core-concepts/builders.md) - Complete builder API reference
- [Architecture](../core-concepts/architecture.md) - Understanding the IR layer
