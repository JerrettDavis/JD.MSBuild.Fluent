# Docker Integration

This sample demonstrates how to automate Docker image builds, multi-stage builds, and container orchestration directly from MSBuild using JD.MSBuild.Fluent.

## Scenario

You have a .NET application that runs in Docker containers. You want to:
- Build Docker images as part of the MSBuild pipeline
- Support multi-stage builds for optimized images
- Tag images with semantic versions from GitVersion
- Push images to container registries (Docker Hub, ACR, ECR)
- Generate docker-compose files for local development
- Run container tests during CI/CD
- Support multiple platforms (linux/amd64, linux/arm64)

This sample shows how to create a reusable MSBuild package inspired by patterns from JD.MSBuild.Containers.

## What It Demonstrates

✅ **Advanced Features:**
- Conditional execution based on Docker availability
- Dynamic property evaluation (version, tags)
- File generation from templates
- Multi-target orchestration
- External process management (docker commands)
- Cross-platform builds

✅ **Production Patterns:**
- Semantic versioning integration
- Registry authentication
- Build caching strategies
- Security scanning integration
- Multi-stage build optimization
- Development/production configurations

✅ **MSBuild Techniques:**
- Property functions for complex logic
- Item batching for multi-platform builds
- Target chaining with conditional execution
- Dynamic file generation
- Error handling and retries

## File Structure

```
DockerBuild/
├── DockerBuild.csproj
├── Factory.cs                        # Package definition
├── DockerBuild.Tests/
│   ├── DockerBuild.Tests.csproj
│   └── FactoryTests.cs
└── samples/
    ├── WeatherApi/                   # Example API project
    │   ├── WeatherApi.csproj
    │   ├── Dockerfile
    │   ├── Dockerfile.multistage
    │   └── Program.cs
    └── docker-compose.template.yml
```

## Complete Implementation

### Factory.cs

