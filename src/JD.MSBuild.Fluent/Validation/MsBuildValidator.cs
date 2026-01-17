using System.Runtime.CompilerServices;
using JD.MSBuild.Fluent.IR;

namespace JD.MSBuild.Fluent.Validation;

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public sealed class MsBuildValidationException : Exception
{
  /// <summary>
  /// Creates a validation exception with the supplied errors.
  /// </summary>
  public MsBuildValidationException(IReadOnlyList<string> errors)
    : base("MSBuild validation failed: " + string.Join(" | ", errors))
  {
    Errors = errors;
  }

  /// <summary>
  /// List of validation errors.
  /// </summary>
  public IReadOnlyList<string> Errors { get; }
}

/// <summary>
/// Validates MSBuild models and package definitions.
/// </summary>
public static class MsBuildValidator
{
  /// <summary>
  /// Validates a project model and throws if invalid.
  /// </summary>
  public static void ValidateProject(MsBuildProject project)
  {
    if (project is null) throw new ArgumentNullException(nameof(project));
    var errors = new List<string>();
    ValidateProject(project, errors, "Project");
    ThrowIfErrors(errors);
  }

  /// <summary>
  /// Validates a package definition and throws if invalid.
  /// </summary>
  public static void ValidatePackageDefinition(PackageDefinition def)
  {
    if (def is null) throw new ArgumentNullException(nameof(def));
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(def.Id))
      errors.Add("PackageDefinition.Id is required.");

    if (def.Packaging.BuildAssetBasename is { } basename && string.IsNullOrWhiteSpace(basename))
      errors.Add("PackagePackagingOptions.BuildAssetBasename cannot be empty.");

    ValidateProject(def.GetBuildProps(), errors, "Build props");
    ValidateProject(def.GetBuildTargets(), errors, "Build targets");

    if (def.Packaging.BuildTransitive)
    {
      ValidateProject(def.GetBuildTransitiveProps(), errors, "BuildTransitive props");
      ValidateProject(def.GetBuildTransitiveTargets(), errors, "BuildTransitive targets");
    }

    if (def.Packaging.EmitSdk)
    {
      ValidateProject(def.GetSdkProps(), errors, "Sdk props");
      ValidateProject(def.GetSdkTargets(), errors, "Sdk targets");
    }

