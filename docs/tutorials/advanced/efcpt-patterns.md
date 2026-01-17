# Tutorial: Recreating JD.Efcpt.Build Patterns

Master advanced MSBuild techniques by recreating patterns from JD.Efcpt.Build, a production package with 40KB+ of generated targets implementing complex EF Core build pipelines.

## Overview

In this tutorial, you'll learn advanced patterns used in real-world MSBuild packages by recreating key techniques from JD.Efcpt.Build:

- Multi-TFM task assembly selection (net472, net8.0, net9.0, net10.0)
- Complex target orchestration with dependency chains
- UsingTask declarations with runtime detection
- Property-driven feature toggles and overrides
- Build profiling and lifecycle hooks
- Late-evaluated property patterns
- Custom task invocations with outputs

**Time**: ~45 minutes  
**Difficulty**: Advanced  
**Context**: JD.Efcpt.Build is a real package that generates SQL databases from EF Core models during build

## What You'll Learn

By completing this tutorial, you will:

- ✅ Implement multi-TFM task assembly resolution
- ✅ Create complex target dependency chains
- ✅ Use late-evaluated properties in targets files
- ✅ Implement build lifecycle hooks (pre/post)
- ✅ Create diagnostic and profiling targets
- ✅ Handle conditional feature enablement
- ✅ Work with custom tasks and output parameters
- ✅ Implement sophisticated error handling

## Prerequisites

- Completed [Creating a Build Integration Package](../intermediate/build-integration.md) tutorial
- Understanding of MSBuild evaluation vs execution phases
- Familiarity with MSBuild version detection
- Experience with custom MSBuild tasks

## The Scenario

You're building an advanced build automation package that:

1. Supports multiple MSBuild runtimes (.NET Framework, .NET Core/5+)
2. Selects correct task assemblies based on MSBuild version
3. Implements a complex multi-stage build pipeline
4. Provides profiling and diagnostic capabilities
5. Uses late-evaluated properties for smart defaults
6. Handles errors gracefully with clear diagnostics

This mirrors real-world requirements for enterprise build packages!

## Pattern 1: Multi-TFM Task Assembly Selection

### The Problem

Your custom MSBuild tasks need to run on:
- .NET Framework MSBuild (Visual Studio, msbuild.exe)
- .NET Core MSBuild (dotnet build, VS 2019+)
- Different .NET versions (8.0, 9.0, 10.0)

### The Solution

Create a sophisticated assembly selection system.

## Step 1: Project Setup

```bash
mkdir AdvancedBuild.Tasks
cd AdvancedBuild.Tasks
dotnet new classlib -n AdvancedBuild.Tasks
cd AdvancedBuild.Tasks
dotnet add package JD.MSBuild.Fluent
```

## Step 2: Configure Multi-TFM Task Assembly Selection

Create `PackageFactory.cs`:

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace AdvancedBuild.Tasks;

