# Installation

This guide walks you through installing JD.MSBuild.Fluent and setting up your development environment for authoring MSBuild packages.

## Prerequisites

Before installing JD.MSBuild.Fluent, ensure you have:

- **.NET SDK 8.0 or later**: Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
- **IDE with C# support**: Visual Studio 2022, Visual Studio Code with C# extension, or JetBrains Rider
- **Basic MSBuild knowledge**: Familiarity with properties, targets, and tasks
- **NuGet package experience**: Understanding of NuGet package structure and consumption

## Installation Methods

### Method 1: NuGet Package (Recommended)

Add JD.MSBuild.Fluent to your build definitions project using the .NET CLI:

```bash
dotnet add package JD.MSBuild.Fluent
```

Or using the Package Manager Console in Visual Studio:

```powershell
Install-Package JD.MSBuild.Fluent
```

Or by adding the PackageReference directly to your `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="JD.MSBuild.Fluent" Version="*" />
  </ItemGroup>
</Project>
```

> **Note**: Replace `Version="*"` with a specific version in production. Check [NuGet.org](https://www.nuget.org/packages/JD.MSBuild.Fluent) for the latest stable version.

### Method 2: CLI Tool (Global)

Install the JD.MSBuild.Fluent CLI tool globally for generating MSBuild assets from any directory:

```bash
dotnet tool install --global JD.MSBuild.Fluent.Cli
```

Update to the latest version:

```bash
dotnet tool update --global JD.MSBuild.Fluent.Cli
```

Verify installation:

```bash
jdmsbuild --version
```

### Method 3: CLI Tool (Local)

For project-specific CLI installations, use a local tool manifest:

```bash
# Create a tool manifest if you don't have one
dotnet new tool-manifest

# Install the CLI locally
dotnet tool install JD.MSBuild.Fluent.Cli
```

Run the local tool:

```bash
dotnet jdmsbuild --version
```

### Method 4: Source Build

Clone the repository and build from source:

```bash
git clone https://github.com/jasondown/JD.MSBuild.Fluent.git
cd JD.MSBuild.Fluent
dotnet build
```

Reference the local build in your project:

```xml
<ItemGroup>
  <ProjectReference Include="..\JD.MSBuild.Fluent\src\JD.MSBuild.Fluent\JD.MSBuild.Fluent.csproj" />
</ItemGroup>
```

## Project Setup

### Creating a Build Definitions Project

Create a new class library project dedicated to your MSBuild package definitions:

```bash
dotnet new classlib -n MyCompany.Build.Definitions
cd MyCompany.Build.Definitions
dotnet add package JD.MSBuild.Fluent
```

**Recommended project structure:**

```
MyCompany.Build.Definitions/
├── MyCompany.Build.Definitions.csproj
├── PackageFactory.cs              # Main factory method
├── Targets/
│   ├── PreBuildTargets.cs         # Pre-build target definitions
│   └── PostBuildTargets.cs        # Post-build target definitions
├── Properties/
│   └── DefaultProperties.cs       # Default property values
└── Tasks/
    └── CustomTaskDefinitions.cs   # UsingTask declarations
```

### Sample Project File

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="JD.MSBuild.Fluent" Version="1.0.0" />
  </ItemGroup>
  
  <!-- Optional: Generate MSBuild assets on build -->
  <Target Name="GenerateMSBuildAssets" AfterTargets="Build">
    <Exec Command="jdmsbuild generate --assembly $(TargetPath) --type MyCompany.Build.PackageFactory --method Create --output $(MSBuildProjectDirectory)/../artifacts/msbuild" />
  </Target>
</Project>
```

## Verification

Create a simple factory to verify your installation:

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
            .Description("Custom MSBuild package for MyCompany")
            .Props(p => p.Property("MyCompanyVersion", "1.0.0"))
            .Targets(t => t.Target("MyCompany_Hello", target => target
                .BeforeTargets("Build")
                .Message("Hello from MyCompany.Build!")))
            .Build();
    }
}
```

Build the project:

```bash
dotnet build
```

Generate MSBuild assets:

```bash
jdmsbuild generate \
    --assembly bin/Debug/net8.0/MyCompany.Build.Definitions.dll \
    --type MyCompany.Build.PackageFactory \
    --method Create \
    --output ./output
```

Verify the generated files:

```bash
ls output/build/
# Should contain: MyCompany.Build.props, MyCompany.Build.targets
```

## IDE Configuration

### Visual Studio

1. Install the latest Visual Studio 2022 with the ".NET desktop development" workload
2. Open your solution
3. IntelliSense and code completion work automatically with JD.MSBuild.Fluent

### Visual Studio Code

1. Install the [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
2. Open the project folder
3. Install the [MSBuild project tools extension](https://marketplace.visualstudio.com/items?itemName=tintoy.msbuild-project-tools) for MSBuild syntax highlighting

### JetBrains Rider

1. Install the latest Rider
2. Open your solution
3. IntelliSense works out of the box

## Common Installation Issues

### Issue: Package Not Found

**Error:**
```
error NU1101: Unable to find package JD.MSBuild.Fluent
```

**Solution:**
Ensure NuGet.org is in your package sources:

```bash
dotnet nuget list source
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

### Issue: .NET SDK Version Mismatch

**Error:**
```
error NETSDK1045: The current .NET SDK does not support targeting .NET 8.0
```

**Solution:**
Update your .NET SDK to version 8.0 or later:

```bash
dotnet --version  # Check current version
# Download latest from dotnet.microsoft.com
```

Or target an earlier framework version in your `.csproj`:

```xml
<TargetFramework>net6.0</TargetFramework>
```

### Issue: CLI Tool Not Found

**Error:**
```
'jdmsbuild' is not recognized as an internal or external command
```

**Solution:**
Ensure the .NET tools directory is in your PATH:

**Windows:**
```powershell
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
```

**Linux/macOS:**
```bash
export PATH="$PATH:$HOME/.dotnet/tools"
```

Add to your shell profile (`.bashrc`, `.zshrc`, etc.) to make permanent.

### Issue: Assembly Load Errors

**Error:**
```
Could not load file or assembly 'JD.MSBuild.Fluent, Version=...'
```

**Solution:**
Clear NuGet caches and restore:

```bash
dotnet nuget locals all --clear
dotnet restore
dotnet build
```

## Next Steps

Now that you have JD.MSBuild.Fluent installed, proceed to:

- [Create Your First Package](first-package.md) - Step-by-step tutorial
- [Quick Start](quick-start.md) - Rapid introduction to core concepts
- [Architecture](../core-concepts/architecture.md) - Understand the framework design

## Additional Resources

- [NuGet Package Page](https://www.nuget.org/packages/JD.MSBuild.Fluent)
- [GitHub Repository](https://github.com/jasondown/JD.MSBuild.Fluent)
- [Release Notes](https://github.com/jasondown/JD.MSBuild.Fluent/releases)
- [Report Issues](https://github.com/jasondown/JD.MSBuild.Fluent/issues)
