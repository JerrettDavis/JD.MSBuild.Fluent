using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
#if !NET472
using System.Runtime.Loader;
#endif
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Packaging;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.MSBuild.Fluent.Tasks;

/// <summary>
/// MSBuild task that generates MSBuild assets (props/targets/SDK files) from a PackageDefinition factory method.
/// </summary>
public class GenerateMSBuildAssets : Task
{
  /// <summary>
  /// Path to the assembly containing the factory method.
  /// </summary>
  [Required]
  public string AssemblyFile { get; set; } = null!;

  /// <summary>
  /// Fully qualified type name containing the factory method.
  /// </summary>
  [Required]
  public string FactoryType { get; set; } = null!;

  /// <summary>
  /// Factory method name (must be static, parameterless, and return PackageDefinition).
  /// </summary>
  [Required]
  public string FactoryMethod { get; set; } = null!;

  /// <summary>
  /// Output directory for generated MSBuild assets.
  /// </summary>
  [Required]
  public string OutputPath { get; set; } = null!;

  /// <summary>
  /// Output: Generated MSBuild asset files.
  /// </summary>
  [Output]
  public ITaskItem[] GeneratedFiles { get; set; } = Array.Empty<ITaskItem>();

#if !NET472
  private class DefinitionFactoryContext : AssemblyLoadContext
  {
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _taskDirectory;

    public DefinitionFactoryContext(string assemblyPath, string taskDirectory) 
      : base(isCollectible: true)
    {
      _resolver = new AssemblyDependencyResolver(assemblyPath);
      _taskDirectory = taskDirectory;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
      // CRITICAL: Load JD.MSBuild.Fluent from the task directory to ensure type identity
      if (assemblyName.Name == "JD.MSBuild.Fluent")
      {
        var fluentPath = Path.Combine(_taskDirectory, "JD.MSBuild.Fluent.dll");
        if (File.Exists(fluentPath))
          return LoadFromAssemblyPath(fluentPath);
      }

      // Use the dependency resolver for other assemblies
      var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
      if (assemblyPath != null)
        return LoadFromAssemblyPath(assemblyPath);

      // Fall back to default context for framework assemblies
      return null;
    }
  }
#else
  static GenerateMSBuildAssets()
  {
    // For .NET Framework, register assembly resolver
    AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
  }

  private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
  {
    var assemblyName = new AssemblyName(args.Name);
    
    // Look in the same directory as the task DLL for assemblies
    var taskAssemblyLocation = typeof(GenerateMSBuildAssets).Assembly.Location;
    var taskDirectory = Path.GetDirectoryName(taskAssemblyLocation);
    if (string.IsNullOrEmpty(taskDirectory))
      return null;
    
    var assemblyPath = Path.Combine(taskDirectory, assemblyName.Name + ".dll");
    return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
  }
#endif

  public override bool Execute()
  {
    try
    {
      Log.LogMessage(MessageImportance.High, 
        $"JD.MSBuild.Fluent: Generating MSBuild assets from {FactoryType}.{FactoryMethod}()...");

      // Validate inputs
      if (!File.Exists(AssemblyFile))
      {
        Log.LogError($"Assembly not found: {AssemblyFile}");
        return false;
      }

      // Load the assembly and invoke the factory
      var definition = LoadDefinitionFromFactory();
      if (definition == null)
        return false;

      // Generate MSBuild assets by writing XML directly
      if (!Directory.Exists(OutputPath))
        Directory.CreateDirectory(OutputPath);
      
      var generatedFiles = new List<string>();
      
      // Write build files
      var buildDir = Path.Combine(OutputPath, "build");
      if (!Directory.Exists(buildDir))
        Directory.CreateDirectory(buildDir);
      
      if (_renderedXml?.BuildProps != null)
      {
        var path = Path.Combine(buildDir, $"{definition.Id}.props");
        File.WriteAllText(path, _renderedXml.BuildProps);
        generatedFiles.Add(path);
        Log.LogMessage(MessageImportance.Low, $"Wrote {path}");
      }
      
      if (_renderedXml?.BuildTargets != null)
      {
        var path = Path.Combine(buildDir, $"{definition.Id}.targets");
        File.WriteAllText(path, _renderedXml.BuildTargets);
        generatedFiles.Add(path);
        Log.LogMessage(MessageImportance.Low, $"Wrote {path}");
      }
      
      // Write buildTransitive files if enabled
      if (definition.Packaging.BuildTransitive)
      {
        var transitiveDir = Path.Combine(OutputPath, "buildTransitive");
        if (!Directory.Exists(transitiveDir))
          Directory.CreateDirectory(transitiveDir);
        
        if (_renderedXml?.BuildTransitiveProps != null)
        {
          var path = Path.Combine(transitiveDir, $"{definition.Id}.props");
          File.WriteAllText(path, _renderedXml.BuildTransitiveProps);
          generatedFiles.Add(path);
          Log.LogMessage(MessageImportance.Low, $"Wrote {path}");
        }
        
        if (_renderedXml?.BuildTransitiveTargets != null)
        {
          var path = Path.Combine(transitiveDir, $"{definition.Id}.targets");
          File.WriteAllText(path, _renderedXml.BuildTransitiveTargets);
          generatedFiles.Add(path);
          Log.LogMessage(MessageImportance.Low, $"Wrote {path}");
        }
      }
      
      // Write SDK files if enabled
      if (definition.Packaging.EmitSdk)
      {
        var sdkDir = definition.Packaging.SdkFlatLayout 
          ? Path.Combine(OutputPath, "Sdk")
          : Path.Combine(OutputPath, "Sdk", definition.Id);
        if (!Directory.Exists(sdkDir))
          Directory.CreateDirectory(sdkDir);
        
        if (_renderedXml?.SdkProps != null)
        {
          var path = Path.Combine(sdkDir, "Sdk.props");
          File.WriteAllText(path, _renderedXml.SdkProps);
          generatedFiles.Add(path);
          Log.LogMessage(MessageImportance.Low, $"Wrote {path}");
        }
        
        if (_renderedXml?.SdkTargets != null)
        {
          var path = Path.Combine(sdkDir, "Sdk.targets");
          File.WriteAllText(path, _renderedXml.SdkTargets);
          generatedFiles.Add(path);
          Log.LogMessage(MessageImportance.Low, $"Wrote {path}");
        }
      }

      GeneratedFiles = generatedFiles.Select(f => new TaskItem(f)).ToArray();

      Log.LogMessage(MessageImportance.High,
        $"JD.MSBuild.Fluent: Generated {generatedFiles.Count} file(s) to {OutputPath}");

      return true;
    }
    catch (Exception ex)
    {
      Log.LogErrorFromException(ex, showStackTrace: true);
      return false;
    }
  }