public static class PackageFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("AdvancedBuild.Tasks")
            .Description("Advanced build automation with multi-TFM support")
            .Props(ConfigureProps)
            .Targets(ConfigureTargets)
            .Pack(options => options.BuildTransitive = true)
            .Build();
    }

    private static void ConfigureProps(PropsBuilder props)
    {
        ConfigureDefaultProperties(props);
    }

    private static void ConfigureTargets(TargetsBuilder targets)
    {
        ConfigureTaskAssemblyResolution(targets);
        ConfigureUsingTaskDeclarations(targets);
        ConfigureBuildPipeline(targets);
        ConfigureDiagnostics(targets);
    }

    // ... implementation next
}
```

## Step 3: Default Properties (Props File)

Properties evaluated early, before project properties:

```csharp
private static void ConfigureDefaultProperties(PropsBuilder props)
{
    props.Comment("==============================================");
    props.Comment(" AdvancedBuild.Tasks Configuration");
    props.Comment("==============================================");
    
    // Feature toggles
    props.PropertyGroup(null, group =>
    {
        group.Comment("Enable/disable the build pipeline");
        group.Property("AdvancedBuildEnabled", "true");
        
        group.Comment("Enable profiling and diagnostics");
        group.Property("AdvancedBuildEnableProfiling", "false");
        
        group.Comment("Verbosity level: normal, detailed, diagnostic");
        group.Property("AdvancedBuildLogVerbosity", "normal");
    }, label: "Feature Configuration");
    
    // Paths and outputs
    props.PropertyGroup("'$(AdvancedBuildEnabled)' == 'true'", group =>
    {
        group.Comment("Output paths for generated artifacts");
        group.Property("AdvancedBuildOutputPath", 
            "$(IntermediateOutputPath)AdvancedBuild",
            condition: "'$(AdvancedBuildOutputPath)' == ''");
    }, label: "Output Configuration");
}
```

## Step 4: Task Assembly Resolution (Targets File)

This goes in the targets file because it needs late-evaluated properties:

```csharp
private static void ConfigureTaskAssemblyResolution(TargetsBuilder targets)
{
    // PropertyGroup at targets file level (late evaluation)
    targets.Comment("==============================================");
    targets.Comment(" Multi-TFM Task Assembly Resolution");
    targets.Comment("==============================================");
    targets.Comment("");
    targets.Comment("Select the correct task assembly based on MSBuild runtime and version.");
    targets.Comment("Supports .NET Framework MSBuild (net472) and .NET Core MSBuild (net8.0-net10.0).");
    
    targets.PropertyGroup(null, group =>
    {
        group.Comment("For .NET Core MSBuild, select TFM based on MSBuild version:");
        group.Comment("  MSBuild 18.0+   (VS 2026, .NET 10 SDK) -> net10.0");
        group.Comment("  MSBuild 17.14+  (VS 2022 17.14+)       -> net10.0");
        group.Comment("  MSBuild 17.12+  (VS 2022 17.12+)       -> net9.0");
        group.Comment("  MSBuild 17.8+   (VS 2022 17.8+)        -> net8.0");
        
        // MSBuild 18.0+ or 17.14+
        group.Property("_AdvancedBuildTasksFolder",
            "net10.0",
            condition: "'$(MSBuildRuntimeType)' == 'Core' and " +
                      "$([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '18.0'))");
        
        group.Property("_AdvancedBuildTasksFolder",
            "net10.0",
            condition: "'$(_AdvancedBuildTasksFolder)' == '' and " +
                      "'$(MSBuildRuntimeType)' == 'Core' and " +
                      "$([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '17.14'))");
        
        // MSBuild 17.12+
        group.Property("_AdvancedBuildTasksFolder",
            "net9.0",
            condition: "'$(_AdvancedBuildTasksFolder)' == '' and " +
                      "'$(MSBuildRuntimeType)' == 'Core' and " +
                      "$([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '17.12'))");
        
        // MSBuild 17.8+ (default for .NET Core)
        group.Property("_AdvancedBuildTasksFolder",
            "net8.0",
            condition: "'$(_AdvancedBuildTasksFolder)' == '' and " +
                      "'$(MSBuildRuntimeType)' == 'Core'");
        
        // .NET Framework MSBuild
        group.Property("_AdvancedBuildTasksFolder",
            "net472",
            condition: "'$(_AdvancedBuildTasksFolder)' == ''");
        
        group.Comment("Compute task assembly path");
        
        // Primary path: NuGet package location
        group.Property("_AdvancedBuildTaskAssembly",
            "$(MSBuildThisFileDirectory)..\\tasks\\$(_AdvancedBuildTasksFolder)\\AdvancedBuild.Tasks.dll");
        
        // Fallback: Local development build
        group.Property("_AdvancedBuildTaskAssembly",
            "$(MSBuildThisFileDirectory)..\\..\\AdvancedBuild.Tasks\\bin\\$(Configuration)\\$(_AdvancedBuildTasksFolder)\\AdvancedBuild.Tasks.dll",
            condition: "!Exists('$(_AdvancedBuildTaskAssembly)')");
    }, label: "Task Assembly Resolution");
}
```

**Key Techniques**:
- **PropertyGroup in targets file**: Late evaluation sees final MSBuild properties
- **Cascading conditions**: Check highest version first, fallback to lower
- **`$([MSBuild]::VersionGreaterThanOrEquals(...))` function**: Version comparison
- **Fallback paths**: Support both NuGet package and local development

## Step 5: Diagnostic Target for Assembly Selection

Add a diagnostic target to troubleshoot assembly selection:

```csharp
private static void ConfigureDiagnostics(TargetsBuilder targets)
{
    targets.Target("AdvancedBuild_DiagnosticInfo", target =>
    {
        target.Label("Display diagnostic information about task assembly selection");
        target.BeforeTargets("AdvancedBuild_Initialize");
        target.Condition("'$(AdvancedBuildEnabled)' == 'true' and " +
                        "'$(AdvancedBuildLogVerbosity)' == 'diagnostic'");
        
        target.Message("AdvancedBuild Task Assembly Diagnostics:", importance: "High");
        target.Message("  MSBuildRuntimeType: $(MSBuildRuntimeType)", importance: "High");
        target.Message("  MSBuildVersion: $(MSBuildVersion)", importance: "High");
        target.Message("  Selected TFM: $(_AdvancedBuildTasksFolder)", importance: "High");
        target.Message("  Task Assembly Path: $(_AdvancedBuildTaskAssembly)", importance: "High");
        target.Message("  Assembly Exists: $([System.IO.File]::Exists('$(_AdvancedBuildTaskAssembly)'))", 
            importance: "High");
        
        // Error if assembly not found
        target.Error(
            "Task assembly not found at: $(_AdvancedBuildTaskAssembly). " +
            "Ensure AdvancedBuild.Tasks package is correctly installed.",
            code: "ADVBUILD001",
            condition: "!Exists('$(_AdvancedBuildTaskAssembly)')");
    });
}
```

**Key Techniques**:
- **Conditional diagnostics**: Only runs when verbosity is diagnostic
- **Property function in message**: `$([System.IO.File]::Exists(...))` for inline checks
- **Error codes**: Standardized error codes for documentation

## Step 6: UsingTask Declarations

Declare custom tasks using the resolved assembly:

```csharp
private static void ConfigureUsingTaskDeclarations(TargetsBuilder targets)
{
    targets.Comment("==============================================");
    targets.Comment(" Custom Task Declarations");
    targets.Comment("==============================================");
    
    // Declare each custom task
    targets.UsingTask("ProcessInputFiles", 
        assemblyFile: "$(_AdvancedBuildTaskAssembly)");
    
    targets.UsingTask("GenerateOutputs", 
        assemblyFile: "$(_AdvancedBuildTaskAssembly)");
    
    targets.UsingTask("ComputeFingerprint", 
        assemblyFile: "$(_AdvancedBuildTaskAssembly)");
    
    targets.UsingTask("InitializeProfiling", 
        assemblyFile: "$(_AdvancedBuildTaskAssembly)");
    
    targets.UsingTask("FinalizeProfiling", 
        assemblyFile: "$(_AdvancedBuildTaskAssembly)");
}
```

**Key Techniques**:
- **Property-based assembly reference**: Uses computed `$(_AdvancedBuildTaskAssembly)`
- **Multiple task declarations**: One UsingTask per custom task

## Pattern 2: Complex Target Orchestration

## Step 7: Multi-Stage Build Pipeline

Create a sophisticated pipeline with dependencies:

```csharp
private static void ConfigureBuildPipeline(TargetsBuilder targets)
{
    ConfigureInitializationStage(targets);
    ConfigurePreProcessingStage(targets);
    ConfigureProcessingStage(targets);
    ConfigurePostProcessingStage(targets);
    ConfigureCleanupStage(targets);
}

