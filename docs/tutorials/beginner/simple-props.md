# Tutorial: Building a Simple Properties Package

Learn the fundamentals of JD.MSBuild.Fluent by creating a properties-only MSBuild package that defines default compilation settings.

## Overview

In this tutorial, you'll create `CompanyStandards.Build`, a simple MSBuild package that sets default properties for C# projects across your organization. This package will:

- Define default property values (LangVersion, Nullable, etc.)
- Set platform-specific properties using Choose/When
- Include conditional property groups for Debug/Release
- Provide override points for consuming projects

**Time**: ~15 minutes  
**Difficulty**: Beginner  
**Output**: A working MSBuild package with props file

## What You'll Learn

By completing this tutorial, you will:

- ✅ Create a PackageDefinition using `Package.Define()`
- ✅ Use `Props()` to configure properties
- ✅ Add conditional property groups
- ✅ Implement Choose/When/Otherwise logic
- ✅ Emit MSBuild XML files
- ✅ Test your package in a real project

## Prerequisites

- .NET SDK 8.0 or later installed
- Basic understanding of MSBuild properties
- Familiarity with C# and fluent APIs
- JD.MSBuild.Fluent package (we'll install it)

## The Scenario

You're building a company-wide build standards package. Every C# project should:

1. Use C# 12 language features
2. Enable nullable reference types
3. Treat warnings as errors in Release builds
4. Use different analyzers on Windows vs Linux
5. Allow individual projects to override these settings

Let's build it!

## Step 1: Create the Project

Create a new class library for your package definitions:

```bash
mkdir CompanyStandards
cd CompanyStandards
dotnet new classlib -n CompanyStandards.Build
cd CompanyStandards.Build
dotnet add package JD.MSBuild.Fluent
```

This creates a project structure:
```
CompanyStandards.Build/
├── CompanyStandards.Build.csproj
└── Class1.cs  (delete this)
```

## Step 2: Define the Package Factory

Create `PackageFactory.cs`:

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace CompanyStandards.Build;

public static class PackageFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("CompanyStandards.Build")
            .Description("Company-wide build standards for C# projects")
            .Props(ConfigureProps)
            .Build();
    }

    private static void ConfigureProps(PropsBuilder props)
    {
        // We'll implement this next
    }
}
```

**Explanation**:
- `Package.Define("CompanyStandards.Build")` - Creates a new package with this ID
- `.Description()` - Adds metadata about the package
- `.Props(ConfigureProps)` - Delegates props configuration to a helper method
- `.Build()` - Returns the final `PackageDefinition`

## Step 3: Add Default Properties

Implement `ConfigureProps` with basic defaults:

```csharp
private static void ConfigureProps(PropsBuilder props)
{
    // Core language settings
    props.Comment("=========================================");
    props.Comment(" Company Build Standards");
    props.Comment("=========================================");
    
    props.PropertyGroup(null, group =>
    {
        group.Comment("C# language settings");
        group.Property("LangVersion", "12");
        group.Property("Nullable", "enable");
        group.Property("ImplicitUsings", "enable");
    }, label: "Language Features");

    props.PropertyGroup(null, group =>
    {
        group.Comment("Code quality settings");
        group.Property("AnalysisMode", "AllEnabledByDefault");
        group.Property("EnforceCodeStyleInBuild", "true");
    }, label: "Code Quality");
}
```

**Explanation**:
- `props.Comment()` - Adds XML comments to the generated file
- `props.PropertyGroup(null, group => { })` - Creates a property group (null condition = always applies)
- `group.Property(name, value)` - Adds a property to the group
- `label:` - Adds a `Label` attribute to the PropertyGroup for documentation

## Step 4: Add Configuration-Specific Properties

Add conditional properties for Debug vs Release:

```csharp
private static void ConfigureProps(PropsBuilder props)
{
    // ... previous code ...

    // Configuration-specific settings
    props.PropertyGroup("'$(Configuration)' == 'Release'", group =>
    {
        group.Property("TreatWarningsAsErrors", "true");
        group.Property("WarningLevel", "9999");
        group.Property("NoWarn", "$(NoWarn);CS1591"); // XML docs not required
    }, label: "Release Configuration");

    props.PropertyGroup("'$(Configuration)' == 'Debug'", group =>
    {
        group.Property("CheckForOverflowUnderflow", "true");
        group.Property("DebugType", "full");
    }, label: "Debug Configuration");
}
```

**Explanation**:
- `"'$(Configuration)' == 'Release'"` - MSBuild condition (note the single quotes!)
- Properties in this group only apply when the condition is true
- `$(NoWarn)` - Appends to existing NoWarn property

## Step 5: Add Platform-Specific Properties

Use `Choose` for platform detection:

```csharp
private static void ConfigureProps(PropsBuilder props)
{
    // ... previous code ...

    // Platform-specific settings
    props.Comment("Platform-specific analyzer paths");
    
    props.Choose(choose =>
    {
        choose.When("$([MSBuild]::IsOSPlatform('Windows'))", whenProps =>
        {
            whenProps.PropertyGroup(null, group =>
            {
                group.Property("AdditionalAnalyzers", 
                    "$(MSBuildThisFileDirectory)../../analyzers/windows");
            });
        });

        choose.When("$([MSBuild]::IsOSPlatform('Linux'))", whenProps =>
        {
            whenProps.PropertyGroup(null, group =>
            {
                group.Property("AdditionalAnalyzers", 
                    "$(MSBuildThisFileDirectory)../../analyzers/linux");
            });
        });

        choose.Otherwise(otherwiseProps =>
        {
            otherwiseProps.PropertyGroup(null, group =>
            {
                group.Property("AdditionalAnalyzers", 
                    "$(MSBuildThisFileDirectory)../../analyzers/generic");
            });
        });
    });
}
```

**Explanation**:
- `$([MSBuild]::IsOSPlatform('Windows'))` - MSBuild function for platform detection
- `choose.When()` - Adds a conditional branch
- `choose.Otherwise()` - Fallback for other platforms
- `$(MSBuildThisFileDirectory)` - Path to the .props file location

## Step 6: Add Override Points

Allow projects to customize settings:

```csharp
private static void ConfigureProps(PropsBuilder props)
{
    // ... previous code ...

    // Override points
    props.Comment("Override points for consuming projects");
    props.Comment("Set CompanyStandardsDisabled=true to disable all defaults");
    
    props.PropertyGroup("'$(CompanyStandardsDisabled)' != 'true'", group =>
    {
        group.Comment("These properties can be overridden in your project file");
        group.Property("GenerateDocumentationFile", "true", 
            condition: "'$(GenerateDocumentationFile)' == ''");
        group.Property("NoWarn", "$(NoWarn);CA1014", 
            condition: "'$(DisableCA1014)' == 'true'");
    }, label: "Overrideable Defaults");
}
```

**Explanation**:
- `"'$(CompanyStandardsDisabled)' != 'true'"` - Master switch to disable the package
- `condition: "'$(GenerateDocumentationFile)' == ''"` - Only set if not already defined
- Projects can override by setting properties in their .csproj

## Complete Code

Here's the full `PackageFactory.cs`:

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace CompanyStandards.Build;

public static class PackageFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("CompanyStandards.Build")
            .Description("Company-wide build standards for C# projects")
            .Props(ConfigureProps)
            .Build();
    }

    private static void ConfigureProps(PropsBuilder props)
    {
        // Header comments
        props.Comment("=========================================");
        props.Comment(" Company Build Standards");
        props.Comment("=========================================");
        
        // Core language settings
        props.PropertyGroup(null, group =>
        {
            group.Comment("C# language settings");
            group.Property("LangVersion", "12");
            group.Property("Nullable", "enable");
            group.Property("ImplicitUsings", "enable");
        }, label: "Language Features");

        // Code quality settings
        props.PropertyGroup(null, group =>
        {
            group.Comment("Code quality settings");
            group.Property("AnalysisMode", "AllEnabledByDefault");
            group.Property("EnforceCodeStyleInBuild", "true");
        }, label: "Code Quality");

        // Configuration-specific settings
        props.PropertyGroup("'$(Configuration)' == 'Release'", group =>
        {
            group.Property("TreatWarningsAsErrors", "true");
            group.Property("WarningLevel", "9999");
            group.Property("NoWarn", "$(NoWarn);CS1591");
        }, label: "Release Configuration");

        props.PropertyGroup("'$(Configuration)' == 'Debug'", group =>
        {
            group.Property("CheckForOverflowUnderflow", "true");
            group.Property("DebugType", "full");
        }, label: "Debug Configuration");

        // Platform-specific settings
        props.Comment("Platform-specific analyzer paths");
        
        props.Choose(choose =>
        {
            choose.When("$([MSBuild]::IsOSPlatform('Windows'))", whenProps =>
            {
                whenProps.PropertyGroup(null, group =>
                {
                    group.Property("AdditionalAnalyzers", 
                        "$(MSBuildThisFileDirectory)../../analyzers/windows");
                });
            });

            choose.When("$([MSBuild]::IsOSPlatform('Linux'))", whenProps =>
            {
                whenProps.PropertyGroup(null, group =>
                {
                    group.Property("AdditionalAnalyzers", 
                        "$(MSBuildThisFileDirectory)../../analyzers/linux");
                });
            });

            choose.Otherwise(otherwiseProps =>
            {
                otherwiseProps.PropertyGroup(null, group =>
                {
                    group.Property("AdditionalAnalyzers", 
                        "$(MSBuildThisFileDirectory)../../analyzers/generic");
                });
            });
        });

        // Override points
        props.Comment("Override points for consuming projects");
        props.Comment("Set CompanyStandardsDisabled=true to disable all defaults");
        
        props.PropertyGroup("'$(CompanyStandardsDisabled)' != 'true'", group =>
        {
            group.Comment("These properties can be overridden in your project file");
            group.Property("GenerateDocumentationFile", "true", 
                condition: "'$(GenerateDocumentationFile)' == ''");
            group.Property("NoWarn", "$(NoWarn);CA1014", 
                condition: "'$(DisableCA1014)' == 'true'");
        }, label: "Overrideable Defaults");
    }
}
```

