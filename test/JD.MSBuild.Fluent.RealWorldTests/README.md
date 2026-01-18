# JD.MSBuild.Fluent.RealWorldTests

Integration tests that validate the scaffolder against real-world MSBuild packages.

## Test Cases

### EfcptBuildRoundTripTests

These tests validate the complete scaffolding workflow:
1. Load XML from JD.Efcpt.Build
2. Scaffold to fluent C# code
3. Compile with Roslyn
4. Execute factory to generate PackageDefinition
5. Render back to XML
6. Compare semantically to ensure equivalence

**Tests:**
- `RoundTrip_EfcptBuild_BuildTransitiveTargets_ProducesSameXml` - Validates 42KB production targets file
- `RoundTrip_EfcptBuild_BuildTransitiveProps_ProducesSameXml` - Validates props file

## Running Tests

### Local Development

These tests require [JD.Efcpt.Build](https://github.com/JerrettDavis/JD.Efcpt.Build) to be cloned in the parent directory:

```bash
# Clone JD.Efcpt.Build alongside JD.MSBuild.Fluent
cd C:\git  # or your repos directory
git clone https://github.com/JerrettDavis/JD.Efcpt.Build.git

# Now tests will run
cd JD.MSBuild.Fluent
dotnet test
```

### CI/CD

The tests are marked with `[Fact(Skip = "...")]` so they are automatically skipped in CI where JD.Efcpt.Build is not available.

## Purpose

These integration tests serve as:
1. **Real-world validation** - Ensures scaffolder works with production MSBuild packages
2. **Regression prevention** - Catches bugs that unit tests miss
3. **Documentation** - Demonstrates scaffolder capabilities

The tests caught several critical bugs during development:
- Context tracking (p vs t builder variables)
- PropertyGroup/ItemGroup handling in .Targets() context
- XML namespace handling
- Duplicate property handling with conditions
