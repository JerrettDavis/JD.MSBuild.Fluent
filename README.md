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
