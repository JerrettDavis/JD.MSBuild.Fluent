# Target Orchestration

Target orchestration is the art of coordinating build tasks through target dependencies, execution order, and incremental builds. This guide covers target definition, dependencies, and advanced orchestration patterns.

## Target Basics

### What Are Targets?

Targets are **named units of work** that execute during the build. They:

- Execute tasks (compile, copy, test)
- Define dependencies on other targets
- Support incremental builds with inputs/outputs
- Run conditionally based on properties

### Simple Target

```csharp
.Target("MyTarget", target => target
    .Message("Executing MyTarget", "High"))
```

**Generated XML:**

```xml
<Target Name="MyTarget">
  <Message Text="Executing MyTarget" Importance="High" />
</Target>
```

### Target with Condition

```csharp
.Target("ConditionalTarget", target => target
    .Condition("'$(RunTarget)' == 'true'")
    .Message("Condition met!"))
```

**Generated XML:**

```xml
<Target Name="ConditionalTarget" Condition="'$(RunTarget)' == 'true'">
  <Message Text="Condition met!" />
</Target>
```

## Execution Order

### BeforeTargets

Run before specific targets:

```csharp
.Target("PreBuild", target => target
    .BeforeTargets("Build")
    .Message("Running before Build"))
```

**Generated XML:**

```xml
<Target Name="PreBuild" BeforeTargets="Build">
  <Message Text="Running before Build" />
</Target>
```

**Execution order:**

```
1. PreBuild
2. Build
```

### AfterTargets

Run after specific targets:

```csharp
.Target("PostBuild", target => target
    .AfterTargets("Build")
    .Message("Running after Build"))
```

**Generated XML:**

```xml
<Target Name="PostBuild" AfterTargets="Build">
  <Message Text="Running after Build" />
</Target>
```

**Execution order:**

```
1. Build
2. PostBuild
```

### DependsOnTargets

Explicitly declare dependencies:

```csharp
.Target("MainTarget", target => target
    .DependsOnTargets("Dependency1;Dependency2;Dependency3")
    .Message("Running MainTarget"))
```

**Generated XML:**

```xml
<Target Name="MainTarget" DependsOnTargets="Dependency1;Dependency2;Dependency3">
  <Message Text="Running MainTarget" />
</Target>
```

**Execution order:**

```
1. Dependency1
2. Dependency2
3. Dependency3
4. MainTarget
```

### Combining Execution Order Attributes

```csharp
.Target("ComplexTarget", target => target
    .DependsOnTargets("SetupTask")
    .BeforeTargets("Build")
    .AfterTargets("PreBuildEvent")
    .Message("Complex orchestration"))
```

**Execution order:**

```
1. PreBuildEvent
2. SetupTask (dependency)
3. ComplexTarget
4. Build
```

## Standard MSBuild Targets

### Common Integration Points

| Target | Phase | Use For |
|--------|-------|---------|
| `BeforeBuild` | Pre-build | Setup, validation |
| `Build` | Main build | Primary compilation |
| `AfterBuild` | Post-build | Packaging, deployment |
| `Clean` | Cleaning | Remove outputs |
| `Rebuild` | Full rebuild | Clean + Build |
| `CoreCompile` | Compilation | Custom compilation |
| `PrepareForBuild` | Early preparation | File generation |
| `BeforeCompile` | Pre-compilation | Code generation |
| `AfterCompile` | Post-compilation | IL weaving |

### Example Integration

```csharp
.Target("ValidateSettings", target => target
    .BeforeTargets("BeforeBuild")
    .Message("Validating project settings"))

.Target("GenerateAssets", target => target
    .BeforeTargets("CoreCompile")
    .Message("Generating compile-time assets"))

.Target("PackageOutputs", target => target
    .AfterTargets("AfterBuild")
    .Message("Packaging build outputs"))
```

## Incremental Builds

### Inputs and Outputs

Enable incremental builds by specifying inputs and outputs:

