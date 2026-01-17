# Multi-Target Framework Patterns

Supporting multiple target frameworks enables packages to adapt to different .NET versions and platforms. This guide covers patterns for multi-TFM MSBuild packages.

## Understanding Target Frameworks

### What is a Target Framework?

A **Target Framework Moniker (TFM)** specifies the .NET platform version:

- `net8.0` - .NET 8
- `net6.0` - .NET 6
- `netstandard2.0` - .NET Standard 2.0
- `net48` - .NET Framework 4.8

### Why Multi-TFM Packages?

Multi-TFM packages provide:

- **Feature availability**: Use newer APIs on newer frameworks
- **Performance**: Optimize for each framework
- **Compatibility**: Support older frameworks
- **Platform-specific logic**: Handle OS or runtime differences

## Basic Framework Detection

### Check Target Framework

```csharp
.Props(p => p
    .PropertyGroup("'$(TargetFramework)' == 'net8.0'", group =>
    {
        group.Property("UseNet8Features", "true");
    })
    
    .PropertyGroup("'$(TargetFramework)' == 'net6.0'", group =>
    {
        group.Property("UseNet6Features", "true");
    }))
```

### Framework Family Detection

```csharp
.Props(p => p
    // .NET (Core/5+)
    .PropertyGroup("$(TargetFramework.StartsWith('net')) and !$(TargetFramework.StartsWith('netstandard')) and !$(TargetFramework.StartsWith('netcoreapp'))", group =>
    {
        group.Property("IsModernDotNet", "true");
    })
    
    // .NET Standard
    .PropertyGroup("$(TargetFramework.StartsWith('netstandard'))", group =>
    {
        group.Property("IsNetStandard", "true");
    })
    
    // .NET Framework
    .PropertyGroup("$(TargetFramework.StartsWith('net4'))", group =>
    {
        group.Property("IsNetFramework", "true");
    }))
```

## Pattern: Framework-Specific Properties

### Different Defaults Per Framework

```csharp
.Props(p => p
    .Choose(choose =>
    {
        choose.When("'$(TargetFramework)' == 'net8.0'", whenProps =>
        {
            whenProps.Property("LangVersion", "12.0");
            whenProps.Property("Nullable", "enable");
            whenProps.Property("ImplicitUsings", "enable");
        });
        
        choose.When("'$(TargetFramework)' == 'net6.0'", whenProps =>
        {
            whenProps.Property("LangVersion", "10.0");
            whenProps.Property("Nullable", "enable");
        });
        
        choose.When("$(TargetFramework.StartsWith('netstandard'))", whenProps =>
        {
            whenProps.Property("LangVersion", "7.3");
            whenProps.Property("Nullable", "disable");
        });
    }))
```

### Progressive Feature Enablement

```csharp
.Props(p => p
    // Base features (all frameworks)
    .Property("MyPackageCore", "true")
    
    // .NET 6+ features
    .PropertyGroup("$([MSBuild]::VersionGreaterThanOrEquals('$(TargetFrameworkVersion)', '6.0'))", group =>
    {
        group.Property("MyPackageAsyncFeatures", "true");
        group.Property("MyPackageSpanSupport", "true");
    })
    
    // .NET 8+ features
    .PropertyGroup("$([MSBuild]::VersionGreaterThanOrEquals('$(TargetFrameworkVersion)', '8.0'))", group =>
    {
        group.Property("MyPackageNet8Features", "true");
        group.Property("MyPackageRequiredMembers", "true");
    }))
```

## Pattern: Framework-Specific Assets

### Different Assemblies Per Framework

```csharp
.Props(p => p
    .Choose(choose =>
    {
        choose.When("'$(TargetFramework)' == 'net6.0'", whenProps =>
        {
            whenProps.ItemGroup(null, group =>
            {
                group.Include("Reference", "$(MSBuildThisFileDirectory)../../lib/net6.0/MyPackage.dll", item => item
                    .Meta("Private", "false")
                    .Meta("HintPath", "$(MSBuildThisFileDirectory)../../lib/net6.0/MyPackage.dll"));
            });
        });
        
        choose.When("'$(TargetFramework)' == 'net8.0'", whenProps =>
        {
            whenProps.ItemGroup(null, group =>
            {
                group.Include("Reference", "$(MSBuildThisFileDirectory)../../lib/net8.0/MyPackage.dll", item => item
                    .Meta("Private", "false")
                    .Meta("HintPath", "$(MSBuildThisFileDirectory)../../lib/net8.0/MyPackage.dll"));
            });
        });
        
        choose.When("$(TargetFramework.StartsWith('netstandard'))", whenProps =>
        {
            whenProps.ItemGroup(null, group =>
            {
                group.Include("Reference", "$(MSBuildThisFileDirectory)../../lib/netstandard2.0/MyPackage.dll", item => item
                    .Meta("Private", "false")
                    .Meta("HintPath", "$(MSBuildThisFileDirectory)../../lib/netstandard2.0/MyPackage.dll"));
            });
        });
    }))
```

