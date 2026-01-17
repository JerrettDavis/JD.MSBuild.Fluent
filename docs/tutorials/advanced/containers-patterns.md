# Tutorial: Recreating Docker Container Patterns

Master build extensibility patterns by recreating Docker integration techniques, including Dockerfile generation, publish hooks, and customization points.

## Overview

In this tutorial, you'll learn advanced extensibility patterns by building a Docker integration package similar to JD.MSBuild.Containers:

- Dynamic Dockerfile generation from project properties
- Integration with dotnet publish pipeline
- Pre/post publish hooks for customization
- Extensibility points for user scripts
- Property-driven container configuration
- Multi-stage build support

**Time**: ~45 minutes  
**Difficulty**: Advanced  
**Context**: Learn patterns for packages that integrate with MSBuild publish pipeline

## What You'll Learn

By completing this tutorial, you will:

- ✅ Generate files dynamically during build
- ✅ Integrate with dotnet publish lifecycle
- ✅ Create extensibility hooks (pre/post scripts)
- ✅ Implement property-driven code generation
- ✅ Support user customization points
- ✅ Handle conditional file generation
- ✅ Create sophisticated publish workflows

## Prerequisites

- Completed [Creating a Build Integration Package](../intermediate/build-integration.md) tutorial
- Understanding of Docker and Dockerfile syntax
- Familiarity with dotnet publish process
- Experience with MSBuild publish targets

## The Scenario

You're building a Docker integration package that:

1. Generates Dockerfiles from project properties
2. Runs before/after publish with user-provided scripts
3. Builds Docker images during publish
4. Provides extensibility hooks for customization
5. Supports multi-stage Docker builds
6. Allows per-configuration customization

This mirrors real-world Docker tooling packages!

## Step 1: Project Setup

```bash
mkdir DockerIntegration.Build
cd DockerIntegration.Build
dotnet new classlib -n DockerIntegration.Build
cd DockerIntegration.Build
dotnet add package JD.MSBuild.Fluent
```

## Step 2: Package Structure

Create `PackageFactory.cs`:

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace DockerIntegration.Build;