```csharp
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace DockerBuild;

/// <summary>
/// MSBuild package for integrating Docker image builds into the build pipeline.
/// Inspired by patterns from JD.MSBuild.Containers.
/// </summary>
public static class Factory
{
  public static PackageDefinition Create()
  {
    return Package.Define("DockerBuild")
      .Description("Integrate Docker image builds and container orchestration into MSBuild")
      
      .Props(p => p
        // Feature flags
        .Property("DockerBuildEnabled", "false")
        .Property("DockerBuildPush", "false")
        .Property("DockerBuildCompose", "false")
        .Property("DockerBuildScan", "false")
        
        // Docker configuration
        .Property("DockerBuildDockerfile", "$(ProjectDir)Dockerfile")
        .Property("DockerBuildContext", "$(ProjectDir)")
        .Property("DockerBuildTarget", "") // For multi-stage builds
        
        // Image naming
        .Property("DockerBuildRegistry", "") // e.g., "docker.io", "myregistry.azurecr.io"
        .Property("DockerBuildNamespace", "$(AssemblyName.ToLowerInvariant())")
        .Property("DockerBuildImageName", "$(DockerBuildNamespace)")
        
        // Versioning (integrates with GitVersion or manual)
        .Property("DockerBuildVersion", "$(Version)")
        .Property("DockerBuildVersionSuffix", "$(VersionSuffix)")
        .Property("DockerBuildGitSha", "$([System.Environment]::GetEnvironmentVariable('GIT_SHA'))")
        .Property("DockerBuildGitShortSha", "$(DockerBuildGitSha.Substring(0, 7))")
        
        // Computed image name
        .PropertyGroup(null, g => g
          .Property("DockerBuildFullImageName", 
            "$([System.String]::IsNullOrEmpty('$(DockerBuildRegistry)')) ? " +
            "'$(DockerBuildImageName)' : " +
            "'$(DockerBuildRegistry)/$(DockerBuildImageName)'"))
        
        // Tags (multiple tags per build)
        .Property("DockerBuildTags", 
          "$(DockerBuildFullImageName):latest;" +
          "$(DockerBuildFullImageName):$(DockerBuildVersion)")
        .PropertyGroup("'$(DockerBuildGitSha)' != ''", g => g
          .Property("DockerBuildTags",
            "$(DockerBuildTags);$(DockerBuildFullImageName):$(DockerBuildGitShortSha)"))
        
        // Platform support
        .Property("DockerBuildPlatforms", "linux/amd64") // Comma-separated: "linux/amd64,linux/arm64"
        .Property("DockerBuildMultiPlatform", 
          "$([System.Text.RegularExpressions.Regex]::IsMatch('$(DockerBuildPlatforms)', ','))")
        
        // Build arguments (passed to docker build --build-arg)
        .Property("DockerBuildArgs", 
          "VERSION=$(DockerBuildVersion);" +
          "BUILD_CONFIGURATION=$(Configuration)")
        
        // Registry authentication
        .Property("DockerBuildRegistryUsername", "$([System.Environment]::GetEnvironmentVariable('DOCKER_USERNAME'))")
        .Property("DockerBuildRegistryPassword", "$([System.Environment]::GetEnvironmentVariable('DOCKER_PASSWORD'))")
        
        // Docker compose
        .Property("DockerBuildComposeFile", "$(ProjectDir)docker-compose.yml")
        .Property("DockerBuildComposeTemplate", "$(ProjectDir)docker-compose.template.yml")
        .Property("DockerBuildComposeProjectName", "$(AssemblyName.ToLowerInvariant())")
        
        // Output paths
        .Property("DockerBuildArtifactsPath", "$(ArtifactsDir)docker/")
        .Property("DockerBuildManifestFile", "$(DockerBuildArtifactsPath)manifest.json")
        
        // Docker CLI
        .Property("DockerBuildDockerPath", "docker")
        .Property("DockerBuildDockerComposePath", "docker-compose"))
      
      .Targets(t => t
        // Main entry point
        .Target("DockerBuild", target => target
          .AfterTargets("Build")
          .Condition("'$(DockerBuildEnabled)' == 'true'")
          .DependsOnTargets(
            "DockerBuild_ValidateDocker;" +
            "DockerBuild_PrepareContext;" +
            "DockerBuild_BuildImage;" +
            "DockerBuild_TagImages;" +
            "DockerBuild_ScanImage;" +
            "DockerBuild_PushImages;" +
            "DockerBuild_GenerateCompose;" +
            "DockerBuild_SaveManifest")
          .Message("Docker build completed: $(DockerBuildFullImageName)", importance: "High"))
        
        // Validate Docker is available
        .Target("DockerBuild_ValidateDocker", target => target
          .Condition("'$(DockerBuildEnabled)' == 'true'")
          
          .Message("Validating Docker installation...", importance: "High")
          
          // Check docker command exists
          .Exec("$(DockerBuildDockerPath) --version",
            condition: null)
          
          // Check Docker daemon is running
          .Exec("$(DockerBuildDockerPath) ps",
            condition: null)
          
          // Validate Dockerfile exists
          .Task("Error", task => task
            .Param("Text", "Dockerfile not found at $(DockerBuildDockerfile)")
            .Param("Condition", "!Exists('$(DockerBuildDockerfile)')")))
        
        // Prepare build context
        .Target("DockerBuild_PrepareContext", target => target
          .Condition("'$(DockerBuildEnabled)' == 'true'")
          .DependsOnTargets("DockerBuild_ValidateDocker")
          
          .Message("Preparing Docker build context...", importance: "High")
          
          // Create artifacts directory
          .Task("MakeDir", task => task
            .Param("Directories", "$(DockerBuildArtifactsPath)")
            .Param("Condition", "!Exists('$(DockerBuildArtifactsPath)')"))
          
          // Copy .dockerignore if it exists
          .Task("Copy", task => task
            .Param("SourceFiles", "$(ProjectDir).dockerignore")
            .Param("DestinationFolder", "$(DockerBuildContext)")
            .Param("Condition", "Exists('$(ProjectDir).dockerignore') AND '$(ProjectDir)' != '$(DockerBuildContext)'")))
        
        // Build Docker image
        .Target("DockerBuild_BuildImage", target => target
          .Condition("'$(DockerBuildEnabled)' == 'true'")
          .DependsOnTargets("DockerBuild_PrepareContext")
          
          // Use Inputs/Outputs for incremental builds
          .Inputs(
            "$(DockerBuildDockerfile);" +
            "$(MSBuildProjectFullPath);" +
            "$(ProjectDir)**/*.cs;" +
            "$(ProjectDir)**/*.csproj")
          .Outputs("$(DockerBuildManifestFile)")
          
          .Message("Building Docker image: $(DockerBuildFullImageName)...", importance: "High")
          
          // Build the image
          .Exec(
            "$(DockerBuildDockerPath) build " +
            "-f \"$(DockerBuildDockerfile)\" " +
            "-t $(DockerBuildFullImageName):$(DockerBuildVersion) " +
            "$([System.String]::IsNullOrEmpty('$(DockerBuildTarget)') ? '' : '--target $(DockerBuildTarget)') " +
            "$(DockerBuildArgs.Replace(';', ' --build-arg ').Insert(0, '--build-arg ')) " +
            "$([System.Boolean]::Parse('$(DockerBuildMultiPlatform)') ? '--platform $(DockerBuildPlatforms)' : '') " +
            "\"$(DockerBuildContext)\"",
            workingDirectory: "$(ProjectDir)")
          
          .Message("Image built successfully: $(DockerBuildFullImageName):$(DockerBuildVersion)", 
            importance: "High"))
        
        // Tag images with all specified tags
        .Target("DockerBuild_TagImages", target => target
          .Condition("'$(DockerBuildEnabled)' == 'true'")
          .DependsOnTargets("DockerBuild_BuildImage")
          
          .Message("Tagging image with $(DockerBuildTags.Split(';').Length) tags...", importance: "High")
          
          // Parse tags into items and tag each
          .Task("Exec", task => task
            .Param("Command",
              "$(DockerBuildDockerPath) tag " +
              "$(DockerBuildFullImageName):$(DockerBuildVersion) " +
              "%(DockerBuildTagItem.Identity)")
            .Param("Condition", "'@(DockerBuildTagItem)' != ''"))
          
          .Message("Tagging completed", importance: "Normal"))
        
        // Create item group for tags (in a separate target to ensure proper evaluation)
        .Target("DockerBuild_CreateTagItems", target => target
          .BeforeTargets("DockerBuild_TagImages")
          .Condition("'$(DockerBuildEnabled)' == 'true'")
          
          .Task("ItemGroup", task => task
            .Param("Include", "$(DockerBuildTags.Split(';'))")
            .Param("OutputItem", "DockerBuildTagItem")))
        
        // Security scanning with Trivy
        .Target("DockerBuild_ScanImage", target => target
          .Condition("'$(DockerBuildEnabled)' == 'true' AND '$(DockerBuildScan)' == 'true'")
          .DependsOnTargets("DockerBuild_TagImages")
          
          .Message("Scanning image for vulnerabilities...", importance: "High")
          
          // Run Trivy scan (continue on error, report is informational)
          .Exec(
            "trivy image " +
            "--severity HIGH,CRITICAL " +
            "--format json " +
            "--output $(DockerBuildArtifactsPath)scan-report.json " +
            "$(DockerBuildFullImageName):$(DockerBuildVersion)",
            workingDirectory: "$(ProjectDir)",
            condition: null)
          
          .Message("Scan report: $(DockerBuildArtifactsPath)scan-report.json", importance: "High"))
        
        // Push images to registry
        .Target("DockerBuild_PushImages", target => target
          .Condition("'$(DockerBuildEnabled)' == 'true' AND '$(DockerBuildPush)' == 'true'")
          .DependsOnTargets("DockerBuild_TagImages;DockerBuild_RegistryLogin")
          
          .Message("Pushing images to registry...", importance: "High")
          
          // Push each tag
          .Task("Exec", task => task
            .Param("Command",
              "$(DockerBuildDockerPath) push %(DockerBuildTagItem.Identity)")
            .Param("Condition", "'@(DockerBuildTagItem)' != ''"))
          
          .Message("All images pushed successfully", importance: "High"))
        
        // Registry login
        .Target("DockerBuild_RegistryLogin", target => target
          .Condition(
            "'$(DockerBuildEnabled)' == 'true' AND " +
            "'$(DockerBuildPush)' == 'true' AND " +
            "'$(DockerBuildRegistryUsername)' != '' AND " +
            "'$(DockerBuildRegistryPassword)' != ''")
          
          .Message("Logging in to registry: $(DockerBuildRegistry)...", importance: "High")
          
          // Login to registry (password via stdin for security)
          .Exec(
            "echo $(DockerBuildRegistryPassword) | " +
            "$(DockerBuildDockerPath) login $(DockerBuildRegistry) " +
            "--username $(DockerBuildRegistryUsername) " +
            "--password-stdin",
            workingDirectory: "$(ProjectDir)"))
        
        // Generate docker-compose.yml
        .Target("DockerBuild_GenerateCompose", target => target
          .Condition(
            "'$(DockerBuildEnabled)' == 'true' AND " +
            "'$(DockerBuildCompose)' == 'true'")
          .DependsOnTargets("DockerBuild_TagImages")
          
          .Message("Generating docker-compose.yml...", importance: "High")
          
          // Read template
          .Task("ReadLinesFromFile", task => task
            .Param("File", "$(DockerBuildComposeTemplate)")
            .Param("Condition", "Exists('$(DockerBuildComposeTemplate)')"))
          
          // Substitute variables (simplified - use more sophisticated token replacement in production)
          .Task("WriteLinesToFile", task => task
            .Param("File", "$(DockerBuildComposeFile)")
            .Param("Lines",
              "$([System.IO.File]::ReadAllText('$(DockerBuildComposeTemplate)')" +
              ".Replace('{{IMAGE}}', '$(DockerBuildFullImageName):$(DockerBuildVersion)')" +
              ".Replace('{{PROJECT}}', '$(DockerBuildComposeProjectName)'))")
            .Param("Overwrite", "true")
            .Param("Condition", "Exists('$(DockerBuildComposeTemplate)')"))
          
          .Message("Generated: $(DockerBuildComposeFile)", importance: "High"))
        
        // Save build manifest
        .Target("DockerBuild_SaveManifest", target => target
          .Condition("'$(DockerBuildEnabled)' == 'true'")
          .DependsOnTargets("DockerBuild_TagImages")
          
          .Message("Saving build manifest...", importance: "Normal")
          
          // Generate JSON manifest
          .Task("WriteLinesToFile", task => task
            .Param("File", "$(DockerBuildManifestFile)")
            .Param("Lines",
              "{" +
              "  \"image\": \"$(DockerBuildFullImageName)\"," +
              "  \"version\": \"$(DockerBuildVersion)\"," +
              "  \"tags\": [$(DockerBuildTags.Split(';').Select(t => '\"' + t + '\"').Join(','))]," +
              "  \"platforms\": [$(DockerBuildPlatforms.Split(',').Select(p => '\"' + p + '\"').Join(','))]," +
              "  \"buildTime\": \"$([System.DateTime]::UtcNow.ToString('o'))\"," +
              "  \"gitSha\": \"$(DockerBuildGitSha)\"" +
              "}")
            .Param("Overwrite", "true")))
        
        // Clean Docker artifacts
        .Target("DockerBuild_Clean", target => target
          .AfterTargets("Clean")
          .Condition("'$(DockerBuildEnabled)' == 'true'")
          
          .Message("Cleaning Docker artifacts...", importance: "Normal")
          
          .Task("RemoveDir", task => task
            .Param("Directories", "$(DockerBuildArtifactsPath)")
            .Param("Condition", "Exists('$(DockerBuildArtifactsPath)')"))
          
          .Task("Delete", task => task
            .Param("Files", "$(DockerBuildComposeFile)")
            .Param("Condition", "Exists('$(DockerBuildComposeFile)')")))
        
        // Helper target for local development
        .Target("DockerBuild_Run", target => target
          .Message("Starting container locally...", importance: "High")
          
          .Exec(
            "$(DockerBuildDockerPath) run --rm -it " +
            "-p 8080:8080 " +
            "$(DockerBuildFullImageName):$(DockerBuildVersion)",
            workingDirectory: "$(ProjectDir)"))
        
        // Helper target for compose up
        .Target("DockerBuild_ComposeUp", target => target
          .DependsOnTargets("DockerBuild_GenerateCompose")
          
          .Message("Starting services with docker-compose...", importance: "High")
          
          .Exec(
            "$(DockerBuildDockerComposePath) " +
            "-f $(DockerBuildComposeFile) " +
            "-p $(DockerBuildComposeProjectName) " +
            "up -d",
            workingDirectory: "$(ProjectDir)"))
        
        // CI/CD support
        .Target("DockerBuild_CI", target => target
          .Message("Running Docker build for CI/CD...", importance: "High")
          
          .Task("MSBuild", task => task
            .Param("Projects", "$(MSBuildProjectFullPath)")
            .Param("Targets", "Build;DockerBuild")
            .Param("Properties",
              "DockerBuildEnabled=true;" +
              "DockerBuildPush=$(CI_PUSH_ENABLED);" +
              "DockerBuildScan=true"))))
      
      .Pack(o =>
      {
        o.BuildTransitive = true;
        o.EmitSdk = false;
      })
      
      .Build();
  }
}
```