### Framework-Specific Task Assemblies

```csharp
.Targets(t => t
    .UsingTask("MyPackage.CustomTask", 
        "$(MSBuildThisFileDirectory)../../tools/net6.0/MyPackage.Tasks.dll",
        condition: "$(TargetFramework.StartsWith('net6'))")
    
    .UsingTask("MyPackage.CustomTask", 
        "$(MSBuildThisFileDirectory)../../tools/net8.0/MyPackage.Tasks.dll",
        condition: "$(TargetFramework.StartsWith('net8'))")
    
    .Target("RunCustomTask", target => target
        .Task("MyPackage.CustomTask", task =>
        {
            task.Param("Input", "$(Input)");
        })))
```

## Pattern: Analyzer Support

### Framework-Specific Analyzers

```csharp
.Props(p => p
    // C# analyzers for modern .NET
    .ItemGroup("$(TargetFramework.StartsWith('net6')) or $(TargetFramework.StartsWith('net8'))", group =>
    {
        group.Include("Analyzer", "$(MSBuildThisFileDirectory)../../analyzers/dotnet/cs/MyAnalyzer.dll", item => item
            .Meta("Visible", "false"));
    })
    
    // Different analyzer for .NET Framework
    .ItemGroup("$(TargetFramework.StartsWith('net4'))", group =>
    {
        group.Include("Analyzer", "$(MSBuildThisFileDirectory)../../analyzers/dotnet/cs/MyAnalyzer.NetFramework.dll", item => item
            .Meta("Visible", "false"));
    }))
```

## Pattern: Dependency Management

### Framework-Specific Package References

```csharp
.Props(p => p
    // System.Text.Json only on older frameworks
    .ItemGroup("$(TargetFramework.StartsWith('netstandard2.0')) or $(TargetFramework.StartsWith('net4'))", group =>
    {
        group.Include("PackageReference", "System.Text.Json", item => item
            .Meta("Version", "8.0.0")
            .Meta("PrivateAssets", "all"));
    })
    
    // Use built-in on modern .NET
    .PropertyGroup("$(TargetFramework.StartsWith('net6')) or $(TargetFramework.StartsWith('net8'))", group =>
    {
        group.Property("UseBuiltInJsonSerializer", "true");
    }))
```

## Pattern: Content Files

### Framework-Specific Content

```csharp
.Props(p => p
    .Choose(choose =>
    {
        choose.When("'$(TargetFramework)' == 'net6.0'", whenProps =>
        {
            whenProps.ItemGroup(null, group =>
            {
                group.Include("Content", "$(MSBuildThisFileDirectory)../../contentFiles/any/net6.0/**/*.*", item => item
                    .Meta("Pack", "true")
                    .Meta("PackagePath", "contentFiles/any/net6.0")
                    .Meta("CopyToOutputDirectory", "PreserveNewest"));
            });
        });
        
        choose.When("'$(TargetFramework)' == 'net8.0'", whenProps =>
        {
            whenProps.ItemGroup(null, group =>
            {
                group.Include("Content", "$(MSBuildThisFileDirectory)../../contentFiles/any/net8.0/**/*.*", item => item
                    .Meta("Pack", "true")
                    .Meta("PackagePath", "contentFiles/any/net8.0")
                    .Meta("CopyToOutputDirectory", "PreserveNewest"));
            });
        });
    }))
```

## Pattern: Build-Time Code Generation

### Generate Different Code Per Framework

```csharp
.Targets(t => t
    .Target("GenerateVersionCode", target => target
        .BeforeTargets("CoreCompile")
        
        // .NET 8: Use C# 12 features
        .PropertyGroup("'$(TargetFramework)' == 'net8.0'", group =>
        {
            group.Property("GeneratedCodeTemplate", "$(MSBuildThisFileDirectory)../../templates/Version.Net8.cs.template");
        })
        
        // .NET 6: Use C# 10 features
        .PropertyGroup("'$(TargetFramework)' == 'net6.0'", group =>
        {
            group.Property("GeneratedCodeTemplate", "$(MSBuildThisFileDirectory)../../templates/Version.Net6.cs.template");
        })
        
        // Generate code
        .Task("Copy", task =>
        {
            task.Param("SourceFiles", "$(GeneratedCodeTemplate)");
            task.Param("DestinationFiles", "$(IntermediateOutputPath)Generated/Version.g.cs");
        })
        
        .ItemGroup(null, group =>
        {
            group.Include("Compile", "$(IntermediateOutputPath)Generated/Version.g.cs");
        })))
```

## Pattern: Conditional Target Execution

### Execute Different Targets Per Framework