private static void ConfigureInitializationStage(TargetsBuilder targets)
{
    targets.Comment("==============================================");
    targets.Comment(" Stage 1: Initialization");
    targets.Comment("==============================================");
    
    targets.Target("AdvancedBuild_Initialize", target =>
    {
        target.Label("Initialize build pipeline and profiling");
        target.BeforeTargets("AdvancedBuild_Validate");
        target.Condition("'$(AdvancedBuildEnabled)' == 'true'");
        
        target.Message("Initializing AdvancedBuild pipeline...", importance: "High");
        
        // Initialize profiling if enabled
        target.Task("InitializeProfiling", task =>
        {
            task.Param("Enabled", "$(AdvancedBuildEnableProfiling)");
            task.Param("ProjectPath", "$(MSBuildProjectFullPath)");
            task.Param("ProjectName", "$(MSBuildProjectName)");
            task.OutputProperty("ProfilingSessionId", "_AdvancedBuildProfilingSession");
        },
        condition: "'$(AdvancedBuildEnableProfiling)' == 'true'");
        
        target.Message("Profiling session initialized: $(_AdvancedBuildProfilingSession)", 
            importance: "Normal",
            condition: "'$(AdvancedBuildEnableProfiling)' == 'true'");
    });
}

private static void ConfigurePreProcessingStage(TargetsBuilder targets)
{
    targets.Comment("==============================================");
    targets.Comment(" Stage 2: Validation and Pre-Processing");
    targets.Comment("==============================================");
    
    targets.Target("AdvancedBuild_Validate", target =>
    {
        target.Label("Validate inputs and environment");
        target.DependsOnTargets("AdvancedBuild_Initialize");
        target.BeforeTargets("AdvancedBuild_Process");
        target.Condition("'$(AdvancedBuildEnabled)' == 'true'");
        
        target.Message("Validating build environment...", importance: "Normal");
        
        // Validate required properties
        target.Error(
            "AdvancedBuildOutputPath must be set",
            code: "ADVBUILD002",
            condition: "'$(AdvancedBuildOutputPath)' == ''");
        
        // Create output directory
        target.Task("MakeDir", task =>
        {
            task.Param("Directories", "$(AdvancedBuildOutputPath)");
        });
        
        target.Message("Validation complete", importance: "Normal");
    });
    
    targets.Target("AdvancedBuild_ComputeFingerprint", target =>
    {
        target.Label("Compute input fingerprint for incremental builds");
        target.DependsOnTargets("AdvancedBuild_Validate");
        target.BeforeTargets("AdvancedBuild_Process");
        target.Condition("'$(AdvancedBuildEnabled)' == 'true'");
        
        // Compute fingerprint from input files
        target.Task("ComputeFingerprint", task =>
        {
            task.Param("InputFiles", "@(AdvancedBuildInput)");
            task.Param("OutputPath", "$(AdvancedBuildOutputPath)");
            task.OutputProperty("Fingerprint", "_AdvancedBuildFingerprint");
            task.OutputProperty("FingerprintChanged", "_AdvancedBuildFingerprintChanged");
        });
        
        target.Message("Input fingerprint: $(_AdvancedBuildFingerprint)", 
            importance: "Low");
        target.Message("Fingerprint changed: $(_AdvancedBuildFingerprintChanged)", 
            importance: "Normal");
    });
}

