# CLI Reference

The CLI generates MSBuild assets from a factory method.

## Install

```bash
dotnet tool install -g JD.MSBuild.Fluent.Cli
```

## Generate assets

```bash
jdmsbuild generate --assembly path/to/MySdk.dll --type MySdk.Factory --method Create --output artifacts/msbuild
```

### Options

- `--assembly`: Path to the assembly containing the factory method.
- `--type`: Fully qualified type name that contains the method.
- `--method`: Factory method name (default: `Create`).
- `--output`: Output directory (default: `artifacts/msbuild`).
- `--example`: Use the built-in example definition.
