using System;
using System.IO;
using System.Linq;
using System.Reflection;
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

  static GenerateMSBuildAssets()
  {
    // Register assembly resolver to find JD.MSBuild.Fluent.dll in the same directory as the task DLL
    AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
  }

  private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
  {
    var assemblyName = new AssemblyName(args.Name);
    
    // Look in the same directory as the task DLL for ANY assembly
    var taskAssemblyLocation = typeof(GenerateMSBuildAssets).Assembly.Location;
    var taskDirectory = Path.GetDirectoryName(taskAssemblyLocation);
    if (string.IsNullOrEmpty(taskDirectory))
      return null;
    
    var assemblyPath = Path.Combine(taskDirectory, assemblyName.Name + ".dll");
    return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
  }

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
    try
    {
      // CRITICAL: Pre-load JD.MSBuild.Fluent.dll from the task directory FIRST
      // This ensures the user assembly uses OUR copy, not a potentially different one from their bin folder
      var taskAssemblyLocation = typeof(GenerateMSBuildAssets).Assembly.Location;
      var taskDirectory = Path.GetDirectoryName(taskAssemblyLocation);
      if (!string.IsNullOrEmpty(taskDirectory))
      {
        var fluentAssemblyPath = Path.Combine(taskDirectory, "JD.MSBuild.Fluent.dll");
        if (File.Exists(fluentAssemblyPath))
        {
          // Force load this assembly into the default context BEFORE loading user assembly
          var fluentAsm = Assembly.LoadFrom(fluentAssemblyPath);
          Log.LogMessage(MessageImportance.Low, $"Pre-loaded JD.MSBuild.Fluent from: {fluentAsm.Location}");
        }
      }
      
      // Now load user assembly - it should resolve JD.MSBuild.Fluent to the one we just loaded
      var assembly = Assembly.LoadFrom(AssemblyFile);
      
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

      // The result is a PackageDefinition from the user's assembly context
      // We need to work with it via reflection since it's a different type identity
      var packageDef = result as PackageDefinition;
      if (packageDef == null)
      {
        // Log detailed type information for debugging
        var resultType = result.GetType();
        var expectedType = typeof(PackageDefinition);
        Log.LogError($"Factory method returned type {resultType.FullName} from assembly {resultType.Assembly.FullName}");
        Log.LogError($"Expected type {expectedType.FullName} from assembly {expectedType.Assembly.FullName}");
        Log.LogError($"Types equal: {resultType == expectedType}");
        Log.LogError($"Assemblies equal: {resultType.Assembly == expectedType.Assembly}");
        return null;
      }

      Log.LogMessage(MessageImportance.Normal, 
        $"Loaded package definition: {packageDef.Id}");

      return packageDef;
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
      Log.LogErrorFromException(ex);
      return null;
    }
  }
}
