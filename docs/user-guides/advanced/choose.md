# Choose/When/Otherwise Constructs

Choose/When/Otherwise provides branching logic in MSBuild, similar to switch/case or if/else if/else statements. This guide covers usage patterns and best practices.

## Choose Basics

### What is Choose?

`Choose` is MSBuild's conditional branching construct that:

- Evaluates multiple conditions sequentially
- Executes the first matching `When` branch
- Falls back to `Otherwise` if no conditions match
- Can contain properties, items, and other elements

### Simple Choose

```csharp
.Choose(choose =>
{
    choose.When("'$(Configuration)' == 'Debug'", whenProps =>
    {
        whenProps.Property("Optimize", "false");
        whenProps.Property("DebugType", "full");
    });
    
    choose.When("'$(Configuration)' == 'Release'", whenProps =>
    {
        whenProps.Property("Optimize", "true");
        whenProps.Property("DebugType", "pdbonly");
    });
    
    choose.Otherwise(otherwiseProps =>
    {
        otherwiseProps.Property("Optimize", "false");
        otherwiseProps.Property("DebugType", "portable");
    });
})
```

**Generated XML:**

```xml
<Choose>
  <When Condition="'$(Configuration)' == 'Debug'">
    <PropertyGroup>
      <Optimize>false</Optimize>
      <DebugType>full</DebugType>
    </PropertyGroup>
  </When>
  <When Condition="'$(Configuration)' == 'Release'">
    <PropertyGroup>
      <Optimize>true</Optimize>
      <DebugType>pdbonly</DebugType>
    </PropertyGroup>
  </When>
  <Otherwise>
    <PropertyGroup>
      <Optimize>false</Optimize>
      <DebugType>portable</DebugType>
    </PropertyGroup>
  </Otherwise>
</Choose>
```

### Choose Without Otherwise

`Otherwise` is optional:

```csharp
.Choose(choose =>
{
    choose.When("'$(EnableFeature)' == 'true'", whenProps =>
    {
        whenProps.Property("FeatureEnabled", "true");
    });
})
```

If no conditions match and there's no `Otherwise`, nothing happens.

## When Conditions

### Simple Equality

```csharp
choose.When("'$(Configuration)' == 'Debug'", ...)
choose.When("'$(Platform)' == 'x64'", ...)
choose.When("'$(TargetFramework)' == 'net8.0'", ...)
```

### Complex Conditions

```csharp
// Multiple conditions with AND
choose.When("'$(Configuration)' == 'Release' and '$(Platform)' == 'x64'", whenProps =>
{
    whenProps.Property("OptimizeFor64Bit", "true");
})

// Multiple conditions with OR
choose.When("'$(Platform)' == 'x64' or '$(Platform)' == 'ARM64'", whenProps =>
{
    whenProps.Property("Use64BitTools", "true");
})

// Negation
choose.When("!('$(SkipOptimization)' == 'true')", whenProps =>
{
    whenProps.Property("Optimize", "true");
})
```

### Function-Based Conditions

```csharp
// OS detection
choose.When("$([MSBuild]::IsOSPlatform('Windows'))", whenProps =>
{
    whenProps.Property("OS", "Windows");
})

// String operations
choose.When("$(TargetFramework.StartsWith('net8'))", whenProps =>
{
    whenProps.Property("IsNet8", "true");
})

// File existence
choose.When("Exists('$(MSBuildProjectDirectory)/config.json')", whenProps =>
{
    whenProps.Property("HasConfig", "true");
})
```

## Pattern: Platform Detection

### Operating System Branching

```csharp
.Choose(choose =>
{
    choose.When("$([MSBuild]::IsOSPlatform('Windows'))", whenProps =>
    {
        whenProps.Property("NativeExtension", ".dll");
        whenProps.Property("ScriptExtension", ".bat");
        whenProps.Property("PathSeparator", ";");
    });
    
    choose.When("$([MSBuild]::IsOSPlatform('Linux'))", whenProps =>
    {
        whenProps.Property("NativeExtension", ".so");
        whenProps.Property("ScriptExtension", ".sh");
        whenProps.Property("PathSeparator", ":");
    });
    
    choose.When("$([MSBuild]::IsOSPlatform('OSX'))", whenProps =>
    {
        whenProps.Property("NativeExtension", ".dylib");
        whenProps.Property("ScriptExtension", ".sh");
        whenProps.Property("PathSeparator", ":");
    });
    
    choose.Otherwise(otherwiseProps =>
    {
        otherwiseProps.Warning("Unknown operating system");
    });
})
```

## Pattern: Framework Detection

### Target Framework Branching

