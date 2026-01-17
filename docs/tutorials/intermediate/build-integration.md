# Tutorial: Creating a Build Integration Package

Learn to integrate with MSBuild's execution phase by creating a package that adds custom build targets, task invocations, and lifecycle hooks.

## Overview

In this tutorial, you'll create `CodeGeneration.Build`, an MSBuild package that:

- Generates C# code from template files during build
- Runs before compilation to include generated files
- Uses incremental builds to skip unchanged templates
- Handles errors gracefully with clear messages
- Provides extensibility hooks for customization

**Time**: ~30 minutes  
**Difficulty**: Intermediate  
**Output**: A build integration package with targets and tasks

## What You'll Learn

By completing this tutorial, you will:

- ✅ Define build targets with `Targets()`
- ✅ Orchestrate target execution with BeforeTargets/AfterTargets/DependsOnTargets
- ✅ Invoke built-in MSBuild tasks (WriteLinesToFile, Copy, etc.)
- ✅ Implement incremental builds with Inputs/Outputs
- ✅ Add error handling and validation
- ✅ Create property groups and item groups inside targets
- ✅ Use message logging with appropriate importance levels
- ✅ Test targets in real build scenarios

## Prerequisites

- Completed [Building a Simple Properties Package](../beginner/simple-props.md) tutorial
- Understanding of MSBuild target execution phases
- Familiarity with MSBuild tasks

## The Scenario

You're building a code generation package for your team. The package should:

1. Find template files (`*.tmpl`) in the project
2. Generate C# files from templates before compilation
3. Only regenerate when templates change (incremental build)
4. Validate that output directory exists
5. Report clear errors if something goes wrong
6. Allow projects to disable or customize the generation

This is a realistic pattern used in many build tools!

## Step 1: Create the Project

```bash
mkdir CodeGeneration.Build
cd CodeGeneration.Build
dotnet new classlib -n CodeGeneration.Build
cd CodeGeneration.Build
dotnet add package JD.MSBuild.Fluent
rm Class1.cs
```

## Step 2: Define Package Structure

Create `PackageFactory.cs` with both props and targets:

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace CodeGeneration.Build;

