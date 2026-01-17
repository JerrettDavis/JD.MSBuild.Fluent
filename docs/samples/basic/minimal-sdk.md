# Minimal SDK Package

This sample demonstrates the fundamental concepts of JD.MSBuild.Fluent by creating a minimal but complete SDK-style package.

## Scenario

You want to create a reusable MSBuild SDK package that:
- Defines build-time properties
- Hooks into the build pipeline with custom targets
- Flows its behavior to transitive dependencies
- Can be consumed as both a regular package reference and an SDK

This is the foundation for any MSBuild package you'll create with JD.MSBuild.Fluent.

## What It Demonstrates

✅ **Core Concepts:**
- Package definition with `Package.Define()`
- Props vs Targets separation (evaluation vs execution)
- Property definition with conditions
- Target creation with build hooks
- Task invocation (Message, WriteLinesToFile)
- Package emission options (SDK, BuildTransitive)

✅ **Best Practices:**
- Proper file organization
- MSBuild property naming conventions
- Conditional property evaluation
- Target naming and hooks
- Output folder configuration

## File Structure

```
MinimalSdkPackage/
├── MinimalSdkPackage.csproj          # Definition project
├── DefinitionFactory.cs              # Package definition
├── MinimalSdkPackage.Tests/          # Unit tests
│   ├── MinimalSdkPackage.Tests.csproj
│   └── FactoryTests.cs
└── TestConsumer/                      # Example consumer
    ├── TestConsumer.csproj
    └── Program.cs
```

## Complete Implementation

### DefinitionFactory.cs

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace MinimalSdkPackage;

/// <summary>
/// Factory for generating the MinimalSdkPackage MSBuild assets.
/// This demonstrates the minimal surface area for a functional SDK package.
/// </summary>
public static class DefinitionFactory
{
  /// <summary>
  /// Creates the package definition.
  /// This method is invoked by the CLI to generate MSBuild XML.
  /// </summary>
  public static PackageDefinition Create()
  {
    return Package.Define("MinimalSdkPackage")
      // Optional but recommended: provide a description for package metadata
      .Description("A minimal SDK-style package demonstrating JD.MSBuild.Fluent fundamentals")
      
      // PROPS: Evaluation-time constructs
      // These are processed when MSBuild loads the project, before any targets run
      .Props(p => p
        // Define a feature flag property (common pattern)
        .Property("MinimalSdkPackageEnabled", "true")
        
        // Conditional property based on Configuration (demonstrates conditions)
        .PropertyGroup("'$(Configuration)' == 'Release'", g => g
          .Property("DefineConstants", "$(DefineConstants);MINIMAL_SDK_RELEASE")
          .Comment("Add a compiler constant in Release builds")))
      
      // TARGETS: Execution-time constructs
      // These define what happens during the build process
      .Targets(t => t
        // Define a custom target that hooks into the build
        .Target("MinimalSdkPackage_Hello", tgt => tgt
          // Run before the Build target (early in the pipeline)
          .BeforeTargets("Build")
          
          // Only run if the feature flag is enabled (respects user opt-out)
          .Condition("'$(MinimalSdkPackageEnabled)' == 'true'")
          
          // Display a message (useful for debugging and visibility)
          .Message("Hello from MinimalSdkPackage v$(MinimalSdkPackageVersion)")
          
          // Write to a file (demonstrates file-based tasks)
          .Task("WriteLinesToFile", task => task
            .Param("File", "$(BaseIntermediateOutputPath)MinimalSdkPackage.txt")
            .Param("Lines", "Hello from MinimalSdkPackage")
            .Param("Overwrite", "true")
            .Param("Encoding", "UTF-8"))))
      
      // PACKAGING: Control how assets are emitted
      .Pack(o =>
      {
        // Emit SDK-style layout (Sdk/MinimalSdkPackage/Sdk.props and Sdk.targets)
        // Enables consumers to use: <Project Sdk="MinimalSdkPackage">
        o.EmitSdk = true;
        
        // Emit buildTransitive folder (props/targets flow to transitive dependencies)
        // Critical for infrastructure packages that should affect the entire dependency tree
        o.BuildTransitive = true;
      })
      
      // Build the definition (immutable after this point)
      .Build();
  }
}
```

### MinimalSdkPackage.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    
    <!-- Package metadata -->
    <PackageId>MinimalSdkPackage</PackageId>
    <Version>1.0.0</Version>
    <Authors>Your Name</Authors>
    <Description>A minimal SDK-style MSBuild package</Description>
    <PackageTags>msbuild;sdk;build-tools</PackageTags>
    
    <!-- Development-only dependency -->
    <DevelopmentDependency>true</DevelopmentDependency>
    <IncludeBuildOutput>false</IncludeBuildOutput>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="JD.MSBuild.Fluent" Version="1.0.0" />
  </ItemGroup>

  <!-- Include generated MSBuild assets in the package -->
  <ItemGroup>
    <None Include="$(ArtifactsDir)msbuild/**/*" Pack="true" PackagePath="" />
  </ItemGroup>
</Project>
```