```csharp
.Choose(choose =>
{
    choose.When("'$(TargetFramework)' == 'net8.0'", whenProps =>
    {
        whenProps.Property("LangVersion", "12.0");
        whenProps.Property("UseNet8Features", "true");
        whenProps.ItemGroup(null, group =>
        {
            group.Include("Reference", "$(MSBuildThisFileDirectory)../../lib/net8.0/MyLib.dll");
        });
    });
    
    choose.When("'$(TargetFramework)' == 'net6.0'", whenProps =>
    {
        whenProps.Property("LangVersion", "10.0");
        whenProps.Property("UseNet6Features", "true");
        whenProps.ItemGroup(null, group =>
        {
            group.Include("Reference", "$(MSBuildThisFileDirectory)../../lib/net6.0/MyLib.dll");
        });
    });
    
    choose.When("$(TargetFramework.StartsWith('netstandard'))", whenProps =>
    {
        whenProps.Property("LangVersion", "7.3");
        whenProps.Property("UseCompatibilityMode", "true");
        whenProps.ItemGroup(null, group =>
        {
            group.Include("Reference", "$(MSBuildThisFileDirectory)../../lib/netstandard2.0/MyLib.dll");
        });
    });
    
    choose.Otherwise(otherwiseProps =>
    {
        otherwiseProps.Error("Unsupported target framework: $(TargetFramework)");
    });
})
```

## Pattern: Build Configuration

### Configuration-Based Settings

```csharp
.Choose(choose =>
{
    choose.When("'$(Configuration)' == 'Debug'", whenProps =>
    {
        whenProps.PropertyGroup(null, group =>
        {
            group.Property("Optimize", "false");
            group.Property("DebugType", "full");
            group.Property("DebugSymbols", "true");
            group.Property("DefineConstants", "$(DefineConstants);DEBUG;TRACE");
        });
    });
    
    choose.When("'$(Configuration)' == 'Release'", whenProps =>
    {
        whenProps.PropertyGroup(null, group =>
        {
            group.Property("Optimize", "true");
            group.Property("DebugType", "pdbonly");
            group.Property("DebugSymbols", "true");
            group.Property("DefineConstants", "$(DefineConstants);TRACE");
        });
    });
    
    choose.When("'$(Configuration)' == 'Production'", whenProps =>
    {
        whenProps.PropertyGroup(null, group =>
        {
            group.Property("Optimize", "true");
            group.Property("DebugType", "none");
            group.Property("DebugSymbols", "false");
            group.Property("DefineConstants", "$(DefineConstants);TRACE;PRODUCTION");
        });
    });
    
    choose.Otherwise(otherwiseProps =>
    {
        // Default to Debug-like settings
        otherwiseProps.PropertyGroup(null, group =>
        {
            group.Property("Optimize", "false");
            group.Property("DebugType", "portable");
        });
    });
})
```

## Pattern: Feature Detection

### Capability-Based Branching

```csharp
.Choose(choose =>
{
    // Check for Git repository
    choose.When("Exists('$(MSBuildProjectDirectory)/.git')", whenProps =>
    {
        whenProps.Property("HasGit", "true");
        whenProps.Property("EnableSourceLink", "true");
    });
    
    // Otherwise, disable Git features
    choose.Otherwise(otherwiseProps =>
    {
        otherwiseProps.Property("HasGit", "false");
        otherwiseProps.Property("EnableSourceLink", "false");
    });
})

.Choose(choose =>
{
    // Check for Docker
    choose.When("Exists('$(MSBuildProjectDirectory)/Dockerfile')", whenProps =>
    {
        whenProps.Property("DockerSupport", "true");
    });
    
    choose.Otherwise(otherwiseProps =>
    {
        whenProps.Property("DockerSupport", "false");
    });
})
```

## Pattern: Items in Choose

### Conditional Item Inclusion

```csharp
.Choose(choose =>
{
    choose.When("'$(IncludeTests)' == 'true'", whenProps =>
    {
        whenProps.ItemGroup(null, group =>
        {
            group.Include("Compile", "test/**/*.cs");
            group.Include("Content", "test/**/*.json");
            group.Include("None", "test/**/*.md");
        });
    });
    
    choose.Otherwise(otherwiseProps =>
    {
        otherwiseProps.ItemGroup(null, group =>
        {
            group.Remove("Compile", "test/**/*.cs");
        });
    });
})
```

## Pattern: Nested Choose

### Nested Branching

```csharp
.Choose(choose =>
{
    choose.When("'$(Configuration)' == 'Release'", whenProps =>
    {
        // Nested Choose based on Platform
        whenProps.Choose(innerChoose =>
        {
            innerChoose.When("'$(Platform)' == 'x64'", innerWhenProps =>
            {
                innerWhenProps.Property("Optimize", "aggressive");
                innerWhenProps.Property("PreferredTarget", "x64");
            });
            
            innerChoose.When("'$(Platform)' == 'ARM64'", innerWhenProps =>
            {
                innerWhenProps.Property("Optimize", "aggressive");
                innerWhenProps.Property("PreferredTarget", "ARM64");
            });
            
            innerChoose.Otherwise(innerOtherwiseProps =>
            {
                innerOtherwiseProps.Property("Optimize", "true");
            });
        });
    });
    
    choose.Otherwise(otherwiseProps =>
    {
        otherwiseProps.Property("Optimize", "false");
    });
})
```

**Use sparingly:** Nested Choose can become hard to read. Consider refactoring to multiple separate Choose statements or using different conditions.

