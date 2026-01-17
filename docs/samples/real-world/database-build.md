# Database Build Integration

This sample demonstrates how to integrate Entity Framework Core migrations and database deployment directly into your MSBuild pipeline using JD.MSBuild.Fluent.

## Scenario

You have a database project that uses EF Core migrations. You want to:
- Automatically apply migrations during development builds
- Generate SQL scripts for production deployment
- Validate migration history before building
- Bundle migration scripts in your deployment package
- Support multiple database providers (SQL Server, PostgreSQL, SQLite)
- Run database tests as part of CI/CD

This sample shows how to create a reusable MSBuild package that handles all of this.

## What It Demonstrates

✅ **Advanced Features:**
- Conditional target execution based on properties
- External tool invocation (dotnet ef)
- File dependency tracking (Inputs/Outputs)
- Multi-stage build pipeline (validate → migrate → package)
- Provider-specific configuration
- Error handling and validation

✅ **Production Patterns:**
- Safe defaults with opt-in behavior
- Fail-fast validation
- Idempotent operations
- Logging and diagnostics
- CI/CD friendly design

✅ **MSBuild Techniques:**
- Exec tasks with working directories
- Property functions for path manipulation
- Item manipulation (collecting migration scripts)
- Target chaining with DependsOnTargets
- Incremental builds with Inputs/Outputs

## File Structure

```
DatabaseBuild/
├── DatabaseBuild.csproj
├── Factory.cs                        # Package definition
├── DatabaseBuild.Tests/
│   ├── DatabaseBuild.Tests.csproj
│   └── FactoryTests.cs
└── samples/
    ├── BookStore.Data/               # Example database project
    │   ├── BookStore.Data.csproj
    │   ├── BookStoreContext.cs
    │   └── Migrations/
    └── BookStore.Api/                # Consumer project
        └── BookStore.Api.csproj
```

## Complete Implementation

### Factory.cs

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace DatabaseBuild;

