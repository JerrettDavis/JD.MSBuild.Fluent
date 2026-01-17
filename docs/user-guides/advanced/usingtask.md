# UsingTask Declarations

UsingTask declarations enable custom MSBuild tasks to be loaded and invoked. This guide covers task declaration, assembly referencing, and patterns for custom task integration.

## UsingTask Basics

### What is UsingTask?

`UsingTask` declares a custom MSBuild task by:

- Specifying the task name
- Pointing to the assembly containing the task
- Optionally specifying a task factory

### Simple UsingTask

Declare a task from an assembly:

```csharp
.Targets(t => t
    .UsingTask("MyCompany.CustomTask", "$(MSBuildThisFileDirectory)../../tools/MyCompany.Tasks.dll")
    
    .Target("RunCustomTask", target => target
        .Task("MyCompany.CustomTask", task =>
        {
            task.Param("InputFile", "$(InputFile)");
            task.Param("OutputFile", "$(OutputFile)");
        })))
```

**Generated XML:**

```xml
<UsingTask TaskName="MyCompany.CustomTask" 
           AssemblyFile="$(MSBuildThisFileDirectory)../../tools/MyCompany.Tasks.dll" />

<Target Name="RunCustomTask">
  <MyCompany.CustomTask InputFile="$(InputFile)" OutputFile="$(OutputFile)" />
</Target>
```

## AssemblyFile vs AssemblyName

### AssemblyFile

Load task from a specific file path:

```csharp
.UsingTask("MyTask", assemblyFile: "$(MSBuildThisFileDirectory)../../tools/Tasks.dll")
```

**Use when:**
- Task DLL is packaged with your MSBuild package
- Controlling exact assembly version
- Assembly not in GAC

**Path patterns:**

```csharp
// Relative to targets file
"$(MSBuildThisFileDirectory)../../tools/MyTasks.dll"

// Relative to project
"$(MSBuildProjectDirectory)/tools/MyTasks.dll"

// Absolute path
"C:/Tools/MyTasks.dll"  // ✗ Not portable

// Property-based
"$(MyTasksPath)/MyTasks.dll"
```

### AssemblyName

Load task from GAC or resolved assembly:

```csharp
.UsingTask("Microsoft.Build.Tasks.Git.GetRepository", 
    assemblyName: "Microsoft.Build.Tasks.Git, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")
```

**Use when:**
- Task in Global Assembly Cache (GAC)
- Task from well-known SDK
- Assembly resolved by MSBuild

## Conditional UsingTask

### Declare Only When Needed

```csharp
.Targets(t => t
    .UsingTask("CustomTask", 
        assemblyFile: "$(MSBuildThisFileDirectory)../../tools/CustomTask.dll",
        condition: "Exists('$(MSBuildThisFileDirectory)../../tools/CustomTask.dll')"))
```

**Generated XML:**

```xml
<UsingTask TaskName="CustomTask" 
           AssemblyFile="$(MSBuildThisFileDirectory)../../tools/CustomTask.dll"
           Condition="Exists('$(MSBuildThisFileDirectory)../../tools/CustomTask.dll')" />
```

### Framework-Specific Tasks

```csharp
.Targets(t => t
    .UsingTask("Net6Task", 
        "$(MSBuildThisFileDirectory)../../tools/net6.0/Tasks.dll",
        condition: "'$(TargetFramework)' == 'net6.0'")
    
    .UsingTask("Net8Task", 
        "$(MSBuildThisFileDirectory)../../tools/net8.0/Tasks.dll",
        condition: "'$(TargetFramework)' == 'net8.0'"))
```

## Task Factories

### RoslynCodeTaskFactory

Define inline C# tasks:

```csharp
.Targets(t => t
    .UsingTask("InlineTask", 
        taskFactory: "RoslynCodeTaskFactory",
        assemblyName: "Microsoft.Build.Tasks.Core"))
```

**Full example with task body (requires lower-level IR manipulation):**

While JD.MSBuild.Fluent doesn't have high-level fluent API for inline task bodies, you can define them using the IR directly:

```csharp
var usingTask = new MsBuildUsingTask
{
    TaskName = "MyInlineTask",
    TaskFactory = "RoslynCodeTaskFactory",
    AssemblyFile = "$(MSBuildToolsPath)\\Microsoft.Build.Tasks.Core.dll"
};

// Note: Task body (ParameterGroup, Task) requires direct IR manipulation
// This is an advanced scenario - prefer external task assemblies for complex logic
```

## Package Integration Patterns

### Pattern: Include Task Assembly in Package

**Project structure:**