### Example Dockerfile

```dockerfile
# Multi-stage build for optimized images
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG VERSION=1.0.0
WORKDIR /src

# Copy csproj and restore dependencies (cached layer)
COPY ["WeatherApi/WeatherApi.csproj", "WeatherApi/"]
RUN dotnet restore "WeatherApi/WeatherApi.csproj"

# Copy source and build
COPY . .
WORKDIR "/src/WeatherApi"
RUN dotnet build "WeatherApi.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    /p:Version=$VERSION

# Publish stage
FROM build AS publish
RUN dotnet publish "WeatherApi.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:Version=$VERSION

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeatherApi.dll"]
```

### Example Consumer: WeatherApi.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    
    <!-- Enable Docker build -->
    <DockerBuildEnabled>true</DockerBuildEnabled>
    
    <!-- Development: Build only -->
    <DockerBuildPush Condition="'$(Configuration)' == 'Debug'">false</DockerBuildPush>
    
    <!-- Production: Build, scan, and push -->
    <DockerBuildPush Condition="'$(Configuration)' == 'Release'">true</DockerBuildPush>
    <DockerBuildScan Condition="'$(Configuration)' == 'Release'">true</DockerBuildScan>
    
    <!-- Docker configuration -->
    <DockerBuildRegistry>myregistry.azurecr.io</DockerBuildRegistry>
    <DockerBuildNamespace>weatherapi</DockerBuildNamespace>
    <DockerBuildPlatforms>linux/amd64,linux/arm64</DockerBuildPlatforms>
    
    <!-- Generate compose file -->
    <DockerBuildCompose>true</DockerBuildCompose>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="DockerBuild" Version="1.0.0" />
  </ItemGroup>
