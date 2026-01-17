namespace JD.MSBuild.Fluent.IR;

/// <summary>
/// In-memory representation of an MSBuild XML project fragment (.props/.targets/etc.).
/// This is intentionally a small, composable IR that can be rendered deterministically.
/// </summary>
public sealed class MsBuildProject
{
  /// <summary>
  /// Ordered project-level elements when order is important.
  /// </summary>
  public List<IMsBuildProjectElement> Elements { get; } = [];
  /// <summary>
  /// Property groups in this project.
  /// </summary>
  public List<MsBuildPropertyGroup> PropertyGroups { get; } = [];
  /// <summary>
  /// Item groups in this project.
  /// </summary>
  public List<MsBuildItemGroup> ItemGroups { get; } = [];
  /// <summary>
  /// Import statements for this project.
  /// </summary>
  public List<MsBuildImport> Imports { get; } = [];
  /// <summary>
  /// UsingTask declarations for this project.
  /// </summary>
  public List<MsBuildUsingTask> UsingTasks { get; } = [];
  /// <summary>
  /// Targets declared in this project.
  /// </summary>
  public List<MsBuildTarget> Targets { get; } = [];
  /// <summary>
  /// Choose elements declared in this project.
  /// </summary>
  public List<MsBuildChoose> Chooses { get; } = [];

  /// <summary>
  /// Optional label comment used by renderers for human readability.
  /// </summary>
  public string? Label { get; set; }
}

/// <summary>
/// Marker interface for project-level elements.
/// </summary>
public interface IMsBuildProjectElement
{
}

/// <summary>
/// Marker interface for entries within a property group.
/// </summary>
public interface IMsBuildPropertyGroupEntry
{
}

/// <summary>
/// Marker interface for entries within an item group.
/// </summary>
public interface IMsBuildItemGroupEntry
{
}

/// <summary>
/// Represents a PropertyGroup element.
/// </summary>
public sealed class MsBuildPropertyGroup : IMsBuildProjectElement
{
  /// <summary>
  /// Optional MSBuild condition for the group.
  /// </summary>
  public string? Condition { get; set; }
  /// <summary>
  /// Optional label attribute for the group.
  /// </summary>
  public string? Label { get; set; }
  /// <summary>
  /// Properties contained in the group.
  /// </summary>
  public List<MsBuildProperty> Properties { get; } = [];
  /// <summary>
  /// Ordered entries within the group, including comments.
  /// </summary>
  public List<IMsBuildPropertyGroupEntry> Entries { get; } = [];
}

/// <summary>
/// Represents a single MSBuild property.
/// </summary>
public sealed class MsBuildProperty : IMsBuildPropertyGroupEntry
{
  /// <summary>
  /// Property name.
  /// </summary>
  public required string Name { get; init; }
  /// <summary>
  /// Property value.
  /// </summary>
  public required string Value { get; init; }
  /// <summary>
  /// Optional condition for the property.
  /// </summary>
  public string? Condition { get; init; }
}

/// <summary>
/// Represents an ItemGroup element.
/// </summary>
public sealed class MsBuildItemGroup : IMsBuildProjectElement
{
  /// <summary>
  /// Optional MSBuild condition for the group.
  /// </summary>
  public string? Condition { get; set; }
  /// <summary>
  /// Optional label attribute for the group.
  /// </summary>
  public string? Label { get; set; }
  /// <summary>
  /// Items contained in the group.
  /// </summary>
  public List<MsBuildItem> Items { get; } = [];
  /// <summary>
  /// Ordered entries within the group, including comments.
  /// </summary>
  public List<IMsBuildItemGroupEntry> Entries { get; } = [];
}

