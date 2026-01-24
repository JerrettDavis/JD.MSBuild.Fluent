namespace JD.MSBuild.Fluent.Packaging;

/// <summary>
/// Options controlling how MSBuild assets are emitted into package folders.
/// </summary>
public sealed class PackagePackagingOptions
{
  /// <summary>
  /// If true, emit buildTransitive assets as well as build.
  /// </summary>
  public bool BuildTransitive { get; set; }

  /// <summary>
  /// If true, also emit SDK layout (Sdk/Sdk.props + Sdk/Sdk.targets).
  /// </summary>
  public bool EmitSdk { get; set; }

  /// <summary>
  /// If true, emit SDK files as Sdk/Sdk.props instead of Sdk/&lt;PackageId&gt;/Sdk.props.
  /// This is required for packages that extend base SDKs (e.g., extending Microsoft.NET.Sdk).
  /// </summary>
  public bool SdkFlatLayout { get; set; }

  /// <summary>
  /// Base file name used for build/*.props and build/*.targets.
  /// Defaults to the package ID.
  /// </summary>
  public string? BuildAssetBasename { get; set; }
}