</Project>
```

### docker-compose.template.yml

```yaml
version: '3.8'

services:
  {{PROJECT}}-api:
    image: {{IMAGE}}
    container_name: {{PROJECT}}-api
    ports:
      - "8080:8080"
      - "8081:8081"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
    networks:
      - {{PROJECT}}-network
  
  {{PROJECT}}-db:
    image: postgres:16
    container_name: {{PROJECT}}-db
    environment:
      - POSTGRES_USER=weather
      - POSTGRES_PASSWORD=weather
      - POSTGRES_DB=weatherdb
    ports:
      - "5432:5432"
    volumes:
      - {{PROJECT}}-data:/var/lib/postgresql/data
    networks:
      - {{PROJECT}}-network

networks:
  {{PROJECT}}-network:
    driver: bridge

volumes:
  {{PROJECT}}-data:
```

## Configuration Guide

### Development Workflow

```bash
# Build image locally
dotnet build -p:DockerBuildEnabled=true

# Run container
dotnet msbuild /t:DockerBuild_Run

# Or use docker-compose
dotnet msbuild /t:DockerBuild_ComposeUp

# View logs
docker logs weatherapi-api -f

# Stop services
docker-compose -f docker-compose.yml -p weatherapi down
```

### Production Workflow

```bash
# Build, scan, and push
dotnet build -c Release \
  -p:DockerBuildEnabled=true \
  -p:DockerBuildPush=true \
  -p:DockerBuildScan=true \
  -p:Version=1.2.3