## Step 7: Generate the MSBuild Files

Build your project and generate the MSBuild XML:

```bash
# Build the project
dotnet build

# Generate MSBuild files (use the CLI tool or programmatically)
# If you have jdmsbuild CLI:
jdmsbuild generate \
    --assembly bin/Debug/net8.0/CompanyStandards.Build.dll \
    --type CompanyStandards.Build.PackageFactory \
    --method Create \
    --output artifacts/msbuild
```

Or generate programmatically with a small console app:

```csharp
// Program.cs
using JD.MSBuild.Fluent.Packaging;
using CompanyStandards.Build;

var definition = PackageFactory.Create();
var emitter = new MsBuildPackageEmitter();
emitter.Emit(definition, "artifacts/msbuild");

Console.WriteLine("Generated MSBuild files to artifacts/msbuild/");
```

## Generated XML Output

Your generated `build/CompanyStandards.Build.props` will look like:

```xml
<Project>
  <!--=========================================-->
  <!-- Company Build Standards-->
  <!--=========================================-->
  <PropertyGroup Label="Language Features">
    <!--C# language settings-->
    <LangVersion>12</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <PropertyGroup Label="Code Quality">
    <!--Code quality settings-->
    <AnalysisMode>AllEnabledByDefault</AnalysisMode>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)' == 'Release'" Label="Release Configuration">
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningLevel>9999</WarningLevel>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)' == 'Debug'" Label="Debug Configuration">
    <CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>
    <DebugType>full</DebugType>
  </PropertyGroup>
  <!--Platform-specific analyzer paths-->
  <Choose>
    <When Condition="$([MSBuild]::IsOSPlatform('Windows'))">
      <PropertyGroup>
        <AdditionalAnalyzers>$(MSBuildThisFileDirectory)../../analyzers/windows</AdditionalAnalyzers>
      </PropertyGroup>
    </When>
    <When Condition="$([MSBuild]::IsOSPlatform('Linux'))">
      <PropertyGroup>
        <AdditionalAnalyzers>$(MSBuildThisFileDirectory)../../analyzers/linux</AdditionalAnalyzers>
      </PropertyGroup>
    </When>
    <Otherwise>
      <PropertyGroup>
        <AdditionalAnalyzers>$(MSBuildThisFileDirectory)../../analyzers/generic</AdditionalAnalyzers>
      </PropertyGroup>
    </Otherwise>
  </Choose>
  <!--Override points for consuming projects-->
  <!--Set CompanyStandardsDisabled=true to disable all defaults-->
  <PropertyGroup Condition="'$(CompanyStandardsDisabled)' != 'true'" Label="Overrideable Defaults">
    <!--These properties can be overridden in your project file-->
    <GenerateDocumentationFile Condition="'$(GenerateDocumentationFile)' == ''">true</GenerateDocumentationFile>
    <NoWarn Condition="'$(DisableCA1014)' == 'true'">$(NoWarn);CA1014</NoWarn>
  </PropertyGroup>
</Project>
```

