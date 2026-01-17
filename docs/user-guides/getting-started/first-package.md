# Creating Your First Package

This comprehensive tutorial guides you through creating a complete MSBuild package using JD.MSBuild.Fluent, from initial setup to generating and testing the final assets.

## What You'll Build

By the end of this tutorial, you'll have created a functional MSBuild package called **MyCompany.Build** that:

- Defines custom properties for versioning and feature flags
- Implements a pre-build target that validates project configuration
- Executes a post-build target that generates a build manifest
- Supports both direct and transitive dependency scenarios
- Generates clean, deterministic MSBuild XML

## Prerequisites

- Completed [Installation](installation.md)
- Basic C# knowledge
- Understanding of MSBuild concepts (properties, targets, tasks)

## Step 1: Create the Project

Create a new class library for your build definitions:

```bash
mkdir MyCompany.Build.Definitions
cd MyCompany.Build.Definitions
dotnet new classlib
dotnet add package JD.MSBuild.Fluent
```

## Step 2: Define the Package Factory

Create a static factory method that returns a `PackageDefinition`:

**PackageFactory.cs:**

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace MyCompany.Build;

public static class PackageFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("MyCompany.Build")
            .Description("Custom MSBuild package for MyCompany projects")
            .Props(ConfigureProperties)
            .Targets(ConfigureTargets)
            .Pack(ConfigurePackaging)
            .Build();
    }

    private static void ConfigureProperties(PropsBuilder props)
    {
        // We'll implement this next
    }

    private static void ConfigureTargets(TargetsBuilder targets)
    {
        // We'll implement this next
    }

    private static void ConfigurePackaging(PackagePackagingOptions options)
    {
        // We'll implement this next
    }
}
```

### Understanding the Structure

- **Package.Define(id)**: Starts building a package with the specified NuGet package ID
- **Description()**: Sets package metadata
- **Props()**: Configures evaluation-time constructs (properties, items)
- **Targets()**: Configures execution-time constructs (targets, tasks)
- **Pack()**: Controls output structure (build/, buildTransitive/, Sdk/)
- **Build()**: Returns the final `PackageDefinition`

## Step 3: Configure Properties

Properties are evaluated before targets execute. Define default values and conditional properties:

```csharp
private static void ConfigureProperties(PropsBuilder props)
{
    // Default property values
    props.Property("MyCompanyBuildEnabled", "true");
    props.Property("MyCompanyBuildVersion", "2.0.0");
    props.Property("MyCompanyOutputPath", "$(MSBuildProjectDirectory)/bin/mycompany");

    // Conditional property group for Release builds
    props.PropertyGroup("'$(Configuration)' == 'Release'", group =>
    {
        group.Property("MyCompanyOptimize", "true");
        group.Property("MyCompanyGenerateManifest", "true");
    });

    // Conditional property group for Debug builds
    props.PropertyGroup("'$(Configuration)' == 'Debug'", group =>
    {
        group.Property("MyCompanyOptimize", "false");
        group.Property("MyCompanyGenerateManifest", "false");
        group.Property("MyCompanyVerboseLogging", "true");
    });

    // Platform-specific properties using Choose
    props.Choose(choose =>
    {
        choose.When("$([MSBuild]::IsOSPlatform('Windows'))", whenProps =>
        {
            whenProps.Property("MyCompanyPlatform", "win");
        });
        
        choose.When("$([MSBuild]::IsOSPlatform('Linux'))", whenProps =>
        {
            whenProps.Property("MyCompanyPlatform", "linux");
        });
        
        choose.Otherwise(otherwiseProps =>
        {
            otherwiseProps.Property("MyCompanyPlatform", "osx");
        });
    });
}
```

### Property Naming Conventions

Follow these conventions for property names:

- **Prefix with your package name**: `MyCompany` prevents collisions
- **Use PascalCase**: `MyCompanyBuildEnabled`, not `mycompany_build_enabled`
- **Be descriptive**: `GenerateManifest` is clearer than `GenMan`
- **Boolean properties**: Use `Enabled`/`Disabled` or `true`/`false` values

## Step 4: Configure Targets

Targets execute during the build process. Define targets that run before and after the Build target:

```csharp
private static void ConfigureTargets(TargetsBuilder targets)
{
    // Pre-build validation target
    targets.Target("MyCompany_ValidateConfiguration", target =>
    {
        target.BeforeTargets("Build");
        target.Condition("'$(MyCompanyBuildEnabled)' == 'true'");

        // Log build information
        target.Message(
            "MyCompany.Build v$(MyCompanyBuildVersion) - Configuration: $(Configuration), Platform: $(MyCompanyPlatform)",
            "High"
        );

        // Validate required properties
        target.PropertyGroup("'$(TargetFramework)' == ''", group =>
        {
            group.Property("_MyCompanyValidationError", "TargetFramework must be specified");
        });

        target.Error(
            "'$(_MyCompanyValidationError)' != ''",
            "$(_MyCompanyValidationError)"
        );

        // Create output directory
        target.Task("MakeDir", task =>
        {
            task.Param("Directories", "$(MyCompanyOutputPath)");
        });
    });

    // Main build target
    targets.Target("MyCompany_Build", target =>
    {
        target.DependsOnTargets("MyCompany_ValidateConfiguration");
        target.Condition("'$(MyCompanyBuildEnabled)' == 'true'");

        target.Message("Executing MyCompany build tasks...", "Normal");

        // Write build info file
        target.Task("WriteLinesToFile", task =>
        {
            task.Param("File", "$(MyCompanyOutputPath)/buildinfo.txt");
            task.Param("Lines", "Build Version: $(MyCompanyBuildVersion)");
            task.Param("Overwrite", "true");
            task.Param("Encoding", "UTF-8");
        });
    });

    // Post-build manifest generation target
    targets.Target("MyCompany_GenerateManifest", target =>
    {
        target.AfterTargets("Build");
        target.Condition("'$(MyCompanyGenerateManifest)' == 'true'");
        target.Inputs("@(Compile)");
        target.Outputs("$(MyCompanyOutputPath)/manifest.json");

        target.Message("Generating build manifest...", "Normal");

        // Generate JSON manifest
        target.Task("WriteLinesToFile", task =>
        {
            task.Param("File", "$(MyCompanyOutputPath)/manifest.json");
            task.Param("Lines", @"{
  ""version"": ""$(MyCompanyBuildVersion)"",
  ""configuration"": ""$(Configuration)"",
  ""platform"": ""$(MyCompanyPlatform)"",
  ""timestamp"": ""$([System.DateTime]::UtcNow.ToString('o'))""
}");
            task.Param("Overwrite", "true");
        });
    });

    // Clean target integration
    targets.Target("MyCompany_Clean", target =>
    {
        target.BeforeTargets("Clean");
        target.Condition("'$(MyCompanyBuildEnabled)' == 'true'");

        target.Message("Cleaning MyCompany build artifacts...", "Normal");

        target.Task("RemoveDir", task =>
        {
            task.Param("Directories", "$(MyCompanyOutputPath)");
        });
    });
}
```

### Target Best Practices

- **Name targets with a prefix**: `MyCompany_` prevents naming conflicts
- **Use appropriate dependencies**: `DependsOnTargets` ensures execution order
- **Add conditions**: Prevent unnecessary execution
- **Use incremental builds**: Specify `Inputs` and `Outputs` for caching
- **Log messages**: Help users understand what's happening
- **Integrate with standard targets**: `BeforeTargets` and `AfterTargets`

## Step 5: Configure Packaging

Control how MSBuild assets are emitted:

```csharp
private static void ConfigurePackaging(PackagePackagingOptions options)
{
    // Emit both build/ and buildTransitive/ folders
    options.BuildTransitive = true;

    // Don't emit SDK-style assets (we're not creating an SDK)
    options.EmitSdk = false;

    // Use the default basename (package ID)
    options.BuildAssetBasename = null;
}
```

### Packaging Options Explained

| Option | Description | When to Use |
|--------|-------------|-------------|
| `BuildTransitive = true` | Generates `buildTransitive/` folder | Settings should flow through transitive dependencies |
| `BuildTransitive = false` | Only generates `build/` folder | Direct consumers only |
| `EmitSdk = true` | Generates `Sdk/` folder | Creating MSBuild SDK packages |
| `EmitSdk = false` | No SDK assets | Standard NuGet packages with MSBuild logic |
| `BuildAssetBasename = "Custom"` | Override output filenames | Custom naming requirements |

## Step 6: Build the Project

Compile your definitions project:

```bash
dotnet build
```

Expected output:

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Step 7: Generate MSBuild Assets

Use the CLI tool to generate MSBuild XML from your definition:

```bash
jdmsbuild generate \
    --assembly bin/Debug/net8.0/MyCompany.Build.Definitions.dll \
    --type MyCompany.Build.PackageFactory \
    --method Create \
    --output ./artifacts/msbuild