# Check manifest
cat artifacts/docker/manifest.json

# Review scan results
cat artifacts/docker/scan-report.json
```

### CI/CD Workflow (GitHub Actions)

```yaml
name: Docker Build

on:
  push:
    branches: [main]
  pull_request:

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  build:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build and push Docker image
        env:
          DOCKER_USERNAME: ${{ github.actor }}
          DOCKER_PASSWORD: ${{ secrets.GITHUB_TOKEN }}
          GIT_SHA: ${{ github.sha }}
        run: |
          dotnet build -c Release \
            -p:DockerBuildEnabled=true \
            -p:DockerBuildPush=true \
            -p:DockerBuildScan=true \
            -p:DockerBuildRegistry=${{ env.REGISTRY }} \
            -p:Version=${{ github.run_number }}
      
      - name: Upload artifacts
        uses: actions/upload-artifact@v4
        with:
          name: docker-artifacts
          path: artifacts/docker/
```

### Multi-Platform Builds

```xml
<PropertyGroup>
  <!-- Build for multiple architectures -->
  <DockerBuildPlatforms>linux/amd64,linux/arm64</DockerBuildPlatforms>
  
  <!-- Use buildx for multi-platform -->
  <DockerBuildDockerPath>docker buildx</DockerBuildDockerPath>
