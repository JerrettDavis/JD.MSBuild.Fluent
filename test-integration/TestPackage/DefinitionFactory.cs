using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace TestPackage;

public static class DefinitionFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("TestPackage.Build")
            .Description("Test package to verify MSBuild integration")
            .Props(p => p
                .Property("TestPackageEnabled", "true"))
            .Targets(t => t
                .Target("TestPackage_Hello", tgt => tgt
                    .BeforeTargets("Build")
                    .Condition("'$(TestPackageEnabled)' == 'true'")
                    .Message("Hello from TestPackage!")))
            .Pack(o => { o.BuildTransitive = true; })
            .Build();
    }
}
