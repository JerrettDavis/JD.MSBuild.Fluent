# TinyBDD Implementation Guide for JD.MSBuild.Fluent

This guide provides step-by-step instructions for implementing TinyBDD best practices across the test suite.

---

## 🎯 Quick Reference

### Before (Current)
```csharp
/// <summary>Feature: Fluent API</summary>
public sealed class BddFluentApiTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public async Task Scenario_Do_something()
    {
        string? tempDir = null;
        try
        {
            await Given("setup", () => { tempDir = CreateTemp(); return "data"; })
                .When("action", x => x + "!")
                .Then("verify", x => x == "data!")
                .AssertPassed();
        }
        finally
        {
            if (tempDir != null) Directory.Delete(tempDir, true);
        }
    }
}
```

### After (Best Practice)
```csharp
[Feature("Fluent API")]
public sealed class BddFluentApiTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Do something")]
    [Fact]
    [Tag("smoke")]
    public async Task DoSomething()
    {
        await Given("setup", CreateSetup)
            .Finally("cleanup", CleanupTemp)
            .When("action", PerformAction)
            .Then("verify", VerifyResult)
            .AssertPassed();
    }

    private static (string data, string tempDir) CreateSetup()
    {
        var tempDir = CreateTemp();
        return ("data", tempDir);
    }

    private static void CleanupTemp((string data, string tempDir) ctx)
    {
        if (Directory.Exists(ctx.tempDir))
            Directory.Delete(ctx.tempDir, true);
    }

    private static string PerformAction((string data, string tempDir) ctx) => 
        ctx.data + "!";

    private static bool VerifyResult(string result)
    {
        result.Should().Be("data!");
        return true;
    }
}
```

---

## 📋 Refactoring Checklist

For each test file:

- [ ] Add `[Feature("...")]` attribute to class
- [ ] Add `[Scenario("...")]` attribute to each test method
- [ ] Add `[Tag("...")]` attributes for categorization
- [ ] Remove "Scenario_" prefix from method names
- [ ] Extract lambda expressions to named methods
- [ ] Replace try/finally with `.Finally()`
- [ ] Use FluentAssertions in Then/And/But steps
- [ ] Ensure class inherits from `TinyBddXunitBase`
- [ ] Consider feature lifecycle hooks for repeated setup

---

## 🔧 Implementation Steps

### Step 1: Update Package Reference

Check and update TinyBDD version in `Directory.Packages.props`:

```xml
<PackageVersion Include="TinyBDD.Xunit" Version="1.0.0" />
```

Latest version check:
```bash
dotnet list package --outdated
```

### Step 2: Add Feature Attribute

**Before:**
```csharp
/// <summary>Feature: Fluent API - Package Definition</summary>
public sealed class BddFluentApiTests(ITestOutputHelper output) : TinyBddXunitBase(output)
```

**After:**
```csharp
[Feature("Fluent API - Package Definition")]
public sealed class FluentApiTests(ITestOutputHelper output) : TinyBddXunitBase(output)
```

**Benefits:**
- Appears in test output
- Creates xUnit trait for filtering: `-filter "Feature=Fluent API - Package Definition"`
- Better discoverability in Test Explorer

### Step 3: Add Scenario Attribute

**Before:**
```csharp
[Fact]
public async Task Scenario_Define_basic_package()
```

**After:**
```csharp
[Scenario("Define basic package")]
[Fact]
public async Task DefineBasicPackage()
```

**Benefits:**
- Cleaner method names
- Scenario name in test output
- xUnit trait: `-filter "Scenario=Define basic package"`

### Step 4: Add Tags

```csharp
[Scenario("Generate package to disk")]
[Fact]
[Tag("integration")]
[Tag("file-system")]
[Tag("slow")]
public async Task GeneratePackageToDisk()
```

**Tag Categories:**
- **smoke**: Quick sanity tests (< 100ms)
- **integration**: Tests with I/O or external dependencies
- **slow**: Tests taking > 1 second
- **category**: Domain-specific (e.g., "generation", "validation", "rendering")

**Filter Examples:**
```bash
# Run only smoke tests
dotnet test --filter "Tag=smoke"

# Exclude slow tests
dotnet test --filter "Tag!=slow"

# Integration tests only
dotnet test --filter "Tag=integration"
```

### Step 5: Extract Lambda Expressions

**Before:**
```csharp
await Given("a package ID", () => "MyPackage")
    .When("defining package", id => Package.Define(id).Build())
    .Then("package ID is set", def => def.Id == "MyPackage")
    .AssertPassed();
```