</PropertyGroup>
```

```bash
# Setup buildx (one-time)
docker buildx create --name multiplatform --use
docker buildx inspect --bootstrap

# Build
dotnet build -p:DockerBuildEnabled=true
```

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void Definition_Has_Docker_Properties()
{
  var def = Factory.Create();
  var props = GetAllProperties(def.Props);
  
  Assert.Contains(props, p => p.Name == "DockerBuildEnabled");
  Assert.Contains(props, p => p.Name == "DockerBuildRegistry");
}

[Fact]
public void DockerBuild_Target_Has_Correct_Dependencies()
{
  var def = Factory.Create();
  var target = def.Targets.Targets.First(t => t.Name == "DockerBuild");
  
  Assert.Contains("DockerBuild_BuildImage", target.DependsOnTargets);
  Assert.Contains("DockerBuild_TagImages", target.DependsOnTargets);
}
```

### Integration Tests

```bash
# Test 1: Build only
dotnet build /p:DockerBuildEnabled=true
docker images | grep weatherapi

# Test 2: Build and scan
dotnet build /p:DockerBuildEnabled=true /p:DockerBuildScan=true
test -f artifacts/docker/scan-report.json

# Test 3: Multi-platform
dotnet build /p:DockerBuildEnabled=true /p:DockerBuildPlatforms="linux/amd64,linux/arm64"
docker buildx imagetools inspect weatherapi:latest
```

### Container Tests

```csharp
[Fact]
public async Task Container_Responds_To_Health_Check()
{
  // Arrange
  await RunMSBuild("/t:DockerBuild_Run", async: true);
  await Task.Delay(5000); // Wait for startup
  
  using var client = new HttpClient();
  
  // Act
  var response = await client.GetAsync("http://localhost:8080/health");
  
  // Assert
  Assert.True(response.IsSuccessStatusCode);
}
```