public static class PackageFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("DockerIntegration.Build")
            .Description("Docker integration for .NET projects with extensibility hooks")
            .Props(ConfigureProps)
            .Targets(ConfigureTargets)
            .Build();
    }

    private static void ConfigureProps(PropsBuilder props)
    {
        ConfigureDockerProperties(props);
        ConfigureExtensibilityPoints(props);
    }

    private static void ConfigureTargets(TargetsBuilder targets)
    {
        ConfigureDockerfileGeneration(targets);
        ConfigurePublishIntegration(targets);
        ConfigureExtensibilityHooks(targets);
        ConfigureImageBuild(targets);
    }

    // ... implementation next
}
```

## Step 3: Docker Configuration Properties

Properties that control Docker generation:

```csharp
private static void ConfigureDockerProperties(PropsBuilder props)
{
    props.Comment("==============================================");
    props.Comment(" Docker Integration Configuration");
    props.Comment("==============================================");
    
    // Core settings
    props.PropertyGroup(null, group =>
    {
        group.Comment("Enable Docker integration");
        group.Property("DockerEnabled", "false");
        
        group.Comment("Docker image name (defaults to project name)");
        group.Property("DockerImageName", "$(MSBuildProjectName.ToLower())",
            condition: "'$(DockerImageName)' == ''");
        
        group.Comment("Docker image tag");
        group.Property("DockerImageTag", "latest",
            condition: "'$(DockerImageTag)' == ''");
        
        group.Comment("Base image for runtime");
        group.Property("DockerBaseImage", "mcr.microsoft.com/dotnet/aspnet:8.0",
            condition: "'$(DockerBaseImage)' == ''");
        
        group.Comment("SDK image for build stage");
        group.Property("DockerSdkImage", "mcr.microsoft.com/dotnet/sdk:8.0",
            condition: "'$(DockerSdkImage)' == ''");
    }, label: "Docker Configuration");
    
    // Dockerfile generation
    props.PropertyGroup(null, group =>
    {
        group.Comment("Generate Dockerfile automatically");
        group.Property("DockerGenerateDockerfile", "true",
            condition: "'$(DockerGenerateDockerfile)' == ''");
        
        group.Comment("Output path for generated Dockerfile");
        group.Property("DockerfileOutputPath", "$(MSBuildProjectDirectory)",
            condition: "'$(DockerfileOutputPath)' == ''");
        
        group.Comment("Dockerfile filename");
        group.Property("DockerfileName", "Dockerfile",
            condition: "'$(DockerfileName)' == ''");
    }, label: "Dockerfile Generation");
    
    // Build configuration
    props.PropertyGroup(null, group =>
    {
        group.Comment("Build Docker image during publish");
        group.Property("DockerBuildOnPublish", "true",
            condition: "'$(DockerBuildOnPublish)' == ''");
        
        group.Comment("Docker build context path");
        group.Property("DockerBuildContext", "$(MSBuildProjectDirectory)",
            condition: "'$(DockerBuildContext)' == ''");
        
        group.Comment("Additional Docker build arguments");
        group.Property("DockerBuildArgs", "",
            condition: "'$(DockerBuildArgs)' == ''");
    }, label: "Docker Build");
}
```

## Step 4: Extensibility Points

Properties that define user customization hooks:

```csharp
private static void ConfigureExtensibilityPoints(PropsBuilder props)
{
    props.Comment("==============================================");
    props.Comment(" Extensibility Hooks");
    props.Comment("==============================================");
    
    props.PropertyGroup(null, group =>
    {
        group.Comment("Script to run before Dockerfile generation");
        group.Property("DockerPreGenerateScript", "");
        
        group.Comment("Script to run after Dockerfile generation");
        group.Property("DockerPostGenerateScript", "");
        
        group.Comment("Script to run before Docker build");
        group.Property("DockerPreBuildScript", "");
        
        group.Comment("Script to run after Docker build");
        group.Property("DockerPostBuildScript", "");
        
        group.Comment("Custom Dockerfile template path");
        group.Property("DockerCustomTemplate", "");
    }, label: "Extensibility Scripts");
    
    // Item definitions for additional files
    props.Comment("Define DockerCopyFile item for files to copy to image");
    props.ItemGroup(null, group =>
    {
        group.Comment("Use: <DockerCopyFile Include=\"config.json\" TargetPath=\"/app/config\" />");
    });
}
```

## Pattern 1: Dynamic Dockerfile Generation

## Step 5: Dockerfile Generation Target

Generate Dockerfile from project properties:

```csharp
private static void ConfigureDockerfileGeneration(TargetsBuilder targets)
{
    targets.Comment("==============================================");
    targets.Comment(" Dockerfile Generation");
    targets.Comment("==============================================");
    
    targets.Target("Docker_GenerateDockerfile", target =>
    {
        target.Label("Generate Dockerfile from project properties");
        target.BeforeTargets("Docker_BuildImage");
        target.Condition("'$(DockerEnabled)' == 'true' and " +
                        "'$(DockerGenerateDockerfile)' == 'true'");
        
        target.Message("Generating Dockerfile for $(DockerImageName):$(DockerImageTag)", 
            importance: "High");
        
        // Compute output path
        target.PropertyGroup(null, group =>
        {
            group.Property("_DockerfileFullPath", 
                "$(DockerfileOutputPath)\\$(DockerfileName)");
        });
        
        // Build Dockerfile content
        target.PropertyGroup(null, group =>
        {
            group.Comment("Build multi-stage Dockerfile content");
            
            // Stage 1: Build
            group.Property("_DockerfileBuildStage",
                "# Build stage^" +
                "FROM $(DockerSdkImage) AS build^" +
                "WORKDIR /src^" +
                "^" +
                "# Copy project file^" +
                "COPY [\"$(MSBuildProjectFile)\", \"./\"]^" +
                "^" +
                "# Restore dependencies^" +
                "RUN dotnet restore \"$(MSBuildProjectFile)\"^" +
                "^" +
                "# Copy source^" +
                "COPY . .^" +
                "^" +
                "# Build^" +
                "RUN dotnet build \"$(MSBuildProjectFile)\" -c $(Configuration) -o /app/build^" +
                "^" +
                "# Publish^" +
                "RUN dotnet publish \"$(MSBuildProjectFile)\" -c $(Configuration) -o /app/publish /p:UseAppHost=false");
            
            // Stage 2: Runtime
            group.Property("_DockerfileRuntimeStage",
                "^" +
                "# Runtime stage^" +
                "FROM $(DockerBaseImage) AS runtime^" +
                "WORKDIR /app^" +
                "^" +
                "# Copy published output^" +
                "COPY --from=build /app/publish .^");
            
            // Additional copy files
            group.Property("_DockerfileCopyFiles", "",
                condition: "'@(DockerCopyFile)' == ''");
            
            // Entry point
            group.Property("_DockerfileEntrypoint",
                "^" +
                "# Entry point^" +
                "ENTRYPOINT [\"dotnet\", \"$(TargetFileName)\"]");
            
            // Combine all stages
            group.Property("_DockerfileContent",
                "# Auto-generated Dockerfile by DockerIntegration.Build^" +
                "# Project: $(MSBuildProjectName)^" +
                "# Configuration: $(Configuration)^" +
                "# Generated: $([System.DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss'))^" +
                "^" +
                "$(_DockerfileBuildStage)" +
                "$(_DockerfileRuntimeStage)" +
                "$(_DockerfileCopyFiles)" +
                "$(_DockerfileEntrypoint)");
        });
        
        // Handle custom copy files
        target.PropertyGroup("'@(DockerCopyFile)' != ''", group =>
        {
            group.Property("_DockerfileCopyFiles",
                "^" +
                "# Additional files^" +
                "@(DockerCopyFile->'COPY [\"%(Identity)\", \"%(TargetPath)\"]^', '')");
        });
        
        // Write Dockerfile
        target.Task("WriteLinesToFile", task =>
        {
            task.Param("File", "$(_DockerfileFullPath)");
            task.Param("Lines", "$(_DockerfileContent)");
            task.Param("Overwrite", "true");
            task.Param("Encoding", "UTF-8");
        });
        
        target.Message("Generated Dockerfile: $(_DockerfileFullPath)", 
            importance: "High");
    });
}
```

**Key Techniques**:
- **Multi-stage Dockerfile**: Separate build and runtime stages
- **Property substitution**: Use MSBuild properties in generated content
- **Line breaks**: `^` for newlines in MSBuild
- **Item transforms**: `@(DockerCopyFile->'...')` generates COPY commands
- **Dynamic content**: Compute Dockerfile sections conditionally

## Pattern 2: Publish Pipeline Integration

## Step 6: Integrate with Publish

Hook into dotnet publish workflow:

```csharp
private static void ConfigurePublishIntegration(TargetsBuilder targets)
{
    targets.Comment("==============================================");
    targets.Comment(" Publish Pipeline Integration");
    targets.Comment("==============================================");
    
    targets.Target("Docker_BeforePublish", target =>
    {
        target.Label("Pre-publish hook for Docker integration");
        target.BeforeTargets("Publish");
        target.DependsOnTargets("Docker_GenerateDockerfile");
        target.Condition("'$(DockerEnabled)' == 'true'");
        
        target.Message("Docker integration: Pre-publish phase", importance: "High");
        
        // Validate Docker is available
        target.Exec("docker --version", workingDirectory: "$(MSBuildProjectDirectory)");
        
        target.Message("Docker validation complete", importance: "Normal");
    });
    
    targets.Target("Docker_AfterPublish", target =>
    {
        target.Label("Post-publish hook for Docker integration");
        target.AfterTargets("Publish");
        target.DependsOnTargets("Docker_BuildImage");
        target.Condition("'$(DockerEnabled)' == 'true'");
        
        target.Message("Docker integration: Post-publish phase", importance: "High");
        target.Message("Docker image: $(DockerImageName):$(DockerImageTag)", 
            importance: "High");
    });
}
```

## Pattern 3: Extensibility Hooks

## Step 7: User Script Execution

Allow users to run custom scripts at key points:

```csharp
private static void ConfigureExtensibilityHooks(TargetsBuilder targets)
{
    targets.Comment("==============================================");
    targets.Comment(" Extensibility Hooks for User Scripts");
    targets.Comment("==============================================");
    
    // Pre-generate hook
    targets.Target("Docker_ExecutePreGenerateScript", target =>
    {
        target.Label("Execute user script before Dockerfile generation");
        target.BeforeTargets("Docker_GenerateDockerfile");
        target.Condition("'$(DockerEnabled)' == 'true' and " +
                        "'$(DockerPreGenerateScript)' != '' and " +
                        "Exists('$(DockerPreGenerateScript)')");
        
        target.Message("Executing pre-generate script: $(DockerPreGenerateScript)", 
            importance: "High");
        
        // Detect script type and run appropriately
        target.PropertyGroup(null, group =>
        {
            group.Property("_DockerScriptExtension", 
                "$([System.IO.Path]::GetExtension('$(DockerPreGenerateScript)'))");
        });
        
        // PowerShell script
        target.Exec(
            "powershell -ExecutionPolicy Bypass -File \"$(DockerPreGenerateScript)\" " +
            "-ProjectDir \"$(MSBuildProjectDirectory)\" " +
            "-Configuration \"$(Configuration)\"",
            workingDirectory: "$(MSBuildProjectDirectory)",
            condition: "'$(_DockerScriptExtension)' == '.ps1'");
        
        // Bash script
        target.Exec(
            "bash \"$(DockerPreGenerateScript)\" " +
            "\"$(MSBuildProjectDirectory)\" " +
            "\"$(Configuration)\"",
            workingDirectory: "$(MSBuildProjectDirectory)",
            condition: "'$(_DockerScriptExtension)' == '.sh'");
        
        // Batch script
        target.Exec(
            "cmd /c \"$(DockerPreGenerateScript)\" " +
            "\"$(MSBuildProjectDirectory)\" " +
            "\"$(Configuration)\"",
            workingDirectory: "$(MSBuildProjectDirectory)",
            condition: "'$(_DockerScriptExtension)' == '.cmd' or '$(_DockerScriptExtension)' == '.bat'");
        
        target.Message("Pre-generate script completed", importance: "Normal");
    });
    
    // Post-generate hook
    targets.Target("Docker_ExecutePostGenerateScript", target =>
    {
        target.Label("Execute user script after Dockerfile generation");
        target.AfterTargets("Docker_GenerateDockerfile");
        target.Condition("'$(DockerEnabled)' == 'true' and " +
                        "'$(DockerPostGenerateScript)' != '' and " +
                        "Exists('$(DockerPostGenerateScript)')");
        
        target.Message("Executing post-generate script: $(DockerPostGenerateScript)", 
            importance: "High");
        
        target.PropertyGroup(null, group =>
        {
            group.Property("_DockerScriptExtension", 
                "$([System.IO.Path]::GetExtension('$(DockerPostGenerateScript)'))");
        });
        
        // Execute script based on extension (same pattern as pre-generate)
        target.Exec(
            "powershell -ExecutionPolicy Bypass -File \"$(DockerPostGenerateScript)\" " +
            "-DockerfilePath \"$(_DockerfileFullPath)\" " +
            "-ImageName \"$(DockerImageName)\"",
            workingDirectory: "$(MSBuildProjectDirectory)",
            condition: "'$(_DockerScriptExtension)' == '.ps1'");
        
        target.Message("Post-generate script completed", importance: "Normal");
    });
    
    // Pre-build hook
    targets.Target("Docker_ExecutePreBuildScript", target =>
    {
        target.Label("Execute user script before Docker build");
        target.BeforeTargets("Docker_BuildImage");
        target.Condition("'$(DockerEnabled)' == 'true' and " +
                        "'$(DockerPreBuildScript)' != '' and " +
                        "Exists('$(DockerPreBuildScript)')");
        
        target.Message("Executing pre-build script: $(DockerPreBuildScript)", 
            importance: "High");
        
        target.PropertyGroup(null, group =>
        {
            group.Property("_DockerScriptExtension", 
                "$([System.IO.Path]::GetExtension('$(DockerPreBuildScript)'))");
        });
        
        target.Exec(
            "powershell -ExecutionPolicy Bypass -File \"$(DockerPreBuildScript)\" " +
            "-Context \"$(DockerBuildContext)\" " +
            "-ImageName \"$(DockerImageName):$(DockerImageTag)\"",
            workingDirectory: "$(MSBuildProjectDirectory)",
            condition: "'$(_DockerScriptExtension)' == '.ps1'");
    });
    
    // Post-build hook
    targets.Target("Docker_ExecutePostBuildScript", target =>
    {
        target.Label("Execute user script after Docker build");
        target.AfterTargets("Docker_BuildImage");
        target.Condition("'$(DockerEnabled)' == 'true' and " +
                        "'$(DockerPostBuildScript)' != '' and " +
                        "Exists('$(DockerPostBuildScript)')");
        
        target.Message("Executing post-build script: $(DockerPostBuildScript)", 
            importance: "High");
        
        target.PropertyGroup(null, group =>
        {
            group.Property("_DockerScriptExtension", 
                "$([System.IO.Path]::GetExtension('$(DockerPostBuildScript)'))");
        });
        
        target.Exec(
            "powershell -ExecutionPolicy Bypass -File \"$(DockerPostBuildScript)\" " +
            "-ImageName \"$(DockerImageName):$(DockerImageTag)\" " +
            "-BuildSuccess \"true\"",
            workingDirectory: "$(MSBuildProjectDirectory)",
            condition: "'$(_DockerScriptExtension)' == '.ps1'");
    });
}
```

**Key Techniques**:
- **Script type detection**: Use file extension to determine execution method
- **Parameter passing**: Pass MSBuild properties as script arguments
- **Conditional execution**: Only run if script exists and Docker is enabled
- **Cross-platform**: Support PowerShell, bash, and batch scripts
- **Multiple hook points**: Pre/post for both generation and build

## Pattern 4: Docker Image Build

## Step 8: Build Docker Image

Execute Docker build with user options:

```csharp
private static void ConfigureImageBuild(TargetsBuilder targets)
{
    targets.Comment("==============================================");
    targets.Comment(" Docker Image Build");
    targets.Comment("==============================================");
    
    targets.Target("Docker_BuildImage", target =>
    {
        target.Label("Build Docker image from Dockerfile");
        target.AfterTargets("Docker_BeforePublish");
        target.BeforeTargets("Docker_AfterPublish");
        target.Condition("'$(DockerEnabled)' == 'true' and " +
                        "'$(DockerBuildOnPublish)' == 'true'");
        
        target.Message("Building Docker image: $(DockerImageName):$(DockerImageTag)", 
            importance: "High");
        
        // Compute Docker build command
        target.PropertyGroup(null, group =>
        {
            group.Comment("Construct Docker build command");
            
            // Base command
            group.Property("_DockerBuildCommand",
                "docker build " +
                "-t $(DockerImageName):$(DockerImageTag) " +
                "-f \"$(_DockerfileFullPath)\"");
            
            // Add build args if specified
            group.Property("_DockerBuildCommand",
                "$(_DockerBuildCommand) $(DockerBuildArgs)",
                condition: "'$(DockerBuildArgs)' != ''");
            
            // Add platform if specified
            group.Property("_DockerBuildCommand",
                "$(_DockerBuildCommand) --platform $(DockerPlatform)",
                condition: "'$(DockerPlatform)' != ''");
            
            // Add build context
            group.Property("_DockerBuildCommand",
                "$(_DockerBuildCommand) \"$(DockerBuildContext)\"");
        });
        
        target.Message("Docker build command: $(_DockerBuildCommand)", 
            importance: "Low");
        
        // Execute Docker build
        target.Exec(
            "$(_DockerBuildCommand)",
            workingDirectory: "$(MSBuildProjectDirectory)");
        
        target.Message("Docker image built successfully: $(DockerImageName):$(DockerImageTag)", 
            importance: "High");
        
        // Output image info
        target.Exec(
            "docker images $(DockerImageName):$(DockerImageTag)",
            workingDirectory: "$(MSBuildProjectDirectory)");
    });
    
    // Optional: Push image to registry
    targets.Target("Docker_PushImage", target =>
    {
        target.Label("Push Docker image to registry");
        target.AfterTargets("Docker_BuildImage");
        target.Condition("'$(DockerEnabled)' == 'true' and " +
                        "'$(DockerPushToRegistry)' == 'true' and " +
                        "'$(DockerRegistry)' != ''");
        
        target.Message("Pushing image to registry: $(DockerRegistry)", 
            importance: "High");
        
        // Tag image for registry
        target.Exec(
            "docker tag $(DockerImageName):$(DockerImageTag) " +
            "$(DockerRegistry)/$(DockerImageName):$(DockerImageTag)",
            workingDirectory: "$(MSBuildProjectDirectory)");
        
        // Push to registry
        target.Exec(
            "docker push $(DockerRegistry)/$(DockerImageName):$(DockerImageTag)",
            workingDirectory: "$(MSBuildProjectDirectory)");
        
        target.Message("Image pushed successfully", importance: "High");
    });
}
```

**Key Techniques**:
- **Dynamic command construction**: Build Docker command from properties
- **Conditional arguments**: Add flags only when specified
- **Multiple commands**: Tag and push for registry support
- **Output capture**: Show Docker image info after build

## Complete Example Project

Here's how a user would use your package:

### Project File

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    
    <!-- Enable Docker integration -->
    <DockerEnabled>true</DockerEnabled>
    <DockerImageName>myapp</DockerImageName>
    <DockerImageTag>v1.0.0</DockerImageTag>
    
    <!-- Customize base images -->
    <DockerBaseImage>mcr.microsoft.com/dotnet/aspnet:8.0-alpine</DockerBaseImage>
    <DockerSdkImage>mcr.microsoft.com/dotnet/sdk:8.0-alpine</DockerSdkImage>
    
    <!-- Extensibility hooks -->
    <DockerPreGenerateScript>scripts\prepare-docker.ps1</DockerPreGenerateScript>
    <DockerPostBuildScript>scripts\test-image.ps1</DockerPostBuildScript>
    
    <!-- Push to registry -->
    <DockerPushToRegistry>false</DockerPushToRegistry>
    <DockerRegistry>myregistry.azurecr.io</DockerRegistry>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Additional files to copy -->
    <DockerCopyFile Include="appsettings.Production.json" 
                     TargetPath="/app/config/" />
  </ItemGroup>
</Project>
```