private static void ConfigureProcessingStage(TargetsBuilder targets)
{
    targets.Comment("==============================================");
    targets.Comment(" Stage 3: Core Processing");
    targets.Comment("==============================================");
    
    targets.Target("AdvancedBuild_Process", target =>
    {
        target.Label("Process inputs and generate outputs");
        target.BeforeTargets("CoreCompile");
        target.DependsOnTargets("AdvancedBuild_ComputeFingerprint");
        target.Condition("'$(AdvancedBuildEnabled)' == 'true' and " +
                        "'$(_AdvancedBuildFingerprintChanged)' == 'true'");
        
        target.Message("Processing inputs...", importance: "High");
        
        // Process input files
        target.Task("ProcessInputFiles", task =>
        {
            task.Param("InputFiles", "@(AdvancedBuildInput)");
            task.Param("OutputPath", "$(AdvancedBuildOutputPath)");
            task.Param("Configuration", "$(Configuration)");
            task.OutputItem("GeneratedFiles", "_AdvancedBuildGeneratedFiles");
            task.OutputProperty("ProcessedCount", "_AdvancedBuildProcessedCount");
        });
        
        target.Message("Processed $(_AdvancedBuildProcessedCount) file(s)", 
            importance: "High");
    });
    
    targets.Target("AdvancedBuild_GenerateOutputs", target =>
    {
        target.Label("Generate final output files");
        target.DependsOnTargets("AdvancedBuild_Process");
        target.BeforeTargets("CoreCompile");
        target.Condition("'$(AdvancedBuildEnabled)' == 'true'");
        
        target.Message("Generating outputs...", importance: "Normal");
        
        // Generate outputs from processed files
        target.Task("GenerateOutputs", task =>
        {
            task.Param("ProcessedFiles", "@(_AdvancedBuildGeneratedFiles)");
            task.Param("OutputPath", "$(AdvancedBuildOutputPath)");
            task.OutputItem("FinalOutputs", "_AdvancedBuildFinalOutputs");
        });
        
        // Include generated files in compilation
        target.ItemGroup(null, group =>
        {
            group.Include("Compile", "@(_AdvancedBuildFinalOutputs)", item =>
            {
                item.Meta("AutoGenerated", "true");
                item.Meta("DesignTime", "true");
                item.Meta("DependentUpon", "AdvancedBuild.Tasks");
            });
        });
        
        target.Message("Added @(_AdvancedBuildFinalOutputs->Count()) files to compilation", 
            importance: "Normal");
    });
}

