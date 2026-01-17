# Troubleshooting Guide

This guide covers common issues, diagnostic techniques, and solutions when working with JD.MSBuild.Fluent.

## Installation Issues

### Issue: Package Not Found

**Symptoms:**

```
error NU1101: Unable to find package 'JD.MSBuild.Fluent'
```

**Causes:**
- NuGet.org not in package sources
- Network connectivity issues
- Typo in package name

**Solutions:**

1. **Verify package sources:**

```bash
dotnet nuget list source
```

2. **Add NuGet.org if missing:**

```bash
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

3. **Clear NuGet caches:**

```bash
dotnet nuget locals all --clear
dotnet restore
```

### Issue: .NET SDK Version Mismatch

**Symptoms:**

```
error NETSDK1045: The current .NET SDK does not support targeting .NET 8.0
```

**Causes:**
- Outdated .NET SDK
- Wrong SDK version selected

**Solutions:**

1. **Check SDK version:**

```bash
dotnet --version
```

2. **Install required SDK:** Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)

3. **Use global.json to specify SDK:**

```json
{
  "sdk": {
    "version": "8.0.100",
    "rollForward": "latestMinor"
  }
}
```

### Issue: CLI Tool Not Found

**Symptoms:**

```
'jdmsbuild' is not recognized as an internal or external command
```

**Causes:**
- CLI not installed
- .NET tools path not in PATH environment variable

**Solutions:**

1. **Install CLI:**

```bash
dotnet tool install --global JD.MSBuild.Fluent.Cli
```

2. **Add tools directory to PATH:**

**Windows:**

```powershell
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
```

**Linux/macOS:**

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
```

3. **Use local tool instead:**

```bash
dotnet tool install JD.MSBuild.Fluent.Cli
dotnet jdmsbuild --version
```

## Generation Issues

### Issue: Assembly Load Failed

**Symptoms:**

```
Error: Could not load assembly at path 'MyPackage.dll'
```

**Causes:**
- File doesn't exist
- File path incorrect
- Permissions issue

**Solutions:**

1. **Verify file exists:**

```bash
ls -la path/to/MyPackage.dll  # Linux/macOS
dir path\to\MyPackage.dll      # Windows
```

2. **Check file permissions:**

```bash
chmod +r path/to/MyPackage.dll  # Linux/macOS
```

3. **Use absolute path:**

```bash
jdmsbuild generate --assembly "$(pwd)/bin/Release/net8.0/MyPackage.dll" --type Factory --method Create --output ./output
```

### Issue: Type Not Found

**Symptoms:**

```
Error: Type 'MyPackage.Factory' not found in assembly
```

**Causes:**
- Type name incorrect
- Type not public
- Namespace missing

**Solutions:**

1. **Use fully-qualified type name:**

```bash
--type "MyCompany.MyPackage.PackageFactory"
```

2. **Verify type is public:**

```csharp
// ✓ Correct
public static class PackageFactory { ... }

// ✗ Wrong
internal static class PackageFactory { ... }
```

3. **List types in assembly:**

```bash
# .NET reflection tool or ILSpy to inspect assembly
```

### Issue: Method Not Found or Invalid

**Symptoms:**

```
Error: Method 'Create' not found on type 'Factory'
Error: Method 'Create' does not return PackageDefinition
```

**Causes:**
- Method name incorrect
- Method not public static
- Method has parameters
- Wrong return type

**Solutions:**

**Verify method signature:**

```csharp
// ✓ Correct
public static PackageDefinition Create()
{
    return Package.Define("MyPackage").Build();
}

// ✗ Wrong - not static
public PackageDefinition Create() { ... }

// ✗ Wrong - has parameters
public static PackageDefinition Create(string id) { ... }

// ✗ Wrong - wrong return type
public static string Create() { ... }
```

### Issue: Generated Files Missing

**Symptoms:**

- No files in output directory
- Some expected files missing

**Causes:**
- Output path incorrect
- Packaging options not set correctly
- File write permissions

**Solutions:**

1. **Verify output directory:**

```bash
ls -la artifacts/msbuild/build/  # Check generated files
```

2. **Check packaging options:**