    ThrowIfErrors(errors);
  }

  private static void ValidateProject(MsBuildProject project, List<string> errors, string context)
  {
    if (project.Elements.Count > 0)
    {
      var elementSet = new HashSet<IMsBuildProjectElement>(ReferenceComparer.Instance);
      for (var i = 0; i < project.Elements.Count; i++)
      {
        var element = project.Elements[i];
        if (!elementSet.Add(element))
          AddError(errors, $"Elements[{i}] is a duplicate reference.", context);
      }

      ValidateElementsPresent(project.Imports, elementSet, errors, context, "Import");
      ValidateElementsPresent(project.PropertyGroups, elementSet, errors, context, "PropertyGroup");
      ValidateElementsPresent(project.ItemGroups, elementSet, errors, context, "ItemGroup");
      ValidateElementsPresent(project.UsingTasks, elementSet, errors, context, "UsingTask");
      ValidateElementsPresent(project.Targets, elementSet, errors, context, "Target");
      ValidateElementsPresent(project.Chooses, elementSet, errors, context, "Choose");

      for (var i = 0; i < project.Elements.Count; i++)
      {
        var element = project.Elements[i];
        var elementContext = $"{context}/{element.GetType().Name}[{i}]";
        ValidateProjectElement(element, errors, elementContext);
      }
      return;
    }

    for (var i = 0; i < project.Imports.Count; i++)
      ValidateImport(project.Imports[i], errors, $"{context}/Import[{i}]");

    for (var i = 0; i < project.Chooses.Count; i++)
      ValidateChoose(project.Chooses[i], errors, $"{context}/Choose[{i}]");

    for (var i = 0; i < project.PropertyGroups.Count; i++)
      ValidatePropertyGroup(project.PropertyGroups[i], errors, $"{context}/PropertyGroup[{i}]");

    for (var i = 0; i < project.ItemGroups.Count; i++)
      ValidateItemGroup(project.ItemGroups[i], errors, $"{context}/ItemGroup[{i}]");

    for (var i = 0; i < project.UsingTasks.Count; i++)
      ValidateUsingTask(project.UsingTasks[i], errors, $"{context}/UsingTask[{i}]");

    for (var i = 0; i < project.Targets.Count; i++)
      ValidateTarget(project.Targets[i], errors, $"{context}/Target[{i}]");
  }

  private static void ValidateProjectElement(IMsBuildProjectElement element, List<string> errors, string context)
  {
    switch (element)
    {
      case MsBuildComment:
        break;
      case MsBuildImport import:
        ValidateImport(import, errors, context);
        break;
      case MsBuildPropertyGroup pg:
        ValidatePropertyGroup(pg, errors, context);
        break;
      case MsBuildItemGroup ig:
        ValidateItemGroup(ig, errors, context);
        break;
      case MsBuildUsingTask ut:
        ValidateUsingTask(ut, errors, context);
        break;
      case MsBuildTarget target:
        ValidateTarget(target, errors, context);
        break;
      case MsBuildChoose choose:
        ValidateChoose(choose, errors, context);
        break;
      default:
        AddError(errors, "Unsupported project element type.", context);
        break;
    }
  }

  private static void ValidateImport(MsBuildImport import, List<string> errors, string context)
  {
    if (string.IsNullOrWhiteSpace(import.Project))
      AddError(errors, "Import.Project is required.", context);
  }

  private static void ValidatePropertyGroup(MsBuildPropertyGroup group, List<string> errors, string context)
  {
    if (group.Entries.Count > 0)
    {
      var entryProperties = new HashSet<MsBuildProperty>(ReferenceEqualityComparer<MsBuildProperty>.Instance);
      var entryIndex = 0;
      foreach (var entry in group.Entries)
      {
        switch (entry)
        {
          case MsBuildComment:
            break;
          case MsBuildProperty prop:
            entryProperties.Add(prop);
            if (string.IsNullOrWhiteSpace(prop.Name))
              AddError(errors, $"Property[{entryIndex}] name is required.", context);
            if (prop.Value is null)
              AddError(errors, $"Property[{entryIndex}] value is required.", context);
            break;
          default:
            AddError(errors, $"Unsupported property group entry type: {entry.GetType().Name}.", context);
            break;
        }
        entryIndex++;
      }

      for (var i = 0; i < group.Properties.Count; i++)
      {
        if (!entryProperties.Contains(group.Properties[i]))
          AddError(errors, $"Property[{i}] is not present in Entries list.", context);
      }

      if (entryProperties.Count != group.Properties.Count)
        AddError(errors, "Entries and Properties are out of sync.", context);

      return;
    }

    for (var i = 0; i < group.Properties.Count; i++)
    {
      var prop = group.Properties[i];
      if (string.IsNullOrWhiteSpace(prop.Name))
        AddError(errors, $"Property[{i}] name is required.", context);
      if (prop.Value is null)
        AddError(errors, $"Property[{i}] value is required.", context);
    }
  }

  private static void ValidateItemGroup(MsBuildItemGroup group, List<string> errors, string context)
  {
    if (group.Entries.Count > 0)
    {
      var entryItems = new HashSet<MsBuildItem>(ReferenceEqualityComparer<MsBuildItem>.Instance);
      var entryIndex = 0;
      foreach (var entry in group.Entries)
      {
        switch (entry)
        {
          case MsBuildComment:
            break;
          case MsBuildItem item:
            entryItems.Add(item);
            ValidateItem(item, errors, context, entryIndex);
            break;
          default:
            AddError(errors, $"Unsupported item group entry type: {entry.GetType().Name}.", context);
            break;
        }
        entryIndex++;
      }

      for (var i = 0; i < group.Items.Count; i++)
      {
        if (!entryItems.Contains(group.Items[i]))
          AddError(errors, $"Item[{i}] is not present in Entries list.", context);
      }

      if (entryItems.Count != group.Items.Count)
        AddError(errors, "Entries and Items are out of sync.", context);

      return;
    }

    for (var i = 0; i < group.Items.Count; i++)
      ValidateItem(group.Items[i], errors, context, i);
  }

  private static void ValidateItem(MsBuildItem item, List<string> errors, string context, int index)
  {
    if (string.IsNullOrWhiteSpace(item.ItemType))
      AddError(errors, $"Item[{index}] type is required.", context);
    if (string.IsNullOrWhiteSpace(item.Spec))
      AddError(errors, $"Item[{index}] spec is required.", context);

    foreach (var meta in item.Metadata)
    {
      if (string.IsNullOrWhiteSpace(meta.Key))
        AddError(errors, $"Item[{index}] metadata name is required.", context);
      if (meta.Value is null)
        AddError(errors, $"Item[{index}] metadata value is required.", context);
    }

    foreach (var attr in item.MetadataAttributes)
    {
      if (string.IsNullOrWhiteSpace(attr.Key))
        AddError(errors, $"Item[{index}] metadata attribute name is required.", context);
      if (attr.Value is null)
        AddError(errors, $"Item[{index}] metadata attribute value is required.", context);
    }
  }

  private static void ValidateUsingTask(MsBuildUsingTask ut, List<string> errors, string context)
  {
    if (string.IsNullOrWhiteSpace(ut.TaskName))
      AddError(errors, "UsingTask.TaskName is required.", context);

    if (string.IsNullOrWhiteSpace(ut.AssemblyFile)
      && string.IsNullOrWhiteSpace(ut.AssemblyName)
      && string.IsNullOrWhiteSpace(ut.TaskFactory))
      AddError(errors, "UsingTask requires AssemblyFile, AssemblyName, or TaskFactory.", context);
  }

  private static void ValidateTarget(MsBuildTarget target, List<string> errors, string context)
  {
    if (string.IsNullOrWhiteSpace(target.Name))
      AddError(errors, "Target name is required.", context);

    for (var i = 0; i < target.Elements.Count; i++)
      ValidateTargetElement(target.Elements[i], errors, $"{context}/Element[{i}]");
  }

  private static void ValidateTargetElement(MsBuildTargetElement element, List<string> errors, string context)
  {
    switch (element)
    {
      case MsBuildTargetComment:
        break;
      case MsBuildPropertyGroupElement pg:
        ValidatePropertyGroup(pg.Group, errors, context);
        break;
      case MsBuildItemGroupElement ig:
        ValidateItemGroup(ig.Group, errors, context);
        break;
      case MsBuildMessageStep msg:
        if (string.IsNullOrWhiteSpace(msg.Text))
          AddError(errors, "Message.Text is required.", context);
        break;
      case MsBuildExecStep exec:
        if (string.IsNullOrWhiteSpace(exec.Command))
          AddError(errors, "Exec.Command is required.", context);
        break;
      case MsBuildErrorStep err:
        if (string.IsNullOrWhiteSpace(err.Text))
          AddError(errors, "Error.Text is required.", context);
        break;
      case MsBuildWarningStep warn:
        if (string.IsNullOrWhiteSpace(warn.Text))
          AddError(errors, "Warning.Text is required.", context);
        break;
      case MsBuildTaskStep task:
        ValidateTask(task, errors, context);
        break;
      default:
        AddError(errors, "Unsupported target element type.", context);
        break;
    }
  }

  private static void ValidateTask(MsBuildTaskStep task, List<string> errors, string context)
  {
    if (string.IsNullOrWhiteSpace(task.TaskName))
      AddError(errors, "Task name is required.", context);

    foreach (var param in task.Parameters)
    {
      if (string.IsNullOrWhiteSpace(param.Key))
        AddError(errors, "Task parameter name is required.", context);
      if (param.Value is null)
        AddError(errors, $"Task parameter '{param.Key}' value is required.", context);
    }

    for (var i = 0; i < task.Outputs.Count; i++)
    {
      var output = task.Outputs[i];
      if (string.IsNullOrWhiteSpace(output.TaskParameter))
        AddError(errors, $"Task output[{i}] TaskParameter is required.", context);
      if (string.IsNullOrWhiteSpace(output.PropertyName) && string.IsNullOrWhiteSpace(output.ItemName))
        AddError(errors, $"Task output[{i}] requires PropertyName or ItemName.", context);
    }
  }

  private static void ValidateChoose(MsBuildChoose choose, List<string> errors, string context)
  {
    for (var i = 0; i < choose.Whens.Count; i++)
    {
      var when = choose.Whens[i];
      var whenContext = $"{context}/When[{i}]";
      if (string.IsNullOrWhiteSpace(when.Condition))
        AddError(errors, "When.Condition is required.", whenContext);

      for (var pgIndex = 0; pgIndex < when.PropertyGroups.Count; pgIndex++)
        ValidatePropertyGroup(when.PropertyGroups[pgIndex], errors, $"{whenContext}/PropertyGroup[{pgIndex}]");

      for (var igIndex = 0; igIndex < when.ItemGroups.Count; igIndex++)
        ValidateItemGroup(when.ItemGroups[igIndex], errors, $"{whenContext}/ItemGroup[{igIndex}]");
    }

    if (choose.Otherwise is not null)
    {
      for (var pgIndex = 0; pgIndex < choose.Otherwise.PropertyGroups.Count; pgIndex++)
        ValidatePropertyGroup(choose.Otherwise.PropertyGroups[pgIndex], errors, $"{context}/Otherwise/PropertyGroup[{pgIndex}]");

      for (var igIndex = 0; igIndex < choose.Otherwise.ItemGroups.Count; igIndex++)
        ValidateItemGroup(choose.Otherwise.ItemGroups[igIndex], errors, $"{context}/Otherwise/ItemGroup[{igIndex}]");
    }
  }

  private static void ValidateElementsPresent<T>(
    IEnumerable<T> elements,
    HashSet<IMsBuildProjectElement> elementSet,
    List<string> errors,
    string context,
    string elementName) where T : IMsBuildProjectElement
  {
    var index = 0;
    foreach (var element in elements)
    {
      if (!elementSet.Contains(element))
        AddError(errors, $"{elementName}[{index}] is not present in Elements list.", context);
      index++;
    }
  }

  private static void AddError(List<string> errors, string message, string? context)
  {
    if (string.IsNullOrWhiteSpace(context))
      errors.Add(message);
    else
      errors.Add($"{context}: {message}");
  }

  private static void ThrowIfErrors(List<string> errors)
  {
    if (errors.Count > 0)
      throw new MsBuildValidationException(errors);
  }

  private sealed class ReferenceComparer : IEqualityComparer<IMsBuildProjectElement>
  {
    public static ReferenceComparer Instance { get; } = new();

    public bool Equals(IMsBuildProjectElement? x, IMsBuildProjectElement? y) => ReferenceEquals(x, y);

    public int GetHashCode(IMsBuildProjectElement obj) => RuntimeHelpers.GetHashCode(obj);
  }

  private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
  {
    public static ReferenceEqualityComparer<T> Instance { get; } = new();

    public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

    public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
  }
}
