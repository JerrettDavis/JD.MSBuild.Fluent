using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace MyCompanyBuildTools;

/// <summary>
/// MSBuild package definition scaffolded from test-scaffold.xml
/// </summary>
public static class DefinitionFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("MyCompany.BuildTools")
            .Targets(t =>
            {
                t.UsingTaskFromFile("MyCustomTask", "$(MSBuildThisFileDirectory)\\..\\tasks\\MyCustomTask.dll");
                t.Target("MyPackage_PreBuild", target =>
                {
                    target.BeforeTargets("Build");
                    target.Condition("'$(MyPackageEnabled)' == 'true'");
                    target.Message("Running MyPackage Pre-Build", "High");
                    target.PropertyGroup(null, group =>
                    {
                        group.Property("_MyPackageTemp", "$(MSBuildProjectDirectory)\\obj\\mypackage");
                    });
                    target.Task("MakeDir", task =>
                    {
                        task.Param("Directories", "$(_MyPackageTemp)");
                    });
                    target.Exec("dotnet --version", "$(MSBuildProjectDirectory)");
                    target.ItemGroup(null, group =>
                    {
                        group.Include("_MyPackageFiles", "src/**/*.cs");
                    });
                    target.Task("MyCustomTask", task =>
                    {
                        task.Param("Files", "@(_MyPackageFiles)");
                        task.Param("OutputPath", "$(_MyPackageTemp)");
                        task.OutputToItem("GeneratedFiles", "MyPackageGenerated");
                    });
                });
                t.Target("MyPackage_PostBuild", target =>
                {
                    target.AfterTargets("Build");
                    target.DependsOnTargets("MyPackage_PreBuild");
                    target.Warning("Post-build complete");
                });
            })
            .Build();
    }

    // Strongly-typed names (optional - uncomment to use)

    // Property names:
    // public readonly struct MyPackageTemp : IMsBuildPropertyName
    // {
    //     public string Name => "_MyPackageTemp";
    // }

    // Item types:
    // public readonly struct MyPackageFilesItem : IMsBuildItemTypeName
    // {
    //     public string Name => "_MyPackageFiles";
    // }

    // Target names:
    // public readonly struct MyPackagePostBuildTarget : IMsBuildTargetName
    // {
    //     public string Name => "MyPackage_PostBuild";
    // }
    // public readonly struct MyPackagePreBuildTarget : IMsBuildTargetName
    // {
    //     public string Name => "MyPackage_PreBuild";
    // }
}
