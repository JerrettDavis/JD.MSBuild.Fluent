namespace JD.MSBuild.Fluent.Typed;

/// <summary>
/// Represents a strongly-typed MSBuild target name.
/// </summary>
public interface IMsBuildTargetName
{
  /// <summary>
  /// Gets the MSBuild target name.
  /// </summary>
  string Name { get; }
}

/// <summary>
/// Represents a strongly-typed MSBuild property name.
/// </summary>
public interface IMsBuildPropertyName
{
  /// <summary>
  /// Gets the MSBuild property name.
  /// </summary>
  string Name { get; }
}

/// <summary>
/// Represents a strongly-typed MSBuild item type name.
/// </summary>
public interface IMsBuildItemTypeName
{
  /// <summary>
  /// Gets the MSBuild item type name.
  /// </summary>
  string Name { get; }
}

/// <summary>
/// Represents a strongly-typed MSBuild item metadata name.
/// </summary>
public interface IMsBuildMetadataName
{
  /// <summary>
  /// Gets the MSBuild metadata name.
  /// </summary>
  string Name { get; }
}

/// <summary>
/// Represents a strongly-typed MSBuild task name.
/// </summary>
public interface IMsBuildTaskName
{
  /// <summary>
  /// Gets the MSBuild task name.
  /// </summary>
  string Name { get; }
}

/// <summary>
/// Represents a strongly-typed MSBuild task parameter name.
/// </summary>
public interface IMsBuildTaskParameterName
{
  /// <summary>
  /// Gets the MSBuild task parameter name.
  /// </summary>
  string Name { get; }
}

/// <summary>
/// Wraps a target name without requiring custom types.
/// </summary>
public readonly struct MsBuildTargetName : IMsBuildTargetName
{
  /// <summary>
  /// Creates a target name wrapper.
  /// </summary>
  public MsBuildTargetName(string name) => Name = name;

  /// <inheritdoc />
  public string Name { get; }

  /// <inheritdoc />
  public override string ToString() => Name;
}

/// <summary>
/// Wraps a property name without requiring custom types.
/// </summary>
public readonly struct MsBuildPropertyName : IMsBuildPropertyName
{
  /// <summary>
  /// Creates a property name wrapper.
  /// </summary>
  public MsBuildPropertyName(string name) => Name = name;

  /// <inheritdoc />
  public string Name { get; }

  /// <inheritdoc />
  public override string ToString() => Name;
}

/// <summary>
/// Wraps an item type name without requiring custom types.
/// </summary>
public readonly struct MsBuildItemTypeName : IMsBuildItemTypeName
{
  /// <summary>
  /// Creates an item type name wrapper.
  /// </summary>
  public MsBuildItemTypeName(string name) => Name = name;

  /// <inheritdoc />
  public string Name { get; }

  /// <inheritdoc />
  public override string ToString() => Name;
}

/// <summary>
/// Wraps an item metadata name without requiring custom types.
/// </summary>
public readonly struct MsBuildMetadataName : IMsBuildMetadataName
{
  /// <summary>
  /// Creates an item metadata name wrapper.
  /// </summary>
  public MsBuildMetadataName(string name) => Name = name;

  /// <inheritdoc />
  public string Name { get; }

  /// <inheritdoc />
  public override string ToString() => Name;
}

/// <summary>
/// Wraps a task parameter name without requiring custom types.
/// </summary>
public readonly struct MsBuildTaskParameterName : IMsBuildTaskParameterName
{
  /// <summary>
  /// Creates a task parameter name wrapper.
  /// </summary>
  public MsBuildTaskParameterName(string name) => Name = name;

  /// <inheritdoc />
  public string Name { get; }

  /// <inheritdoc />
  public override string ToString() => Name;
}

/// <summary>
/// Specifies how to derive a task name from a CLR type.
/// </summary>
public enum MsBuildTaskNameStyle
{
  /// <summary>
  /// Uses the simple type name.
  /// </summary>
  Name,
  /// <summary>
  /// Uses the full type name with namespace.
  /// </summary>
  FullName
}