### FactoryTests.cs

```csharp
using JD.MSBuild.Fluent.Render;
using Xunit;

namespace MinimalSdkPackage.Tests;

public class FactoryTests
{
  [Fact]
  public void Create_Returns_Valid_Definition()
  {
    // Act
    var def = DefinitionFactory.Create();
    
    // Assert
    Assert.Equal("MinimalSdkPackage", def.Id);
    Assert.NotNull(def.Description);
    Assert.NotEmpty(def.Props.PropertyGroups);
    Assert.NotEmpty(def.Targets.Targets);
  }
  
  [Fact]
  public void Props_Contains_Enabled_Property()
  {
    // Arrange
    var def = DefinitionFactory.Create();
    
    // Act
    var props = def.Props.PropertyGroups
      .SelectMany(g => g.Properties)
      .ToList();
    
    // Assert
    Assert.Contains(props, p => p.Name == "MinimalSdkPackageEnabled" && p.Value == "true");
  }
  
  [Fact]
  public void Props_Contains_Release_Condition()
  {
    // Arrange
    var def = DefinitionFactory.Create();
    
    // Act
    var releaseGroups = def.Props.PropertyGroups
      .Where(g => g.Condition?.Contains("Release") == true)
      .ToList();
    
    // Assert
    Assert.NotEmpty(releaseGroups);
  }
  
  [Fact]
  public void Targets_Contains_Hello_Target()
  {
    // Arrange
    var def = DefinitionFactory.Create();
    
    // Act
    var targets = def.Targets.Targets;
    
    // Assert
    Assert.Contains(targets, t => t.Name == "MinimalSdkPackage_Hello");
  }
  
  [Fact]
  public void Hello_Target_Has_BeforeTargets()
  {
    // Arrange
    var def = DefinitionFactory.Create();
    var target = def.Targets.Targets.First(t => t.Name == "MinimalSdkPackage_Hello");
    
    // Assert
    Assert.Equal("Build", target.BeforeTargets);
  }
  
  [Fact]
  public void Packaging_Enables_Sdk_And_Transitive()
  {
    // Arrange
    var def = DefinitionFactory.Create();
    
    // Assert
    Assert.True(def.Packaging.EmitSdk);
    Assert.True(def.Packaging.BuildTransitive);
  }
  
  [Fact]
  public void Generated_Props_Is_Valid_Xml()
  {
    // Arrange
    var def = DefinitionFactory.Create();
    var renderer = new MsBuildXmlRenderer();
    
    // Act
    var xml = renderer.Render(def.Props);
    
    // Assert
    Assert.StartsWith("<?xml version", xml);
    Assert.Contains("<Project", xml);
    Assert.Contains("MinimalSdkPackageEnabled", xml);
  }
  
  [Fact]
  public void Generated_Targets_Is_Valid_Xml()
  {
    // Arrange
    var def = DefinitionFactory.Create();
    var renderer = new MsBuildXmlRenderer();
    
    // Act
    var xml = renderer.Render(def.Targets);
    
    // Assert
    Assert.StartsWith("<?xml version", xml);
    Assert.Contains("<Target Name=\"MinimalSdkPackage_Hello\"", xml);
    Assert.Contains("WriteLinesToFile", xml);
  }
}
```

