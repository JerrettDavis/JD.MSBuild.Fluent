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
            .Props(p =>
            {
                p.PropertyGroup(null, group =>
                {
                    group.Property("MyPackageEnabled", "true");
                    group.Property("MyPackageVersion", "1.0.0");
                });
                p.Property("MyPackageOptimized", "true", "'$(Configuration)' == 'Release'");
                p.Choose(choose =>
                {
                    choose.When("$([MSBuild]::IsOSPlatform('Windows'))", whenProps =>
                    {
                        whenProps.Property("MyPackagePlatform", "Windows");
                    });
                    choose.Otherwise(otherwiseProps =>
                    {
                        otherwiseProps.Property("MyPackagePlatform", "Unix");
                    });
                });
                p.ItemGroup(null, group =>
                {
                    group.Include("None", "README.md", item =>
                    {
                        item.Meta("Pack", "true");
                        item.Meta("PackagePath", "docs/");
                    });
                    group.Include("Content", "assets/**/*");
                });
})
            .Build();
    }

    // Strongly-typed names (optional - uncomment to use)

    // Property names:
    // public readonly struct MyPackageEnabled : IMsBuildPropertyName
    // {
    //     public string Name => "MyPackageEnabled";
    // }
    // public readonly struct MyPackageOptimized : IMsBuildPropertyName
    // {
    //     public string Name => "MyPackageOptimized";
    // }
    // public readonly struct MyPackagePlatform : IMsBuildPropertyName
    // {
    //     public string Name => "MyPackagePlatform";
    // }
    // public readonly struct MyPackageVersion : IMsBuildPropertyName
    // {
    //     public string Name => "MyPackageVersion";
    // }

    // Item types:
    // public readonly struct ContentItem : IMsBuildItemTypeName
    // {
    //     public string Name => "Content";
    // }
    // public readonly struct NoneItem : IMsBuildItemTypeName
    // {
    //     public string Name => "None";
    // }
}
