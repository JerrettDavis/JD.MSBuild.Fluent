# Usage Guide

## Project layout

The emitter writes a standard NuGet MSBuild layout:

- `build/<id>.props`
- `build/<id>.targets`
- `buildTransitive/<id>.props`
- `buildTransitive/<id>.targets`
- `Sdk/<id>/Sdk.props`
- `Sdk/<id>/Sdk.targets`

Enable buildTransitive and SDK output using the packaging options:

```csharp
.Pack(o => { o.BuildTransitive = true; o.EmitSdk = true; })
```

## Determinism

The renderer canonicalizes ordering for properties, items, metadata, and task parameters.
This keeps diffs stable while preserving author intent.

## Validation

Validation runs during render and emit to ensure your model is consistent and safe to produce.
It will throw `MsBuildValidationException` with a list of errors when validation fails.