Notice how your fluent C# code translated to clean, readable MSBuild XML!

## Step 8: Test the Package

Create a test project to validate your package:

```bash
cd ..
mkdir TestApp
cd TestApp
dotnet new console -n TestApp
cd TestApp
```

Manually import your generated props (simulate NuGet package import):

Edit `TestApp.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!-- Import your generated props -->
  <Import Project="..\..\CompanyStandards.Build\artifacts\msbuild\build\CompanyStandards.Build.props" />

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>
```

Build and check that properties are applied:

```bash
dotnet build -v:n | findstr LangVersion
# Should show: LangVersion=12

dotnet build -v:n -c Release | findstr TreatWarningsAsErrors
# Should show: TreatWarningsAsErrors=true
```

## Testing Override Points

Test that projects can override your defaults:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="..\..\CompanyStandards.Build\artifacts\msbuild\build\CompanyStandards.Build.props" />

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    
    <!-- Override company defaults -->
    <LangVersion>11</LangVersion>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>
</Project>
```

Build and verify your overrides took effect:

```bash
dotnet build -v:n | findstr LangVersion
# Should show: LangVersion=11 (your override)
```

## Testing the Master Switch

Test disabling the entire package:

```xml
<PropertyGroup>
  <CompanyStandardsDisabled>true</CompanyStandardsDisabled>