/// <summary>
/// MSBuild package for integrating Entity Framework Core database builds.
/// Inspired by patterns from JD.Efcpt.Build.
/// </summary>
public static class Factory
{
  public static PackageDefinition Create()
  {
    return Package.Define("DatabaseBuild")
      .Description("Integrate EF Core migrations and database deployment into MSBuild")
      
      .Props(p => p
        // Feature flags (all disabled by default for safety)
        .Property("DatabaseBuildEnabled", "false")
        .Property("DatabaseBuildAutoMigrate", "false")
        .Property("DatabaseBuildGenerateScripts", "false")
        .Property("DatabaseBuildValidateOnly", "false")
        
        // Configuration
        .Property("DatabaseBuildProvider", "SqlServer") // SqlServer, PostgreSQL, SQLite
        .Property("DatabaseBuildStartupProject", "$(MSBuildProjectFullPath)")
        .Property("DatabaseBuildProject", "$(MSBuildProjectFullPath)")
        .Property("DatabaseBuildConfiguration", "$(Configuration)")
        .Property("DatabaseBuildContext", "") // Empty = auto-detect
        
        // Connection strings (use environment variables in production)
        .PropertyGroup("'$(DatabaseBuildProvider)' == 'SqlServer'", g => g
          .Property("DatabaseBuildConnectionString", 
            "Server=(localdb)\\mssqllocaldb;Database=$(MSBuildProjectName);Integrated Security=true"))
        .PropertyGroup("'$(DatabaseBuildProvider)' == 'PostgreSQL'", g => g
          .Property("DatabaseBuildConnectionString",
            "Host=localhost;Database=$(MSBuildProjectName);Username=postgres;Password=postgres"))
        .PropertyGroup("'$(DatabaseBuildProvider)' == 'SQLite'", g => g
          .Property("DatabaseBuildConnectionString",
            "Data Source=$(BaseIntermediateOutputPath)$(MSBuildProjectName).db"))
        
        // Output paths
        .Property("DatabaseBuildScriptOutputPath", "$(ArtifactsDir)migrations/")
        .Property("DatabaseBuildScriptFileName", "$(MSBuildProjectName)-$(DatabaseBuildProvider).sql")
        
        // Tool paths
        .Property("DatabaseBuildDotNetEfPath", "dotnet ef")
        
        // Incremental build support
        .Property("DatabaseBuildMigrationsPath", "$(ProjectDir)Migrations/")
        .Property("DatabaseBuildLastRunMarker", "$(BaseIntermediateOutputPath)DatabaseBuild.lastrun"))
      
      .Targets(t => t
        // Main entry point - hooks into BeforeBuild
        .Target("DatabaseBuild", target => target
          .BeforeTargets("Build")
          .Condition("'$(DatabaseBuildEnabled)' == 'true'")
          .DependsOnTargets("DatabaseBuild_Validate;DatabaseBuild_Migrate;DatabaseBuild_GenerateScripts")
          .Message("Database build completed successfully", importance: "High"))
        
        // Validate migrations and context
        .Target("DatabaseBuild_Validate", target => target
          .Condition("'$(DatabaseBuildEnabled)' == 'true'")
          // Check if migrations folder exists
          .Task("Error", task => task
            .Param("Text", "Migrations folder not found at $(DatabaseBuildMigrationsPath). Run 'dotnet ef migrations add Initial' first.")
            .Param("Condition", "!Exists('$(DatabaseBuildMigrationsPath)')"))
          
          // List migrations (validates EF tools and configuration)
          .Exec(
            "$(DatabaseBuildDotNetEfPath) migrations list " +
            "--project \"$(DatabaseBuildProject)\" " +
            "--startup-project \"$(DatabaseBuildStartupProject)\" " +
            "--configuration $(DatabaseBuildConfiguration) " +
            "--no-build",
            workingDirectory: "$(ProjectDir)",
            condition: "!$(DatabaseBuildValidateOnly)"))
        
        // Apply migrations to database
        .Target("DatabaseBuild_Migrate", target => target
          .Condition("'$(DatabaseBuildEnabled)' == 'true' AND '$(DatabaseBuildAutoMigrate)' == 'true' AND '$(DatabaseBuildValidateOnly)' == 'false'")
          .DependsOnTargets("DatabaseBuild_Validate")
          
          .Message("Applying migrations to $(DatabaseBuildProvider) database...", importance: "High")
          
          // Update database
          .Exec(
            "$(DatabaseBuildDotNetEfPath) database update " +
            "--project \"$(DatabaseBuildProject)\" " +
            "--startup-project \"$(DatabaseBuildStartupProject)\" " +
            "--configuration $(DatabaseBuildConfiguration) " +
            "--connection \"$(DatabaseBuildConnectionString)\" " +
            "--no-build",
            workingDirectory: "$(ProjectDir)")
          
          // Write marker file for incremental builds
          .Task("WriteLinesToFile", task => task
            .Param("File", "$(DatabaseBuildLastRunMarker)")
            .Param("Lines", "$([System.DateTime]::UtcNow.ToString('o'))")
            .Param("Overwrite", "true")))
        
        // Generate SQL scripts
        .Target("DatabaseBuild_GenerateScripts", target => target
          .Condition("'$(DatabaseBuildEnabled)' == 'true' AND '$(DatabaseBuildGenerateScripts)' == 'true'")
          .DependsOnTargets("DatabaseBuild_Validate")
          
          // Use Inputs/Outputs for incremental builds
          .Inputs("$(DatabaseBuildMigrationsPath)**/*.cs")
          .Outputs("$(DatabaseBuildScriptOutputPath)$(DatabaseBuildScriptFileName)")
          
          .Message("Generating migration scripts to $(DatabaseBuildScriptOutputPath)...", importance: "High")
          
          // Create output directory
          .Task("MakeDir", task => task
            .Param("Directories", "$(DatabaseBuildScriptOutputPath)")
            .Param("Condition", "!Exists('$(DatabaseBuildScriptOutputPath)')"))
          
          // Generate idempotent script (safe to run multiple times)
          .Exec(
            "$(DatabaseBuildDotNetEfPath) migrations script " +
            "--project \"$(DatabaseBuildProject)\" " +
            "--startup-project \"$(DatabaseBuildStartupProject)\" " +
            "--configuration $(DatabaseBuildConfiguration) " +
            "--idempotent " +
            "--output \"$(DatabaseBuildScriptOutputPath)$(DatabaseBuildScriptFileName)\" " +
            "--no-build",
            workingDirectory: "$(ProjectDir)")
          
          .Message("Script generated: $(DatabaseBuildScriptOutputPath)$(DatabaseBuildScriptFileName)", importance: "High"))
        
        // Add generated scripts to publish output
        .Target("DatabaseBuild_IncludeScripts", target => target
          .AfterTargets("ComputeFilesToPublish")
          .Condition("'$(DatabaseBuildEnabled)' == 'true' AND '$(DatabaseBuildGenerateScripts)' == 'true'")
          
          .Task("ItemGroup", task => task
            .Param("Include", "$(DatabaseBuildScriptOutputPath)**/*.sql")
            .Param("DestinationRelativePath", "migrations/%(Filename)%(Extension)")
            .Param("OutputItem", "ResolvedFileToPublish")))
        
        // Clean generated files
        .Target("DatabaseBuild_Clean", target => target
          .AfterTargets("Clean")
          .Condition("'$(DatabaseBuildEnabled)' == 'true'")
          
          .Task("RemoveDir", task => task
            .Param("Directories", "$(DatabaseBuildScriptOutputPath)")
            .Param("Condition", "Exists('$(DatabaseBuildScriptOutputPath)')"))
          
          .Task("Delete", task => task
            .Param("Files", "$(DatabaseBuildLastRunMarker)")
            .Param("Condition", "Exists('$(DatabaseBuildLastRunMarker)')")))
        
        // CI/CD support - validate without applying
        .Target("DatabaseBuild_CI", target => target
          .Message("Running database build validation for CI/CD...", importance: "High")
          .Task("MSBuild", task => task
            .Param("Projects", "$(MSBuildProjectFullPath)")
            .Param("Targets", "DatabaseBuild")
            .Param("Properties", 
              "DatabaseBuildEnabled=true;" +
              "DatabaseBuildValidateOnly=true;" +
              "DatabaseBuildAutoMigrate=false;" +
              "DatabaseBuildGenerateScripts=true"))))
      
      .Pack(o =>
      {
        o.BuildTransitive = true;
        o.EmitSdk = false; // Not an SDK, just a build package
      })
      
      .Build();
  }
}
```

### DatabaseBuild.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <PackageId>DatabaseBuild</PackageId>
    <Version>1.0.0</Version>
    <Authors>Your Company</Authors>
    <Description>Integrate EF Core migrations into MSBuild</Description>
    <PackageTags>msbuild;efcore;database;migrations</PackageTags>
    
    <DevelopmentDependency>true</DevelopmentDependency>
    <IncludeBuildOutput>false</IncludeBuildOutput>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="JD.MSBuild.Fluent" Version="1.0.0" />
  </ItemGroup>

  <ItemGroup>
    <None Include="$(ArtifactsDir)msbuild/**/*" Pack="true" PackagePath="" />
  </ItemGroup>
</Project>
```