```csharp
.Targets(t => t
    .Target("Net8Build", target => target
        .Condition("'$(TargetFramework)' == 'net8.0'")
        .AfterTargets("Build")
        .Message("Running .NET 8 specific build logic", "High")
        .Task("SomeNet8SpecificTask", ...))
    
    .Target("Net6Build", target => target
        .Condition("'$(TargetFramework)' == 'net6.0'")
        .AfterTargets("Build")
        .Message("Running .NET 6 specific build logic", "High")
        .Task("SomeNet6SpecificTask", ...))
    
    .Target("NetStandardBuild", target => target
        .Condition("$(TargetFramework.StartsWith('netstandard'))")
        .AfterTargets("Build")
        .Message("Running .NET Standard specific build logic", "High")
        .Task("SomeNetStandardTask", ...)))
```

## Pattern: Platform and Framework Combined

### OS and Framework Detection

```csharp
.Props(p => p
    .Choose(choose =>
    {
        // Windows + .NET 8
        choose.When("$([MSBuild]::IsOSPlatform('Windows')) and '$(TargetFramework)' == 'net8.0'", whenProps =>
        {
            whenProps.Property("NativeLib", "runtimes/win-x64/native/mylib.dll");
        });
        
        // Linux + .NET 8
        choose.When("$([MSBuild]::IsOSPlatform('Linux')) and '$(TargetFramework)' == 'net8.0'", whenProps =>
        {
            whenProps.Property("NativeLib", "runtimes/linux-x64/native/libmylib.so");
        });
        
        // macOS + .NET 8
        choose.When("$([MSBuild]::IsOSPlatform('OSX')) and '$(TargetFramework)' == 'net8.0'", whenProps =>
        {
            whenProps.Property("NativeLib", "runtimes/osx-x64/native/libmylib.dylib");
        });
    }))
```

## Testing Multi-TFM Packages

### Create Test Projects

**Test with multiple frameworks:**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net6.0;net8.0;netstandard2.0</TargetFrameworks>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MyPackage" Version="1.0.0" />
  </ItemGroup>
</Project>
```

**Build all frameworks:**

```bash
dotnet build
```

**Build specific framework:**

```bash
dotnet build -f net8.0
```

### Debug Framework-Specific Logic

```csharp
.Target("DebugFramework", target => target
    .Message("TargetFramework: $(TargetFramework)", "High")
    .Message("TargetFrameworkVersion: $(TargetFrameworkVersion)", "High")
    .Message("TargetFrameworkIdentifier: $(TargetFrameworkIdentifier)", "High")
    .Message("IsModernDotNet: $(IsModernDotNet)", "High")
    .Message("IsNetStandard: $(IsNetStandard)", "High")
    .Message("IsNetFramework: $(IsNetFramework)", "High"))
```

Run: `dotnet build -t:DebugFramework -f net8.0`

## Best Practices

### DO: Use Choose for Complex Logic

```csharp
// ✓ Clear branching
.Choose(choose =>
{
    choose.When("'$(TargetFramework)' == 'net8.0'", ...);
    choose.When("'$(TargetFramework)' == 'net6.0'", ...);
    choose.Otherwise(...);
})
```

### DO: Provide Fallbacks

```csharp
// ✓ Fallback for unknown frameworks
.Choose(choose =>
{
    choose.When("$(TargetFramework.StartsWith('net8'))", ...);
    choose.When("$(TargetFramework.StartsWith('net6'))", ...);
    choose.Otherwise(otherwiseProps =>
    {
        otherwiseProps.Property("UseCompatibilityMode", "true");
    });
})
```

### DO: Test All Target Frameworks

Always test your package with all supported frameworks:

```bash
dotnet pack
dotnet test --framework net6.0
dotnet test --framework net8.0
```

### DON'T: Hardcode Framework Versions

```csharp
// ✗ Hardcoded
"'$(TargetFrameworkVersion)' == '8.0'"

// ✓ Use comparison
"$([MSBuild]::VersionGreaterThanOrEquals('$(TargetFrameworkVersion)', '8.0'))"
```

### DON'T: Forget About .NET Standard

```csharp
// ✗ Only handles modern .NET
.PropertyGroup("$(TargetFramework.StartsWith('net'))", ...)

// ✓ Considers .NET Standard
.PropertyGroup("$(TargetFramework.StartsWith('net')) and !$(TargetFramework.StartsWith('netstandard'))", ...)
```

## Summary

| Pattern | Use For |
|---------|---------|
| Framework detection | Conditional logic based on TFM |
| Choose/When/Otherwise | Complex multi-framework branching |
| Version comparison | Progressive feature enablement |
| Framework-specific assets | Different assemblies per framework |
| Platform + Framework | Combined OS and TFM detection |

## Next Steps

- [Conditional Logic](../properties-items/conditionals.md) - Condition patterns
- [Choose/When/Otherwise](choose.md) - Branching logic
- [Package Structure](../core-concepts/package-structure.md) - NuGet layout for multi-TFM
- [MSBuild Target Frameworks (Microsoft Docs)](https://learn.microsoft.com/en-us/dotnet/standard/frameworks)
