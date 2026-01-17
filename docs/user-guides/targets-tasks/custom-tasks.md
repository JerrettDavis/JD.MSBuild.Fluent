# Custom Tasks

Learn to declare and invoke custom MSBuild tasks using JD.MSBuild.Fluent's type-safe API. This guide covers UsingTask declarations, task parameter bindings, output mappings, and multi-target framework scenarios.

## Overview

Custom MSBuild tasks extend the build system with domain-specific logic. To use a custom task:

1. Declare it with `UsingTask`
2. Invoke it with `Task()` within a target
3. Configure parameters and outputs

## UsingTask Declarations

### Basic UsingTask

Declare a task from an assembly:

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

Package.Define("Contoso.Build")
    .Targets(targets =>
    {
        targets.UsingTask(
            taskName: "Contoso.Build.Tasks.MyCustomTask",
            assemblyFile: "$(MSBuildThisFileDirectory)../tasks/net472/Contoso.Build.Tasks.dll");
    })
    .Build();
```

### UsingTask with AssemblyName

Reference a task from a strong-named assembly:

```csharp
targets.UsingTask(
    taskName: "Contoso.Build.Tasks.MyCustomTask",
    assemblyFile: null,
    assemblyName: "Contoso.Build.Tasks, Version=1.0.0.0, Culture=neutral, PublicKeyToken=abc123");
```

### Conditional UsingTask

Declare different assemblies based on runtime:

```csharp
// .NET Framework
targets.UsingTask(
    taskName: "Contoso.Build.Tasks.MyTask",
    assemblyFile: "$(MSBuildThisFileDirectory)../tasks/net472/Contoso.Build.Tasks.dll",
    condition: "'$(MSBuildRuntimeType)' != 'Core'");

// .NET Core/5+
targets.UsingTask(
    taskName: "Contoso.Build.Tasks.MyTask",
    assemblyFile: "$(MSBuildThisFileDirectory)../tasks/netstandard2.0/Contoso.Build.Tasks.dll",
    condition: "'$(MSBuildRuntimeType)' == 'Core'");
```

### Multi-Target Framework Pattern

Support multiple TFMs with Choose:

```csharp
Package.Define("Contoso.Build")
    .Props(props =>
    {
        // Define task assembly path based on MSBuild runtime
        props.Choose(choose =>
        {
            choose.When("'$(MSBuildRuntimeType)' == 'Core'", whenProps =>
            {
                whenProps.Property("ContosoTasksAssembly", 
                    "$(MSBuildThisFileDirectory)../tasks/netstandard2.0/Contoso.Build.Tasks.dll");
            });
            choose.Otherwise(otherwiseProps =>
            {
                otherwiseProps.Property("ContosoTasksAssembly", 
                    "$(MSBuildThisFileDirectory)../tasks/net472/Contoso.Build.Tasks.dll");
            });
        });
    })
    .Targets(targets =>
    {
        // Use the computed property
        targets.UsingTask(
            taskName: "Contoso.Build.Tasks.MyTask",
            assemblyFile: "$(ContosoTasksAssembly)");
    })
    .Build();
```

### TaskFactory

Use task factories for inline or dynamically generated tasks:

```csharp
targets.UsingTask(
    taskName: "ContosoInlineTask",
    assemblyFile: null,
    taskFactory: "RoslynCodeTaskFactory",
    assemblyName: "$(MSBuildToolsPath)\\Microsoft.Build.Tasks.Core.dll");
```

## Strongly-Typed Task References

### MsBuildTaskReference

Create reusable task references:

```csharp
using JD.MSBuild.Fluent.Typed;

// Define a task reference
public static class ContosoTasks
{
    public static MsBuildTaskReference MyCustomTask { get; } = new MsBuildTaskReference
    {
        Name = "Contoso.Build.Tasks.MyCustomTask",
        AssemblyFile = "$(ContosoTasksAssembly)"
    };
}

// Declare it
targets.UsingTask(ContosoTasks.MyCustomTask);

// Invoke it
target.Task(ContosoTasks.MyCustomTask, task =>
{
    task.Param("Input", "value");
});
```

### FromType<T>

Generate task references from CLR types:

```csharp
using JD.MSBuild.Fluent.Typed;

// Define a CLR task class
namespace Contoso.Build.Tasks
{
    public class MyCustomTask : Microsoft.Build.Utilities.Task
    {
        public string Input { get; set; }
        public string Output { get; set; }

        public override bool Execute()
        {
            // Task logic
            return true;
        }
    }
}