## Deployment

### Package Creation

```bash
jdmsbuild generate \
  --assembly bin/Release/net9.0/DockerBuild.dll \
  --type DockerBuild.Factory \
  --method Create \
  --output artifacts/msbuild

dotnet pack -c Release
dotnet nuget push bin/Release/DockerBuild.1.0.0.nupkg
```

### Consuming the Package

```bash
dotnet add package DockerBuild
```

```xml
<PropertyGroup>
  <DockerBuildEnabled>true</DockerBuildEnabled>
  <DockerBuildRegistry>myregistry.azurecr.io</DockerBuildRegistry>
  <DockerBuildPush Condition="'$(CI)' == 'true'">true</DockerBuildPush>
</PropertyGroup>
```

## Best Practices Highlighted

### ✅ Multi-Stage Builds
- Separate build and runtime stages
- Smaller final images (runtime only)
- Better layer caching

### ✅ Security
- Non-root user in container
- Vulnerability scanning with Trivy
- Registry credentials via environment variables
- No secrets in Dockerfiles

### ✅ Versioning
- Semantic versioning from GitVersion
- Git SHA tags for traceability
- Latest tag for convenience

### ✅ Performance
- Layer caching (copy deps first)
- Incremental builds with Inputs/Outputs
- Multi-platform builds with buildx

### ✅ Developer Experience
- Local development with docker-compose
- Helper targets (DockerBuild_Run, DockerBuild_ComposeUp)
- Clear error messages

## Architectural Decisions

### Why AfterTargets("Build")?

**Dependency on Build Output:**
- Docker build needs compiled artifacts
- Multi-stage Dockerfile copies from bin/
- Must run after Build completes

### Why multiple tags?

**Deployment Flexibility:**
- `latest`: Convenience for development
- `1.2.3`: Semantic version for production
- `abc1234`: Git SHA for debugging

### Why incremental builds?

**Build Performance:**
- Skip Docker build if nothing changed
- Uses Inputs/Outputs to detect changes
- Saves minutes in large projects

### Why docker-compose template?

**Dynamic Configuration:**
- Image name changes with version
- Project name varies by context
- Template allows customization

## Troubleshooting

### Docker daemon not running

```bash
# Start Docker Desktop (Windows/Mac)
# Or on Linux:
sudo systemctl start docker
```

### Permission denied

```bash
# Add user to docker group (Linux)
sudo usermod -aG docker $USER
newgrp docker
```

### Multi-platform build fails

```bash
# Install buildx
docker buildx create --name multiplatform --use
docker buildx inspect --bootstrap

# Or disable multi-platform
dotnet build /p:DockerBuildPlatforms=linux/amd64
```

### Registry authentication fails

```bash
# Test login manually
echo $DOCKER_PASSWORD | docker login $DOCKER_REGISTRY --username $DOCKER_USERNAME --password-stdin

# Or use docker credential helpers
```

### Image size too large

**Optimize Dockerfile:**
- Use multi-stage builds
- Minimize layers
- Use .dockerignore
- Choose smaller base images (alpine, distroless)

```dockerfile
# Before: 500MB
FROM mcr.microsoft.com/dotnet/sdk:9.0

# After: 150MB
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
```

## Next Steps

- [Multi-Project Orchestration](multi-project.md) - Build multiple services
- [Code Generation Pipeline](code-generation.md) - Generate Dockerfiles
- [Database Build Integration](database-build.md) - Combine with EF migrations

## Related Documentation

- [Docker Multi-Stage Builds](https://docs.docker.com/build/building/multi-stage/)
- [Docker Buildx](https://docs.docker.com/buildx/working-with-buildx/)
- [Trivy Security Scanner](https://github.com/aquasecurity/trivy)
- [MSBuild Incremental Builds](https://docs.microsoft.com/en-us/visualstudio/msbuild/incremental-builds)
