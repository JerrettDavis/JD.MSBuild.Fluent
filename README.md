# JD.MSBuild.Fluent

A strongly-typed, fluent DSL for authoring MSBuild `.props`, `.targets`, and SDK assets, then emitting them into the exact NuGet folder layout (`build/`, `buildTransitive/`, `Sdk/<id>/...`).

The goal is to make authoring MSBuild packages feel like writing normal C# - DRY, SOLID, refactorable - while still producing **100% standard MSBuild XML**.

## Documentation

- Docs site sources live in `docs/` (DocFX).
- API reference is generated from XML documentation comments.

## Quick start

### Author a definition

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace MySdk;

public static class Factory
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

### Generate assets

1. Build the project containing your factory (`Factory.Create`) to produce an assembly.
2. Run:

```bash
jdmsbuild generate --assembly path/to/MySdk.dll --type MySdk.Factory --method Create --output artifacts/msbuild
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
