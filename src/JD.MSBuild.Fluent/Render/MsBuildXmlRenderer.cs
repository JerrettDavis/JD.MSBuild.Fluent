using System.Text;
using System.Xml;
using JD.MSBuild.Fluent.IR;
using JD.MSBuild.Fluent.Validation;

namespace JD.MSBuild.Fluent.Render;

/// <summary>
/// Renders <see cref="MsBuildProject"/> models into MSBuild XML.
/// </summary>
public sealed class MsBuildXmlRenderer
{
  /// <summary>
  /// Creates a renderer with optional render options.
  /// </summary>
  public MsBuildXmlRenderer(MsBuildXmlRenderOptions? options = null)
  {
    Options = options ?? MsBuildXmlRenderOptions.Default;
  }

  /// <summary>
  /// Render options used by this renderer.
  /// </summary>
  public MsBuildXmlRenderOptions Options { get; }

  /// <summary>
  /// Renders a project to a string.
  /// </summary>
  public string RenderToString(MsBuildProject project)
  {
    using var sw = new StringWriter();
    Render(project, sw);
    return sw.ToString();
  }

  /// <summary>
  /// Renders a project to the provided writer.
  /// </summary>
  public void Render(MsBuildProject project, TextWriter writer)
  {
    MsBuildValidator.ValidateProject(project);

    var settings = new XmlWriterSettings
    {
      Indent = true,
      IndentChars = "  ",
      NewLineChars = "\n",
      Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
      OmitXmlDeclaration = true
    };

    using var xw = XmlWriter.Create(writer, settings);
    WriteStartElement(xw, "Project");

    if (!string.IsNullOrWhiteSpace(project.Label))
    {
      xw.WriteComment(project.Label);
    }

    if (project.Elements.Count > 0)
    {
      foreach (var element in project.Elements)
      {
        switch (element)
        {
          case MsBuildImport import:
            WriteStartElement(xw, "Import");
            xw.WriteAttributeString("Project", import.Project);
            if (!string.IsNullOrWhiteSpace(import.Sdk))
              xw.WriteAttributeString("Sdk", import.Sdk);
            if (!string.IsNullOrWhiteSpace(import.Condition))
              xw.WriteAttributeString("Condition", import.Condition);
            xw.WriteEndElement();
            break;

          case MsBuildComment comment:
            xw.WriteComment(comment.Text);
            break;

          case MsBuildChoose choose:
            WriteChoose(xw, choose);
            break;

          case MsBuildPropertyGroup pg:
            WritePropertyGroup(xw, pg);
            break;

          case MsBuildItemGroup ig:
            WriteItemGroup(xw, ig);
            break;

          case MsBuildUsingTask ut:
            WriteUsingTask(xw, ut);
            break;

          case MsBuildTarget target:
            WriteTarget(xw, target);
            break;

          default:
            throw new NotSupportedException($"Unknown project element type: {element.GetType().FullName}");
        }
      }
    }
    else
    {
      foreach (var import in project.Imports)
      {
        WriteStartElement(xw, "Import");
        xw.WriteAttributeString("Project", import.Project);
        if (!string.IsNullOrWhiteSpace(import.Sdk))
          xw.WriteAttributeString("Sdk", import.Sdk);
        if (!string.IsNullOrWhiteSpace(import.Condition))
          xw.WriteAttributeString("Condition", import.Condition);
        xw.WriteEndElement();
      }

      foreach (var choose in project.Chooses)
        WriteChoose(xw, choose);

    foreach (var pg in Canonicalize(project.PropertyGroups))
      WritePropertyGroup(xw, pg);

    foreach (var ig in Canonicalize(project.ItemGroups))
      WriteItemGroup(xw, ig);

    foreach (var ut in Canonicalize(project.UsingTasks))
      WriteUsingTask(xw, ut);

      foreach (var target in project.Targets)
        WriteTarget(xw, target);
    }

    xw.WriteEndElement();
    xw.Flush();
    writer.Write("\n");
  }

  private IEnumerable<MsBuildPropertyGroup> Canonicalize(IEnumerable<MsBuildPropertyGroup> groups)
  {
    // We keep group order, but stabilize property ordering within each group.
    // This keeps diffs boring while preserving author intent.
    foreach (var g in groups)
    {
      Canonicalize(g);
      yield return g;
    }
  }

  private IEnumerable<MsBuildItemGroup> Canonicalize(IEnumerable<MsBuildItemGroup> groups)
  {
    foreach (var g in groups)
    {
      Canonicalize(g);

      yield return g;
    }
  }