**After:**
```csharp
await Given("a package ID", CreatePackageId)
    .When("defining package", DefinePackage)
    .Then("package ID is set", VerifyPackageId)
    .AssertPassed();

// At class level
private static string CreatePackageId() => "MyPackage";

private static PackageDefinition DefinePackage(string id) => 
    Package.Define(id).Build();

private static bool VerifyPackageId(PackageDefinition def)
{
    def.Id.Should().Be("MyPackage");
    return true;
}
```

**Benefits:**
- Easier debugging (can set breakpoints)
- Better stack traces
- Reduced allocations (TinyBDD recommendation)
- Reusable across tests
- Better IDE support (Go to Definition, Find References)

### Step 6: Use .Finally() for Cleanup

**Before:**
```csharp
string? tempDir = null;
try
{
    await Given("setup", () =>
        {
            tempDir = Path.GetTempPath() + Guid.NewGuid();
            Directory.CreateDirectory(tempDir);
            return tempDir;
        })
        .When("do work", dir => /* ... */)
        .Then("verify", result => /* ... */)
        .AssertPassed();
}
finally
{
    if (tempDir != null && Directory.Exists(tempDir))
        Directory.Delete(tempDir, recursive: true);
}
```

**After:**
```csharp
await Given("setup", CreateTempDirectory)
    .Finally("cleanup temp directory", CleanupDirectory)
    .When("do work", DoWork)
    .Then("verify", Verify)
    .AssertPassed();

private static string CreateTempDirectory()
{
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempDir);
    return tempDir;
}

private static void CleanupDirectory(string tempDir)
{
    if (Directory.Exists(tempDir))
        Directory.Delete(tempDir, recursive: true);
}
```

**Benefits:**
- Cleanup guaranteed even on assertion failures
- Shows in test output: `Finally cleanup temp directory [OK] 5 ms`
- Cleaner code, no try/finally
- Multiple Finally handlers supported

### Step 7: Use Feature Lifecycle Hooks

For tests with expensive setup (e.g., TestContainers, temp directories):

```csharp
[Feature("Integration Tests with Database")]
public sealed class DatabaseIntegrationTests : TinyBddXunitBase
{
    public DatabaseIntegrationTests(ITestOutputHelper output) : base(output) { }

    protected override ScenarioChain<object>? ConfigureFeatureSetup()
    {
        return Given("a test database container", () =>
        {
            // This runs ONCE for all tests in this class
            var container = new SqlServerTestcontainer();
            container.StartAsync().Wait();
            return (object)container;
        });
    }

    protected override ScenarioChain<object>? ConfigureFeatureTeardown()
    {
        return Given("cleanup database", () =>
        {
            if (FeatureState is SqlServerTestcontainer container)
                container.DisposeAsync().AsTask().Wait();
            return new object();
        });
    }

    [Scenario("First test uses database")]
    [Fact]
    public async Task FirstTest()
    {
        await GivenFeature<SqlServerTestcontainer>("the database")
            .When("inserting data", async db => await InsertDataAsync(db))
            .Then("data exists", success => success)
            .AssertPassed();
    }

    [Scenario("Second test reuses same database")]
    [Fact]
    public async Task SecondTest()
    {
        // Reuses same container from feature setup
        await GivenFeature<SqlServerTestcontainer>("the database")
            .When("querying data", async db => await QueryDataAsync(db))
            .Then("data found", result => result != null)
            .AssertPassed();
    }
}
```

**Benefits:**
- Faster test execution (setup once, not per test)
- Shared expensive resources
- Automatic cleanup via `ConfigureFeatureTeardown()`

---

## 🎨 Advanced Patterns

### Pattern 1: Parameterized Tests with TinyBDD

```csharp
[Theory]
[InlineData("net8.0", "8.0")]
[InlineData("net9.0", "9.0")]
[InlineData("net10.0", "10.0")]
[Scenario("Parse target framework")]
[Tag("parser")]
public async Task ParseTargetFramework(string input, string expectedVersion)
{
    await Given("a target framework string", () => input)
        .When("parsing", ParseTfm)
        .Then("version matches expected", version => 
        {
            version.Should().Be(expectedVersion);
            return true;
        })
        .AssertPassed();
}
```

### Pattern 2: Complex Assertions with And/But

```csharp
await Given("a package definition", CreatePackage)
    .When("validating", Validate)
    .Then("validation succeeds", result => result.IsValid)
    .And("no errors reported", result => !result.Errors.Any())
    .But("warnings may exist", result => result.Warnings.Any() || !result.Warnings.Any())
    .AssertPassed();
```

