using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
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

      // Generate MSBuild assets
      var emitter = new MsBuildPackageEmitter();
      emitter.Emit(definition, OutputPath);

      // Collect generated files
      var generatedFiles = Directory.GetFiles(OutputPath, "*.*", SearchOption.AllDirectories)
        .Where(f => f.EndsWith(".props", StringComparison.OrdinalIgnoreCase) || 
                    f.EndsWith(".targets", StringComparison.OrdinalIgnoreCase))
        .Select(f => new TaskItem(f))
        .ToArray();

      GeneratedFiles = generatedFiles;

      Log.LogMessage(MessageImportance.High,
        $"JD.MSBuild.Fluent: Generated {generatedFiles.Length} file(s) to {OutputPath}");

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
      // Use JSON serialization/deserialization to bridge the type identity gap.
      try
      {
        // Serialize the result from the user's context
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions 
        { 
          WriteIndented = false,
          IncludeFields = true,
          DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        });
        
        Log.LogMessage(MessageImportance.Low, "Serialized PackageDefinition to JSON");
        Log.LogMessage(MessageImportance.Low, $"JSON: {json}");
        
        // Deserialize into our context
        var packageDef = JsonSerializer.Deserialize<PackageDefinition>(json, new JsonSerializerOptions
        {
          IncludeFields = true,
          PropertyNameCaseInsensitive = true
        });
        
        if (packageDef == null)
        {
          Log.LogError("Failed to deserialize PackageDefinition from JSON");
          return null;
        }
        
        Log.LogMessage(MessageImportance.Normal, 
          $"Loaded package definition: {packageDef.Id}");
        Log.LogMessage(MessageImportance.Normal, 
          $"Packaging.BuildTransitive = {packageDef.Packaging.BuildTransitive}");

        return packageDef;
      }
      catch (JsonException jsonEx)
      {
        Log.LogError($"JSON serialization failed: {jsonEx.Message}");
        Log.LogError("This likely means PackageDefinition or its properties are not JSON-serializable");
        return null;
      }
    }
    catch (ReflectionTypeLoadException ex)
    {
      Log.LogError($"Failed to load types from assembly: {ex.Message}");
      foreach (var loaderEx in ex.LoaderExceptions)
      {
        if (loaderEx != null)
          Log.LogError($"  - {loaderEx.Message}");
      }
      return null;
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
}