```csharp
.Target("ProcessTemplates", target => target
    .Inputs("@(Template)")
    .Outputs("@(Template->'$(IntermediateOutputPath)%(Filename).cs')")
    .Message("Processing templates")
    .Task("Exec", task =>
    {
        task.Param("Command", "template-tool %(Template.Identity) --output $(IntermediateOutputPath)%(Template.Filename).cs");
    }))
```

**Generated XML:**

```xml
<Target Name="ProcessTemplates" 
        Inputs="@(Template)" 
        Outputs="@(Template->'$(IntermediateOutputPath)%(Filename).cs')">
  <Message Text="Processing templates" />
  <Exec Command="template-tool %(Template.Identity) --output $(IntermediateOutputPath)%(Template.Filename).cs" />
</Target>
```

**Behavior:**
- Runs only if inputs are newer than outputs
- Skips if all outputs are up-to-date
- Enables fast incremental builds

### Input/Output Patterns

```csharp
// Single file input/output
.Inputs("$(MSBuildProjectFile)")
.Outputs("$(IntermediateOutputPath)generated.cs")

// Item-based input/output
.Inputs("@(Compile)")
.Outputs("$(OutputPath)$(AssemblyName).dll")

// Multiple inputs
.Inputs("@(Compile);@(EmbeddedResource)")
.Outputs("$(TargetPath)")

// Transformed outputs
.Inputs("@(ContentSource)")
.Outputs("@(ContentSource->'$(OutputPath)content/%(Filename)%(Extension)')")
```

### Returns

Specify target return values:

```csharp
.Target("GetVersionInfo", target => target
    .Returns("@(VersionInfo)")
    .ItemGroup(null, group =>
    {
        group.Include("VersionInfo", "$(Version)", item => item
            .Meta("Revision", "$(Revision)")
            .Meta("BuildDate", "$([System.DateTime]::UtcNow.ToString('yyyy-MM-dd'))"));
    }))
```

## Target Dependencies

### Linear Dependencies

```csharp
.Target("Step1", target => target
    .Message("Step 1"))

.Target("Step2", target => target
    .DependsOnTargets("Step1")
    .Message("Step 2"))

.Target("Step3", target => target
    .DependsOnTargets("Step2")
    .Message("Step 3"))
```

**Execution:** Step1 → Step2 → Step3

### Parallel Dependencies

```csharp
.Target("PrepareA", target => target
    .Message("Prepare A"))

.Target("PrepareB", target => target
    .Message("Prepare B"))

.Target("Main", target => target
    .DependsOnTargets("PrepareA;PrepareB")
    .Message("Main target"))
```

**Execution:** PrepareA and PrepareB (potentially parallel) → Main

### Diamond Dependencies

```csharp
.Target("Common", target => target
    .Message("Common"))

.Target("BranchA", target => target
    .DependsOnTargets("Common")
    .Message("Branch A"))

.Target("BranchB", target => target
    .DependsOnTargets("Common")
    .Message("Branch B"))

.Target("Merge", target => target
    .DependsOnTargets("BranchA;BranchB")
    .Message("Merge"))
```

**Execution:** Common → BranchA, BranchB → Merge

**Key:** Common runs only once despite multiple dependents.

## Target Patterns

### Pattern: Validation Target

```csharp
.Target("ValidateConfiguration", target => target
    .BeforeTargets("Build")
    
    .Error("'$(TargetFramework)' == ''", "TargetFramework must be specified")
    .Error("'$(OutputPath)' == ''", "OutputPath must be specified")
    
    .Warning("'$(Configuration)' != 'Debug' AND '$(Configuration)' != 'Release'", 
        "Unexpected configuration: $(Configuration)")
    
    .Message("Configuration validated", "Normal"))
```

### Pattern: Setup Target

```csharp
.Target("EnsureDirectories", target => target
    .BeforeTargets("BeforeBuild")
    
    .Task("MakeDir", task =>
    {
        task.Param("Directories", "$(OutputPath);$(IntermediateOutputPath);$(CustomOutputPath)");
    })
    
    .Message("Created required directories", "Normal"))
```