## Configuration

### Generate MSBuild Assets

```bash
# Build the definition project
dotnet build MinimalSdkPackage.csproj

# Generate MSBuild XML files
jdmsbuild generate \
  --assembly bin/Debug/net9.0/MinimalSdkPackage.dll \
  --type MinimalSdkPackage.DefinitionFactory \
  --method Create \
  --output artifacts/msbuild

# Verify output
ls artifacts/msbuild
```

### Expected Output Structure

```
artifacts/msbuild/
├── build/
│   ├── MinimalSdkPackage.props
│   └── MinimalSdkPackage.targets
├── buildTransitive/
│   ├── MinimalSdkPackage.props
│   └── MinimalSdkPackage.targets
└── Sdk/
    └── MinimalSdkPackage/
        ├── Sdk.props
        └── Sdk.targets
```

### Generated MinimalSdkPackage.props

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <MinimalSdkPackageEnabled>true</MinimalSdkPackageEnabled>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <!-- Add a compiler constant in Release builds -->
    <DefineConstants>$(DefineConstants);MINIMAL_SDK_RELEASE</DefineConstants>
  </PropertyGroup>
</Project>
```

### Generated MinimalSdkPackage.targets

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <Target Name="MinimalSdkPackage_Hello" 
          BeforeTargets="Build" 
          Condition="'$(MinimalSdkPackageEnabled)' == 'true'">
    <Message Text="Hello from MinimalSdkPackage v$(MinimalSdkPackageVersion)" Importance="High" />
    <WriteLinesToFile File="$(BaseIntermediateOutputPath)MinimalSdkPackage.txt"
                      Lines="Hello from MinimalSdkPackage"
                      Overwrite="true"
                      Encoding="UTF-8" />
  </Target>
</Project>
```

## Testing Strategy

### Unit Tests

Test the definition itself (fast, no MSBuild required):

```bash
dotnet test MinimalSdkPackage.Tests
```

### Integration Tests

Test the generated package in a real project:

```bash
# Pack the package locally
dotnet pack MinimalSdkPackage.csproj -o packages/

# Create a test consumer
dotnet new console -n TestConsumer
cd TestConsumer

# Add local package source
dotnet nuget add source ../packages -n local

# Reference the package
dotnet add package MinimalSdkPackage --version 1.0.0

# Build and observe output
dotnet build -v:normal
```

**Expected output:**
```
Hello from MinimalSdkPackage v1.0.0
```

Check for the generated file:
```bash
cat obj/Debug/net9.0/MinimalSdkPackage.txt
```

### SDK-Style Consumption

Test as an SDK (alternative consumption model):

```xml
<!-- TestConsumer.csproj -->
<Project Sdk="Microsoft.NET.Sdk;MinimalSdkPackage">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>
```

```bash
dotnet build
```

## Deployment

### Package for NuGet

```bash
# Generate assets
jdmsbuild generate \
  --assembly bin/Release/net9.0/MinimalSdkPackage.dll \
  --type MinimalSdkPackage.DefinitionFactory \
  --method Create \
  --output artifacts/msbuild

# Pack
dotnet pack -c Release

# Publish
dotnet nuget push bin/Release/MinimalSdkPackage.1.0.0.nupkg -s https://api.nuget.org/v3/index.json
```

### Consumption