/// <summary>
/// Describes a task declaration for UsingTask entries.
/// </summary>
public readonly struct MsBuildTaskReference : IMsBuildTaskName
{
  /// <summary>
  /// Creates a task reference with an explicit name and optional assembly details.
  /// </summary>
  public MsBuildTaskReference(string name, string? assemblyFile = null, string? assemblyName = null, string? taskFactory = null)
  {
    Name = name;
    AssemblyFile = assemblyFile;
    AssemblyName = assemblyName;
    TaskFactory = taskFactory;
  }

  /// <inheritdoc />
  public string Name { get; }

  /// <summary>
  /// Optional AssemblyFile for UsingTask.
  /// </summary>
  public string? AssemblyFile { get; }

  /// <summary>
  /// Optional AssemblyName for UsingTask.
  /// </summary>
  public string? AssemblyName { get; }

  /// <summary>
  /// Optional TaskFactory for UsingTask.
  /// </summary>
  public string? TaskFactory { get; }

  /// <summary>
  /// Builds a task reference from a CLR type.
  /// </summary>
  public static MsBuildTaskReference FromType(Type taskType, MsBuildTaskNameStyle nameStyle = MsBuildTaskNameStyle.FullName, string? assemblyFile = null, string? assemblyName = null, string? taskFactory = null)
  {
    if (taskType is null) throw new ArgumentNullException(nameof(taskType));
    var rawName = nameStyle == MsBuildTaskNameStyle.Name ? taskType.Name : (taskType.FullName ?? taskType.Name);
    var name = rawName.Replace('+', '.');
    return new MsBuildTaskReference(name, assemblyFile, assemblyName, taskFactory);
  }

  /// <summary>
  /// Builds a task reference from a CLR type.
  /// </summary>
  public static MsBuildTaskReference FromType<TTask>(MsBuildTaskNameStyle nameStyle = MsBuildTaskNameStyle.FullName, string? assemblyFile = null, string? assemblyName = null, string? taskFactory = null)
    => FromType(typeof(TTask), nameStyle, assemblyFile, assemblyName, taskFactory);

  /// <inheritdoc />
  public override string ToString() => Name;
}

/// <summary>
/// Common MSBuild target names.
/// </summary>
public static class MsBuildTargets
{
  /// <summary>
  /// Build target.
  /// </summary>
  public readonly struct Build : IMsBuildTargetName
  {
    /// <inheritdoc />
    public string Name => "Build";
  }
  /// <summary>
  /// CoreCompile target.
  /// </summary>
  public readonly struct CoreCompile : IMsBuildTargetName
  {
    /// <inheritdoc />
    public string Name => "CoreCompile";
  }
  /// <summary>
  /// Compile target.
  /// </summary>
  public readonly struct Compile : IMsBuildTargetName
  {
    /// <inheritdoc />
    public string Name => "Compile";
  }
  /// <summary>
  /// Clean target.
  /// </summary>
  public readonly struct Clean : IMsBuildTargetName
  {
    /// <inheritdoc />
    public string Name => "Clean";
  }
  /// <summary>
  /// Rebuild target.
  /// </summary>
  public readonly struct Rebuild : IMsBuildTargetName
  {
    /// <inheritdoc />
    public string Name => "Rebuild";
  }
  /// <summary>
  /// Pack target.
  /// </summary>
  public readonly struct Pack : IMsBuildTargetName
  {
    /// <inheritdoc />
    public string Name => "Pack";
  }
  /// <summary>
  /// Publish target.
  /// </summary>
  public readonly struct Publish : IMsBuildTargetName
  {
    /// <inheritdoc />
    public string Name => "Publish";
  }
  /// <summary>
  /// Restore target.
  /// </summary>
  public readonly struct Restore : IMsBuildTargetName
  {
    /// <inheritdoc />
    public string Name => "Restore";
  }
  /// <summary>
  /// ResolveReferences target.
  /// </summary>
  public readonly struct ResolveReferences : IMsBuildTargetName
  {
    /// <inheritdoc />
    public string Name => "ResolveReferences";
  }
  /// <summary>
  /// ResolveAssemblyReferences target.
  /// </summary>
  public readonly struct ResolveAssemblyReferences : IMsBuildTargetName
  {
    /// <inheritdoc />
    public string Name => "ResolveAssemblyReferences";
  }
}