```

Expected output:

```
Generating MSBuild assets from MyCompany.Build.PackageFactory.Create...
Generated: artifacts/msbuild/build/MyCompany.Build.props
Generated: artifacts/msbuild/build/MyCompany.Build.targets
Generated: artifacts/msbuild/buildTransitive/MyCompany.Build.props
Generated: artifacts/msbuild/buildTransitive/MyCompany.Build.targets
✓ Generation completed successfully
```

## Step 8: Inspect Generated Files

Examine the generated MSBuild XML:

**artifacts/msbuild/build/MyCompany.Build.props:**

```xml
<Project>
  <!-- Generated by JD.MSBuild.Fluent -->
  <PropertyGroup>
    <MyCompanyBuildEnabled>true</MyCompanyBuildEnabled>
    <MyCompanyBuildVersion>2.0.0</MyCompanyBuildVersion>
    <MyCompanyOutputPath>$(MSBuildProjectDirectory)/bin/mycompany</MyCompanyOutputPath>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <MyCompanyOptimize>true</MyCompanyOptimize>
    <MyCompanyGenerateManifest>true</MyCompanyGenerateManifest>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <MyCompanyOptimize>false</MyCompanyOptimize>
    <MyCompanyGenerateManifest>false</MyCompanyGenerateManifest>
    <MyCompanyVerboseLogging>true</MyCompanyVerboseLogging>
  </PropertyGroup>
  <Choose>
    <When Condition="$([MSBuild]::IsOSPlatform('Windows'))">
      <PropertyGroup>
        <MyCompanyPlatform>win</MyCompanyPlatform>
      </PropertyGroup>
    </When>
    <When Condition="$([MSBuild]::IsOSPlatform('Linux'))">
      <PropertyGroup>
        <MyCompanyPlatform>linux</MyCompanyPlatform>
      </PropertyGroup>
    </When>
    <Otherwise>
      <PropertyGroup>
        <MyCompanyPlatform>osx</MyCompanyPlatform>
      </PropertyGroup>
    </Otherwise>
  </Choose>