### Pre-Generate Script (scripts/prepare-docker.ps1)

```powershell
param(
    [string]$ProjectDir,
    [string]$Configuration
)

Write-Host "Preparing Docker build for $ProjectDir"
Write-Host "Configuration: $Configuration"

# Custom preparation logic
# e.g., download runtime dependencies, generate config files
```

### Post-Build Script (scripts/test-image.ps1)

```powershell
param(
    [string]$ImageName,
    [string]$BuildSuccess
)

Write-Host "Testing Docker image: $ImageName"

# Run container for testing
docker run --rm -d --name test-container -p 8080:80 $ImageName
Start-Sleep -Seconds 5

# Test endpoint
$response = Invoke-WebRequest -Uri "http://localhost:8080/health"
if ($response.StatusCode -eq 200) {
    Write-Host "Health check passed!"
} else {
    Write-Error "Health check failed!"
    exit 1
}

# Cleanup
docker stop test-container
```

## Publishing with Docker

```bash
# Publish project - triggers Docker build
dotnet publish -c Release

# Output shows:
# Generating Dockerfile for myapp:v1.0.0
# Executing pre-generate script: scripts\prepare-docker.ps1
# Generated Dockerfile: Dockerfile
# Building Docker image: myapp:v1.0.0
# Successfully built abc123def456
# Executing post-build script: scripts\test-image.ps1
# Testing Docker image: myapp:v1.0.0
# Health check passed!
# Docker image: myapp:v1.0.0
```