  private IEnumerable<MsBuildUsingTask> Canonicalize(IEnumerable<MsBuildUsingTask> tasks)
    => Options.CanonicalizeUsingTasks
      ? tasks.OrderBy(t => t.TaskName, StringComparer.Ordinal)
      : tasks;

  private void WritePropertyGroup(XmlWriter xw, MsBuildPropertyGroup group)
  {
    Canonicalize(group);
    WriteStartElement(xw, "PropertyGroup");
    if (!string.IsNullOrWhiteSpace(group.Label))
      xw.WriteAttributeString("Label", group.Label);
    if (!string.IsNullOrWhiteSpace(group.Condition))
      xw.WriteAttributeString("Condition", group.Condition);

    var entries = group.Entries.Count > 0
      ? group.Entries
      : group.Properties.Cast<IMsBuildPropertyGroupEntry>();

    foreach (var entry in entries)
    {
      switch (entry)
      {
        case MsBuildComment comment:
          xw.WriteComment(comment.Text);
          break;
        case MsBuildProperty p:
          WriteStartElement(xw, p.Name);
          if (!string.IsNullOrWhiteSpace(p.Condition))
            xw.WriteAttributeString("Condition", p.Condition);
          xw.WriteString(p.Value);
          xw.WriteEndElement();
          break;
        default:
          throw new NotSupportedException($"Unknown property group entry type: {entry.GetType().FullName}");
      }
    }

    xw.WriteEndElement();
  }

  private void WriteItemGroup(XmlWriter xw, MsBuildItemGroup group)
  {
    Canonicalize(group);
    WriteStartElement(xw, "ItemGroup");
    if (!string.IsNullOrWhiteSpace(group.Label))
      xw.WriteAttributeString("Label", group.Label);
    if (!string.IsNullOrWhiteSpace(group.Condition))
      xw.WriteAttributeString("Condition", group.Condition);

    var entries = group.Entries.Count > 0
      ? group.Entries
      : group.Items.Cast<IMsBuildItemGroupEntry>();

    foreach (var entry in entries)
    {
      switch (entry)
      {
        case MsBuildComment comment:
          xw.WriteComment(comment.Text);
          break;
        case MsBuildItem i:
          WriteStartElement(xw, i.ItemType);
          switch (i.Operation)
          {
            case MsBuildItemOperation.Include:
              xw.WriteAttributeString("Include", i.Spec);
              break;
            case MsBuildItemOperation.Remove:
              xw.WriteAttributeString("Remove", i.Spec);
              break;
            case MsBuildItemOperation.Update:
              xw.WriteAttributeString("Update", i.Spec);
              break;
          }

          if (!string.IsNullOrWhiteSpace(i.Exclude))
            xw.WriteAttributeString("Exclude", i.Exclude);

          if (!string.IsNullOrWhiteSpace(i.Condition))
            xw.WriteAttributeString("Condition", i.Condition);

          IEnumerable<KeyValuePair<string, string>> metadataAttributes = Options.CanonicalizeItemMetadata
            ? i.MetadataAttributes.OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            : i.MetadataAttributes;
          foreach (var md in metadataAttributes)
            xw.WriteAttributeString(md.Key, md.Value);

          foreach (var md in i.Metadata)
          {
            WriteStartElement(xw, md.Key);
            xw.WriteString(md.Value);
            xw.WriteEndElement();
          }

          xw.WriteEndElement();
          break;
        default:
          throw new NotSupportedException($"Unknown item group entry type: {entry.GetType().FullName}");
      }
    }

    xw.WriteEndElement();
  }

  private void WriteUsingTask(XmlWriter xw, MsBuildUsingTask ut)
  {
    WriteStartElement(xw, "UsingTask");
    xw.WriteAttributeString("TaskName", ut.TaskName);

    if (!string.IsNullOrWhiteSpace(ut.AssemblyFile))
      xw.WriteAttributeString("AssemblyFile", ut.AssemblyFile);
    if (!string.IsNullOrWhiteSpace(ut.AssemblyName))
      xw.WriteAttributeString("AssemblyName", ut.AssemblyName);
    if (!string.IsNullOrWhiteSpace(ut.TaskFactory))
      xw.WriteAttributeString("TaskFactory", ut.TaskFactory);
    if (!string.IsNullOrWhiteSpace(ut.Condition))
      xw.WriteAttributeString("Condition", ut.Condition);

    xw.WriteEndElement();
  }