### Example Consumer: BookStore.Data.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <!-- Enable database build integration -->
    <DatabaseBuildEnabled>true</DatabaseBuildEnabled>
    
    <!-- Development: Auto-migrate on build -->
    <DatabaseBuildAutoMigrate Condition="'$(Configuration)' == 'Debug'">true</DatabaseBuildAutoMigrate>
    
    <!-- Production: Generate scripts only -->
    <DatabaseBuildGenerateScripts Condition="'$(Configuration)' == 'Release'">true</DatabaseBuildGenerateScripts>
    
    <!-- Configuration -->
    <DatabaseBuildProvider>SqlServer</DatabaseBuildProvider>
    <DatabaseBuildContext>BookStoreContext</DatabaseBuildContext>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="DatabaseBuild" Version="1.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

### BookStoreContext.cs

```csharp
using Microsoft.EntityFrameworkCore;

namespace BookStore.Data;

public class BookStoreContext : DbContext
{
  public BookStoreContext(DbContextOptions<BookStoreContext> options) 
    : base(options) { }
  
  public DbSet<Book> Books => Set<Book>();
  public DbSet<Author> Authors => Set<Author>();
  
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Book>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
      entity.Property(e => e.ISBN).HasMaxLength(13);
      entity.HasOne(e => e.Author)
        .WithMany(a => a.Books)
        .HasForeignKey(e => e.AuthorId);
    });
    
    modelBuilder.Entity<Author>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
    });
  }
}

public class Book
{
  public int Id { get; set; }
  public string Title { get; set; } = string.Empty;
  public string? ISBN { get; set; }
  public int AuthorId { get; set; }
  public Author Author { get; set; } = null!;
}

public class Author
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public List<Book> Books { get; set; } = new();
}
```

## Configuration Guide

### Development Workflow