</Project>
```

**artifacts/msbuild/build/MyCompany.Build.targets:**

```xml
<Project>
  <!-- Generated by JD.MSBuild.Fluent -->
  <Target Name="MyCompany_ValidateConfiguration" BeforeTargets="Build" Condition="'$(MyCompanyBuildEnabled)' == 'true'">
    <Message Text="MyCompany.Build v$(MyCompanyBuildVersion) - Configuration: $(Configuration), Platform: $(MyCompanyPlatform)" Importance="High" />
    <PropertyGroup Condition="'$(TargetFramework)' == ''">
      <_MyCompanyValidationError>TargetFramework must be specified</_MyCompanyValidationError>
    </PropertyGroup>
    <Error Condition="'$(_MyCompanyValidationError)' != ''" Text="$(_MyCompanyValidationError)" />
    <MakeDir Directories="$(MyCompanyOutputPath)" />
  </Target>
  <Target Name="MyCompany_Build" DependsOnTargets="MyCompany_ValidateConfiguration" Condition="'$(MyCompanyBuildEnabled)' == 'true'">
    <Message Text="Executing MyCompany build tasks..." Importance="Normal" />
    <WriteLinesToFile File="$(MyCompanyOutputPath)/buildinfo.txt" Lines="Build Version: $(MyCompanyBuildVersion)" Overwrite="true" Encoding="UTF-8" />
  </Target>
  <Target Name="MyCompany_GenerateManifest" AfterTargets="Build" Condition="'$(MyCompanyGenerateManifest)' == 'true'" Inputs="@(Compile)" Outputs="$(MyCompanyOutputPath)/manifest.json">
    <Message Text="Generating build manifest..." Importance="Normal" />
    <WriteLinesToFile File="$(MyCompanyOutputPath)/manifest.json" Lines="{&#xA;  &quot;version&quot;: &quot;$(MyCompanyBuildVersion)&quot;,&#xA;  &quot;configuration&quot;: &quot;$(Configuration)&quot;,&#xA;  &quot;platform&quot;: &quot;$(MyCompanyPlatform)&quot;,&#xA;  &quot;timestamp&quot;: &quot;$([System.DateTime]::UtcNow.ToString('o'))&quot;&#xA;}" Overwrite="true" />
  </Target>
  <Target Name="MyCompany_Clean" BeforeTargets="Clean" Condition="'$(MyCompanyBuildEnabled)' == 'true'">
    <Message Text="Cleaning MyCompany build artifacts..." Importance="Normal" />
    <RemoveDir Directories="$(MyCompanyOutputPath)" />
  </Target>