  private PackageDefinition? LoadDefinitionFromFactory()
  {
#if !NET472
    DefinitionFactoryContext? context = null;
#endif
    
    try
    {
      // Get task directory for loading JD.MSBuild.Fluent
      var taskAssemblyLocation = typeof(GenerateMSBuildAssets).Assembly.Location;
      var taskDirectory = Path.GetDirectoryName(taskAssemblyLocation);
      if (string.IsNullOrEmpty(taskDirectory))
      {
        Log.LogError("Could not determine task assembly directory");
        return null;
      }

#if !NET472
      // Create isolated load context that forces JD.MSBuild.Fluent to load from task directory
      context = new DefinitionFactoryContext(AssemblyFile, taskDirectory);
      
      // Load user assembly in the isolated context
      var assembly = context.LoadFromAssemblyPath(AssemblyFile);
#else
      // For .NET Framework, pre-load JD.MSBuild.Fluent from task directory
      var fluentAssemblyPath = Path.Combine(taskDirectory, "JD.MSBuild.Fluent.dll");
      if (File.Exists(fluentAssemblyPath))
      {
        var fluentAsm = Assembly.LoadFrom(fluentAssemblyPath);
        Log.LogMessage(MessageImportance.Low, $"Pre-loaded JD.MSBuild.Fluent from: {fluentAsm.Location}");
      }
      
      // Load user assembly
      var assembly = Assembly.LoadFrom(AssemblyFile);
#endif
      
      Log.LogMessage(MessageImportance.Low, 
        $"Loaded user assembly: {assembly.FullName} from {assembly.Location}");
      
      // Find type
      var type = assembly.GetType(FactoryType);
      if (type == null)
      {
        Log.LogError($"Type '{FactoryType}' not found in assembly {AssemblyFile}");
        return null;
      }

      // Find method
      var method = type.GetMethod(FactoryMethod, 
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
      
      if (method == null)
      {
        Log.LogError($"Method '{FactoryMethod}' not found on type {FactoryType}");
        return null;
      }

      // Validate method signature
      if (method.GetParameters().Length != 0)
      {
        Log.LogError($"Factory method {FactoryType}.{FactoryMethod} must be parameterless");
        return null;
      }

      if (method.ReturnType.FullName != "JD.MSBuild.Fluent.PackageDefinition")
      {
        Log.LogError($"Factory method {FactoryType}.{FactoryMethod} must return PackageDefinition (got {method.ReturnType.FullName})");
        return null;
      }

      // Invoke factory method
      var result = method.Invoke(null, null);
      if (result == null)
      {
        Log.LogError($"Factory method {FactoryType}.{FactoryMethod} returned null");
        return null;
      }

      // CRITICAL: Due to AssemblyLoadContext isolation, direct casting fails even when assemblies match.
      // Solution: Use reflection to render XML directly from user's objects, avoiding serialization.
      return ExtractPackageDefinitionViaReflection(result);
    }
    catch (Exception ex)
    {
      Log.LogErrorFromException(ex, showStackTrace: true);
      return null;
    }
    finally
    {
#if !NET472
      // Don't unload the context yet - we still need the PackageDefinition object
      // MSBuild will clean up after the task completes
#endif
    }
  }

  /// <summary>
  /// Extracts PackageDefinition data via reflection and renders projects to XML strings.
  /// This avoids serialization issues with abstract types across AssemblyLoadContext boundaries.
  /// </summary>
  private PackageDefinition? ExtractPackageDefinitionViaReflection(object userPackageDef)
  {
    var userType = userPackageDef.GetType();
    var userAssembly = userType.Assembly;
      
      Log.LogMessage(MessageImportance.Low, "Extracting PackageDefinition via reflection...");
      
      // Get Id property
      var idProp = userType.GetProperty("Id");
      var id = idProp?.GetValue(userPackageDef) as string;
      if (string.IsNullOrEmpty(id))
      {
        Log.LogError("PackageDefinition.Id is null or empty");
        return null;
      }
      
      // Create renderer from user's assembly
      var rendererType = userAssembly.GetType("JD.MSBuild.Fluent.Render.MsBuildXmlRenderer");
      if (rendererType == null)
      {
        Log.LogError("Could not find MsBuildXmlRenderer in user assembly");
        return null;
      }
      
      var renderer = Activator.CreateInstance(rendererType, new object?[] { null });
      var renderMethod = rendererType.GetMethod("RenderToString");
      if (renderMethod == null)
      {
        Log.LogError("Could not find RenderToString method on MsBuildXmlRenderer");
        return null;
      }
      
      // Helper to render a project property
      string? RenderProject(string propertyName)
      {
        try
        {
          var prop = userType.GetProperty(propertyName);
          var project = prop?.GetValue(userPackageDef);
          if (project == null)
          {
            Log.LogMessage(MessageImportance.Low, $"Property {propertyName} is null");
            return null;
          }
          
          var xml = renderMethod.Invoke(renderer, new[] { project }) as string;
          Log.LogMessage(MessageImportance.Low, $"Rendered {propertyName}: {xml?.Length ?? 0} chars");
          return xml;
        }
        catch (Exception ex)
        {
          Log.LogError($"Failed to render {propertyName}: {ex.Message}");
          return null;
        }
      }
      
      // Render all projects to XML
      var buildPropsXml = RenderProject("BuildProps");
      var buildTargetsXml = RenderProject("BuildTargets");
      var buildTransitivePropsXml = RenderProject("BuildTransitiveProps");
      var buildTransitiveTargetsXml = RenderProject("BuildTransitiveTargets");
      var sdkPropsXml = RenderProject("SdkProps");
      var sdkTargetsXml = RenderProject("SdkTargets");
      
      // Extract packaging settings
      var packagingProp = userType.GetProperty("Packaging");
      var packaging = packagingProp?.GetValue(userPackageDef);
      bool buildTransitive = false;
      bool emitSdk = false;
      bool sdkFlatLayout = false;
      
      if (packaging != null)
      {
        var buildTransitiveProp = packaging.GetType().GetProperty("BuildTransitive");
        buildTransitive = (buildTransitiveProp?.GetValue(packaging) as bool?) ?? false;
        
        var emitSdkProp = packaging.GetType().GetProperty("EmitSdk");
        emitSdk = (emitSdkProp?.GetValue(packaging) as bool?) ?? false;
        
        var sdkFlatLayoutProp = packaging.GetType().GetProperty("SdkFlatLayout");
        sdkFlatLayout = (sdkFlatLayoutProp?.GetValue(packaging) as bool?) ?? false;
      }
      
      Log.LogMessage(MessageImportance.Low, 
        $"Extracted: Id={id}, BuildTransitive={buildTransitive}, EmitSdk={emitSdk}, SdkFlatLayout={sdkFlatLayout}, " +
        $"Props={buildPropsXml != null}, Targets={buildTargetsXml != null}, " +
        $"TransitiveProps={buildTransitivePropsXml != null}, TransitiveTargets={buildTransitiveTargetsXml != null}, " +
        $"SdkProps={sdkPropsXml != null}, SdkTargets={sdkTargetsXml != null}");
      
      // Create PackageDefinition with XML strings
      // We'll store the XML in a custom structure that MsBuildPackageEmitter can use
      var packageDef = new PackageDefinition 
      { 
        Id = id,
      };
      
      packageDef.Packaging.BuildTransitive = buildTransitive;
      packageDef.Packaging.EmitSdk = emitSdk;
      packageDef.Packaging.SdkFlatLayout = sdkFlatLayout;
      
      // Store XML strings in the package definition
      // We need to modify MsBuildPackageEmitter to accept these strings
      _renderedXml = new RenderedXml
      {
        BuildProps = buildPropsXml,
        BuildTargets = buildTargetsXml,
        BuildTransitiveProps = buildTransitivePropsXml,
        BuildTransitiveTargets = buildTransitiveTargetsXml,
        SdkProps = sdkPropsXml,
        SdkTargets = sdkTargetsXml
      };
      
      return packageDef;
    }
    
    private RenderedXml? _renderedXml;
    
    private class RenderedXml
    {
      public string? BuildProps { get; set; }
      public string? BuildTargets { get; set; }
      public string? BuildTransitiveProps { get; set; }
      public string? BuildTransitiveTargets { get; set; }
      public string? SdkProps { get; set; }
      public string? SdkTargets { get; set; }
    }
  }
