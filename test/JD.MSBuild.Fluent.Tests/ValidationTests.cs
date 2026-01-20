using JD.MSBuild.Fluent.IR;
using JD.MSBuild.Fluent.Packaging;
using JD.MSBuild.Fluent.Render;
using JD.MSBuild.Fluent.Validation;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace JD.MSBuild.Fluent.Tests;

public sealed class ValidationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public async Task RendererThrowsOnInvalidTarget()
    {
        await Given("a project with invalid target", CreateInvalidTargetProject)
            .When("rendering to XML", RenderProject)
            .Then("validation exception is thrown", VerifyValidationException)
            .And("error contains target name requirement", VerifyTargetNameError)
            .AssertPassed();
    }

    [Fact]
    public async Task EmitterThrowsOnMissingPackageId()
    {
        await Given("a package with empty ID", CreateEmptyIdPackageContext)
            .Finally("cleanup temp directory", CleanupTempDirectory)
            .When("emitting package", EmitPackage)
            .Then("validation exception is thrown", VerifyPackageValidationException)
            .And("error contains package ID requirement", VerifyPackageIdError)
            .AssertPassed();
    }

    [Fact]
    public async Task ValidationRequiresProjectElementsToIncludeLists()
    {
        await Given("a project with mismatched elements", CreateMismatchedElementsProject)
            .When("validating project", ValidateProject)
            .Then("validation exception is thrown", VerifyElementValidationException)
            .And("error mentions PropertyGroup not in Elements", VerifyPropertyGroupError)
            .AssertPassed();
    }

    #region Helper Methods - Given

    private static MsBuildProject CreateInvalidTargetProject()
    {
        var project = new MsBuildProject();
        project.Elements.Add(new MsBuildTarget { Name = string.Empty });
        return project;
    }

    private static (PackageDefinition package, string dir) CreateEmptyIdPackageContext()
    {
        var def = new PackageDefinition { Id = string.Empty };
        var dir = Path.Combine(Path.GetTempPath(), "JD.MSBuild.Fluent.Tests", Guid.NewGuid().ToString("n"));
        return (def, dir);
    }

    private static MsBuildProject CreateMismatchedElementsProject()
    {
        var project = new MsBuildProject();
        var group = new MsBuildPropertyGroup();
        project.Elements.Add(new MsBuildPropertyGroup());
        project.PropertyGroups.Add(group);
        return project;
    }

    #endregion

    #region Helper Methods - When

    private static (MsBuildProject project, Exception? exception) RenderProject(MsBuildProject project)
    {
        var renderer = new MsBuildXmlRenderer();
        try
        {
            renderer.RenderToString(project);
            return (project, null);
        }
        catch (Exception ex)
        {
            return (project, ex);
        }
    }

    private static ((PackageDefinition package, string dir) ctx, Exception? exception) EmitPackage(
        (PackageDefinition package, string dir) ctx)
    {
        var emitter = new MsBuildPackageEmitter();
        try
        {
            emitter.Emit(ctx.package, ctx.dir);
            return (ctx, null);
        }
        catch (Exception ex)
        {
            return (ctx, ex);
        }
    }

    private static (MsBuildProject project, Exception? exception) ValidateProject(MsBuildProject project)
    {
        try
        {
            MsBuildValidator.ValidateProject(project);
            return (project, null);
        }
        catch (Exception ex)
        {
            return (project, ex);
        }
    }

    #endregion

    #region Helper Methods - Then

    private static bool VerifyValidationException((MsBuildProject project, Exception? exception) ctx)
    {
        Assert.NotNull(ctx.exception);
        Assert.IsType<MsBuildValidationException>(ctx.exception);
        return true;
    }

    private static bool VerifyTargetNameError((MsBuildProject project, Exception? exception) ctx)
    {
        var ex = Assert.IsType<MsBuildValidationException>(ctx.exception);
        Assert.Contains(ex.Errors, e => e.Contains("Target name is required."));
        return true;
    }

    private static bool VerifyPackageValidationException(((PackageDefinition package, string dir) ctx, Exception? exception) result)
    {
        Assert.NotNull(result.exception);
        Assert.IsType<MsBuildValidationException>(result.exception);
        return true;
    }

    private static bool VerifyPackageIdError(((PackageDefinition package, string dir) ctx, Exception? exception) result)
    {
        var ex = Assert.IsType<MsBuildValidationException>(result.exception);
        Assert.Contains("PackageDefinition.Id is required.", ex.Errors);
        return true;
    }

    private static bool VerifyElementValidationException((MsBuildProject project, Exception? exception) ctx)
    {
        Assert.NotNull(ctx.exception);
        Assert.IsType<MsBuildValidationException>(ctx.exception);
        return true;
    }

    private static bool VerifyPropertyGroupError((MsBuildProject project, Exception? exception) ctx)
    {
        var ex = Assert.IsType<MsBuildValidationException>(ctx.exception);
        Assert.Contains(ex.Errors, e => e.Contains("PropertyGroup[0] is not present in Elements list."));
        return true;
    }

    #endregion

    #region Helper Methods - Finally

    private static void CleanupTempDirectory((PackageDefinition package, string dir) ctx)
    {
        try
        {
            if (Directory.Exists(ctx.dir))
                Directory.Delete(ctx.dir, recursive: true);
        }
        catch
        {
            // Ignore cleanup failures
        }
    }

    #endregion
}