### Pattern: Cleanup Target

```csharp
.Target("CleanCustomOutputs", target => target
    .BeforeTargets("Clean")
    
    .Task("RemoveDir", task =>
    {
        task.Param("Directories", "$(CustomOutputPath)");
    })
    
    .Task("Delete", task =>
    {
        task.Param("Files", "@(CustomGeneratedFile)");
    })
    
    .Message("Cleaned custom outputs", "Normal"))
```

### Pattern: Code Generation Target

```csharp
.Target("GenerateCode", target => target
    .BeforeTargets("CoreCompile")
    .Inputs("@(CodeTemplate)")
    .Outputs("@(CodeTemplate->'$(IntermediateOutputPath)Generated/%(Filename).g.cs')")
    
    .Task("MakeDir", task =>
    {
        task.Param("Directories", "$(IntermediateOutputPath)Generated");
    })
    
    .Task("Exec", task =>
    {
        task.Param("Command", "codegen %(CodeTemplate.Identity) --output $(IntermediateOutputPath)Generated/%(CodeTemplate.Filename).g.cs");
    })
    
    .ItemGroup(null, group =>
    {
        group.Include("Compile", "$(IntermediateOutputPath)Generated/**/*.g.cs");
    }))
```

### Pattern: Test Orchestration

```csharp
.Target("RunUnitTests", target => target
    .DependsOnTargets("Build")
    
    .Task("Exec", task =>
    {
        task.Param("Command", "dotnet test $(OutputPath)$(AssemblyName).dll --no-build");
    }))

.Target("RunIntegrationTests", target => target
    .DependsOnTargets("RunUnitTests")
    .Condition("'$(RunIntegrationTests)' == 'true'")
    
    .Task("Exec", task =>
    {
        task.Param("Command", "dotnet test $(OutputPath)$(AssemblyName).IntegrationTests.dll --no-build");
    }))
```

### Pattern: Multi-Step Asset Processing

```csharp
.Target("DownloadAssets", target => target
    .Outputs("$(IntermediateOutputPath)assets-downloaded.stamp")
    
    .Task("DownloadFile", task =>
    {
        task.Param("SourceUrl", "$(AssetUrl)");
        task.Param("DestinationFolder", "$(IntermediateOutputPath)assets");
    })
    
    .Task("Touch", task =>
    {
        task.Param("Files", "$(IntermediateOutputPath)assets-downloaded.stamp");
        task.Param("AlwaysCreate", "true");
    }))

.Target("ProcessAssets", target => target
    .DependsOnTargets("DownloadAssets")
    .Inputs("$(IntermediateOutputPath)assets-downloaded.stamp")
    .Outputs("$(IntermediateOutputPath)assets-processed.stamp")
    
    .Task("Exec", task =>
    {
        task.Param("Command", "asset-processor $(IntermediateOutputPath)assets --output $(OutputPath)assets");
    })
    
    .Task("Touch", task =>
    {
        task.Param("Files", "$(IntermediateOutputPath)assets-processed.stamp");
        task.Param("AlwaysCreate", "true");
    }))

.Target("CopyAssets", target => target
    .DependsOnTargets("ProcessAssets")
    .AfterTargets("Build")
    
    .Task("Copy", task =>
    {
        task.Param("SourceFiles", "@(ProcessedAsset)");
        task.Param("DestinationFolder", "$(OutputPath)assets");
    }))
```

## Advanced Orchestration

### Conditional Target Execution

```csharp
.Target("OptionalTarget", target => target
    .Condition("'$(RunOptionalTarget)' == 'true'")
    .BeforeTargets("Build")
    .Message("Optional target running"))

.Target("PlatformSpecificTarget", target => target
    .Condition("$([MSBuild]::IsOSPlatform('Windows'))")
    .AfterTargets("Build")
    .Message("Windows-specific processing"))
```

### Target Chaining