## Pattern: Validation with Choose

### Environment Validation

```csharp
.Choose(choose =>
{
    choose.When("'$(TargetFramework)' == ''", whenProps =>
    {
        whenProps.Error("TargetFramework property must be set");
    });
    
    choose.When("'$(Configuration)' == ''", whenProps =>
    {
        whenProps.Error("Configuration property must be set");
    });
    
    choose.When("!Exists('$(MSBuildProjectFile)')", whenProps =>
    {
        whenProps.Error("Project file not found at $(MSBuildProjectFile)");
    });
    
    choose.Otherwise(otherwiseProps =>
    {
        otherwiseProps.Message("Environment validated successfully", "Normal");
    });
})
```

## Choose vs PropertyGroup Conditions

### When to Use Choose

Use `Choose` when:

```csharp
// ✓ Multiple mutually exclusive branches
.Choose(choose =>
{
    choose.When("condition1", ...);
    choose.When("condition2", ...);
    choose.When("condition3", ...);
    choose.Otherwise(...);
})
```

### When to Use PropertyGroup Conditions

Use conditional `PropertyGroup` when:

```csharp
// ✓ Independent conditions that can overlap
.PropertyGroup("condition1", group => { ... })
.PropertyGroup("condition2", group => { ... })
.PropertyGroup("condition3", group => { ... })
```

## Debugging Choose

### Log Which Branch Executed

```csharp
.Choose(choose =>
{
    choose.When("'$(Configuration)' == 'Debug'", whenProps =>
    {
        whenProps.Message("Choose: Debug branch", "High");
        whenProps.Property("Optimize", "false");
    });
    
    choose.When("'$(Configuration)' == 'Release'", whenProps =>
    {
        whenProps.Message("Choose: Release branch", "High");
        whenProps.Property("Optimize", "true");
    });
    
    choose.Otherwise(otherwiseProps =>
    {
        otherwiseProps.Message("Choose: Otherwise branch (Configuration=$(Configuration))", "High");
        otherwiseProps.Property("Optimize", "false");
    });
})
```

Run with: `dotnet build -v:normal`

## Best Practices

### DO: Use Otherwise for Defaults

```csharp
// ✓ Provide fallback
.Choose(choose =>
{
    choose.When(..., ...);
    choose.When(..., ...);
    choose.Otherwise(otherwiseProps =>
    {
        otherwiseProps.Property("DefaultValue", "fallback");
    });
})
```

### DO: Order Conditions Logically

```csharp
// ✓ Most specific to least specific
.Choose(choose =>
{
    choose.When("'$(Configuration)' == 'Production'", ...);  // Most specific
    choose.When("'$(Configuration)' == 'Release'", ...);
    choose.When("'$(Configuration)' == 'Debug'", ...);
    choose.Otherwise(...);  // Catch-all
})
```

### DO: Keep Branches Similar in Structure

```csharp
// ✓ Consistent structure
.Choose(choose =>
{
    choose.When("...", whenProps =>
    {
        whenProps.Property("PropA", "value1");
        whenProps.Property("PropB", "value1");
    });
    
    choose.When("...", whenProps =>
    {
        whenProps.Property("PropA", "value2");
        whenProps.Property("PropB", "value2");
    });
})
```

### DON'T: Nest Deeply

```csharp
// ✗ Hard to follow
.Choose(choose =>
{
    choose.When("...", whenProps =>
    {
        whenProps.Choose(innerChoose =>
        {
            innerChoose.When("...", innerWhenProps =>
            {
                innerWhenProps.Choose(...);  // ✗ Too deep
            });
        });
    });
})

// ✓ Flatten with combined conditions
.Choose(choose =>
{
    choose.When("condition1 and condition2", ...);
    choose.When("condition1 and condition3", ...);
})
```

### DON'T: Use for Simple Binary Choice

```csharp
// ✗ Overkill for binary choice
.Choose(choose =>
{
    choose.When("'$(Debug)' == 'true'", ...);
    choose.Otherwise(...);
})

// ✓ Use conditional PropertyGroup
.PropertyGroup("'$(Debug)' == 'true'", ...)
.PropertyGroup("'$(Debug)' != 'true'", ...)
```

## Summary

| Concept | Description | Use For |
|---------|-------------|---------|
| `Choose` | Container for branching logic | Multi-way branching |
| `When` | Conditional branch | Specific conditions |
| `Otherwise` | Default/fallback branch | No conditions matched |
| Nested `Choose` | Choose within When | Complex scenarios (use sparingly) |

**Execution order:**
1. Evaluate `When` conditions sequentially
2. Execute first matching `When` branch
3. If no matches, execute `Otherwise`
4. If no `Otherwise`, skip

## Next Steps

- [Conditional Logic](../properties-items/conditionals.md) - Condition patterns
- [Multi-Target Framework](multi-tfm.md) - Framework-specific branching
- [Properties](../properties-items/properties.md) - Property definitions
- [Items](../properties-items/items.md) - Item collections