</PropertyGroup>
```

## What You Learned

Congratulations! You've completed your first MSBuild package with JD.MSBuild.Fluent. You now know how to:

✅ **Create a package** using `Package.Define()`  
✅ **Configure properties** with `Props()` and `PropertyGroup()`  
✅ **Add conditions** for Debug/Release configurations  
✅ **Use Choose/When** for platform-specific logic  
✅ **Provide override points** for consuming projects  
✅ **Generate MSBuild XML** from fluent definitions  
✅ **Test your package** in real projects  

## Key Concepts

- **Package.Define()**: Entry point for package definitions
- **Props()**: Configure evaluation-time properties and items
- **PropertyGroup()**: Group related properties with optional conditions
- **Choose/When/Otherwise**: Multi-way branching for complex conditions
- **Comments and Labels**: Document your generated XML
- **Override patterns**: Check if properties exist before setting defaults

## Next Steps

Now that you've mastered properties, learn about targets:

- **[Creating a Build Integration Package](../intermediate/build-integration.md)** - Add targets and tasks to your package
- **[Fluent Builders Reference](../../user-guides/core-concepts/builders.md)** - Explore all builder methods
- **[Best Practices](../../user-guides/best-practices/index.md)** - Learn professional patterns

## Challenge: Extend the Package

Try these exercises to reinforce your learning:

1. **Add item groups**: Include common files like `.editorconfig`
2. **Add more configurations**: Support custom configurations like "Staging"
3. **Add TFM-specific settings**: Different properties for net6.0 vs net8.0
4. **Add imports**: Import a shared props file from a tools directory

## Common Pitfalls

### Condition Syntax

```csharp
// ✅ Correct - single quotes in condition
.PropertyGroup("'$(Configuration)' == 'Release'", ...)

// ❌ Wrong - double quotes
.PropertyGroup("\"$(Configuration)\" == \"Release\"", ...)
```

### Property Override Order

```csharp
// ✅ Correct - check if empty first
group.Property("MyProp", "default", condition: "'$(MyProp)' == ''");

// ❌ Wrong - unconditional override
group.Property("MyProp", "default");
```

### Platform Detection

```csharp
// ✅ Correct - use MSBuild function
"$([MSBuild]::IsOSPlatform('Windows'))"

// ❌ Brittle - string comparison
"'$(OS)' == 'Windows_NT'"
```

## Troubleshooting

**Properties not applied?**
- Check that the .props file is imported correctly
- Verify the import is before your PropertyGroup
- Use `dotnet build -v:d` to see property evaluation

**Overrides not working?**
- Ensure your override comes after the import
- Check for condition typos in the generated XML
- Properties set with conditions can't be overridden

**Platform detection not working?**
- Test on the actual platform (can't test Linux detection on Windows)
- Check MSBuild version (older versions may not support IsOSPlatform)

## Related Documentation

- [Quick Start](../../user-guides/getting-started/quick-start.md)
- [PropsBuilder API](../../user-guides/core-concepts/builders.md#propsbuilder)
- [Best Practices - Property Design](../../user-guides/best-practices/index.md#property-design)