// Generate reference with full name
var taskRef = MsBuildTaskReference.FromType<Contoso.Build.Tasks.MyCustomTask>(
    nameStyle: MsBuildTaskNameStyle.FullName,
    assemblyFile: "$(ContosoTasksAssembly)");

// Declare
targets.UsingTask(taskRef);

// Invoke
target.Task(taskRef, task =>
{
    task.Param("Input", "$(InputValue)");
});
```

### UsingTask<T> Overload

Declare and invoke with CLR types directly:

```csharp
using Contoso.Build.Tasks;

// Declare
targets.UsingTask<MyCustomTask>(
    assemblyFile: "$(ContosoTasksAssembly)",
    nameStyle: MsBuildTaskNameStyle.FullName);

// Invoke
target.Task<MyCustomTask>(task =>
{
    task.Param("Input", "$(InputValue)");
}, nameStyle: MsBuildTaskNameStyle.FullName);
```

### MsBuildTaskNameStyle

Control how task names are derived:

```csharp
public enum MsBuildTaskNameStyle
{
    FullName,      // "Contoso.Build.Tasks.MyCustomTask"
    TypeNameOnly   // "MyCustomTask"
}

// Full name
targets.UsingTask<MyCustomTask>(
    assemblyFile: "$(ContosoTasksAssembly)",
    nameStyle: MsBuildTaskNameStyle.FullName);

// Type name only
targets.UsingTask<MyCustomTask>(
    assemblyFile: "$(ContosoTasksAssembly)",
    nameStyle: MsBuildTaskNameStyle.TypeNameOnly);
```

## Task Invocation

### Basic Invocation

Invoke a declared task:

```csharp
targets.Target("Contoso_RunTask", target =>
{
    target.Task("Contoso.Build.Tasks.MyCustomTask", task =>
    {
        task.Param("Input", "$(InputValue)");
        task.Param("Verbose", "true");
    });
});
```

### Task Parameters

Set task parameters using `Param()`:

```csharp
target.Task("MyCustomTask", task =>
{
    // String parameters
    task.Param("StringParam", "value");
    
    // Boolean parameters
    task.Param("BoolParam", "true");
    
    // Property references
    task.Param("PropertyParam", "$(MyProperty)");
    
    // Item references
    task.Param("ItemParam", "@(MyItem)");
    
    // Computed values
    task.Param("ComputedParam", "$(BaseDir)/output");
    
    // Metadata references
    task.Param("MetadataParam", "%(Compile.Filename)");
});
```

### Strongly-Typed Parameters

Define typed parameter names:

```csharp
using JD.MSBuild.Fluent.Typed;

public readonly struct InputFileParameter : IMsBuildTaskParameterName
{
    public string Name => "InputFile";
}

public readonly struct OutputFileParameter : IMsBuildTaskParameterName
{
    public string Name => "OutputFile";
}

// Usage
task.Param<InputFileParameter>("$(InputPath)");
task.Param<OutputFileParameter>("$(OutputPath)");

// Or with instances
task.Param(new InputFileParameter(), "$(InputPath)");
```

### Task Conditions

Execute tasks conditionally:

```csharp
target.Task("MyCustomTask", task =>
{
    task.Param("Input", "$(InputValue)");
}, condition: "'$(RunTask)' == 'true'");
```

## Task Outputs

Tasks can output values to properties or items.

### Output to Property

Capture a task output parameter in a property:

```csharp
target.Task("MyCustomTask", task =>
{
    task.Param("Input", "$(InputFile)");
    
    // Capture "ResultCount" output to property "FileCount"
    task.OutputProperty("ResultCount", "FileCount");
});

// Use the property in subsequent tasks
target.Message("Processed $(FileCount) files");
```

### Output to Item

Capture a task output parameter in an item:

```csharp
target.Task("MyCustomTask", task =>
{
    task.Param("SearchPath", "$(SourceDir)");
    
    // Capture "DiscoveredFiles" output to item "FoundFiles"
    task.OutputItem("DiscoveredFiles", "FoundFiles");
});

// Use the item in subsequent tasks
target.Task("Copy", copyTask =>
{
    copyTask.Param("SourceFiles", "@(FoundFiles)");
    copyTask.Param("DestinationFolder", "$(OutputPath)");
});
```

### Conditional Outputs

Capture outputs based on conditions:

```csharp
target.Task("MyCustomTask", task =>
{
    task.Param("Input", "$(InputFile)");
    
    task.OutputProperty("ResultCount", "FileCount", 
        condition: "'$(CaptureCount)' == 'true'");
    
    task.OutputItem("GeneratedFiles", "GeneratedFiles",
        condition: "'$(CaptureFiles)' == 'true'");
});
```

### Strongly-Typed Outputs

Use typed names for outputs:

```csharp
using JD.MSBuild.Fluent.Typed;