private static void ConfigurePostProcessingStage(TargetsBuilder targets)
{
    targets.Comment("==============================================");
    targets.Comment(" Stage 4: Post-Processing and Finalization");
    targets.Comment("==============================================");
    
    targets.Target("AdvancedBuild_Finalize", target =>
    {
        target.Label("Finalize build and profiling");
        target.AfterTargets("CoreCompile");
        target.DependsOnTargets("AdvancedBuild_GenerateOutputs");
        target.Condition("'$(AdvancedBuildEnabled)' == 'true'");
        
        target.Message("Finalizing AdvancedBuild pipeline...", importance: "High");
        
        // Finalize profiling
        target.Task("FinalizeProfiling", task =>
        {
            task.Param("SessionId", "$(_AdvancedBuildProfilingSession)");
            task.Param("OutputPath", "$(AdvancedBuildOutputPath)");
        },
        condition: "'$(AdvancedBuildEnableProfiling)' == 'true'");
        
        target.Message("Build pipeline completed successfully", importance: "High");
    });
}

private static void ConfigureCleanupStage(TargetsBuilder targets)
{
    targets.Comment("==============================================");
    targets.Comment(" Stage 5: Cleanup");
    targets.Comment("==============================================");
    
    targets.Target("AdvancedBuild_Clean", target =>
    {
        target.Label("Clean generated files and artifacts");
        target.AfterTargets("Clean");
        target.Condition("'$(AdvancedBuildEnabled)' == 'true'");
        
        target.Message("Cleaning AdvancedBuild artifacts...", importance: "High");
        
        target.Task("RemoveDir", task =>
        {
            task.Param("Directories", "$(AdvancedBuildOutputPath)");
        });
        
        target.Message("Cleanup complete", importance: "Normal");
    });
}
```

**Key Techniques**:
- **Multi-stage pipeline**: Initialize → Validate → Process → Finalize → Cleanup
- **Complex dependencies**: `DependsOnTargets` creates execution graph
- **Task outputs**: `OutputProperty` and `OutputItem` pass data between targets
- **Conditional execution**: `$(_AdvancedBuildFingerprintChanged)` skips unchanged builds
- **Fingerprinting**: Custom task computes hash of inputs for incremental builds
- **Item metadata**: `AutoGenerated`, `DesignTime`, `DependentUpon` for IDE integration

## Complete Code Structure

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace AdvancedBuild.Tasks;

public static class PackageFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("AdvancedBuild.Tasks")
            .Description("Advanced build automation with multi-TFM support")
            .Props(ConfigureProps)
            .Targets(ConfigureTargets)
            .Pack(options => options.BuildTransitive = true)
            .Build();
    }

    private static void ConfigureProps(PropsBuilder props)
    {
        ConfigureDefaultProperties(props);
    }

    private static void ConfigureTargets(TargetsBuilder targets)
    {
        ConfigureTaskAssemblyResolution(targets);
        ConfigureUsingTaskDeclarations(targets);
        ConfigureBuildPipeline(targets);
        ConfigureDiagnostics(targets);
    }

    // ... all previous implementation methods ...
}
```

## Pattern 3: Late-Evaluated Properties

### Why Use Targets Files for Properties?

Properties in **.props** files are evaluated BEFORE the project file, so they see SDK defaults, not user overrides.

Properties in **.targets** files are evaluated AFTER the project file, so they see final values.

### Example: Derive Property from User Setting

In the **targets** file (late evaluation):

