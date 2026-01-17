# Sample Gallery

Welcome to the JD.MSBuild.Fluent sample gallery. These samples demonstrate how to author production-quality MSBuild packages using a strongly-typed, fluent DSL instead of hand-crafted XML.

## Why Use JD.MSBuild.Fluent?

- **Type-safe**: Catch errors at compile time, not runtime
- **Refactorable**: Apply SOLID principles to your build logic
- **DRY**: Share common patterns across multiple packages
- **Testable**: Unit test your MSBuild definitions before deployment
- **Maintainable**: Write MSBuild logic that reads like normal C#
- **Deterministic**: Predictable XML output with canonicalized ordering

## Sample Categories

### Basic Samples

Start here if you're new to JD.MSBuild.Fluent. These samples demonstrate fundamental concepts and simple use cases.

| Sample | Description | Key Concepts |
|--------|-------------|--------------|
| [Minimal SDK Package](basic/minimal-sdk.md) | A minimal SDK-style package with props, targets, and SDK layout | Package definition, props/targets separation, SDK emission, build transitive |
| [Property Definition Package](basic/properties.md) | Define and export MSBuild properties with conditions and grouping | PropertyGroups, conditional properties, property evaluation |
| [Simple Target Package](basic/simple-target.md) | Create custom targets that hook into the build pipeline | Target definition, BeforeTargets/AfterTargets, Message tasks |

### Real-World Samples

Production-ready examples demonstrating complete implementations of common scenarios.

| Sample | Description | Complexity | Lines of Code |
|--------|-------------|------------|---------------|
| [Database Build Integration](real-world/database-build.md) | Integrate EF Core migrations and database deployment into the build | ⭐⭐⭐ | ~300 |
| [Docker Integration](real-world/docker-integration.md) | Automate Docker image builds and container orchestration | ⭐⭐⭐ | ~350 |
| [Code Generation Pipeline](real-world/code-generation.md) | Run T4 templates or source generators with dependency tracking | ⭐⭐ | ~200 |
| [Multi-Project Orchestration](real-world/multi-project.md) | Coordinate builds across multiple projects with shared configuration | ⭐⭐⭐⭐ | ~400 |

## Quick Start

### 1. Install the Package

Add JD.MSBuild.Fluent to your definition project:

```bash
dotnet add package JD.MSBuild.Fluent
```

### 2. Create a Factory

Define your package in a static factory method:

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace MyCompany.Build;

public static class Factory
{
  public static PackageDefinition Create() => Package.Define("MyCompany.Build.Common")
    .Description("Shared build infrastructure for MyCompany projects")
    .Props(p => p
      .Property("MyCompanyRootDir", "$(MSBuildThisFileDirectory)../../../"))
    .Targets(t => t
      .Target("ValidateBuild", target => target
        .BeforeTargets("Build")
        .Message("Building with MyCompany.Build.Common")))
    .Pack(o => { o.BuildTransitive = true; })
    .Build();
}
```

### 3. Generate MSBuild Assets

Build your definition project and generate the MSBuild XML:

```bash
# Build the definition assembly
dotnet build

# Generate MSBuild assets
jdmsbuild generate \
  --assembly bin/Debug/net9.0/MyCompany.Build.dll \
  --type MyCompany.Build.Factory \
  --method Create \
  --output artifacts/msbuild
```

### 4. Package and Distribute

Copy the generated assets into a NuGet package:

```xml
<ItemGroup>
  <None Include="artifacts/msbuild/**/*" Pack="true" PackagePath="" />
</ItemGroup>
```

## Common Patterns

### Pattern: Conditional Configuration

```csharp
.Props(p => p
  .PropertyGroup("'$(Configuration)' == 'Release'", g => g
    .Property("Optimize", "true")
    .Property("DebugSymbols", "false"))
  .PropertyGroup("'$(Configuration)' == 'Debug'", g => g
    .Property("Optimize", "false")
    .Property("DebugSymbols", "true")))
```

### Pattern: Target Dependencies

```csharp
.Targets(t => t
  .Target("PrepareResources", target => target
    .BeforeTargets("Build")
    .Message("Preparing resources..."))
  .Target("ValidateResources", target => target
    .DependsOnTargets("PrepareResources")
    .Message("Validating resources...")))