public readonly struct ResultCountParameter : IMsBuildTaskParameterName
{
    public string Name => "ResultCount";
}

public readonly struct FileCountProperty : IMsBuildPropertyName
{
    public string Name => "FileCount";
}

public readonly struct GeneratedFilesParameter : IMsBuildTaskParameterName
{
    public string Name => "GeneratedFiles";
}

public readonly struct GeneratedFilesItem : IMsBuildItemTypeName
{
    public string Name => "GeneratedFiles";
}

// Output to property
task.OutputProperty<ResultCountParameter, FileCountProperty>();

// Output to item
task.OutputItem<GeneratedFilesParameter, GeneratedFilesItem>();
```

## Complete Custom Task Example

### Custom Task Implementation

```csharp
// Task implementation (in Contoso.Build.Tasks assembly)
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Contoso.Build.Tasks
{
    public class ProcessFiles : Task
    {
        [Required]
        public string InputDirectory { get; set; }

        [Required]
        public string OutputDirectory { get; set; }

        public string Pattern { get; set; } = "*.txt";

        public bool Verbose { get; set; }

        [Output]
        public int ProcessedCount { get; set; }

        [Output]
        public ITaskItem[] ProcessedFiles { get; set; }

        public override bool Execute()
        {
            var files = Directory.GetFiles(InputDirectory, Pattern);
            var processed = new List<TaskItem>();

            foreach (var file in files)
            {
                var outputFile = Path.Combine(OutputDirectory, Path.GetFileName(file));
                File.Copy(file, outputFile, overwrite: true);
                processed.Add(new TaskItem(outputFile));

                if (Verbose)
                {
                    Log.LogMessage(MessageImportance.Normal, $"Processed: {file}");
                }
            }

            ProcessedCount = processed.Count;
            ProcessedFiles = processed.ToArray();

            Log.LogMessage(MessageImportance.High, $"Processed {ProcessedCount} files");
            return true;
        }
    }
}
```

### Package Definition

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.Typed;

namespace Contoso.Build
{
    // Strongly-typed names
    public readonly struct ProcessFilesTask : IMsBuildTaskName
    {
        public string Name => "Contoso.Build.Tasks.ProcessFiles";
    }

    public readonly struct InputDirectoryParam : IMsBuildTaskParameterName
    {
        public string Name => "InputDirectory";
    }

    public readonly struct OutputDirectoryParam : IMsBuildTaskParameterName
    {
        public string Name => "OutputDirectory";
    }

    public readonly struct ProcessedCountParam : IMsBuildTaskParameterName
    {
        public string Name => "ProcessedCount";
    }

    public readonly struct ProcessedFilesParam : IMsBuildTaskParameterName
    {
        public string Name => "ProcessedFiles";
    }

    public readonly struct ProcessedCountProperty : IMsBuildPropertyName
    {
        public string Name => "ContosoProcessedCount";
    }

    public readonly struct ProcessedFilesItem : IMsBuildItemTypeName
    {
        public string Name => "ContosoProcessedFiles";
    }

    public static class PackageFactory
    {
        public static PackageDefinition Create()
        {
            return Package.Define("Contoso.Build")
                .Props(ConfigureProps)
                .Targets(ConfigureTargets)
                .Pack(o => o.BuildTransitive = true)
                .Build();
        }

        private static void ConfigureProps(PropsBuilder props)
        {
            // Default properties
            props.Property("ContosoEnabled", "true");
            props.Property("ContosoInputDir", "$(MSBuildProjectDirectory)/input");
            props.Property("ContosoOutputDir", "$(MSBuildProjectDirectory)/output");
            props.Property("ContosoPattern", "*.txt");
            props.Property("ContosoVerbose", "false");

            // Task assembly path
            props.Choose(choose =>
            {
                choose.When("'$(MSBuildRuntimeType)' == 'Core'", w =>
                {
                    w.Property("ContosoTasksAssembly",
                        "$(MSBuildThisFileDirectory)../tasks/netstandard2.0/Contoso.Build.Tasks.dll");
                });
                choose.Otherwise(o =>
                {
                    o.Property("ContosoTasksAssembly",
                        "$(MSBuildThisFileDirectory)../tasks/net472/Contoso.Build.Tasks.dll");
                });
            });
        }

        private static void ConfigureTargets(TargetsBuilder targets)
        {
            // Declare the task
            targets.UsingTask(
                taskName: "Contoso.Build.Tasks.ProcessFiles",
                assemblyFile: "$(ContosoTasksAssembly)");

            // Target that uses the task
            targets.Target("Contoso_ProcessFiles", target =>
            {
                target.BeforeTargets("Build");
                target.Condition("'$(ContosoEnabled)' == 'true'");
                target.Label("Process files with Contoso task");

                // Create output directory
                target.Task("MakeDir", task =>
                {
                    task.Param("Directories", "$(ContosoOutputDir)");
                });

                // Invoke custom task
                target.Task<ProcessFilesTask>(task =>
                {
                    task.Param<InputDirectoryParam>("$(ContosoInputDir)");
                    task.Param<OutputDirectoryParam>("$(ContosoOutputDir)");
                    task.Param("Pattern", "$(ContosoPattern)");
                    task.Param("Verbose", "$(ContosoVerbose)");

                    // Capture outputs
                    task.OutputProperty<ProcessedCountParam, ProcessedCountProperty>();
                    task.OutputItem<ProcessedFilesParam, ProcessedFilesItem>();
                });

                // Report results
                target.Message("Processed $(ContosoProcessedCount) files", importance: "High");

                // Optionally add processed files to project
                target.ItemGroup("'$(ContosoAddToProject)' == 'true'", group =>
                {
                    group.Include<ProcessedFilesItem>("@(ContosoProcessedFiles)");
                });
            });
        }
    }
}
```