/// <summary>
/// Represents a single MSBuild item.
/// </summary>
public sealed class MsBuildItem : IMsBuildItemGroupEntry
{
  /// <summary>
  /// Item type name (e.g., Compile, None).
  /// </summary>
  public required string ItemType { get; init; }
  /// <summary>
  /// Include, Remove, or Update.
  /// </summary>
  public required MsBuildItemOperation Operation { get; init; }
  /// <summary>
  /// Item include/remove/update spec.
  /// </summary>
  public required string Spec { get; init; }
  /// <summary>
  /// Optional exclude spec.
  /// </summary>
  public string? Exclude { get; set; }
  /// <summary>
  /// Optional condition for the item.
  /// </summary>
  public string? Condition { get; init; }
  /// <summary>
  /// Item metadata elements.
  /// </summary>
  public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);
  /// <summary>
  /// Item metadata expressed as attributes.
  /// </summary>
  public Dictionary<string, string> MetadataAttributes { get; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Operation used to define an item.
/// </summary>
public enum MsBuildItemOperation
{
  /// <summary>
  /// Include an item.
  /// </summary>
  Include,
  /// <summary>
  /// Remove an item.
  /// </summary>
  Remove,
  /// <summary>
  /// Update an item.
  /// </summary>
  Update
}

/// <summary>
/// Represents an Import element.
/// </summary>
public sealed class MsBuildImport : IMsBuildProjectElement
{
  /// <summary>
  /// Project path to import.
  /// </summary>
  public required string Project { get; init; }
  /// <summary>
  /// Optional SDK attribute on the import.
  /// </summary>
  public string? Sdk { get; init; }
  /// <summary>
  /// Optional import condition.
  /// </summary>
  public string? Condition { get; init; }
}

/// <summary>
/// Represents an MSBuild XML comment.
/// </summary>
public sealed class MsBuildComment : IMsBuildProjectElement, IMsBuildPropertyGroupEntry, IMsBuildItemGroupEntry
{
  /// <summary>
  /// Comment text.
  /// </summary>
  public required string Text { get; init; }
}

/// <summary>
/// Represents a UsingTask declaration.
/// </summary>
public sealed class MsBuildUsingTask : IMsBuildProjectElement
{
  /// <summary>
  /// Task name.
  /// </summary>
  public required string TaskName { get; init; }
  /// <summary>
  /// Assembly file path.
  /// </summary>
  public string? AssemblyFile { get; init; }
  /// <summary>
  /// Assembly name for the task.
  /// </summary>
  public string? AssemblyName { get; init; }
  /// <summary>
  /// TaskFactory name.
  /// </summary>
  public string? TaskFactory { get; init; }
  /// <summary>
  /// Optional condition for the UsingTask.
  /// </summary>
  public string? Condition { get; init; }
}

/// <summary>
/// Represents a Target element.
/// </summary>
public sealed class MsBuildTarget : IMsBuildProjectElement
{
  /// <summary>
  /// Target name.
  /// </summary>
  public required string Name { get; init; }
  /// <summary>
  /// Optional target condition.
  /// </summary>
  public string? Condition { get; set; }
  /// <summary>
  /// BeforeTargets attribute.
  /// </summary>
  public string? BeforeTargets { get; set; }
  /// <summary>
  /// AfterTargets attribute.
  /// </summary>
  public string? AfterTargets { get; set; }
  /// <summary>
  /// DependsOnTargets attribute.
  /// </summary>
  public string? DependsOnTargets { get; set; }
  /// <summary>
  /// Inputs attribute.
  /// </summary>
  public string? Inputs { get; set; }
  /// <summary>
  /// Outputs attribute.
  /// </summary>
  public string? Outputs { get; set; }
  /// <summary>
  /// Optional label attribute.
  /// </summary>
  public string? Label { get; set; }

  /// <summary>
  /// Ordered elements inside the target.
  /// </summary>
  public List<MsBuildTargetElement> Elements { get; } = [];
}

/// <summary>
/// Base type for elements inside a target.
/// </summary>
public abstract class MsBuildTargetElement
{
}

/// <summary>
/// Represents a comment inside a target.
/// </summary>
public sealed class MsBuildTargetComment : MsBuildTargetElement
{
  /// <summary>
  /// Comment text.
  /// </summary>
  public required string Text { get; init; }
}

/// <summary>
/// Wraps a PropertyGroup inside a target.
/// </summary>
public sealed class MsBuildPropertyGroupElement : MsBuildTargetElement
{
  /// <summary>
  /// Creates a property group element.
  /// </summary>
  public MsBuildPropertyGroupElement(MsBuildPropertyGroup group) => Group = group;
  /// <summary>
  /// The wrapped property group.
  /// </summary>
  public MsBuildPropertyGroup Group { get; }
}

/// <summary>
/// Wraps an ItemGroup inside a target.
/// </summary>
public sealed class MsBuildItemGroupElement : MsBuildTargetElement
{
  /// <summary>
  /// Creates an item group element.
  /// </summary>
  public MsBuildItemGroupElement(MsBuildItemGroup group) => Group = group;
  /// <summary>
  /// The wrapped item group.
  /// </summary>
  public MsBuildItemGroup Group { get; }
}

/// <summary>
/// Base type for MSBuild target steps.
/// </summary>
public abstract class MsBuildStep : MsBuildTargetElement
{
  /// <summary>
  /// Optional condition on the step.
  /// </summary>
  public string? Condition { get; set; }
}

/// <summary>
/// Represents a Message task.
/// </summary>
public sealed class MsBuildMessageStep : MsBuildStep
{
  /// <summary>
  /// Message text.
  /// </summary>
  public required string Text { get; init; }
  /// <summary>
  /// Message importance (default High).
  /// </summary>
  public string Importance { get; init; } = "High";
}

/// <summary>
/// Represents an Exec task.
/// </summary>
public sealed class MsBuildExecStep : MsBuildStep
{
  /// <summary>
  /// Command to execute.
  /// </summary>
  public required string Command { get; init; }
  /// <summary>
  /// Optional working directory.
  /// </summary>
  public string? WorkingDirectory { get; init; }
}

/// <summary>
/// Represents an arbitrary task invocation.
/// </summary>
public sealed class MsBuildTaskStep : MsBuildStep
{
  /// <summary>
  /// Task name.
  /// </summary>
  public required string TaskName { get; init; }
  /// <summary>
  /// Task parameters as attributes.
  /// </summary>
  public Dictionary<string, string> Parameters { get; } = new(StringComparer.Ordinal);
  /// <summary>
  /// Output declarations.
  /// </summary>
  public List<MsBuildTaskOutput> Outputs { get; } = [];
}

/// <summary>
/// Represents an Error task.
/// </summary>
public sealed class MsBuildErrorStep : MsBuildStep
{
  /// <summary>
  /// Error text.
  /// </summary>
  public required string Text { get; init; }
  /// <summary>
  /// Optional error code.
  /// </summary>
  public string? Code { get; init; }
}

/// <summary>
/// Represents a Warning task.
/// </summary>
public sealed class MsBuildWarningStep : MsBuildStep
{
  /// <summary>
  /// Warning text.
  /// </summary>
  public required string Text { get; init; }
  /// <summary>
  /// Optional warning code.
  /// </summary>
  public string? Code { get; init; }
}

/// <summary>
/// Represents an Output element on a task.
/// </summary>
public sealed class MsBuildTaskOutput
{
  /// <summary>
  /// The task parameter being output.
  /// </summary>
  public required string TaskParameter { get; init; }
  /// <summary>
  /// Optional property name to receive the output.
  /// </summary>
  public string? PropertyName { get; init; }
  /// <summary>
  /// Optional item name to receive the output.
  /// </summary>
  public string? ItemName { get; init; }
  /// <summary>
  /// Optional condition on the output.
  /// </summary>
  public string? Condition { get; init; }
}

/// <summary>
/// Represents a Choose element.
/// </summary>
public sealed class MsBuildChoose : IMsBuildProjectElement
{
  /// <summary>
  /// When clauses.
  /// </summary>
  public List<MsBuildWhen> Whens { get; } = [];
  /// <summary>
  /// Optional Otherwise clause.
  /// </summary>
  public MsBuildOtherwise? Otherwise { get; set; }
}

/// <summary>
/// Represents a When clause inside a Choose.
/// </summary>
public sealed class MsBuildWhen
{
  /// <summary>
  /// Condition for the when clause.
  /// </summary>
  public required string Condition { get; init; }
  /// <summary>
  /// Property groups inside the when clause.
  /// </summary>
  public List<MsBuildPropertyGroup> PropertyGroups { get; } = [];
  /// <summary>
  /// Item groups inside the when clause.
  /// </summary>
  public List<MsBuildItemGroup> ItemGroups { get; } = [];
}

/// <summary>
/// Represents an Otherwise clause inside a Choose.
/// </summary>
public sealed class MsBuildOtherwise
{
  /// <summary>
  /// Property groups inside the otherwise clause.
  /// </summary>
  public List<MsBuildPropertyGroup> PropertyGroups { get; } = [];
  /// <summary>
  /// Item groups inside the otherwise clause.
  /// </summary>
  public List<MsBuildItemGroup> ItemGroups { get; } = [];
}