```

### Pattern: File Generation Tasks

```csharp
.Targets(t => t
  .Target("GenerateConfig", target => target
    .Task("WriteLinesToFile", task => task
      .Param("File", "$(IntermediateOutputPath)config.json")
      .Param("Lines", "{\"version\": \"1.0\"}")
      .Param("Overwrite", "true"))))
```

### Pattern: External Tool Execution

```csharp
.Targets(t => t
  .Target("RunCodeGen", target => target
    .Exec("dotnet tool run code-generator --input $(ProjectDir)schema.yaml", 
          workingDirectory: "$(ProjectDir)")))
```

## Best Practices

### ✅ DO

- **Separate concerns**: Use `.Props()` for evaluation-time logic, `.Targets()` for execution-time logic
- **Use conditions wisely**: Leverage MSBuild conditions to make packages flexible
- **Enable BuildTransitive**: Use `o.BuildTransitive = true` to flow settings to transitive dependencies
- **Test your definitions**: Write unit tests that invoke your factory and validate the output
- **Version carefully**: Semantic versioning matters for build infrastructure
- **Document behavior**: Add XML comments to your factory methods explaining what they do

### ❌ DON'T

- **Don't mix evaluation and execution**: Keep properties/items in Props, targets/tasks in Targets
- **Don't hardcode paths**: Use MSBuild properties like `$(MSBuildThisFileDirectory)` for portability
- **Don't ignore conditions**: Always consider when your logic should run (Configuration, Platform, etc.)
- **Don't forget testing**: Broken build packages can break entire organizations
- **Don't over-complicate**: Start simple, add complexity only when needed

## Testing Your Definitions

```csharp
[Fact]
public void Factory_Creates_Valid_Definition()
{
  // Arrange & Act
  var def = Factory.Create();
  
  // Assert
  Assert.Equal("MyCompany.Build.Common", def.Id);
  Assert.NotEmpty(def.Props.PropertyGroups);
  Assert.NotEmpty(def.Targets.Targets);
}

[Fact]
public void Generated_Props_Contains_RootDir_Property()
{
  // Arrange
  var def = Factory.Create();
  var renderer = new MsBuildXmlRenderer();
  
  // Act
  var xml = renderer.Render(def.Props);
  
  // Assert
  Assert.Contains("MyCompanyRootDir", xml);
}
```

## Troubleshooting

### Issue: Package not being applied

**Symptom**: Your package is installed, but props/targets aren't running.

**Solutions**:
1. Verify package layout: Check that files are in `build/` or `buildTransitive/` directories
2. Check naming: Files must match pattern `build/{PackageId}.props` and `build/{PackageId}.targets`
3. Enable MSBuild logging: `dotnet build /v:diag` to see import resolution
4. Verify conditions: Ensure condition expressions evaluate to `true`

### Issue: Build order problems

**Symptom**: Targets run in the wrong order or dependencies aren't respected.

**Solutions**:
1. Use `BeforeTargets`/`AfterTargets` instead of custom ordering
2. Specify explicit `DependsOnTargets` for dependencies
3. Review [MSBuild target execution order](https://docs.microsoft.com/en-us/visualstudio/msbuild/target-build-order)

### Issue: Properties not flowing transitively

**Symptom**: Direct consumers see properties, but transitive consumers don't.

**Solutions**:
1. Enable `BuildTransitive`: Set `o.BuildTransitive = true` in `.Pack()`
2. Use `buildTransitive/` folder: Define separate `.BuildTransitiveProps()` if needed
3. Check conditions: Transitive properties must evaluate correctly in all contexts

## Additional Resources

- [MSBuild Concepts](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-concepts)
- [NuGet Package Conventions](https://docs.microsoft.com/en-us/nuget/create-packages/creating-a-package)
- [MSBuild Reserved Properties](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-reserved-and-well-known-properties)
- [MSBuild Common Targets](https://docs.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-items)

## Contributing Samples

Have a great sample to share? Submit a PR with:
- Complete, working code (definition + test project)
- README with setup instructions
- Explanation of the scenario and why your approach works
- Documentation of any gotchas or edge cases

---

*Next: Start with the [Minimal SDK Package](basic/minimal-sdk.md) sample to learn the basics.*