public static class PackageFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("CodeGeneration.Build")
            .Description("Code generation from templates during build")
            .Props(ConfigureProps)
            .Targets(ConfigureTargets)
            .Build();
    }

    private static void ConfigureProps(PropsBuilder props)
    {
        // Props for configuration
    }

    private static void ConfigureTargets(TargetsBuilder targets)
    {
        // Targets for code generation
    }
}
```

## Step 3: Configure Properties

Add properties to control code generation:

```csharp
private static void ConfigureProps(PropsBuilder props)
{
    props.Comment("Code Generation Settings");
    
    // Core configuration
    props.PropertyGroup(null, group =>
    {
        group.Comment("Enable/disable code generation");
        group.Property("CodeGenEnabled", "true");
        
        group.Comment("Template file pattern");
        group.Property("CodeGenTemplatePattern", "**/*.tmpl");
        
        group.Comment("Output directory for generated files");
        group.Property("CodeGenOutputPath", 
            "$(IntermediateOutputPath)Generated",
            condition: "'$(CodeGenOutputPath)' == ''");
        
        group.Comment("Generated file extension");
        group.Property("CodeGenOutputExtension", ".g.cs");
    }, label: "Code Generation Configuration");

    // Item definition for template files
    props.Comment("Define TemplateFile item type");
    props.ItemGroup(null, group =>
    {
        group.Include("TemplateFile", "$(CodeGenTemplatePattern)", item =>
        {
            item.Meta("Visible", "false");
        },
        condition: "'$(CodeGenEnabled)' == 'true'");
    });
}
```

**Explanation**:
- Properties control code generation behavior
- Override point pattern: `condition: "'$(CodeGenOutputPath)' == ''"` allows user customization
- `TemplateFile` item automatically includes template files when enabled
- `Visible=false` hides them from Visual Studio's Solution Explorer

## Step 4: Add Validation Target

Create a target that validates prerequisites:

```csharp
private static void ConfigureTargets(TargetsBuilder targets)
{
    targets.Target("CodeGen_ValidateEnvironment", target =>
    {
        target.Label("Validate code generation prerequisites");
        target.BeforeTargets("CodeGen_GenerateFiles");
        target.Condition("'$(CodeGenEnabled)' == 'true'");
        
        // Check that we have templates
        target.Warning(
            "No template files found matching pattern: $(CodeGenTemplatePattern)",
            code: "CODEGEN001",
            condition: "'@(TemplateFile)' == ''"
        );
        
        // Log what we found
        target.Message(
            "Found @(TemplateFile->Count()) template file(s) to process",
            importance: "High"
        );
        
        target.Message(
            "Output directory: $(CodeGenOutputPath)",
            importance: "Normal"
        );
    });
}
```

**Explanation**:
- `BeforeTargets("CodeGen_GenerateFiles")` - Runs before code generation
- `target.Warning()` - Non-fatal warning if no templates found
- `code:` - Standardized error code for documentation
- `@(TemplateFile->Count())` - MSBuild transform to count items
- Different importance levels for different message types

## Step 5: Add Directory Creation Target

Ensure output directory exists:

```csharp
private static void ConfigureTargets(TargetsBuilder targets)
{
    // ... previous validation target ...

    targets.Target("CodeGen_PrepareOutputDirectory", target =>
    {
        target.Label("Create output directory for generated files");
        target.DependsOnTargets("CodeGen_ValidateEnvironment");
        target.Condition("'$(CodeGenEnabled)' == 'true'");
        
        // Create the output directory
        target.Task("MakeDir", task =>
        {
            task.Param("Directories", "$(CodeGenOutputPath)");
        });
        
        target.Message(
            "Created output directory: $(CodeGenOutputPath)",
            importance: "Low"
        );
    });
}
```

**Explanation**:
- `DependsOnTargets` - Ensures validation runs first
- `MakeDir` task - Built-in MSBuild task to create directories
- `importance: "Low"` - Only visible at detailed verbosity
- Idempotent - MakeDir doesn't fail if directory exists

## Step 6: Add Code Generation Target

The core target that generates files:

```csharp
private static void ConfigureTargets(TargetsBuilder targets)
{
    // ... previous targets ...

    targets.Target("CodeGen_GenerateFiles", target =>
    {
        target.Label("Generate C# files from templates");
        target.BeforeTargets("CoreCompile");
        target.DependsOnTargets("CodeGen_PrepareOutputDirectory");
        target.Condition("'$(CodeGenEnabled)' == 'true' AND '@(TemplateFile)' != ''");
        
        // Incremental build support
        target.Inputs("@(TemplateFile)");
        target.Outputs("$(CodeGenOutputPath)%(TemplateFile.Filename)$(CodeGenOutputExtension)");
        
        target.Message(
            "Generating code from template: %(TemplateFile.Identity)",
            importance: "High"
        );
        
        // Read template content
        target.PropertyGroup(null, group =>
        {
            group.Comment("Compute output filename");
            group.Property("_CodeGenOutputFile", 
                "$(CodeGenOutputPath)%(TemplateFile.Filename)$(CodeGenOutputExtension)");
        });
        
        // For this tutorial, we'll do simple template processing
        // In a real scenario, you'd invoke a custom task here
        target.Task("WriteLinesToFile", task =>
        {
            task.Param("File", "$(_CodeGenOutputFile)");
            task.Param("Lines", 
                "// Auto-generated from %(TemplateFile.Identity)^" +
                "namespace Generated^" +
                "{^" +
                "    public class %(TemplateFile.Filename)^" +
                "    {^" +
                "        public static string SourceTemplate => \"%(TemplateFile.Identity)\";^" +
                "        public static string GeneratedAt => \"$([System.DateTime]::Now.ToString())\";^" +
                "    }^" +
                "}");
            task.Param("Overwrite", "true");
            task.Param("Encoding", "UTF-8");
        });
        
        target.Message(
            "Generated: $(_CodeGenOutputFile)",
            importance: "Normal"
        );
    });
}
```

**Explanation**:
- `BeforeTargets("CoreCompile")` - Must run before C# compilation
- `Inputs`/`Outputs` - Enables incremental builds (skips if outputs are newer)
- `%(TemplateFile.Filename)` - Batching notation processes each template separately
- `PropertyGroup` inside target - Dynamic property computation during execution
- `WriteLinesToFile` - Generates the C# file
- `^` in Lines - Escape for newline in MSBuild
- `$([System.DateTime]::Now.ToString())` - MSBuild property function

## Step 7: Add Generated Files to Compilation

Include generated files in compilation:

```csharp
private static void ConfigureTargets(TargetsBuilder targets)
{
    // ... previous targets ...

    targets.Target("CodeGen_IncludeGeneratedFiles", target =>
    {
        target.Label("Add generated files to compilation");
        target.DependsOnTargets("CodeGen_GenerateFiles");
        target.BeforeTargets("CoreCompile");
        target.Condition("'$(CodeGenEnabled)' == 'true'");
        
        // Add generated files to Compile items
        target.ItemGroup(null, group =>
        {
            group.Include("Compile", "$(CodeGenOutputPath)**/*$(CodeGenOutputExtension)", item =>
            {
                item.Meta("AutoGen", "true");
                item.Meta("DependentUpon", "CodeGeneration.Build");
            });
        });
        
        target.Message(
            "Added generated files to compilation",
            importance: "Normal"
        );
    });
}
```

**Explanation**:
- Runs after generation but before compilation
- Adds generated files to `Compile` item
- Metadata helps IDEs understand these are generated files

## Step 8: Add Clean Target

Clean up generated files:

```csharp
private static void ConfigureTargets(TargetsBuilder targets)
{
    // ... previous targets ...

    targets.Target("CodeGen_Clean", target =>
    {
        target.Label("Clean generated files");
        target.AfterTargets("Clean");
        target.Condition("'$(CodeGenEnabled)' == 'true'");
        
        target.Message(
            "Cleaning generated files from: $(CodeGenOutputPath)",
            importance: "High"
        );
        
        // Delete generated files
        target.Task("RemoveDir", task =>
        {
            task.Param("Directories", "$(CodeGenOutputPath)");
        });
    });
}
```

**Explanation**:
- `AfterTargets("Clean")` - Runs after standard Clean target
- `RemoveDir` - Deletes the entire output directory

## Complete Code

Here's the full `PackageFactory.cs`:

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace CodeGeneration.Build;

public static class PackageFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("CodeGeneration.Build")
            .Description("Code generation from templates during build")
            .Props(ConfigureProps)
            .Targets(ConfigureTargets)
            .Build();
    }

    private static void ConfigureProps(PropsBuilder props)
    {
        props.Comment("Code Generation Settings");
        
        props.PropertyGroup(null, group =>
        {
            group.Comment("Enable/disable code generation");
            group.Property("CodeGenEnabled", "true");
            
            group.Comment("Template file pattern");
            group.Property("CodeGenTemplatePattern", "**/*.tmpl");
            
            group.Comment("Output directory for generated files");
            group.Property("CodeGenOutputPath", 
                "$(IntermediateOutputPath)Generated",
                condition: "'$(CodeGenOutputPath)' == ''");
            
            group.Comment("Generated file extension");
            group.Property("CodeGenOutputExtension", ".g.cs");
        }, label: "Code Generation Configuration");

        props.Comment("Define TemplateFile item type");
        props.ItemGroup(null, group =>
        {
            group.Include("TemplateFile", "$(CodeGenTemplatePattern)", item =>
            {
                item.Meta("Visible", "false");
            },
            condition: "'$(CodeGenEnabled)' == 'true'");
        });
    }

    private static void ConfigureTargets(TargetsBuilder targets)
    {
        // Validation
        targets.Target("CodeGen_ValidateEnvironment", target =>
        {
            target.Label("Validate code generation prerequisites");
            target.BeforeTargets("CodeGen_GenerateFiles");
            target.Condition("'$(CodeGenEnabled)' == 'true'");
            
            target.Warning(
                "No template files found matching pattern: $(CodeGenTemplatePattern)",
                code: "CODEGEN001",
                condition: "'@(TemplateFile)' == ''"
            );
            
            target.Message(
                "Found @(TemplateFile->Count()) template file(s) to process",
                importance: "High"
            );
            
            target.Message(
                "Output directory: $(CodeGenOutputPath)",
                importance: "Normal"
            );
        });

        // Preparation
        targets.Target("CodeGen_PrepareOutputDirectory", target =>
        {
            target.Label("Create output directory for generated files");
            target.DependsOnTargets("CodeGen_ValidateEnvironment");
            target.Condition("'$(CodeGenEnabled)' == 'true'");
            
            target.Task("MakeDir", task =>
            {
                task.Param("Directories", "$(CodeGenOutputPath)");
            });
            
            target.Message(
                "Created output directory: $(CodeGenOutputPath)",
                importance: "Low"
            );
        });

        // Code Generation
        targets.Target("CodeGen_GenerateFiles", target =>
        {
            target.Label("Generate C# files from templates");
            target.BeforeTargets("CoreCompile");
            target.DependsOnTargets("CodeGen_PrepareOutputDirectory");
            target.Condition("'$(CodeGenEnabled)' == 'true' AND '@(TemplateFile)' != ''");
            
            target.Inputs("@(TemplateFile)");
            target.Outputs("$(CodeGenOutputPath)%(TemplateFile.Filename)$(CodeGenOutputExtension)");
            
            target.Message(
                "Generating code from template: %(TemplateFile.Identity)",
                importance: "High"
            );
            
            target.PropertyGroup(null, group =>
            {
                group.Comment("Compute output filename");
                group.Property("_CodeGenOutputFile", 
                    "$(CodeGenOutputPath)%(TemplateFile.Filename)$(CodeGenOutputExtension)");
            });
            
            target.Task("WriteLinesToFile", task =>
            {
                task.Param("File", "$(_CodeGenOutputFile)");
                task.Param("Lines", 
                    "// Auto-generated from %(TemplateFile.Identity)^" +
                    "namespace Generated^" +
                    "{^" +
                    "    public class %(TemplateFile.Filename)^" +
                    "    {^" +
                    "        public static string SourceTemplate => \"%(TemplateFile.Identity)\";^" +
                    "        public static string GeneratedAt => \"$([System.DateTime]::Now.ToString())\";^" +
                    "    }^" +
                    "}");
                task.Param("Overwrite", "true");
                task.Param("Encoding", "UTF-8");
            });
            
            target.Message(
                "Generated: $(_CodeGenOutputFile)",
                importance: "Normal"
            );
        });

        // Include in compilation
        targets.Target("CodeGen_IncludeGeneratedFiles", target =>
        {
            target.Label("Add generated files to compilation");
            target.DependsOnTargets("CodeGen_GenerateFiles");
            target.BeforeTargets("CoreCompile");
            target.Condition("'$(CodeGenEnabled)' == 'true'");
            
            target.ItemGroup(null, group =>
            {
                group.Include("Compile", "$(CodeGenOutputPath)**/*$(CodeGenOutputExtension)", item =>
                {
                    item.Meta("AutoGen", "true");
                    item.Meta("DependentUpon", "CodeGeneration.Build");
                });
            });
            
            target.Message(
                "Added generated files to compilation",
                importance: "Normal"
            );
        });

        // Cleanup
        targets.Target("CodeGen_Clean", target =>
        {
            target.Label("Clean generated files");
            target.AfterTargets("Clean");
            target.Condition("'$(CodeGenEnabled)' == 'true'");
            
            target.Message(
                "Cleaning generated files from: $(CodeGenOutputPath)",
                importance: "High"
            );
            
            target.Task("RemoveDir", task =>
            {
                task.Param("Directories", "$(CodeGenOutputPath)");
            });
        });
    }
}
```

