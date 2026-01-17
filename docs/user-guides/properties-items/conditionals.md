# Conditional Logic

MSBuild's conditional logic enables dynamic build behavior based on properties, platform, configuration, and more. This guide covers condition syntax, Choose/When/Otherwise, and patterns for robust conditional logic.

## Condition Basics

### Condition Syntax

Conditions use **MSBuild expression syntax**:

```
'$(PropertyName)' == 'value'
'$(PropertyName)' != 'value'
Exists('path')
'%(ItemMetadata)' == 'value'
```

**Key rules:**
- Single quotes around property/item references and string literals
- Double equals (`==`) for equality, `!=` for inequality
- Logical operators: `and`, `or`, `not`
- Case-sensitive string comparisons by default

### Property Conditions

```csharp
.Property("MyProp", "value", "'$(Configuration)' == 'Release'")
```

**Generated XML:**

```xml
<PropertyGroup>
  <MyProp Condition="'$(Configuration)' == 'Release'">value</MyProp>
</PropertyGroup>
```

### Property Group Conditions

```csharp
.PropertyGroup("'$(Configuration)' == 'Debug'", group =>
{
    group.Property("DebugSymbols", "true");
    group.Property("Optimize", "false");
})
```

**Generated XML:**

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DebugSymbols>true</DebugSymbols>
  <Optimize>false</Optimize>
</PropertyGroup>
```

### Item Conditions

```csharp
.Item("Content", MsBuildItemOperation.Include, "debug.config", 
    condition: "'$(Configuration)' == 'Debug'")
```

**Generated XML:**

```xml
<ItemGroup>
  <Content Include="debug.config" Condition="'$(Configuration)' == 'Debug'" />
</ItemGroup>
```

### Target Conditions

```csharp
.Target("DebugTarget", target => target
    .Condition("'$(Configuration)' == 'Debug'")
    .Message("Running debug target"))
```

**Generated XML:**

```xml
<Target Name="DebugTarget" Condition="'$(Configuration)' == 'Debug'">
  <Message Text="Running debug target" />
</Target>
```

## Common Condition Patterns

### Configuration Checks

```csharp
// Debug configuration
"'$(Configuration)' == 'Debug'"

// Release configuration
"'$(Configuration)' == 'Release'"

// Not Debug
"'$(Configuration)' != 'Debug'"
```

### Platform Checks

```csharp
// Specific platforms
"'$(Platform)' == 'x64'"
"'$(Platform)' == 'x86'"
"'$(Platform)' == 'ARM64'"
"'$(Platform)' == 'AnyCPU'"

// OS detection
"$([MSBuild]::IsOSPlatform('Windows'))"
"$([MSBuild]::IsOSPlatform('Linux'))"
"$([MSBuild]::IsOSPlatform('OSX'))"
```

### Target Framework Checks

```csharp
// Specific framework
"'$(TargetFramework)' == 'net8.0'"
"'$(TargetFramework)' == 'net6.0'"
"'$(TargetFramework)' == 'netstandard2.0'"

// Framework family
"$(TargetFramework.StartsWith('net'))"
"$(TargetFramework.StartsWith('netstandard'))"
"$(TargetFramework.StartsWith('netcoreapp'))"
```

### Property Existence Checks

```csharp
// Property is empty/not set
"'$(MyProp)' == ''"

// Property is set (has value)
"'$(MyProp)' != ''"

// Property equals specific value
"'$(MyProp)' == 'ExpectedValue'"
```

### File Existence Checks

```csharp
// File exists
"Exists('$(MSBuildProjectDirectory)/config.json')"

// Directory exists
"Exists('$(MSBuildProjectDirectory)/bin')"

// File doesn't exist
"!Exists('path/to/file.txt')"
```

### Boolean Properties

```csharp
// Boolean true
"'$(MyBoolProp)' == 'true'"

// Boolean false
"'$(MyBoolProp)' == 'false'"