```csharp
targets.PropertyGroup(null, group =>
{
    group.Comment("Derive setting from user's Nullable property");
    group.Property("AdvancedBuildUseNullable", "true",
        condition: "'$(AdvancedBuildUseNullable)' == '' and " +
                  "('$(Nullable)' == 'enable' or '$(Nullable)' == 'Enable')");
    
    group.Property("AdvancedBuildUseNullable", "false",
        condition: "'$(AdvancedBuildUseNullable)' == '' and '$(Nullable)' != ''");
}, label: "Derived Settings");
```

This pattern:
1. Checks if user explicitly set `AdvancedBuildUseNullable`
2. If not, derives it from `Nullable` property
3. Only works in targets file because it needs user's `Nullable` value

## Generated XML Structure

Your package generates clean, organized MSBuild XML:

### build/AdvancedBuild.Tasks.props
```xml
<Project>
  <!--==============================================-->
  <!-- AdvancedBuild.Tasks Configuration-->
  <!--==============================================-->
  <PropertyGroup Label="Feature Configuration">
    <!--Enable/disable the build pipeline-->
    <AdvancedBuildEnabled>true</AdvancedBuildEnabled>
    <!--Enable profiling and diagnostics-->
    <AdvancedBuildEnableProfiling>false</AdvancedBuildEnableProfiling>
    <!--Verbosity level: normal, detailed, diagnostic-->
    <AdvancedBuildLogVerbosity>normal</AdvancedBuildLogVerbosity>
  </PropertyGroup>
  <!-- ... -->
</Project>
```

### buildTransitive/AdvancedBuild.Tasks.targets
```xml
<Project>
  <!--==============================================-->
  <!-- Multi-TFM Task Assembly Resolution-->
  <!--==============================================-->
  <PropertyGroup Label="Task Assembly Resolution">
    <!--For .NET Core MSBuild, select TFM based on MSBuild version:-->
    <!--  MSBuild 18.0+   (VS 2026, .NET 10 SDK) -> net10.0-->
    <!--  MSBuild 17.14+  (VS 2022 17.14+)       -> net10.0-->
    <!--  MSBuild 17.12+  (VS 2022 17.12+)       -> net9.0-->
    <!--  MSBuild 17.8+   (VS 2022 17.8+)        -> net8.0-->
    <_AdvancedBuildTasksFolder Condition="'$(MSBuildRuntimeType)' == 'Core' and $([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '18.0'))">net10.0</_AdvancedBuildTasksFolder>
    <!-- ... -->
  </PropertyGroup>
  
  <!--==============================================-->
  <!-- Custom Task Declarations-->
  <!--==============================================-->
  <UsingTask TaskName="ProcessInputFiles" AssemblyFile="$(_AdvancedBuildTaskAssembly)" />
  <!-- ... -->
  
  <!--==============================================-->
  <!-- Stage 1: Initialization-->
  <!--==============================================-->
  <Target Name="AdvancedBuild_Initialize" Label="Initialize build pipeline and profiling" 
          BeforeTargets="AdvancedBuild_Validate" Condition="'$(AdvancedBuildEnabled)' == 'true'">
    <!-- ... -->
  </Target>
  <!-- ... -->
</Project>
```

## Testing the Package

### Test Multi-TFM Resolution

Create test script `Test-TaskResolution.ps1`:

```powershell
# Test .NET Core MSBuild (various versions)
dotnet build -v:diag -p:AdvancedBuildLogVerbosity=diagnostic | Select-String "Selected TFM"

# Test .NET Framework MSBuild
msbuild.exe -v:diag -p:AdvancedBuildLogVerbosity=diagnostic | Select-String "Selected TFM"
```

### Test Build Pipeline

Create test project with inputs:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    
    <!-- Enable profiling -->
    <AdvancedBuildEnableProfiling>true</AdvancedBuildEnableProfiling>
    <AdvancedBuildLogVerbosity>detailed</AdvancedBuildLogVerbosity>
  </PropertyGroup>
  
  <ItemGroup>
    <AdvancedBuildInput Include="input1.txt" />
    <AdvancedBuildInput Include="input2.txt" />
  </ItemGroup>
