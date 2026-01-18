# JD.MSBuild.Fluent

A strongly-typed, fluent DSL for authoring MSBuild `.props`, `.targets`, and SDK assets, then emitting them into the exact NuGet folder layout (`build/`, `buildTransitive/`, `Sdk/<id>/...`).

The goal is to make authoring MSBuild packages feel like writing normal C# - DRY, SOLID, refactorable - while still producing **100% standard MSBuild XML**.

## Documentation

- [Full documentation](docs/)
- API reference is generated from XML documentation comments.

## Quick start

### 1. Install the package

```xml
<PackageReference Include="JD.MSBuild.Fluent" Version="*" />
```

### 2. Define your MSBuild assets

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace MySdk;

public static class DefinitionFactory
{
  public static PackageDefinition Create() => Package.Define("MySdk")
    .Props(p => p
      .Property("MySdkEnabled", "true"))
    .Targets(t => t
      .Target("MySdk_Hello", target => target
        .BeforeTargets("Build")
        .Condition("'$(MySdkEnabled)' == 'true'")
        .Message("Hello from MySdk")))
    .Pack(o => { o.BuildTransitive = true; o.EmitSdk = true; })
    .Build();
}
```

### 3. Generate assets automatically

**MSBuild automatically generates assets during build** - no CLI required!

Configure in your `.csproj`:

```xml
<PropertyGroup>
  <!-- Enable generation (optional - defaults to true) -->
  <JDMSBuildFluentGenerateEnabled>true</JDMSBuildFluentGenerateEnabled>
  
  <!-- Specify factory type (optional - auto-detects DefinitionFactory) -->
  <JDMSBuildFluentDefinitionType>MySdk.DefinitionFactory</JDMSBuildFluentDefinitionType>
  
  <!-- Output directory (optional - defaults to obj/msbuild) -->
  <JDMSBuildFluentOutputDir>$(MSBuildProjectDirectory)\msbuild</JDMSBuildFluentOutputDir>
</PropertyGroup>
```

Generated files are included in the build and packaged automatically:
- `build/<id>.props`
- `build/<id>.targets`
- `buildTransitive/<id>.props` and `.targets` (if enabled)
- `Sdk/<id>/Sdk.props` and `Sdk.targets` (if enabled)

### Optional: CLI for manual generation

Install the CLI tool globally:

```bash
dotnet tool install -g JD.MSBuild.Fluent.Cli
```

Generate manually:

```bash
jdmsbuild generate --assembly path/to/MySdk.dll --type MySdk.DefinitionFactory --method Create --output msbuild
```

Or generate the built-in example:

```bash
jdmsbuild generate --example --output artifacts/msbuild
```

### Convert existing XML to fluent (scaffolding)

Migrate existing MSBuild XML files to fluent API:

```bash
# Install CLI if not already installed
dotnet tool install -g JD.MSBuild.Fluent.Cli

# Scaffold from existing XML
jdmsbuild scaffold --xml MyPackage.targets --output DefinitionFactory.cs --package-id MyCompany.MyPackage
```

This converts your XML into idiomatic fluent C# code that you can then customize and maintain.

## Migration from XML

The `scaffold` command helps you migrate existing MSBuild packages:

1. **Start with your XML**: Any `.props` or `.targets` file
2. **Generate fluent code**: `jdmsbuild scaffold --xml build/MyPackage.targets --output src/DefinitionFactory.cs`
3. **Review and adjust**: The generated code is a starting point - refactor as needed
4. **Build**: Generated assets are created automatically during build

**Example**:

Original XML (`MyPackage.targets`):
```xml
<Project>
  <Target Name="Hello" BeforeTargets="Build">
    <Message Text="Hello from MyPackage!" Importance="High" />
  </Target>
</Project>
```

Generated fluent code:
```csharp
public static class DefinitionFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("MyPackage")
            .Targets(t =>
            {
                t.Target("Hello", target =>
                {
                    target.BeforeTargets("Build");
                    target.Message("Hello from MyPackage!", "High");
                });
            })
            .Build();
    }
}
```

## Samples

- `samples/MinimalSdkPackage` contains a minimal, end-to-end definition and output.

## Output layout

- `build/<id>.props`
- `build/<id>.targets`
- (optional) `buildTransitive/...`
- (optional) `Sdk/<id>/Sdk.props`
- (optional) `Sdk/<id>/Sdk.targets`

## Determinism

The renderer canonicalizes common sources of MSBuild churn (property ordering, item metadata ordering, task parameter ordering) so that diffs remain meaningful.