### Generated MSBuild XML

**build/Contoso.Build.targets:**

```xml
<Project>
  <!-- Generated by JD.MSBuild.Fluent -->
  
  <UsingTask TaskName="Contoso.Build.Tasks.ProcessFiles" 
             AssemblyFile="$(ContosoTasksAssembly)" />

  <Target Name="Contoso_ProcessFiles" 
          BeforeTargets="Build" 
          Condition="'$(ContosoEnabled)' == 'true'"
          Label="Process files with Contoso task">
    
    <MakeDir Directories="$(ContosoOutputDir)" />
    
    <Contoso.Build.Tasks.ProcessFiles 
        InputDirectory="$(ContosoInputDir)"
        OutputDirectory="$(ContosoOutputDir)"
        Pattern="$(ContosoPattern)"
        Verbose="$(ContosoVerbose)">
      <Output TaskParameter="ProcessedCount" PropertyName="ContosoProcessedCount" />
      <Output TaskParameter="ProcessedFiles" ItemName="ContosoProcessedFiles" />
    </Contoso.Build.Tasks.ProcessFiles>
    
    <Message Text="Processed $(ContosoProcessedCount) files" Importance="High" />
    
    <ItemGroup Condition="'$(ContosoAddToProject)' == 'true'">
      <ProcessedFilesItem Include="@(ContosoProcessedFiles)" />
    </ItemGroup>
  </Target>
</Project>
```

## Advanced Scenarios

### Dynamic Task Assembly Resolution

Compute task assembly paths based on multiple factors:

```csharp
props.PropertyGroup(null, group =>
{
    // Base path
    group.Property("ContosoTasksBase", 
        "$(MSBuildThisFileDirectory)../tasks");
    
    // Target framework
    group.Property("ContosoTasksTFM", "netstandard2.0");
    group.Property("ContosoTasksTFM", "net472",
        condition: "'$(MSBuildRuntimeType)' != 'Core'");
    
    // Full path
    group.Property("ContosoTasksAssembly",
        "$(ContosoTasksBase)/$(ContosoTasksTFM)/Contoso.Build.Tasks.dll");
});
```

### Task Batching

Process items in batches:

```csharp
targets.Target("Contoso_BatchProcess", target =>
{
    target.Inputs("%(FileSet.Identity)");
    target.Outputs("$(OutputPath)%(FileSet.Filename).processed");

    // This executes once per unique FileSet item
    target.Task("ProcessFiles", task =>
    {
        task.Param("InputDirectory", "%(FileSet.RootDir)%(FileSet.Directory)");
        task.Param("OutputDirectory", "$(OutputPath)");
    });
});
```

### Inline Tasks with TaskFactory

```csharp
targets.UsingTask(
    taskName: "ContosoInlineTask",
    assemblyFile: null,
    taskFactory: "RoslynCodeTaskFactory",
    assemblyName: "$(MSBuildToolsPath)\\Microsoft.Build.Tasks.Core.dll");

// Note: Inline task bodies require additional ParameterGroup and TaskBody elements
// which are not yet fully supported by the fluent API. Use XML directly for inline tasks.
```