</Project>
```

Build and observe:
```bash
dotnet build -v:normal
```

Expected output:
```
Initializing AdvancedBuild pipeline...
Profiling session initialized: {guid}
Validating build environment...
Validation complete
Input fingerprint: abc123...
Fingerprint changed: true
Processing inputs...
Processed 2 file(s)
Generating outputs...
Added 2 files to compilation
Finalizing AdvancedBuild pipeline...
Build pipeline completed successfully
```

## What You Learned

Congratulations! You've mastered advanced MSBuild patterns:

✅ **Multi-TFM task assembly selection** with version detection  
✅ **Complex target orchestration** with dependency chains  
✅ **Late-evaluated properties** in targets files  
✅ **Build profiling and lifecycle hooks**  
✅ **Diagnostic and troubleshooting targets**  
✅ **Custom task invocations** with outputs  
✅ **Incremental build patterns** with fingerprinting  
✅ **Enterprise-grade error handling**  

## Key Concepts

- **Multi-TFM Support**: Select correct assembly based on MSBuild runtime and version
- **Late Evaluation**: Targets files see final property values after user overrides
- **Target Dependencies**: Create execution graphs with DependsOnTargets
- **Task Outputs**: Pass data between targets with OutputProperty and OutputItem
- **Fingerprinting**: Hash inputs to enable smart incremental builds
- **Profiling Hooks**: Initialize/Finalize pattern for diagnostics
- **Diagnostic Targets**: Conditional targets for troubleshooting

## Real-World Applications

These patterns are used in production packages:

- **JD.Efcpt.Build**: Generates SQL databases from EF Core (40KB+ targets)
- **JD.MSBuild.Containers**: Docker integration with multi-stage builds
- **Microsoft.NET.Sdk**: The .NET SDK itself uses these patterns
- **NuGet package authors**: Create robust, professional build packages

## Next Steps

- **[Recreating Docker Container Patterns](containers-patterns.md)** - Learn publish integration and extensibility hooks
- **[Best Practices](../../user-guides/best-practices/index.md)** - Professional patterns for production packages

## Challenge: Extend the Package

Try these exercises:

1. **Add version detection for MSBuild 19.0+** for future .NET versions
2. **Implement caching**: Cache fingerprints across builds
3. **Add telemetry**: Send anonymous usage data to diagnostics endpoint
4. **Support custom task factories**: Use RoslynCodeTaskFactory for inline tasks
5. **Add incremental cleaning**: Only clean generated files, not intermediate artifacts

## Common Pitfalls

### MSBuild Version Detection

```csharp
// ✅ Correct - cascading conditions
group.Property("_TasksFolder", "net10.0", 
    condition: "'$(MSBuildRuntimeType)' == 'Core' and " +
              "$([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '18.0'))");

// ❌ Wrong - overlapping conditions
group.Property("_TasksFolder", "net10.0", 
    condition: "$([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '17.0'))");
```

### Late Evaluation

```csharp
// ✅ Correct - targets file for late evaluation
targets.PropertyGroup(null, group =>
{
    group.Property("Derived", "$(UserSetting)");
});

// ❌ Wrong - props file sees default, not user value
props.PropertyGroup(null, group =>
{
    group.Property("Derived", "$(UserSetting)");  // Sees SDK default!
});
```

### Task Output Parameters

```csharp
// ✅ Correct - capture output
target.Task("MyTask", task =>
{
    task.OutputProperty("Result", "_Result");
});
target.Message("Result: $(_Result)");

// ❌ Wrong - output not captured
target.Task("MyTask", task =>
{
    task.Param("Output", "Result");  // This is an input parameter!
});
```

## Troubleshooting

**Wrong task assembly loaded?**
- Check diagnostic output with `-p:AdvancedBuildLogVerbosity=diagnostic`
- Verify MSBuildVersion with `dotnet --info` or `msbuild.exe -version`
- Ensure task assembly exists at computed path

**Target execution order wrong?**
- Use `-v:diag` to see full target graph
- Check DependsOnTargets, BeforeTargets, AfterTargets
- Verify conditions don't skip critical targets

**Properties not evaluated correctly?**
- Remember props files evaluate early, targets files late
- Check if property needs user's value (use targets file)
- Use diagnostic messages to trace property values

## Related Documentation

- [Working with Targets](../../user-guides/targets-tasks/targets.md)
- [Custom Tasks](../../user-guides/targets-tasks/custom-tasks.md)
- [Best Practices - Multi-Target Framework Support](../../user-guides/best-practices/index.md#multi-target-framework-support)
- [MSBuild Concepts](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-concepts)
