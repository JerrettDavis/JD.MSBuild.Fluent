# MinimalSdkPackage sample

This sample demonstrates a minimal fluent package definition and how to emit MSBuild assets.

## Build

```bash
dotnet build
```

## Generate assets

```bash
jdmsbuild generate --assembly bin/Debug/net10.0/MinimalSdkPackage.dll --type MinimalSdkPackage.DefinitionFactory --method Create --output artifacts/msbuild
```