</Project>
```

### Key Observations

- **Canonical formatting**: Properties are alphabetically sorted within groups
- **Deterministic output**: Regenerating produces identical XML
- **Standard MSBuild**: No custom extensions required
- **Human-readable**: Comments and formatting aid understanding

## Step 9: Test Locally

Test your generated MSBuild assets with a sample project:

Create a test project:

```bash
mkdir ../TestProject
cd ../TestProject
dotnet new console
```

Reference your MSBuild package by importing the generated files:

**Directory.Build.props:**

```xml
<Project>
  <Import Project="../MyCompany.Build.Definitions/artifacts/msbuild/build/MyCompany.Build.props" />
</Project>
```

**Directory.Build.targets:**

```xml
<Project>
  <Import Project="../MyCompany.Build.Definitions/artifacts/msbuild/build/MyCompany.Build.targets" />
</Project>
```

Build the test project:

```bash
dotnet build -c Release
```

Expected output:

```
MyCompany.Build v2.0.0 - Configuration: Release, Platform: win
Executing MyCompany build tasks...
Generating build manifest...
Build succeeded.
```

Verify generated artifacts:

```bash
ls bin/mycompany/
# buildinfo.txt  manifest.json
```

## Step 10: Create a NuGet Package

Package your MSBuild assets for distribution:

**MyCompany.Build.Definitions.csproj:**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>MyCompany.Build</PackageId>
    <Version>2.0.0</Version>
    <Authors>MyCompany</Authors>
    <Description>Custom MSBuild package for MyCompany projects</Description>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="JD.MSBuild.Fluent" Version="1.0.0" />
  </ItemGroup>

  <!-- Include generated MSBuild assets in the package -->
  <ItemGroup>
    <None Include="artifacts/msbuild/build/**/*.*" Pack="true" PackagePath="build" />
    <None Include="artifacts/msbuild/buildTransitive/**/*.*" Pack="true" PackagePath="buildTransitive" />
  </ItemGroup>

  <!-- Generate assets before packing -->
  <Target Name="GenerateMSBuildAssets" BeforeTargets="Pack">
    <Exec Command="jdmsbuild generate --assembly $(TargetPath) --type MyCompany.Build.PackageFactory --method Create --output artifacts/msbuild" />
  </Target>
</Project>
```

Create the package:

```bash
dotnet pack
```

The generated `.nupkg` file contains:

```
MyCompany.Build.2.0.0.nupkg
├── build/
│   ├── MyCompany.Build.props
│   └── MyCompany.Build.targets
├── buildTransitive/
│   ├── MyCompany.Build.props
│   └── MyCompany.Build.targets
└── lib/net8.0/
    └── MyCompany.Build.Definitions.dll
```

## Testing the NuGet Package

Test the packaged version:

```bash
# Create a local NuGet feed
mkdir ../LocalFeed
cp bin/Debug/MyCompany.Build.2.0.0.nupkg ../LocalFeed/

# Create a test project
mkdir ../PackageTest
cd ../PackageTest
dotnet new console

# Add local feed and install package
dotnet nuget add source ../LocalFeed -n LocalFeed
dotnet add package MyCompany.Build -v 2.0.0

# Build and verify
dotnet build -c Release
```

## Common Issues and Solutions

### Issue: Properties Not Being Set

**Symptom**: Properties like `$(MyCompanyBuildEnabled)` are empty.

**Solution**: Ensure `.props` file is imported before your project content. MSBuild auto-imports `build/{PackageId}.props` early in the evaluation phase.

### Issue: Targets Not Executing

**Symptom**: Targets don't run during build.

**Solution**: Check target conditions and dependencies. Add diagnostic messages to verify execution:

```csharp
target.Message("Target executing: MyCompany_Build", "High");
```

### Issue: Generated Files Missing

**Symptom**: `dotnet pack` doesn't include MSBuild assets.

**Solution**: Verify the `<None Include=...>` items in your `.csproj` and that the `GenerateMSBuildAssets` target runs before packing.

## Next Steps

Congratulations! You've created a complete MSBuild package. Continue learning:

- [Intermediate Representation (IR)](../core-concepts/ir.md) - Understand the IR layer
- [Working with Properties](../properties-items/properties.md) - Advanced property patterns
- [Target Orchestration](../targets-tasks/orchestration.md) - Complex target dependencies
- [Strongly-Typed Helpers](../advanced/strongly-typed.md) - Type-safe property and target names

## Summary

You've learned how to:

- ✅ Create a build definitions project
- ✅ Define properties with conditional logic
- ✅ Implement targets with tasks
- ✅ Configure packaging options
- ✅ Generate MSBuild XML assets
- ✅ Test locally with sample projects
- ✅ Package for NuGet distribution

These skills form the foundation for authoring sophisticated MSBuild packages with JD.MSBuild.Fluent.