### Pattern 3: State Transformation

```csharp
await Given("a package builder", () => Package.Define("Test"))
    .When("adding property", b => b.Props(p => p.Property("Key", "Value")))
    .And("adding target", b => b.Targets(t => t.Target<TBuild>()))
    .And("building", b => b.Build())
    .Then("package is complete", def => def.Props != null && def.Targets != null)
    .AssertPassed();
```

### Pattern 4: Conditional Execution

```csharp
[Fact]
[Tag("windows-only")]
public async Task WindowsSpecificTest()
{
    if (!OperatingSystem.IsWindows())
    {
        Output.WriteLine("Skipping: Test requires Windows");
        return;
    }

    await Given("Windows environment", () => Environment.OSVersion)
        .When("checking platform", os => os.Platform)
        .Then("is Windows", platform => platform == PlatformID.Win32NT)
        .AssertPassed();
}
```

Or use xUnit's Skip:
```csharp
[Fact]
[Tag("windows-only")]
public async Task WindowsSpecificTest()
{
    Skip.IfNot(OperatingSystem.IsWindows(), "Test requires Windows");
    // ...
}
```

---

## 📊 Migration Priority

### Priority 1: Quick Wins (30 min total)
1. `BddFluentApiTests.cs` - 110 lines, straightforward
2. `PackagingValidationTests.cs` - 88 lines, simple validations

### Priority 2: Medium (1 hour)
3. `BddPackagingTests.cs` - 131 lines
4. `BddTargetOrchestrationTests.cs` - 162 lines
5. `ValidationTests.cs` - 55 lines, needs full conversion

### Priority 3: Complex (2 hours)
6. `BddEndToEndTests.cs` - 234 lines, has cleanup
7. `BddTaskInvocationTests.cs` - 191 lines
8. `BddRealWorldPatternsTests.cs` - 247 lines

### Priority 4: Large Tests (3 hours)
9. `GoldenGenerationTests.cs` - 150 lines
10. `GeneratorSpecTests.cs` - 172 lines
11. `EfcptParityGenerationTests.cs` - 275 lines
12. `EfcptCanonicalParityTests.cs` - 401 lines
13. `CoverageTests.cs` - 614 lines (largest!)

---

## ✅ Validation Checklist

After refactoring each file:

- [ ] All tests still pass: `dotnet test`
- [ ] Test output shows feature/scenario names
- [ ] Tags appear in test output
- [ ] No compilation warnings
- [ ] Code coverage unchanged or improved
- [ ] Test execution time similar or faster

Run this command to verify tags work:
```bash
dotnet test --filter "Tag=smoke" --logger "console;verbosity=detailed"
```

---

## 🚀 CI/CD Integration

Update `.github/workflows/ci.yml`:

```yaml
- name: Run smoke tests
  run: dotnet test --filter "Tag=smoke" --logger "trx;LogFileName=smoke-results.trx"

- name: Run all tests except slow
  run: dotnet test --filter "Tag!=slow" --logger "trx;LogFileName=all-results.trx"

- name: Run integration tests (only on main)
  if: github.ref == 'refs/heads/main'
  run: dotnet test --filter "Tag=integration"
```

---

## 📚 Additional Resources

- [TinyBDD GitHub](https://github.com/JerrettDavis/TinyBDD)
- [TinyBDD README](C:\git\TinyBDD\README.md)
- [Example Refactored Test](./test/JD.MSBuild.Fluent.Tests/BddFluentApiTests.Refactored.Example.cs)
- [xUnit Trait Filtering](https://xunit.net/docs/running-tests-in-vs#filtering-tests)

---

## 🆘 Troubleshooting

### Issue: Feature attribute not creating trait

**Solution**: Ensure TinyBDD.Xunit package is up to date:
```bash
dotnet list package | grep TinyBDD
dotnet add package TinyBDD.Xunit --version <latest>
```

### Issue: Test output not showing steps

**Solution**: Verify `ITestOutputHelper` is passed to base class:
```csharp
public MyTests(ITestOutputHelper output) : base(output) { }
```

### Issue: Finally not executing

**Solution**: Ensure `.AssertPassed()` is called (triggers Finally handlers):
```csharp
await Given(...)
    .Finally(...)
    .Then(...)
    .AssertPassed();  // Required!
```

### Issue: Performance regression after refactoring

**Solution**: Check if optimization is disabled. Most tests should NOT have `[DisableOptimization]`.

---

**Happy Testing!** 🎉