## Generated Dockerfile Example

Your package generates:

```dockerfile
# Auto-generated Dockerfile by DockerIntegration.Build
# Project: MyWebApp
# Configuration: Release
# Generated: 2024-12-20 14:30:15

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project file
COPY ["MyWebApp.csproj", "./"]

# Restore dependencies
RUN dotnet restore "MyWebApp.csproj"

# Copy source
COPY . .

# Build
RUN dotnet build "MyWebApp.csproj" -c Release -o /app/build

# Publish
RUN dotnet publish "MyWebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Additional files
COPY ["appsettings.Production.json", "/app/config/"]

# Entry point
ENTRYPOINT ["dotnet", "MyWebApp.dll"]
```

## What You Learned

Congratulations! You've mastered publish integration and extensibility patterns:

✅ **Dynamic file generation** from project properties  
✅ **Publish pipeline integration** with Before/AfterTargets  
✅ **Extensibility hooks** for user scripts  
✅ **Multi-stage command construction**  
✅ **Cross-platform script execution**  
✅ **Property-driven code generation**  
✅ **Sophisticated customization points**  

## Key Concepts

- **Publish Integration**: Hook into dotnet publish with Before/AfterTargets
- **Dynamic Generation**: Build complex file content from properties
- **Extensibility Hooks**: Allow users to inject custom logic at key points
- **Script Execution**: Detect and run PowerShell, bash, batch scripts
- **Property Substitution**: Use MSBuild properties in generated content
- **Item Transforms**: Generate repeated content from item collections
- **Command Construction**: Build complex CLI commands dynamically