// Boolean not true
"'$(MyBoolProp)' != 'true'"
```

## Logical Operators

### AND Operator

Multiple conditions must all be true:

```csharp
.PropertyGroup("'$(Configuration)' == 'Release' AND '$(Platform)' == 'x64'", group =>
{
    group.Property("OptimizeForX64", "true");
})
```

**Alternative syntax with `and`:**

```csharp
"'$(Configuration)' == 'Release' and '$(Platform)' == 'x64'"
```

### OR Operator

At least one condition must be true:

```csharp
.PropertyGroup("'$(Platform)' == 'x64' OR '$(Platform)' == 'ARM64'", group =>
{
    group.Property("Use64BitTools", "true");
})
```

**Alternative syntax with `or`:**

```csharp
"'$(Platform)' == 'x64' or '$(Platform)' == 'ARM64'"
```

### NOT Operator

Negate a condition:

```csharp
.PropertyGroup("!('$(Configuration)' == 'Debug')", group =>
{
    group.Property("NonDebugMode", "true");
})
```

### Complex Conditions

Combine operators with parentheses:

```csharp
"('$(Configuration)' == 'Release' or '$(Configuration)' == 'Production') and '$(Platform)' == 'x64'"
```

## Choose/When/Otherwise

### Basic Choose Structure

Choose the first matching `When` clause:

```csharp
.Choose(choose =>
{
    choose.When("'$(Configuration)' == 'Debug'", whenProps =>
    {
        whenProps.Property("LogLevel", "Verbose");
        whenProps.Property("EnableDiagnostics", "true");
    });
    
    choose.When("'$(Configuration)' == 'Release'", whenProps =>
    {
        whenProps.Property("LogLevel", "Minimal");
        whenProps.Property("EnableDiagnostics", "false");
    });
    
    choose.Otherwise(otherwiseProps =>
    {
        otherwiseProps.Property("LogLevel", "Normal");
        whenProps.Property("EnableDiagnostics", "false");
    });
})
```

**Generated XML:**

```xml
<Choose>
  <When Condition="'$(Configuration)' == 'Debug'">
    <PropertyGroup>
      <LogLevel>Verbose</LogLevel>
      <EnableDiagnostics>true</EnableDiagnostics>
    </PropertyGroup>
  </When>
  <When Condition="'$(Configuration)' == 'Release'">
    <PropertyGroup>
      <LogLevel>Minimal</LogLevel>
      <EnableDiagnostics>false</EnableDiagnostics>
    </PropertyGroup>
  </When>
  <Otherwise>
    <PropertyGroup>
      <LogLevel>Normal</LogLevel>
      <EnableDiagnostics>false</EnableDiagnostics>
    </PropertyGroup>
  </Otherwise>