## Step 9: Generate and Test

Generate the MSBuild files:

```bash
dotnet build
jdmsbuild generate \
    --assembly bin/Debug/net8.0/CodeGeneration.Build.dll \
    --type CodeGeneration.Build.PackageFactory \
    --method Create \
    --output artifacts/msbuild
```

## Generated Targets XML

Your `build/CodeGeneration.Build.targets` will contain:

```xml
<Project>
  <Target Name="CodeGen_ValidateEnvironment" 
          Label="Validate code generation prerequisites" 
          BeforeTargets="CodeGen_GenerateFiles" 
          Condition="'$(CodeGenEnabled)' == 'true'">
    <Warning Text="No template files found matching pattern: $(CodeGenTemplatePattern)" 
             Code="CODEGEN001" 
             Condition="'@(TemplateFile)' == ''" />
    <Message Text="Found @(TemplateFile-&gt;Count()) template file(s) to process" 
             Importance="High" />
    <Message Text="Output directory: $(CodeGenOutputPath)" 
             Importance="Normal" />
  </Target>
  
  <!-- Additional targets... -->
</Project>
```

## Step 10: Create a Test Project

Test with a real project:

```bash
cd ..
mkdir TestApp
cd TestApp
dotnet new console -n TestApp
cd TestApp
```