## Real-World Applications

These patterns are used in:

- **JD.MSBuild.Containers**: Docker integration for .NET
- **Microsoft.NET.Sdk.Publish**: Built-in publish targets
- **Azure DevOps tasks**: Build and deployment automation
- **Custom deployment tools**: Cloud platform integrations

## Next Steps

You've completed all tutorials! Next:

- **Apply patterns to your own packages**
- **Read [Best Practices](../../user-guides/best-practices/index.md)** for production patterns
- **Explore [API Reference](../../user-guides/core-concepts/builders.md)** for all features
- **Study real packages**: JD.Efcpt.Build, JD.MSBuild.Containers source code

## Challenge: Extend the Package

Try these exercises:

1. **Add Docker Compose support**: Generate docker-compose.yml files
2. **Implement image tagging strategies**: Support semantic versioning, git SHA
3. **Add image scanning**: Integrate with Trivy or Snyk
4. **Support multiple registries**: Push to multiple registries simultaneously
5. **Add health checks**: Generate HEALTHCHECK instructions in Dockerfile
6. **Implement caching**: Use Docker BuildKit cache mounts

## Common Pitfalls

### Line Breaks in Generated Content

```csharp
// ✅ Correct - use ^ for newlines
group.Property("_Content",
    "Line 1^" +
    "Line 2^" +
    "Line 3");

// ❌ Wrong - literal newlines don't work
group.Property("_Content",
    @"Line 1
    Line 2
    Line 3");
```

