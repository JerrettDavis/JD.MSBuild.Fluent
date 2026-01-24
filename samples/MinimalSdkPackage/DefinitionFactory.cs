using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace MinimalSdkPackage;

public static class DefinitionFactory
{
  public static PackageDefinition Create()
  {
    return Package.Define("MinimalSdkPackage")
      .Description("A minimal SDK-style package generated from a fluent DSL")
      .Props(p => p
        .Property("MinimalSdkPackageEnabled", "true")
        .PropertyGroup("'$(Configuration)' == 'Release'", g => g
          .Property("DefineConstants", "$(DefineConstants);MINIMAL_SDK_RELEASE")))
      .Targets(t => t
        .Target("MinimalSdkPackage_Hello", tgt => tgt
          .BeforeTargets("Build")
          .Condition("'$(MinimalSdkPackageEnabled)' == 'true'")
          .Message("Hello from MinimalSdkPackage")
          .Task("WriteLinesToFile", task => task
            .Param("File", "$(BaseIntermediateOutputPath)MinimalSdkPackage.txt")
            .Param("Lines", "Hello from MinimalSdkPackage"))))
      .Pack(o => { o.EmitSdk = true; o.SdkFlatLayout = true; o.BuildTransitive = true; })
      .Build();
  }
}