```
MyCompany.Build/
├── tools/
│   └── MyCompany.Tasks.dll
├── build/
│   └── MyCompany.Build.targets
```

**Define UsingTask:**

```csharp
.Targets(t => t
    .UsingTask("MyCompany.Tasks.ProcessAssets", 
        "$(MSBuildThisFileDirectory)../../tools/MyCompany.Tasks.dll")
    
    .Target("ProcessAssets", target => target
        .Task("MyCompany.Tasks.ProcessAssets", task =>
        {
            task.Param("InputFiles", "@(Asset)");
            task.Param("OutputPath", "$(OutputPath)/processed");
        })))
```

**Package in .csproj:**

```xml
<ItemGroup>
  <None Include="tools/MyCompany.Tasks.dll" Pack="true" PackagePath="tools" />
</ItemGroup>
```

### Pattern: Framework-Specific Task Assemblies

```csharp
.Targets(t => t
    // .NET 6.0 task
    .UsingTask("MyCompany.Tasks.ProcessAssets",
        "$(MSBuildThisFileDirectory)../../tools/net6.0/MyCompany.Tasks.dll",
        condition: "$(TargetFramework.StartsWith('net6'))")
    
    // .NET 8.0 task
    .UsingTask("MyCompany.Tasks.ProcessAssets",
        "$(MSBuildThisFileDirectory)../../tools/net8.0/MyCompany.Tasks.dll",
        condition: "$(TargetFramework.StartsWith('net8'))")
    
    .Target("ProcessAssets", target => target
        .Task("MyCompany.Tasks.ProcessAssets", task =>
        {
            task.Param("InputFiles", "@(Asset)");
        })))
```

### Pattern: Optional Task

```csharp
.Targets(t => t
    // Declare task if assembly exists
    .UsingTask("OptionalTask", 
        "$(OptionalTaskPath)/OptionalTask.dll",
        condition: "Exists('$(OptionalTaskPath)/OptionalTask.dll')")
    
    .Target("RunOptionalTask", target => target
        .Condition("Exists('$(OptionalTaskPath)/OptionalTask.dll')")
        .Task("OptionalTask", task =>
        {
            task.Param("Input", "$(Input)");
        })))
```

### Pattern: Versioned Task Assembly

```csharp
.Props(p => p
    .Property("MyTasksVersion", "2.0.0", "'$(MyTasksVersion)' == ''")
    .Property("MyTasksPath", "$(MSBuildThisFileDirectory)../../tools/v$(MyTasksVersion)"))

.Targets(t => t
    .UsingTask("MyTask", "$(MyTasksPath)/MyTasks.dll")
    
    .Target("UseTask", target => target
        .Message("Using MyTasks v$(MyTasksVersion)", "Normal")
        .Task("MyTask", task =>
        {
            task.Param("Input", "data");
        })))
```

## Common Task Scenarios

### Scenario: Code Generation Task

```csharp
.Targets(t => t
    .UsingTask("MyCompany.CodeGenerator", 
        "$(MSBuildThisFileDirectory)../../tools/CodeGenerator.dll")
    
    .Target("GenerateCode", target => target
        .BeforeTargets("CoreCompile")
        .Inputs("@(CodeTemplate)")
        .Outputs("@(CodeTemplate->'$(IntermediateOutputPath)Generated/%(Filename).g.cs')")
        
        .Task("MakeDir", task =>
        {
            task.Param("Directories", "$(IntermediateOutputPath)Generated");
        })
        
        .Task("MyCompany.CodeGenerator", task =>
        {
            task.Param("Templates", "@(CodeTemplate)");
            task.Param("OutputDirectory", "$(IntermediateOutputPath)Generated");
            task.Param("Namespace", "$(RootNamespace).Generated");
            task.Output("GeneratedFiles", "GeneratedCode");
        })
        
        .ItemGroup(null, group =>
        {
            group.Include("Compile", "@(GeneratedCode)");
        })))
```

### Scenario: Asset Processing Task

```csharp
.Targets(t => t
    .UsingTask("MyCompany.AssetProcessor", 
        "$(MSBuildThisFileDirectory)../../tools/AssetProcessor.dll")
    
    .Target("ProcessAssets", target => target
        .AfterTargets("Build")
        .Inputs("@(UnprocessedAsset)")
        .Outputs("@(UnprocessedAsset->'$(OutputPath)assets/%(Filename)%(Extension)')")
        
        .Task("MyCompany.AssetProcessor", task =>
        {
            task.Param("InputAssets", "@(UnprocessedAsset)");
            task.Param("OutputPath", "$(OutputPath)assets");
            task.Param("Quality", "$(AssetQuality)");
            task.Param("Optimize", "$(OptimizeAssets)");
            task.Output("ProcessedAssets", "ProcessedAssetList");
        })
        
        .Message("Processed @(ProcessedAssetList->Count()) assets", "High")))
```

