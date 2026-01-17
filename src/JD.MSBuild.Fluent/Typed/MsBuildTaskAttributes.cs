namespace JD.MSBuild.Fluent.Typed;

/// <summary>
/// Marks a task type for source generation of strongly-typed MSBuild helpers.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class MsBuildTaskAttribute : Attribute
{
  /// <summary>
  /// Optional override for the MSBuild task name.
  /// </summary>
  public string? Name { get; set; }

  /// <summary>
  /// Specifies how to derive the MSBuild task name when <see cref="Name"/> is not set.
  /// </summary>
  public MsBuildTaskNameStyle NameStyle { get; set; } = MsBuildTaskNameStyle.FullName;

  /// <summary>
  /// Optional AssemblyFile value for generated task references.
  /// </summary>
  public string? AssemblyFile { get; set; }

  /// <summary>
  /// Optional AssemblyName value for generated task references.
  /// </summary>
  public string? AssemblyName { get; set; }

  /// <summary>
  /// Optional TaskFactory value for generated task references.
  /// </summary>
  public string? TaskFactory { get; set; }

  /// <summary>
  /// If true, uses the containing assembly name when <see cref="AssemblyName"/> is not set.
  /// </summary>
  public bool UseAssemblyName { get; set; }
}

/// <summary>
/// Skips MSBuild task parameter generation for a property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class MsBuildIgnoreAttribute : Attribute
{
}