### External Task Libraries

Reference tasks from external NuGet packages:

```csharp
props.ItemGroup(null, group =>
{
    group.Include("PackageReference", "ExternalTaskPackage", item =>
    {
        item.Meta("Version", "1.0.0");
        item.Meta("PrivateAssets", "all");
    });
});

targets.UsingTask(
    taskName: "ExternalTask",
    assemblyFile: "$(NuGetPackageRoot)/externaltaskpackage/1.0.0/tasks/net472/ExternalTaskPackage.dll");
```

## Best Practices

### Strongly-Typed Everything

Define typed names for tasks, parameters, and outputs:

```csharp
// ✅ Good - compile-time safety
task.Param<InputFileParameter>("$(InputFile)");
task.OutputProperty<ResultParameter, ResultProperty>();

// ❌ Avoid - typos not caught
task.Param("InputFlie", "$(InputFile)");  // Typo!
```

### Reusable Task References

Extract task references for reuse:

```csharp
// ✅ Good
public static class ContosoTasks
{
    public static readonly MsBuildTaskReference ProcessFiles = new()
    {
        Name = "Contoso.Build.Tasks.ProcessFiles",
        AssemblyFile = "$(ContosoTasksAssembly)"
    };
}

targets.UsingTask(ContosoTasks.ProcessFiles);
target.Task(ContosoTasks.ProcessFiles, task => { });

// ❌ Avoid - duplicated strings
targets.UsingTask("Contoso.Build.Tasks.ProcessFiles", "$(ContosoTasksAssembly)");
target.Task("Contoso.Build.Tasks.ProcessFiles", task => { });
```

### Multi-TFM Support

Always support both .NET Framework and .NET Core:

```csharp
// ✅ Good - supports both runtimes
props.Choose(choose =>
{
    choose.When("'$(MSBuildRuntimeType)' == 'Core'", w => 
        w.Property("TaskAssembly", "$(TasksBase)/netstandard2.0/Tasks.dll"));
    choose.Otherwise(o => 
        o.Property("TaskAssembly", "$(TasksBase)/net472/Tasks.dll"));
});

// ❌ Avoid - only supports one runtime
props.Property("TaskAssembly", "$(TasksBase)/net472/Tasks.dll");
```

### Capture Important Outputs

Always capture task outputs for diagnostics and subsequent tasks:

```csharp
// ✅ Good
task.OutputProperty("ProcessedCount", "ProcessedCount");
task.OutputItem("ProcessedFiles", "ProcessedFiles");
target.Message("Processed $(ProcessedCount) files");

// ❌ Avoid - no output capture
task.Param("Input", "$(Input)");
// Can't use results
```

### Error Handling

Validate prerequisites before invoking tasks:

```csharp
// ✅ Good
target.Error("InputDirectory property is required",
    code: "CONTOSO001",
    condition: "'$(InputDirectory)' == ''");

target.Task("ProcessFiles", task => { /* ... */ });

// ❌ Avoid - task fails with unclear error
target.Task("ProcessFiles", task => { /* ... */ });
```

## Troubleshooting

### Task Not Found

**Check UsingTask declaration**: Ensure taskName and assemblyFile are correct.

**Check assembly path**: Verify the assembly exists at the specified path.

```csharp
// Add diagnostic message
target.Message("Task assembly: $(ContosoTasksAssembly)");
target.Warning("Task assembly not found",
    condition: "!Exists('$(ContosoTasksAssembly)')");
```

### Wrong Assembly Loaded

**Check MSBuildRuntimeType**: Ensure correct TFM is selected.

```csharp
// Add diagnostic output
target.Message("MSBuildRuntimeType: $(MSBuildRuntimeType)");
target.Message("Task assembly: $(ContosoTasksAssembly)");
```

### Task Parameters Not Set

**Check parameter names**: Ensure they match the task's properties exactly (case-sensitive).

**Check property values**: Ensure properties exist and have values.

```csharp
// Add diagnostic output
target.Message("InputDirectory: $(InputDirectory)");
```

### Outputs Not Captured

**Check Output element**: Ensure TaskParameter matches the task's Output property name.

**Check property/item names**: Ensure they're not being overwritten later.

## Next Steps

- [Targets](targets.md) - Target orchestration and execution
- [Best Practices](../best-practices/index.md) - Patterns for robust packages
- [Migration Guide](../migration/from-xml.md) - Convert XML to fluent API

## Related Topics

- [Fluent Builders](../core-concepts/builders.md) - Complete builder API reference
- [Architecture](../core-concepts/architecture.md) - Understanding the framework design