```bash
# 1. Create initial migration
cd BookStore.Data
dotnet ef migrations add Initial

# 2. Build (auto-applies migration)
dotnet build
# Output: "Applying migrations to SqlServer database..."
# Output: "Database build completed successfully"

# 3. Add new migration
dotnet ef migrations add AddPublishDate
dotnet build  # Applies automatically
```

### Production Workflow

```bash
# 1. Build in Release mode (generates scripts, doesn't apply)
dotnet build -c Release
# Output: "Generating migration scripts to artifacts/migrations/..."
# Output: "Script generated: artifacts/migrations/BookStore.Data-SqlServer.sql"

# 2. Review generated script
cat artifacts/migrations/BookStore.Data-SqlServer.sql

# 3. Deploy to production database
# (Use your deployment tool: Azure Pipelines, Octopus, etc.)
sqlcmd -S prod-server -d BookStoreDb -i artifacts/migrations/BookStore.Data-SqlServer.sql
```

### CI/CD Workflow

```yaml
# Azure Pipelines example
- task: DotNetCoreCLI@2
  displayName: 'Validate Database Build'
  inputs:
    command: 'build'
    projects: 'BookStore.Data/BookStore.Data.csproj'
    arguments: '/t:DatabaseBuild_CI'

- task: PublishBuildArtifacts@1
  displayName: 'Publish Migration Scripts'
  inputs:
    PathtoPublish: 'artifacts/migrations'
    ArtifactName: 'migrations'
```

### Provider-Specific Configuration

#### SQL Server (default)

```xml
<PropertyGroup>
  <DatabaseBuildProvider>SqlServer</DatabaseBuildProvider>
  <DatabaseBuildConnectionString>Server=(localdb)\mssqllocaldb;Database=BookStore;Integrated Security=true</DatabaseBuildConnectionString>
</PropertyGroup>
```

#### PostgreSQL

```xml
<PropertyGroup>
  <DatabaseBuildProvider>PostgreSQL</DatabaseBuildProvider>
  <DatabaseBuildConnectionString>Host=localhost;Database=bookstore;Username=postgres;Password=postgres</DatabaseBuildConnectionString>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.0" />
</ItemGroup>
```

#### SQLite (great for testing)

```xml
<PropertyGroup>
  <DatabaseBuildProvider>SQLite</DatabaseBuildProvider>
  <DatabaseBuildConnectionString>Data Source=$(BaseIntermediateOutputPath)bookstore.db</DatabaseBuildConnectionString>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0" />
</ItemGroup>
```

## Testing Strategy

### Unit Tests (Definition Validation)

```csharp
[Fact]
public void Definition_Has_Required_Properties()
{
  var def = Factory.Create();
  var props = GetAllProperties(def.Props);
  
  Assert.Contains(props, p => p.Name == "DatabaseBuildEnabled");
  Assert.Contains(props, p => p.Name == "DatabaseBuildProvider");
}

[Fact]
public void Definition_Has_All_Targets()
{
  var def = Factory.Create();
  var targets = def.Targets.Targets.Select(t => t.Name).ToList();
  
  Assert.Contains("DatabaseBuild", targets);
  Assert.Contains("DatabaseBuild_Validate", targets);
  Assert.Contains("DatabaseBuild_Migrate", targets);
  Assert.Contains("DatabaseBuild_GenerateScripts", targets);
}
```

### Integration Tests (Real Builds)

```bash
# Test 1: Validate-only mode
dotnet build /p:DatabaseBuildEnabled=true /p:DatabaseBuildValidateOnly=true

# Test 2: Auto-migrate mode
dotnet build /p:DatabaseBuildEnabled=true /p:DatabaseBuildAutoMigrate=true

# Test 3: Script generation
dotnet build /p:DatabaseBuildEnabled=true /p:DatabaseBuildGenerateScripts=true

# Test 4: CI mode
dotnet build /t:DatabaseBuild_CI
```

### Automated Testing

```csharp
[Theory]
[InlineData("SqlServer")]
[InlineData("PostgreSQL")]
[InlineData("SQLite")]
public async Task Can_Build_With_Provider(string provider)
{
  // Arrange
  var projectPath = CreateTestProject(provider);
  
  // Act
  var result = await RunMSBuild(projectPath, new Dictionary<string, string>
  {
    ["DatabaseBuildEnabled"] = "true",
    ["DatabaseBuildAutoMigrate"] = "true",
    ["DatabaseBuildProvider"] = provider
  });
  
  // Assert
  Assert.True(result.Success);
  Assert.Contains("Database build completed successfully", result.Output);
}
```