Create a template file `Hello.tmpl`:

```
Template for Hello.cs
```

Edit `TestApp.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!-- Import your package -->
  <Import Project="..\..\CodeGeneration.Build\artifacts\msbuild\build\CodeGeneration.Build.props" />
  <Import Project="..\..\CodeGeneration.Build\artifacts\msbuild\build\CodeGeneration.Build.targets" />

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <!-- Explicitly include template for testing -->
  <ItemGroup>
    <TemplateFile Include="Hello.tmpl" />
  </ItemGroup>
</Project>
```

Build and see code generation in action:

```bash
dotnet build -v:n
```

Output should show:
```
Found 1 template file(s) to process
Generating code from template: Hello.tmpl
Generated: obj\Debug\net8.0\Generated\Hello.g.cs
Added generated files to compilation
```

Check the generated file:

```bash
type obj\Debug\net8.0\Generated\Hello.g.cs
```

You'll see:
```csharp
// Auto-generated from Hello.tmpl
namespace Generated
{
    public class Hello
    {
        public static string SourceTemplate => "Hello.tmpl";
        public static string GeneratedAt => "12/20/2024 10:30:15 AM";
    }
}
```

## Step 11: Test Incremental Build

Build again without changes:

```bash
dotnet build -v:n
```

The code generation target should be skipped (outputs are newer than inputs).