/// <summary>
/// Common MSBuild property names.
/// </summary>
public static class MsBuildProperties
{
  /// <summary>
  /// Configuration property.
  /// </summary>
  public readonly struct Configuration : IMsBuildPropertyName
  {
    /// <inheritdoc />
    public string Name => "Configuration";
  }
  /// <summary>
  /// Platform property.
  /// </summary>
  public readonly struct Platform : IMsBuildPropertyName
  {
    /// <inheritdoc />
    public string Name => "Platform";
  }
  /// <summary>
  /// TargetFramework property.
  /// </summary>
  public readonly struct TargetFramework : IMsBuildPropertyName
  {
    /// <inheritdoc />
    public string Name => "TargetFramework";
  }
  /// <summary>
  /// TargetFrameworks property.
  /// </summary>
  public readonly struct TargetFrameworks : IMsBuildPropertyName
  {
    /// <inheritdoc />
    public string Name => "TargetFrameworks";
  }
  /// <summary>
  /// AssemblyName property.
  /// </summary>
  public readonly struct AssemblyName : IMsBuildPropertyName
  {
    /// <inheritdoc />
    public string Name => "AssemblyName";
  }
  /// <summary>
  /// RootNamespace property.
  /// </summary>
  public readonly struct RootNamespace : IMsBuildPropertyName
  {
    /// <inheritdoc />
    public string Name => "RootNamespace";
  }
  /// <summary>
  /// OutputPath property.
  /// </summary>
  public readonly struct OutputPath : IMsBuildPropertyName
  {
    /// <inheritdoc />
    public string Name => "OutputPath";
  }
  /// <summary>
  /// IntermediateOutputPath property.
  /// </summary>
  public readonly struct IntermediateOutputPath : IMsBuildPropertyName
  {
    /// <inheritdoc />
    public string Name => "IntermediateOutputPath";
  }
  /// <summary>
  /// BaseIntermediateOutputPath property.
  /// </summary>
  public readonly struct BaseIntermediateOutputPath : IMsBuildPropertyName
  {
    /// <inheritdoc />
    public string Name => "BaseIntermediateOutputPath";
  }
  /// <summary>
  /// MSBuildProjectName property.
  /// </summary>
  public readonly struct MSBuildProjectName : IMsBuildPropertyName
  {
    /// <inheritdoc />
    public string Name => "MSBuildProjectName";
  }
  /// <summary>
  /// MSBuildProjectDirectory property.
  /// </summary>
  public readonly struct MSBuildProjectDirectory : IMsBuildPropertyName
  {
    /// <inheritdoc />
    public string Name => "MSBuildProjectDirectory";
  }
  /// <summary>
  /// MSBuildProjectFullPath property.
  /// </summary>
  public readonly struct MSBuildProjectFullPath : IMsBuildPropertyName
  {
    /// <inheritdoc />
    public string Name => "MSBuildProjectFullPath";
  }
  /// <summary>
  /// MSBuildThisFileDirectory property.
  /// </summary>
  public readonly struct MSBuildThisFileDirectory : IMsBuildPropertyName
  {
    /// <inheritdoc />
    public string Name => "MSBuildThisFileDirectory";
  }
  /// <summary>
  /// PackageId property.
  /// </summary>
  public readonly struct PackageId : IMsBuildPropertyName
  {
    /// <inheritdoc />
    public string Name => "PackageId";
  }
  /// <summary>
  /// PackageVersion property.
  /// </summary>
  public readonly struct PackageVersion : IMsBuildPropertyName
  {
    /// <inheritdoc />
    public string Name => "PackageVersion";
  }
}

/// <summary>
/// Common MSBuild item type names.
/// </summary>
public static class MsBuildItemTypes
{
  /// <summary>
  /// Compile item.
  /// </summary>
  public readonly struct Compile : IMsBuildItemTypeName
  {
    /// <inheritdoc />
    public string Name => "Compile";
  }
  /// <summary>
  /// None item.
  /// </summary>
  public readonly struct None : IMsBuildItemTypeName
  {
    /// <inheritdoc />
    public string Name => "None";
  }
  /// <summary>
  /// Content item.
  /// </summary>
  public readonly struct Content : IMsBuildItemTypeName
  {
    /// <inheritdoc />
    public string Name => "Content";
  }
  /// <summary>
  /// EmbeddedResource item.
  /// </summary>
  public readonly struct EmbeddedResource : IMsBuildItemTypeName
  {
    /// <inheritdoc />
    public string Name => "EmbeddedResource";
  }
  /// <summary>
  /// Reference item.
  /// </summary>
  public readonly struct Reference : IMsBuildItemTypeName
  {
    /// <inheritdoc />
    public string Name => "Reference";
  }
  /// <summary>
  /// ProjectReference item.
  /// </summary>
  public readonly struct ProjectReference : IMsBuildItemTypeName
  {
    /// <inheritdoc />
    public string Name => "ProjectReference";
  }
  /// <summary>
  /// PackageReference item.
  /// </summary>
  public readonly struct PackageReference : IMsBuildItemTypeName
  {
    /// <inheritdoc />
    public string Name => "PackageReference";
  }
}
