# CLI Reference

The JD.MSBuild.Fluent Command-Line Interface (CLI) generates MSBuild XML assets from your fluent package definitions. This guide covers installation, commands, and usage patterns.

## Installation

### Global Installation

Install the CLI tool globally for use anywhere:

```bash
dotnet tool install --global JD.MSBuild.Fluent.Cli
```

### Local Installation

Install as a local tool in your repository:

```bash
# Create tool manifest if needed
dotnet new tool-manifest

# Install locally
dotnet tool install JD.MSBuild.Fluent.Cli
```

### Verify Installation

```bash
jdmsbuild --version
```

Expected output:

```
JD.MSBuild.Fluent CLI v1.0.0
```

## Commands

### generate

Generate MSBuild XML assets from a package definition.

**Syntax:**

```bash
jdmsbuild generate [options]
```

**Options:**

| Option | Description | Required |
|--------|-------------|----------|
| `--assembly <path>` | Path to assembly containing factory | Yes (unless `--example`) |
| `--type <name>` | Fully-qualified type name of factory class | Yes (unless `--example`) |
| `--method <name>` | Static method name that returns `PackageDefinition` | Yes (unless `--example`) |
| `--output <path>` | Output directory for generated MSBuild files | Yes |
| `--example` | Generate built-in example instead of loading assembly | No |
| `--verbose` | Enable verbose logging | No |
| `--help` | Show help information | No |

**Examples:**

Generate from compiled assembly:

```bash
jdmsbuild generate \
    --assembly bin/Release/net8.0/MyCompany.Build.dll \
    --type MyCompany.Build.PackageFactory \
    --method Create \
    --output artifacts/msbuild
```

Generate built-in example:

```bash
jdmsbuild generate --example --output artifacts/msbuild
```

Generate with verbose logging:

```bash
jdmsbuild generate \
    --assembly MyPackage.dll \
    --type MyPackage.Factory \
    --method Create \
    --output ./output \
    --verbose
```

### version

Display CLI version information:

```bash
jdmsbuild version
```

Or:

```bash
jdmsbuild --version
```

### help

Display help information:

```bash
jdmsbuild --help
```

Display help for specific command:

```bash
jdmsbuild generate --help
```

## Usage Patterns

### Pattern: Integrate with Build

Generate assets automatically during build:

**MyPackage.csproj:**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="JD.MSBuild.Fluent" Version="1.0.0" />
  </ItemGroup>

  <!-- Generate after build -->
  <Target Name="GenerateMSBuildAssets" AfterTargets="Build">
    <Exec Command="jdmsbuild generate --assembly $(TargetPath) --type $(RootNamespace).PackageFactory --method Create --output $(MSBuildProjectDirectory)/../artifacts/msbuild" />
  </Target>
</Project>
```

### Pattern: Pre-Pack Generation

Generate before creating NuGet package:

```xml
<Target Name="GenerateMSBuildAssets" BeforeTargets="Pack">
  <Exec Command="jdmsbuild generate --assembly $(TargetPath) --type $(RootNamespace).PackageFactory --method Create --output $(MSBuildProjectDirectory)/generated" />
</Target>

<ItemGroup>
  <None Include="generated/build/**/*.*" Pack="true" PackagePath="build" />
  <None Include="generated/buildTransitive/**/*.*" Pack="true" PackagePath="buildTransitive" />
</ItemGroup>
```

### Pattern: CI/CD Pipeline

Generate in CI/CD pipeline:

**GitHub Actions:**

```yaml
name: Build

on: [push]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Install JD.MSBuild.Fluent CLI
        run: dotnet tool install --global JD.MSBuild.Fluent.Cli
      
      - name: Build definitions
        run: dotnet build src/MyCompany.Build.Definitions
      
      - name: Generate MSBuild assets
        run: |
          jdmsbuild generate \
            --assembly src/MyCompany.Build.Definitions/bin/Release/net8.0/MyCompany.Build.Definitions.dll \
            --type MyCompany.Build.PackageFactory \
            --method Create \
            --output artifacts/msbuild
      
      - name: Upload artifacts
        uses: actions/upload-artifact@v3
        with:
          name: msbuild-assets
          path: artifacts/msbuild
```

**Azure Pipelines:**

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '8.0.x'
  
  - script: dotnet tool install --global JD.MSBuild.Fluent.Cli
    displayName: 'Install CLI'
  
  - script: dotnet build src/MyCompany.Build.Definitions
    displayName: 'Build definitions'
  
  - script: |
      jdmsbuild generate \
        --assembly src/MyCompany.Build.Definitions/bin/Release/net8.0/MyCompany.Build.Definitions.dll \
        --type MyCompany.Build.PackageFactory \
        --method Create \
        --output $(Build.ArtifactStagingDirectory)/msbuild
    displayName: 'Generate MSBuild assets'
  
  - task: PublishBuildArtifacts@1
    inputs:
      pathToPublish: '$(Build.ArtifactStagingDirectory)/msbuild'
      artifactName: 'msbuild-assets'
```

### Pattern: Development Workflow

Script for local development:

**generate-assets.ps1:**

```powershell
#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Debug"
)

Write-Host "Building definitions..." -ForegroundColor Green
dotnet build src/MyCompany.Build.Definitions -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}

Write-Host "Generating MSBuild assets..." -ForegroundColor Green
jdmsbuild generate `
    --assembly "src/MyCompany.Build.Definitions/bin/$Configuration/net8.0/MyCompany.Build.Definitions.dll" `
    --type "MyCompany.Build.PackageFactory" `
    --method "Create" `
    --output "artifacts/msbuild" `
    --verbose