Modify `Hello.tmpl` and rebuild:

```bash
echo New content >> Hello.tmpl
dotnet build -v:n
```

Code generation runs again because input changed!

## Step 12: Test Clean

```bash
dotnet clean
```

The generated files should be deleted from `obj/Debug/net8.0/Generated/`.

## Test Customization

Projects can customize the behavior:

```xml
<PropertyGroup>
  <!-- Custom output path -->
  <CodeGenOutputPath>$(MSBuildProjectDirectory)\GeneratedCode\</CodeGenOutputPath>
  
  <!-- Custom extension -->
  <CodeGenOutputExtension>.generated.cs</CodeGenOutputExtension>
  
  <!-- Disable entirely -->
  <!-- <CodeGenEnabled>false</CodeGenEnabled> -->
</PropertyGroup>
```

## What You Learned

Congratulations! You've built a complete build integration package. You now know how to:

✅ **Define targets** that run during the build process  
✅ **Orchestrate execution** with BeforeTargets, AfterTargets, DependsOnTargets  
✅ **Implement incremental builds** with Inputs and Outputs  
✅ **Invoke MSBuild tasks** like WriteLinesToFile, MakeDir, RemoveDir  
✅ **Add error handling** with Warning and Error tasks  
✅ **Log messages** with appropriate importance levels  
✅ **Manipulate items** dynamically during build  
✅ **Integrate with MSBuild lifecycle** (Clean, CoreCompile, etc.)  
✅ **Test targets** in real build scenarios  