</Choose>
```

### Platform-Specific Choose

```csharp
.Choose(choose =>
{
    choose.When("$([MSBuild]::IsOSPlatform('Windows'))", whenProps =>
    {
        whenProps.Property("NativeLib", "mylib.dll");
        whenProps.Property("PathSeparator", ";");
    });
    
    choose.When("$([MSBuild]::IsOSPlatform('Linux'))", whenProps =>
    {
        whenProps.Property("NativeLib", "libmylib.so");
        whenProps.Property("PathSeparator", ":");
    });
    
    choose.When("$([MSBuild]::IsOSPlatform('OSX'))", whenProps =>
    {
        whenProps.Property("NativeLib", "libmylib.dylib");
        whenProps.Property("PathSeparator", ":");
    });
})
```

### Target Framework Choose

```csharp
.Choose(choose =>
{
    choose.When("$(TargetFramework.StartsWith('net6'))", whenProps =>
    {
        whenProps.Property("UseNet6Features", "true");
    });
    
    choose.When("$(TargetFramework.StartsWith('net8'))", whenProps =>
    {
        whenProps.Property("UseNet8Features", "true");
    });
    
    choose.When("$(TargetFramework.StartsWith('netstandard'))", whenProps =>
    {
        whenProps.Property("UseNetStandardCompatibility", "true");
    });
})
```

### Choose with Items

```csharp
.Choose(choose =>
{
    choose.When("'$(IncludeTests)' == 'true'", whenProps =>
    {
        whenProps.ItemGroup(null, group =>
        {
            group.Include("Compile", "test/**/*.cs");
            group.Include("Content", "test/**/*.json");
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

## Advanced Condition Patterns

### Pattern: Default Property Values

Provide defaults that can be overridden:

```csharp
.Property("MyPackageEnabled", "true", "'$(MyPackageEnabled)' == ''")
.Property("MyPackageVersion", "1.0.0", "'$(MyPackageVersion)' == ''")
.Property("MyPackageVerbose", "false", "'$(MyPackageVerbose)' == ''")
```

**Behavior:**
- If property already set → Keep existing value
- If property empty/unset → Use default

### Pattern: Configuration Cascading

Set base values, then override for specific configurations:

```csharp
.PropertyGroup(null, group =>
{
    // Base values for all configurations
    group.Property("Optimize", "false");
    group.Property("DebugType", "full");
    group.Property("TreatWarningsAsErrors", "false");
})

.PropertyGroup("'$(Configuration)' == 'Release'", group =>
{
    // Override for Release
    group.Property("Optimize", "true");
    group.Property("DebugType", "pdbonly");
    group.Property("TreatWarningsAsErrors", "true");
})
```

### Pattern: Feature Detection

Check for features before using them:

```csharp
.PropertyGroup("Exists('$(MSBuildProjectDirectory)/.git')", group =>
{
    group.Property("HasGitRepository", "true");
})

.PropertyGroup("'$(HasGitRepository)' == 'true'", group =>
{
    group.Property("SourceRevisionId", "$([System.IO.File]::ReadAllText('$(MSBuildProjectDirectory)/.git/HEAD').Trim())");
})
```

### Pattern: Environment-Based Configuration

Use environment variables in conditions:

```csharp
.PropertyGroup("'$(CI)' == 'true'", group =>
{
    group.Property("ContinuousIntegrationBuild", "true");
    group.Property("Deterministic", "true");
})

.PropertyGroup("'$(DOTNET_RUNNING_IN_CONTAINER)' == 'true'", group =>
{
    group.Property("ContainerBuild", "true");
})
```

### Pattern: Version Range Checks

Check version ranges:

```csharp
.Choose(choose =>
{
    choose.When("$([MSBuild]::VersionGreaterThanOrEquals('$(TargetFrameworkVersion)', '8.0'))", whenProps =>
    {
        whenProps.Property("UseModernFeatures", "true");
    });
    
    choose.Otherwise(otherwiseProps =>
    {
        otherwiseProps.Property("UseLegacyCompatibility", "true");
    });
})
```

## MSBuild Functions

### String Functions

```csharp
// StartsWith
"$(Property.StartsWith('prefix'))"

// EndsWith
"$(Property.EndsWith('.dll'))"

// Contains
"$(Property.Contains('substring'))"

// ToLower
"$(Property.ToLower()) == 'lowercase'"

// ToUpper
"$(Property.ToUpper()) == 'UPPERCASE'"

// Trim
"$(Property.Trim())"
```

### Math Functions

```csharp
// Comparison
"$([MSBuild]::ValueOrDefault('$(NumericProp)', '0')) > 5"

// Arithmetic (in property value, not condition)
.Property("Result", "$([MSBuild]::Add($(A), $(B)))")
```

### File System Functions

```csharp
// Path operations
"$([System.IO.Path]::GetFileName('$(FilePath)')) == 'expected.txt'"

// Directory operations
"$([System.IO.Directory]::Exists('$(FolderPath)'))"

// File operations
"$([System.IO.File]::Exists('$(FilePath)'))"
```

### DateTime Functions

```csharp
// Current year
"$([System.DateTime]::Now.Year) >= 2024"

// Date comparison
"$([System.DateTime]::Parse('$(BuildDate)')) > $([System.DateTime]::UtcNow.AddDays(-7))"
```

## Debugging Conditions

### Test Conditions with Messages

```csharp
.Targets(t => t
    .Target("TestConditions", target => target
        .Message("Configuration: $(Configuration)", "High")
        .Message("Platform: $(Platform)", "High")
        .Message("Is Windows: $([MSBuild]::IsOSPlatform('Windows'))", "High")
        .Message("Debug mode: $([MSBuild]::ValueOrDefault('$(IsDebug)', 'false'))", "High")))
```

### Conditional Warnings

```csharp
.Targets(t => t
    .Target("ValidateConfiguration", target => target
        .Warning("'$(Configuration)' == ''", "Configuration property is not set")
        .Warning("'$(TargetFramework)' == ''", "TargetFramework property is not set")))
```

## Best Practices

### DO: Use Meaningful Conditions

```csharp
// ✓ Clear intent
"'$(Configuration)' == 'Release' AND '$(Platform)' == 'x64'"

// ✗ Unclear
"'$(C)' == 'R' AND '$(P)' == 'x64'"
```

### DO: Check Property Existence

```csharp
// ✓ Safe default
.Property("MyProp", "default", "'$(MyProp)' == ''")

// ✗ Assumes property exists
.Property("MyProp", "$(MyProp)ExtendedValue")  // Breaks if MyProp not set
```

### DO: Use Exists() for Files

```csharp
// ✓ Check before importing
.Import("Custom.props", "Exists('Custom.props')")

// ✗ Import unconditionally (causes error if missing)
.Import("Custom.props")
```

### DON'T: Overcomplicate Conditions

```csharp
// ✓ Simple and readable
.Choose(choose =>
{
    choose.When("'$(Platform)' == 'x64'", ...);
    choose.When("'$(Platform)' == 'x86'", ...);
    choose.Otherwise(...);
})

// ✗ Complex nested logic
.PropertyGroup("(('$(A)' == '1' and '$(B)' == '2') or ('$(C)' == '3' and '$(D)' == '4')) and !('$(E)' == '5')", ...)
```

### DON'T: Compare Booleans to Strings Inconsistently

```csharp
// ✓ Consistent
"'$(MyBool)' == 'true'"
"'$(MyBool)' == 'false'"

// ✗ Inconsistent
"$(MyBool)"  // Ambiguous - what values are truthy?
"'$(MyBool)' == '1'"  // Not idiomatic
```

## Summary

| Pattern | Condition Syntax |
|---------|------------------|
| Property equality | `'$(Prop)' == 'value'` |
| Property inequality | `'$(Prop)' != 'value'` |
| Property empty | `'$(Prop)' == ''` |
| Property not empty | `'$(Prop)' != ''` |
| File exists | `Exists('path')` |
| OS platform | `$([MSBuild]::IsOSPlatform('Windows'))` |
| Logical AND | `condition1 and condition2` |
| Logical OR | `condition1 or condition2` |
| Logical NOT | `!(condition)` |

## Next Steps

- [Choose/When/Otherwise](../advanced/choose.md) - Deep dive into Choose constructs
- [Target Orchestration](../targets-tasks/orchestration.md) - Conditional target execution
- [Multi-Target Framework](../advanced/multi-tfm.md) - Framework-specific logic
- [MSBuild Conditions (Microsoft Docs)](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-conditions)