```bash
dotnet add package MinimalSdkPackage
```

Or in `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="MinimalSdkPackage" Version="1.0.0" />
</ItemGroup>
```

## Best Practices Demonstrated

### ✅ Proper Naming
- Package ID matches namespace and file names
- Target names use `{PackageId}_` prefix to avoid conflicts
- Properties use `{PackageId}` prefix

### ✅ Feature Flags
- `MinimalSdkPackageEnabled` allows users to disable functionality
- Default value is `true` (opt-out, not opt-in)

### ✅ Conditional Logic
- Release-specific properties only apply in Release builds
- Target only runs when enabled

### ✅ Output Locations
- Uses `$(BaseIntermediateOutputPath)` for generated files (cleaned with `dotnet clean`)
- Avoids writing to `$(ProjectDir)` (source control pollution)

### ✅ Build Hooks
- Uses `BeforeTargets="Build"` (declarative, MSBuild manages order)
- Avoids hardcoded target order numbers

### ✅ Packaging
- `EmitSdk = true` enables SDK consumption
- `BuildTransitive = true` flows to transitive dependencies

## Architectural Decisions

### Why separate Props and Targets?

**Evaluation vs Execution:**
- Props run during project evaluation (before the build starts)
- Targets run during build execution (when the build is running)

**Impact:**
- Properties in Targets won't be available to the rest of the project
- Targets in Props will cause evaluation errors

### Why BeforeTargets instead of DependsOnTargets?

**Flexibility:**
- `BeforeTargets` hooks are less fragile than dependencies
- Multiple packages can hook `BeforeTargets="Build"` without conflicts
- MSBuild resolves the order automatically

### Why BuildTransitive?

**Transitive Dependencies:**
- Without `BuildTransitive`, only direct consumers see your package
- Infrastructure packages (like this) should affect the entire dependency tree
- Users expect consistent behavior regardless of reference depth

### Why write to BaseIntermediateOutputPath?

**Build Hygiene:**
- `obj/` folder is designed for build artifacts
- `dotnet clean` removes these files automatically
- Avoids polluting source control with generated files

## Troubleshooting

### Package not applying

**Check package restoration:**
```bash
dotnet restore --force
dotnet nuget locals all --clear
```

**Verify package layout:**
```bash
unzip -l packages/MinimalSdkPackage.1.0.0.nupkg | grep build/
```

**Enable diagnostic logging:**
```bash
dotnet build /v:diag > build.log
grep -i "MinimalSdkPackage" build.log
```

### Target not running

**Check the condition:**
```xml
<!-- Add to csproj to force enable -->
<PropertyGroup>
  <MinimalSdkPackageEnabled>true</MinimalSdkPackageEnabled>
</PropertyGroup>
```

**Verify target exists:**
```bash
dotnet msbuild /t:MinimalSdkPackage_Hello
```

### Properties not available

**Evaluation order matters:**
```xml
<!-- Props are evaluated top-to-bottom -->
<!-- Your package's props import early, your project's props come later -->
<!-- To override, set properties AFTER the package import -->
```

### File not generated

**Check output path:**
```bash
# See where MSBuild thinks it should be
dotnet build /p:MinimalSdkPackageEnabled=true /v:diag | grep BaseIntermediateOutputPath
```

**Verify task parameters:**
- `File` path must be valid
- Parent directory must exist (use `MakeDir` task if needed)

## Next Steps

- [Property Definition Package](properties.md) - Learn advanced property techniques
- [Simple Target Package](simple-target.md) - Deep dive into target creation
- [Database Build Integration](../real-world/database-build.md) - See a real-world application

## Related Documentation

- [PackageDefinition API](../../api/JD.MSBuild.Fluent.PackageDefinition.yml)
- [Package.Define() method](../../api/JD.MSBuild.Fluent.Fluent.Package.yml)
- [MSBuild Props and Targets](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-dot-props-and-dot-targets-files)