## Deployment

### Package Creation

```bash
# Generate MSBuild assets
jdmsbuild generate \
  --assembly bin/Release/net9.0/DatabaseBuild.dll \
  --type DatabaseBuild.Factory \
  --method Create \
  --output artifacts/msbuild

# Pack
dotnet pack -c Release

# Publish
dotnet nuget push bin/Release/DatabaseBuild.1.0.0.nupkg
```

### Consuming the Package

```bash
dotnet add package DatabaseBuild
```

Enable in your database project:

```xml
<PropertyGroup>
  <DatabaseBuildEnabled>true</DatabaseBuildEnabled>
  <DatabaseBuildAutoMigrate Condition="'$(Configuration)' == 'Debug'">true</DatabaseBuildAutoMigrate>
  <DatabaseBuildGenerateScripts>true</DatabaseBuildGenerateScripts>
</PropertyGroup>
```

## Best Practices Highlighted

### ✅ Safe Defaults
- All features disabled by default (`DatabaseBuildEnabled=false`)
- Requires explicit opt-in for destructive operations
- Fail-fast validation before making changes

### ✅ Environment Separation
- Development: Auto-migrate (`DatabaseBuildAutoMigrate=true`)
- Production: Script generation only (`DatabaseBuildGenerateScripts=true`)
- CI: Validation only (`DatabaseBuild_CI` target)

### ✅ Incremental Builds
- Uses `Inputs`/`Outputs` to skip unchanged migrations
- Marker file tracks last successful run
- Clean target removes generated artifacts

### ✅ Provider Abstraction
- Single package supports multiple database providers
- Configuration-driven provider selection
- Connection strings configured per provider

### ✅ Idempotent Scripts
- `--idempotent` flag generates safe scripts
- Scripts check current database state
- Safe to run multiple times

## Architectural Decisions

### Why disabled by default?

**Safety First:**
- Database changes can be destructive
- Automatic migrations can cause data loss
- Users must explicitly opt-in

### Why separate validation and migration?

**Fail Fast:**
- Validate configuration before attempting changes
- Catch errors early (missing tools, invalid context)
- CI can validate without applying

### Why generate idempotent scripts?

**Production Safety:**
- Scripts can be reviewed before deployment
- Safe to run multiple times (won't fail if already applied)
- Supports partial deployment recovery

### Why use BeforeTargets("Build")?

**Early Execution:**
- Database must be current before code compilation
- Code might reference new schema elements
- Avoid runtime errors from schema mismatches

## Troubleshooting

### dotnet ef command not found

```bash
# Install globally
dotnet tool install --global dotnet-ef

# Or install locally
dotnet new tool-manifest
dotnet tool install dotnet-ef
```

Update property:

```xml
<PropertyGroup>
  <DatabaseBuildDotNetEfPath>dotnet tool run dotnet-ef</DatabaseBuildDotNetEfPath>
</PropertyGroup>
```

### Migrations not detected

**Check paths:**

```xml
<PropertyGroup>
  <DatabaseBuildMigrationsPath>$(ProjectDir)Migrations\</DatabaseBuildMigrationsPath>
</PropertyGroup>
```

**Verify folder exists:**

```bash
ls Migrations/
```

### Connection string errors

**Use environment variables:**

```xml
<PropertyGroup>
  <DatabaseBuildConnectionString Condition="'$(DB_CONNECTION_STRING)' != ''">$(DB_CONNECTION_STRING)</DatabaseBuildConnectionString>
</PropertyGroup>
```

```bash
export DB_CONNECTION_STRING="Server=localhost;Database=bookstore;..."
dotnet build
```

### Target runs but migrations don't apply

**Enable diagnostic logging:**

```bash
dotnet build /v:diag /p:DatabaseBuildEnabled=true
```

**Check output for errors:**

```bash
dotnet build /p:DatabaseBuildEnabled=true 2>&1 | tee build.log
grep -i "error" build.log
```

## Next Steps

- [Docker Integration](docker-integration.md) - Combine database and container builds
- [Code Generation Pipeline](code-generation.md) - Generate code from database schema
- [Multi-Project Orchestration](multi-project.md) - Coordinate multiple database projects

## Related Documentation

- [Entity Framework Core Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [MSBuild Targets](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-targets)
- [MSBuild Incremental Builds](https://docs.microsoft.com/en-us/visualstudio/msbuild/incremental-builds)
