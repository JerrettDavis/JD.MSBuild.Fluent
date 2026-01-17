# Getting Started

JD.MSBuild.Fluent lets you author MSBuild `.props` and `.targets` in C# and emit them into a NuGet-ready layout.

## Install

Add the package to your project:

```bash
dotnet add package JD.MSBuild.Fluent
```

If you want the CLI:

```bash
dotnet tool install -g JD.MSBuild.Fluent.Cli
```

## Create a definition

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

public static class Factory
{
  public static PackageDefinition Create() => Package.Define("MySdk")
    .Props(p => p.Property("MySdkEnabled", "true"))
    .Targets(t => t.Target("MySdk_Hello", tgt => tgt
      .BeforeTargets("Build")
      .Condition("'$(MySdkEnabled)' == 'true'")
      .Message("Hello from MySdk")))
    .Pack(o => { o.BuildTransitive = true; o.EmitSdk = true; })
    .Build();
}
```

## Generate assets

```bash
jdmsbuild generate --assembly path/to/MySdk.dll --type MySdk.Factory --method Create --output artifacts/msbuild
```