```csharp
.Target("Phase1", target => target
    .Message("Phase 1 complete")
    .PropertyGroup(null, group =>
    {
        group.Property("Phase1Complete", "true");
    }))

.Target("Phase2", target => target
    .DependsOnTargets("Phase1")
    .Condition("'$(Phase1Complete)' == 'true'")
    .Message("Phase 2 executing"))

.Target("Phase3", target => target
    .DependsOnTargets("Phase2")
    .Message("Phase 3 finalizing"))
```

### Dynamic Target Selection

```csharp
.Target("SelectTargets", target => target
    .PropertyGroup(null, group =>
    {
        group.Property("TargetsToRun", "Target1;Target2", "'$(Configuration)' == 'Debug'");
        group.Property("TargetsToRun", "Target3;Target4", "'$(Configuration)' == 'Release'");
    }))

.Target("ExecuteSelected", target => target
    .DependsOnTargets("SelectTargets;$(TargetsToRun)")
    .Message("Executed selected targets"))
```

## Debugging Targets

### Log Target Execution

```csharp
.Target("DebugTarget", target => target
    .Message("========== DebugTarget START ==========", "High")
    .Message("Configuration: $(Configuration)", "High")
    .Message("Platform: $(Platform)", "High")
    .Message("TargetFramework: $(TargetFramework)", "High")
    
    // Target logic here
    
    .Message("========== DebugTarget END ==========", "High"))
```

### List Target Dependencies

Run MSBuild with `/targets` flag to see all targets:

```bash
dotnet msbuild -targets
```

Or from code:

```bash
dotnet msbuild -t:MyTarget -v:detailed
```

## Best Practices

### DO: Use Meaningful Names

```csharp
// ✓ Clear
.Target("GenerateVersionInfo", ...)
.Target("ValidateConfiguration", ...)

// ✗ Unclear
.Target("Gen", ...)
.Target("Val", ...)
```

### DO: Add Descriptions with Messages

```csharp
.Target("MyTarget", target => target
    .Message("=== MyTarget: Performing critical operation ===", "High")
    // Operations
    .Message("=== MyTarget: Complete ===", "High"))
```

### DO: Use Inputs/Outputs for Performance

```csharp
// ✓ Incremental
.Inputs("@(Source)")
.Outputs("$(OutputPath)result.dll")

// ✗ Always runs
// No inputs/outputs specified
```

### DON'T: Create Circular Dependencies

```csharp
// ✗ Circular dependency (A → B → A)
.Target("A", target => target.DependsOnTargets("B"))
.Target("B", target => target.DependsOnTargets("A"))
```

### DON'T: Overuse BeforeTargets/AfterTargets

```csharp
// ✓ Explicit
.Target("MyTarget", target => target
    .DependsOnTargets("Dependency")
    .BeforeTargets("Build"))

// ✗ Too many injection points
.Target("MyTarget", target => target
    .BeforeTargets("Target1;Target2;Target3;Target4;Target5"))
```

## Summary

| Concept | Fluent API | XML Output |
|---------|-----------|------------|
| Simple target | `.Target("Name", t => ...)` | `<Target Name="Name">` |
| Before targets | `.BeforeTargets("Build")` | `BeforeTargets="Build"` |
| After targets | `.AfterTargets("Build")` | `AfterTargets="Build"` |
| Dependencies | `.DependsOnTargets("A;B")` | `DependsOnTargets="A;B"` |
| Inputs | `.Inputs("@(Source)")` | `Inputs="@(Source)"` |
| Outputs | `.Outputs("$(Output)")` | `Outputs="$(Output)"` |
| Condition | `.Condition("...")` | `Condition="..."` |

## Next Steps

- [Built-in Tasks](builtin-tasks.md) - Tasks available in targets
- [Task Outputs](task-outputs.md) - Capture task results
- [UsingTask](../advanced/usingtask.md) - Custom task declarations
- [MSBuild Targets (Microsoft Docs)](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-targets)