```csharp
.Pack(options =>
{
    options.BuildTransitive = true;  // Enable buildTransitive/
    options.EmitSdk = true;          // Enable Sdk/
})
```

3. **Use verbose mode:**

```bash
jdmsbuild generate --assembly MyPackage.dll --type Factory --method Create --output ./output --verbose
```

## Runtime Issues

### Issue: Properties Not Being Set

**Symptoms:**

- Properties defined in `.props` file don't have values
- `$(MyProperty)` evaluates to empty

**Causes:**
- Import order incorrect
- Property overridden later
- Condition not met

**Solutions:**

1. **Check import order:**

MSBuild imports `build/{PackageId}.props` early. Verify your package ID matches.

2. **Debug property values:**

```csharp
.Targets(t => t
    .Target("DebugProperties", target => target
        .Message("MyProperty = $(MyProperty)", "High")))
```

Run: `dotnet build -t:DebugProperties`

3. **Check conditions:**

```csharp
// Ensure condition is met
.Property("MyProp", "value", "'$(Configuration)' == 'Release'")
```

Build with: `dotnet build -c Release`

### Issue: Targets Not Executing

**Symptoms:**

- Targets defined in `.targets` file don't run
- No log messages from targets

**Causes:**
- Condition not met
- Target not in dependency chain
- Import order incorrect

**Solutions:**

1. **Verify target conditions:**

```csharp
.Target("MyTarget", target => target
    .Condition("'$(MyPackageEnabled)' == 'true'")  // Check this condition
    .Message("Target executing"))
```

2. **Check execution order:**

```csharp
.Target("MyTarget", target => target
    .BeforeTargets("Build")  // Ensure proper hook
    .Message("Target should run before Build"))
```

3. **Enable detailed logging:**

```bash
dotnet build -v:detailed | grep "MyTarget"
```

### Issue: Item Not Included

**Symptoms:**

- Items defined in package not appearing in consuming project
- `@(MyItem)` is empty

**Causes:**
- Item condition not met
- Item removed by project
- Wrong item operation

**Solutions:**

1. **Debug item values:**

```csharp
.Targets(t => t
    .Target("DebugItems", target => target
        .Message("MyItem count: @(MyItem->Count())", "High")
        .Message("MyItem values: @(MyItem)", "High")))
```

2. **Check item conditions:**

```csharp
.Item("MyItem", MsBuildItemOperation.Include, "spec", 
    condition: "'$(Configuration)' == 'Debug'")
```

3. **Verify operation type:**

```csharp
// Correct for adding items
.Item("MyItem", MsBuildItemOperation.Include, "files/*.txt")

// Don't use Remove in package props
// .Item("MyItem", MsBuildItemOperation.Remove, "...")  // ✗
```

## NuGet Package Issues

### Issue: MSBuild Files Not Included in Package

**Symptoms:**

- `.props` or `.targets` files missing from `.nupkg`
- Package installs but doesn't import MSBuild files

**Causes:**
- Files not included in package
- Wrong PackagePath

**Solutions:**

1. **Include generated files in package:**

```xml
<ItemGroup>
  <None Include="artifacts/msbuild/build/**/*.*" Pack="true" PackagePath="build" />
  <None Include="artifacts/msbuild/buildTransitive/**/*.*" Pack="true" PackagePath="buildTransitive" />
</ItemGroup>
```

2. **Inspect package contents:**

```bash
unzip -l bin/Debug/MyPackage.1.0.0.nupkg | grep build
```

3. **Generate before packing:**

```xml
<Target Name="GenerateAssets" BeforeTargets="Pack">
  <Exec Command="jdmsbuild generate ..." />
</Target>
```

### Issue: MSBuild Files Not Auto-Imported

**Symptoms:**

- Package installed successfully
- MSBuild files in package
- Properties/targets not taking effect

**Causes:**
- Filename doesn't match package ID
- Files in wrong folder

**Solutions:**

1. **Verify filename matches package ID:**

```
build/
├── MyPackage.props      ← Must match <PackageId>
└── MyPackage.targets    ← Must match <PackageId>
```

2. **Check PackageId in .csproj:**