### Script Path Detection

```csharp
// ✅ Correct - check exists before running
target.Condition("Exists('$(DockerPreBuildScript)')");

// ❌ Wrong - may fail if script missing
target.Condition("'$(DockerPreBuildScript)' != ''");
```

### Property Evaluation in Generated Files

```csharp
// ✅ Correct - escape MSBuild properties
group.Property("_Dockerfile",
    "ENV PROJECT_NAME=$(MSBuildProjectName)");  // Evaluates during generation

// If you want literal $(var) in output:
group.Property("_Dockerfile",
    "ENV PROJECT_NAME=$$(MSBuildProjectName)");  // Double $$ escapes
```

## Troubleshooting

**Dockerfile not generated?**
- Check `DockerEnabled` and `DockerGenerateDockerfile` properties
- Verify target conditions evaluate to true
- Use `-v:normal` to see target execution

**Script not executing?**
- Verify script file exists at specified path
- Check file extension matches supported types (.ps1, .sh, .cmd, .bat)
- Ensure script is executable (chmod +x on Linux/macOS)

**Docker build fails?**
- Check generated Dockerfile for syntax errors
- Verify Docker is installed and running
- Review Docker build command with `-v:diagnostic`

**Properties not substituted?**
- Ensure properties are defined before target runs
- Check property evaluation phase (props vs targets)
- Use diagnostic messages to trace values

## Related Documentation

- [Working with Targets](../../user-guides/targets-tasks/targets.md)
- [Best Practices - Code Organization](../../user-guides/best-practices/index.md#code-organization)
- [Recreating JD.Efcpt.Build Patterns](efcpt-patterns.md)
- [MSBuild Publish Targets](https://learn.microsoft.com/en-us/dotnet/core/deploying/deploy-with-cli)