## Key Concepts

- **Target orchestration**: BeforeTargets, AfterTargets, DependsOnTargets
- **Incremental builds**: Inputs and Outputs skip unchanged files
- **Task batching**: `%(Item.Property)` processes each item separately
- **Dynamic properties**: PropertyGroup inside targets evaluates at runtime
- **Item manipulation**: ItemGroup inside targets modifies items during build
- **Built-in tasks**: MakeDir, WriteLinesToFile, RemoveDir, Copy, etc.
- **Message importance**: High, Normal, Low for controlling output verbosity

## Next Steps

Ready for advanced patterns? Try these tutorials:

- **[Recreating JD.Efcpt.Build Patterns](../advanced/efcpt-patterns.md)** - Multi-TFM support, complex orchestration
- **[Recreating Docker Container Patterns](../advanced/containers-patterns.md)** - Publish integration, extensibility hooks

## Challenge: Extend the Package

Try these exercises:

1. **Add custom task**: Create a real template processor (using T4, Scriban, etc.)
2. **Support multiple output languages**: Generate C#, TypeScript, SQL
3. **Add validation**: Ensure templates have required headers
4. **Add caching**: Use hash files to skip unchanged templates even if timestamp differs
5. **Add watch mode**: Regenerate automatically during dotnet watch

## Common Pitfalls

### Target Order

```csharp
// ✅ Correct - validation before generation
targets.Target("Validate", t => t.BeforeTargets("Generate"));
targets.Target("Generate", t => t.BeforeTargets("CoreCompile"));

// ❌ Wrong - might compile before generation
targets.Target("Generate", t => t.AfterTargets("Build"));
```

### Incremental Build Syntax

```csharp
// ✅ Correct - batching with %(...)
target.Inputs("@(TemplateFile)");
target.Outputs("$(OutputPath)%(TemplateFile.Filename).g.cs");

// ❌ Wrong - no batching, single execution
target.Inputs("@(TemplateFile)");
target.Outputs("$(OutputPath)generated.cs");
```

### Condition Placement

```csharp
// ✅ Correct - check at target level
target.Condition("'$(Enabled)' == 'true'");

// ❌ Less efficient - checking in every task
target.Task("Copy", t => t.Param(...), condition: "'$(Enabled)' == 'true'");
target.Task("Delete", t => t.Param(...), condition: "'$(Enabled)' == 'true'");
```

## Troubleshooting

**Target not running?**
- Check the condition evaluates to true
- Verify BeforeTargets/AfterTargets reference correct target names
- Use `-v:d` verbosity to see target execution order

**Incremental build not working?**
- Ensure Inputs and Outputs paths are correct
- Check for batching syntax `%(Item.Property)`
- Delete obj/ folder to force full rebuild

**Generated files not compiling?**
- Verify IncludeGeneratedFiles runs before CoreCompile
- Check that Compile item includes are correct
- Look in obj/ folder to confirm files were generated

## Related Documentation

- [Working with Targets](../../user-guides/targets-tasks/targets.md)
- [TargetsBuilder API](../../user-guides/core-concepts/builders.md#targetsbuilder)
- [TargetBuilder API](../../user-guides/core-concepts/builders.md#targetbuilder)
- [Best Practices - Target Design](../../user-guides/best-practices/index.md#target-design)
- [MSBuild Task Reference](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-task-reference)