```xml
<PropertyGroup>
  <PackageId>MyPackage</PackageId>  <!-- Must match filename -->
</PropertyGroup>
```

3. **Verify folder structure:**

```bash
unzip -l MyPackage.1.0.0.nupkg
# Should show: build/MyPackage.props
#             build/MyPackage.targets
```

## Build Performance Issues

### Issue: Slow Builds

**Symptoms:**

- Builds take longer after adding package
- Targets run every time even when not needed

**Causes:**
- No incremental build support
- Targets without inputs/outputs
- Expensive operations on every build

**Solutions:**

1. **Add Inputs/Outputs:**

```csharp
.Target("ProcessFiles", target => target
    .Inputs("@(InputFile)")
    .Outputs("@(InputFile->'$(OutputPath)%(Filename).processed')")
    // MSBuild skips if outputs are up-to-date
    .Task("ProcessFiles", ...))
```

2. **Add conditions:**

```csharp
.Target("ExpensiveTarget", target => target
    .Condition("'$(RunExpensiveTarget)' == 'true'")  // Only when needed
    .Task(...))
```

3. **Profile build:**

```bash
dotnet build -v:detailed -bl:msbuild.binlog
# Open msbuild.binlog in MSBuild Structured Log Viewer
```

## Diagnostic Tools

### MSBuild Structured Log Viewer

View detailed build logs:

1. **Generate binary log:**

```bash
dotnet build -bl:msbuild.binlog
```

2. **Install viewer:**

```bash
dotnet tool install --global MSBuildStructuredLogViewer
```

3. **Open log:**

```bash
StructuredLogViewer msbuild.binlog
```

### MSBuild Verbosity Levels

Increase logging detail:

```bash
dotnet build -v:q       # Quiet
dotnet build -v:m       # Minimal (default)
dotnet build -v:n       # Normal
dotnet build -v:d       # Detailed
dotnet build -v:diag    # Diagnostic (most verbose)
```

### Property Dump Target

Create a target to dump all properties:

```csharp
.Target("DumpAllProperties", target => target
    .Message("Configuration: $(Configuration)", "High")
    .Message("Platform: $(Platform)", "High")
    .Message("TargetFramework: $(TargetFramework)", "High")
    .Message("OutputPath: $(OutputPath)", "High")
    .Message("IntermediateOutputPath: $(IntermediateOutputPath)", "High")
    .Message("MyPackageEnabled: $(MyPackageEnabled)", "High")
    // Add all your custom properties
    )
```

Run: `dotnet build -t:DumpAllProperties`

## Getting Help

### Check Documentation

- [Installation Guide](../getting-started/installation.md)
- [First Package](../getting-started/first-package.md)
- [Package Structure](../core-concepts/package-structure.md)
- [CLI Reference](../cli/index.md)

### Report Issues

If you encounter a bug:

1. **Search existing issues:** [GitHub Issues](https://github.com/jasondown/JD.MSBuild.Fluent/issues)

2. **Create new issue with:**
   - JD.MSBuild.Fluent version
   - .NET SDK version
   - Operating system
   - Minimal reproduction code
   - Complete error message
   - Steps to reproduce

3. **Include diagnostic information:**

```bash
dotnet --info
jdmsbuild --version
dotnet build -v:diag > build.log 2>&1
```

### Community Support

- **GitHub Discussions:** Ask questions and share ideas
- **Stack Overflow:** Tag questions with `jd-msbuild-fluent`

## Summary

Common issues and quick solutions:

| Issue | Quick Fix |
|-------|-----------|
| Package not found | `dotnet nuget locals all --clear && dotnet restore` |
| CLI not found | Add .NET tools to PATH |
| Type not found | Use fully-qualified type name |
| Properties not set | Check import order and conditions |
| Targets not executing | Check conditions and BeforeTargets/AfterTargets |
| Files not in package | Include generated files with Pack="true" |
| Slow builds | Add Inputs/Outputs to targets |

## Next Steps

- [Working with Properties](../properties-items/properties.md) - Property troubleshooting
- [Target Orchestration](../targets-tasks/orchestration.md) - Target debugging
- [Package Structure](../core-concepts/package-structure.md) - Understanding NuGet layout
