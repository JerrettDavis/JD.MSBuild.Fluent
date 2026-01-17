using JD.MSBuild.Fluent.IR;
using JD.MSBuild.Fluent.Render;
using JD.MSBuild.Fluent.Validation;

namespace JD.MSBuild.Fluent.Packaging;

/// <summary>
/// Emits MSBuild assets into NuGet package folder layout.
/// </summary>
public sealed class MsBuildPackageEmitter
{
  private readonly MsBuildXmlRenderer _renderer;

  /// <summary>
  /// Creates a new emitter using the provided renderer or the default renderer.
  /// </summary>
  public MsBuildPackageEmitter(MsBuildXmlRenderer? renderer = null)
  {
    _renderer = renderer ?? new MsBuildXmlRenderer();
  }

  /// <summary>
  /// Emits MSBuild assets to the specified output directory.
  /// </summary>
  /// <param name="def">Package definition to emit.</param>
  /// <param name="outputDirectory">Output folder for build/buildTransitive/Sdk assets.</param>
  public void Emit(PackageDefinition def, string outputDirectory)
  {
    if (string.IsNullOrWhiteSpace(outputDirectory)) throw new ArgumentException("Output directory is required.", nameof(outputDirectory));

    MsBuildValidator.ValidatePackageDefinition(def);

    Directory.CreateDirectory(outputDirectory);

    var basename = def.Packaging.BuildAssetBasename ?? def.Id;

    // build
    EmitBuildAssets(Path.Combine(outputDirectory, "build"), basename, def.GetBuildProps(), def.GetBuildTargets());

    // buildTransitive
    if (def.Packaging.BuildTransitive)
      EmitBuildAssets(Path.Combine(outputDirectory, "buildTransitive"), basename, def.GetBuildTransitiveProps(), def.GetBuildTransitiveTargets());

    // Sdk/<id>/Sdk.props & Sdk.targets
    if (def.Packaging.EmitSdk)
    {
      var sdkDir = Path.Combine(outputDirectory, "Sdk", def.Id);
      Directory.CreateDirectory(sdkDir);
      File.WriteAllText(Path.Combine(sdkDir, "Sdk.props"), _renderer.RenderToString(def.GetSdkProps()));
      File.WriteAllText(Path.Combine(sdkDir, "Sdk.targets"), _renderer.RenderToString(def.GetSdkTargets()));
    }
  }

  private void EmitBuildAssets(string folder, string basename, MsBuildProject props, MsBuildProject targets)
  {
    Directory.CreateDirectory(folder);
    File.WriteAllText(Path.Combine(folder, basename + ".props"), _renderer.RenderToString(props));
    File.WriteAllText(Path.Combine(folder, basename + ".targets"), _renderer.RenderToString(targets));
  }
}