### Scenario: Validation Task

```csharp
.Targets(t => t
    .UsingTask("MyCompany.ConfigValidator", 
        "$(MSBuildThisFileDirectory)../../tools/Validator.dll")
    
    .Target("ValidateConfiguration", target => target
        .BeforeTargets("Build")
        
        .Task("MyCompany.ConfigValidator", task =>
        {
            task.Param("ConfigFile", "$(MSBuildProjectDirectory)/config.json");
            task.Param("Schema", "$(MSBuildThisFileDirectory)../../schemas/config-schema.json");
            task.Param("TreatWarningsAsErrors", "$(TreatWarningsAsErrors)");
            task.Output("IsValid", "ConfigIsValid");
        })
        
        .Error("'$(ConfigIsValid)' == 'false'", "Configuration validation failed")))
```

## Debugging UsingTask

### Verify Assembly Path

```csharp
.Target("DebugTaskAssembly", target => target
    .Message("Task assembly path: $(MSBuildThisFileDirectory)../../tools/MyTask.dll", "High")
    .Message("Assembly exists: $([System.IO.File]::Exists('$(MSBuildThisFileDirectory)../../tools/MyTask.dll'))", "High"))
```

Run: `dotnet build -t:DebugTaskAssembly`

### Log Task Invocation

```csharp
.Target("RunTaskWithLogging", target => target
    .Message("=== Running Custom Task ===", "High")
    .Message("Input: $(Input)", "Normal")
    
    .Task("MyCustomTask", task =>
    {
        task.Param("Input", "$(Input)");
        task.Output("Result", "TaskResult");
    })
    
    .Message("Output: $(TaskResult)", "High")
    .Message("=== Task Complete ===", "High"))
```

## Best Practices

### DO: Use Conditional Declarations

```csharp
// ✓ Only declare if assembly exists
.UsingTask("MyTask", "$(TaskPath)/MyTask.dll",
    condition: "Exists('$(TaskPath)/MyTask.dll')")
```

### DO: Version Task Assemblies

```csharp
// ✓ Include version in path
"$(MSBuildThisFileDirectory)../../tools/v2.0/MyTask.dll"

// Or use property
"$(MSBuildThisFileDirectory)../../tools/v$(MyTaskVersion)/MyTask.dll"
```

### DO: Package Task Dependencies

If your task has dependencies, include them:

```xml
<ItemGroup>
  <None Include="tools/MyTask.dll" Pack="true" PackagePath="tools" />
  <None Include="tools/Newtonsoft.Json.dll" Pack="true" PackagePath="tools" />
  <None Include="tools/SomeOtherDep.dll" Pack="true" PackagePath="tools" />
</ItemGroup>
```

### DON'T: Hardcode Absolute Paths

```csharp
// ✗ Not portable
.UsingTask("MyTask", "C:\\MyTasks\\MyTask.dll")

// ✓ Relative to targets file
.UsingTask("MyTask", "$(MSBuildThisFileDirectory)../../tools/MyTask.dll")
```

### DON'T: Forget to Handle Missing Assemblies

```csharp
// ✗ Will error if assembly missing
.UsingTask("MyTask", "$(TaskPath)/MyTask.dll")

// ✓ Conditional declaration and usage
.UsingTask("MyTask", "$(TaskPath)/MyTask.dll",
    condition: "Exists('$(TaskPath)/MyTask.dll')")

.Target("UseTask", target => target
    .Condition("Exists('$(TaskPath)/MyTask.dll')")
    .Task("MyTask", ...))
```

## Summary

| Concept | Syntax | Use For |
|---------|--------|---------|
| Assembly file | `.UsingTask("Task", "path/to/Assembly.dll")` | Packaged task assemblies |
| Assembly name | `.UsingTask("Task", assemblyName: "...")` | GAC or SDK tasks |
| Conditional | `.UsingTask(..., condition: "...")` | Optional tasks |
| Task factory | `.UsingTask(..., taskFactory: "...")` | Inline tasks |

## Next Steps

- [Built-in Tasks](builtin-tasks.md) - Standard MSBuild tasks
- [Task Outputs](task-outputs.md) - Capturing task results
- [Target Orchestration](orchestration.md) - Using tasks in targets
- [Custom MSBuild Tasks (Microsoft Docs)](https://learn.microsoft.com/en-us/visualstudio/msbuild/tutorial-custom-task-code-generation)