  private void WriteTarget(XmlWriter xw, MsBuildTarget t)
  {
    WriteStartElement(xw, "Target");
    xw.WriteAttributeString("Name", t.Name);

    if (!string.IsNullOrWhiteSpace(t.Label))
      xw.WriteAttributeString("Label", t.Label);
    if (!string.IsNullOrWhiteSpace(t.BeforeTargets))
      xw.WriteAttributeString("BeforeTargets", t.BeforeTargets);
    if (!string.IsNullOrWhiteSpace(t.AfterTargets))
      xw.WriteAttributeString("AfterTargets", t.AfterTargets);
    if (!string.IsNullOrWhiteSpace(t.DependsOnTargets))
      xw.WriteAttributeString("DependsOnTargets", t.DependsOnTargets);
    if (!string.IsNullOrWhiteSpace(t.Inputs))
      xw.WriteAttributeString("Inputs", t.Inputs);
    if (!string.IsNullOrWhiteSpace(t.Outputs))
      xw.WriteAttributeString("Outputs", t.Outputs);
    if (!string.IsNullOrWhiteSpace(t.Condition))
      xw.WriteAttributeString("Condition", t.Condition);

    foreach (var element in t.Elements)
    {
      switch (element)
      {
        case MsBuildPropertyGroupElement pg:
          WritePropertyGroup(xw, pg.Group);
          break;

        case MsBuildItemGroupElement ig:
          WriteItemGroup(xw, ig.Group);
          break;

        case MsBuildMessageStep m:
          WriteStartElement(xw, "Message");
          xw.WriteAttributeString("Text", m.Text);
          if (!string.IsNullOrWhiteSpace(m.Importance))
            xw.WriteAttributeString("Importance", m.Importance);
          if (!string.IsNullOrWhiteSpace(m.Condition))
            xw.WriteAttributeString("Condition", m.Condition);
          xw.WriteEndElement();
          break;

        case MsBuildTargetComment comment:
          xw.WriteComment(comment.Text);
          break;

        case MsBuildExecStep e:
          WriteStartElement(xw, "Exec");
          xw.WriteAttributeString("Command", e.Command);
          if (!string.IsNullOrWhiteSpace(e.WorkingDirectory))
            xw.WriteAttributeString("WorkingDirectory", e.WorkingDirectory);
          if (!string.IsNullOrWhiteSpace(e.Condition))
            xw.WriteAttributeString("Condition", e.Condition);
          xw.WriteEndElement();
          break;

        case MsBuildTaskStep task:
          WriteStartElement(xw, task.TaskName);
          if (!string.IsNullOrWhiteSpace(task.Condition))
            xw.WriteAttributeString("Condition", task.Condition);
          IEnumerable<KeyValuePair<string, string>> parameters = Options.CanonicalizeTaskParameters
            ? task.Parameters.OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            : task.Parameters;
          foreach (var p in parameters)
            xw.WriteAttributeString(p.Key, p.Value);
          foreach (var output in task.Outputs)
          {
            WriteStartElement(xw, "Output");
            xw.WriteAttributeString("TaskParameter", output.TaskParameter);
            if (!string.IsNullOrWhiteSpace(output.PropertyName))
              xw.WriteAttributeString("PropertyName", output.PropertyName);
            if (!string.IsNullOrWhiteSpace(output.ItemName))
              xw.WriteAttributeString("ItemName", output.ItemName);
            if (!string.IsNullOrWhiteSpace(output.Condition))
              xw.WriteAttributeString("Condition", output.Condition);
            xw.WriteEndElement();
          }
          xw.WriteEndElement();
          break;

        case MsBuildErrorStep err:
          WriteStartElement(xw, "Error");
          xw.WriteAttributeString("Text", err.Text);
          if (!string.IsNullOrWhiteSpace(err.Code))
            xw.WriteAttributeString("Code", err.Code);
          if (!string.IsNullOrWhiteSpace(err.Condition))
            xw.WriteAttributeString("Condition", err.Condition);
          xw.WriteEndElement();
          break;

        case MsBuildWarningStep warn:
          WriteStartElement(xw, "Warning");
          xw.WriteAttributeString("Text", warn.Text);
          if (!string.IsNullOrWhiteSpace(warn.Code))
            xw.WriteAttributeString("Code", warn.Code);
          if (!string.IsNullOrWhiteSpace(warn.Condition))
            xw.WriteAttributeString("Condition", warn.Condition);
          xw.WriteEndElement();
          break;

        default:
          throw new NotSupportedException($"Unknown target element type: {element.GetType().FullName}");
      }
    }

    xw.WriteEndElement();
  }

