using FluentAssertions;
using JD.MSBuild.Fluent.IR;
using JD.MSBuild.Fluent.Packaging;
using JD.MSBuild.Fluent.Render;
using JD.MSBuild.Fluent.Validation;

namespace JD.MSBuild.Fluent.Tests;

public sealed class ValidationTests
{
  [Fact]
  public void Renderer_throws_on_invalid_target()
  {
    var project = new MsBuildProject();
    project.Elements.Add(new MsBuildTarget { Name = string.Empty });

    var renderer = new MsBuildXmlRenderer();
    Action act = () => renderer.RenderToString(project);

    var ex = act.Should().Throw<MsBuildValidationException>().Which;
    ex.Errors.Should().Contain(e => e.Contains("Target name is required."));
  }

  [Fact]
  public void Emitter_throws_on_missing_package_id()
  {
    var def = new PackageDefinition { Id = string.Empty };
    var emitter = new MsBuildPackageEmitter();
    var dir = Path.Combine(Path.GetTempPath(), "JD.MSBuild.Fluent.Tests", Guid.NewGuid().ToString("n"));

    try
    {
      var act = () => emitter.Emit(def, dir);
      var ex = act.Should().Throw<MsBuildValidationException>().Which;
      ex.Errors.Should().Contain("PackageDefinition.Id is required.");
    }
    finally
    {
      try { Directory.Delete(dir, recursive: true); } catch { }
    }
  }

  [Fact]
  public void Validation_requires_project_elements_to_include_lists()
  {
    var project = new MsBuildProject();
    var group = new MsBuildPropertyGroup();
    project.Elements.Add(new MsBuildPropertyGroup());
    project.PropertyGroups.Add(group);

    var act = () => MsBuildValidator.ValidateProject(project);
    var ex = act.Should().Throw<MsBuildValidationException>().Which;
    ex.Errors.Should().Contain(e => e.Contains("PropertyGroup[0] is not present in Elements list."));
  }
}