if ($LASTEXITCODE -ne 0) {
    Write-Error "Generation failed"
    exit 1
}

Write-Host "Assets generated successfully!" -ForegroundColor Green
```

**generate-assets.sh:**

```bash
#!/bin/bash

set -e

CONFIGURATION=${1:-Debug}

echo "Building definitions..."
dotnet build src/MyCompany.Build.Definitions -c $CONFIGURATION

echo "Generating MSBuild assets..."
jdmsbuild generate \
    --assembly "src/MyCompany.Build.Definitions/bin/$CONFIGURATION/net8.0/MyCompany.Build.Definitions.dll" \
    --type "MyCompany.Build.PackageFactory" \
    --method "Create" \
    --output "artifacts/msbuild" \
    --verbose

echo "Assets generated successfully!"
```

## Output Structure

The `generate` command creates the following structure:

```
output/
├── build/
│   ├── PackageId.props
│   └── PackageId.targets
├── buildTransitive/      (if PackagePackagingOptions.BuildTransitive = true)
│   ├── PackageId.props
│   └── PackageId.targets
└── Sdk/                  (if PackagePackagingOptions.EmitSdk = true)
    └── Sdk/
        ├── Sdk.props
        └── Sdk.targets
```

## Error Messages

### Assembly Not Found

**Error:**

```
Error: Could not load assembly at path 'path/to/assembly.dll'
```

**Solution:**

- Verify the assembly path is correct
- Ensure the assembly has been built
- Check file permissions

### Type Not Found

**Error:**

```
Error: Type 'MyCompany.Factory' not found in assembly
```

**Solution:**

- Verify the type name is fully qualified (namespace + class name)
- Ensure the type is `public`
- Check for typos in the type name

### Method Not Found

**Error:**

```
Error: Method 'Create' not found on type 'MyCompany.Factory'
```

**Solution:**

- Verify the method name is correct
- Ensure the method is `public static`
- Check that the method returns `PackageDefinition`
- Verify the method has no parameters

### Invalid Return Type

**Error:**

```
Error: Method 'Create' does not return PackageDefinition
```

**Solution:**

- Ensure the method signature is:
  ```csharp
  public static PackageDefinition Create()
  ```

## Debugging

### Enable Verbose Mode

Use `--verbose` for detailed logging:

```bash
jdmsbuild generate --assembly MyPackage.dll --type MyPackage.Factory --method Create --output ./output --verbose
```

Output includes:

- Assembly loading details
- Type resolution steps
- Method invocation
- Rendering progress
- File write operations

### Dry Run (Future Feature)

Validate without writing files:

```bash
jdmsbuild generate --assembly MyPackage.dll --type MyPackage.Factory --method Create --output ./output --dry-run
```

## Programmatic Usage

You can also use the emitter programmatically without the CLI:

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.Packaging;

var definition = Package.Define("MyPackage")
    .Props(p => p.Property("Prop1", "Value1"))
    .Targets(t => t.Target("Target1", target => target.Message("Hello")))
    .Build();

var emitter = new MsBuildPackageEmitter();
emitter.Emit(definition, outputDirectory: "artifacts/msbuild");
```

## Environment Variables

The CLI respects these environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `JDMSBUILD_VERBOSE` | Enable verbose logging | `false` |
| `JDMSBUILD_OUTPUT` | Default output directory | (none) |

**Example:**

```bash
export JDMSBUILD_VERBOSE=true
jdmsbuild generate --assembly MyPackage.dll --type Factory --method Create --output ./output
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | General error |
| 2 | Invalid arguments |
| 3 | Assembly load error |
| 4 | Type not found |
| 5 | Method not found |
| 6 | Method invocation error |
| 7 | File write error |

## Best Practices

### DO: Version the CLI in Your Repository

Use a local tool manifest to lock the CLI version:

```bash
dotnet new tool-manifest
dotnet tool install JD.MSBuild.Fluent.Cli --version 1.0.0
```

**Commit `.config/dotnet-tools.json`** to source control.

### DO: Integrate into Build Process

Generate assets during build for consistency:

```xml
<Target Name="GenerateAssets" AfterTargets="Build">
  <Exec Command="dotnet jdmsbuild generate ..." />
</Target>
```

### DO: Validate Generated Files

Add a target to verify generated files:

```xml
<Target Name="ValidateGeneratedFiles" AfterTargets="GenerateMSBuildAssets">
  <Error Condition="!Exists('$(OutputPath)/../artifacts/msbuild/build/PackageId.props')" 
         Text="Generated .props file not found" />
  <Error Condition="!Exists('$(OutputPath)/../artifacts/msbuild/build/PackageId.targets')" 
         Text="Generated .targets file not found" />
</Target>
```

### DON'T: Commit Generated Files to Source Control

Generated files should be **build artifacts**, not source:

**.gitignore:**

```
artifacts/msbuild/
generated/
```

Regenerate during build or CI/CD.

## Summary

The JD.MSBuild.Fluent CLI provides:

- **Simple interface**: Generate assets with a single command
- **Build integration**: Hook into MSBuild targets
- **CI/CD support**: Use in any pipeline
- **Validation**: Clear error messages for common issues

## Next Steps

- [Installation Guide](../getting-started/installation.md) - Install CLI and library
- [First Package](../getting-started/first-package.md) - Create and generate your first package
- [Package Structure](../core-concepts/package-structure.md) - Understand output layout
- [Troubleshooting](../troubleshooting/index.md) - Common issues and solutions