  private void WriteChoose(XmlWriter xw, MsBuildChoose choose)
  {
    WriteStartElement(xw, "Choose");

    foreach (var when in choose.Whens)
    {
      WriteStartElement(xw, "When");
      xw.WriteAttributeString("Condition", when.Condition);
      foreach (var pg in when.PropertyGroups)
        WritePropertyGroup(xw, pg);
      foreach (var ig in when.ItemGroups)
        WriteItemGroup(xw, ig);
      xw.WriteEndElement();
    }

    if (choose.Otherwise is not null)
    {
      WriteStartElement(xw, "Otherwise");
      foreach (var pg in choose.Otherwise.PropertyGroups)
        WritePropertyGroup(xw, pg);
      foreach (var ig in choose.Otherwise.ItemGroups)
        WriteItemGroup(xw, ig);
      xw.WriteEndElement();
    }

    xw.WriteEndElement();
  }

  private void Canonicalize(MsBuildPropertyGroup group)
  {
    if (!Options.CanonicalizePropertyGroups)
      return;

    if (group.Entries.Count > 0)
    {
      if (group.Entries.Any(e => e is MsBuildComment))
        return;

      var props = group.Entries.OfType<MsBuildProperty>().ToList();
      if (props.Count != group.Entries.Count)
        return;

      if (props.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != props.Count)
        return;

      props.Sort((a, b) => StringComparer.Ordinal.Compare(a.Name, b.Name));
      group.Properties.Clear();
      group.Properties.AddRange(props);
      group.Entries.Clear();
      group.Entries.AddRange(props);
      return;
    }

    if (group.Properties.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != group.Properties.Count)
      return;

    group.Properties.Sort((a, b) => StringComparer.Ordinal.Compare(a.Name, b.Name));
  }

  private void Canonicalize(MsBuildItemGroup group)
  {
    if (Options.CanonicalizeItemGroups)
    {
      if (group.Entries.Count > 0 && group.Entries.Any(e => e is MsBuildComment))
        goto MetadataOnly;

      if (group.Entries.Count > 0 && group.Entries.Any(e => e is not MsBuildItem))
        goto MetadataOnly;

      // Group items by type/op/spec for determinism.
      var items = group.Entries.Count > 0
        ? group.Entries.Cast<MsBuildItem>().ToList()
        : group.Items;

      items.Sort((a, b) =>
      {
        var c1 = StringComparer.Ordinal.Compare(a.ItemType, b.ItemType);
        if (c1 != 0) return c1;
        var c2 = a.Operation.CompareTo(b.Operation);
        if (c2 != 0) return c2;
        return StringComparer.Ordinal.Compare(a.Spec, b.Spec);
      });

      if (group.Entries.Count > 0)
      {
        group.Items.Clear();
        group.Items.AddRange(items);
        group.Entries.Clear();
        foreach (var item in items)
          group.Entries.Add(item);
      }
    }

MetadataOnly:
    if (Options.CanonicalizeItemMetadata)
    {
      foreach (var item in group.Items)
      {
        // Stabilize metadata ordering.
        var ordered = item.Metadata.OrderBy(kvp => kvp.Key, StringComparer.Ordinal).ToList();
        item.Metadata.Clear();
        foreach (var kvp in ordered)
          item.Metadata[kvp.Key] = kvp.Value;
      }
    }
  }

  private void WriteStartElement(XmlWriter xw, string localName)
  {
    if (string.IsNullOrWhiteSpace(Options.MsBuildXmlns))
      xw.WriteStartElement(localName);
    else
      xw.WriteStartElement(string.Empty, localName, Options.MsBuildXmlns);
  }
}

/// <summary>
/// Controls XML rendering and canonicalization behavior.
/// </summary>
public sealed record MsBuildXmlRenderOptions
{
  /// <summary>
  /// Default rendering options.
  /// </summary>
  public static MsBuildXmlRenderOptions Default { get; } = new();

  /// <summary>
  /// XML namespace to apply to MSBuild elements. Set to empty to omit xmlns.
  /// </summary>
  public string MsBuildXmlns { get; init; } = "http://schemas.microsoft.com/developer/msbuild/2003";
  /// <summary>
  /// If true, canonicalizes property ordering inside property groups.
  /// </summary>
  public bool CanonicalizePropertyGroups { get; init; } = true;
  /// <summary>
  /// If true, canonicalizes item ordering inside item groups.
  /// </summary>
  public bool CanonicalizeItemGroups { get; init; } = true;
  /// <summary>
  /// If true, canonicalizes item metadata ordering.
  /// </summary>
  public bool CanonicalizeItemMetadata { get; init; } = true;
  /// <summary>
  /// If true, canonicalizes UsingTask ordering.
  /// </summary>
  public bool CanonicalizeUsingTasks { get; init; } = true;
  /// <summary>
  /// If true, canonicalizes task parameter attribute ordering.
  /// </summary>
  public bool CanonicalizeTaskParameters { get; init; } = true;
}
