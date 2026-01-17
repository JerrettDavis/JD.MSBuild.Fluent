using System.Xml.Linq;
using JD.MSBuild.Fluent.IR;

namespace JD.MSBuild.Fluent.Parse;

/// <summary>
/// Parses MSBuild XML into <see cref="MsBuildProject"/> instances.
/// </summary>
public sealed class MsBuildXmlParser
{
  /// <summary>
  /// Parses a file from disk into a project model.
  /// </summary>
  public MsBuildProject ParseFile(string path)
  {
    if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
    var doc = XDocument.Load(path, LoadOptions.PreserveWhitespace);
    return Parse(doc);
  }

  /// <summary>
  /// Parses a raw XML string into a project model.
  /// </summary>
  public MsBuildProject Parse(string xml)
  {
    if (xml is null) throw new ArgumentNullException(nameof(xml));
    var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
    return Parse(doc);
  }

  private MsBuildProject Parse(XDocument doc)
  {
    var root = doc.Root ?? throw new InvalidOperationException("MSBuild XML has no root element.");
    if (!string.Equals(root.Name.LocalName, "Project", StringComparison.Ordinal))
      throw new NotSupportedException($"Unsupported root element: {root.Name.LocalName}");

    var project = new MsBuildProject();

    foreach (var node in root.Nodes())
    {
      switch (node)
      {
        case XComment comment:
          Add(project, new MsBuildComment { Text = comment.Value });
          break;
        case XElement element:
          ParseProjectElement(project, element);
          break;
      }
    }

    return project;
  }

  private static void ParseProjectElement(MsBuildProject project, XElement element)
  {
    switch (element.Name.LocalName)
    {
      case "Import":
        Add(project, ParseImport(element));
        break;
      case "PropertyGroup":
        Add(project, ParsePropertyGroup(element));
        break;
      case "ItemGroup":
        Add(project, ParseItemGroup(element));
        break;
      case "UsingTask":
        Add(project, ParseUsingTask(element));
        break;
      case "Target":
        Add(project, ParseTarget(element));
        break;
      case "Choose":
        Add(project, ParseChoose(element));
        break;
      default:
        throw new NotSupportedException($"Unsupported project element: {element.Name.LocalName}");
    }
  }

  private static MsBuildImport ParseImport(XElement element)
  {
    var project = Attr(element, "Project") ?? throw new InvalidOperationException("Import is missing Project.");
    return new MsBuildImport
    {
      Project = project,
      Sdk = Attr(element, "Sdk"),
      Condition = Attr(element, "Condition")
    };
  }

  private static MsBuildPropertyGroup ParsePropertyGroup(XElement element)
  {
    var group = new MsBuildPropertyGroup
    {
      Condition = Attr(element, "Condition"),
      Label = Attr(element, "Label")
    };

    foreach (var node in element.Nodes())
    {
      switch (node)
      {
        case XComment comment:
          group.Entries.Add(new MsBuildComment { Text = comment.Value });
          break;
        case XElement propertyElement:
        {
          var property = new MsBuildProperty
          {
            Name = propertyElement.Name.LocalName,
            Value = propertyElement.Value,
            Condition = Attr(propertyElement, "Condition")
          };
          group.Properties.Add(property);
          group.Entries.Add(property);
          break;
        }
      }
    }

    return group;
  }

  private static MsBuildItemGroup ParseItemGroup(XElement element)
  {
    var group = new MsBuildItemGroup
    {
      Condition = Attr(element, "Condition"),
      Label = Attr(element, "Label")
    };

    foreach (var node in element.Nodes())
    {
      switch (node)
      {
        case XComment comment:
          group.Entries.Add(new MsBuildComment { Text = comment.Value });
          break;
        case XElement itemElement:
        {
          var include = Attr(itemElement, "Include");
          var remove = Attr(itemElement, "Remove");
          var update = Attr(itemElement, "Update");

          var (operation, spec) = include is not null
            ? (MsBuildItemOperation.Include, include)
            : remove is not null
              ? (MsBuildItemOperation.Remove, remove)
              : update is not null
                ? (MsBuildItemOperation.Update, update)
                : throw new NotSupportedException($"Item '{itemElement.Name.LocalName}' is missing Include/Remove/Update.");

          var item = new MsBuildItem
          {
            ItemType = itemElement.Name.LocalName,
            Operation = operation,
            Spec = spec,
            Exclude = Attr(itemElement, "Exclude"),
            Condition = Attr(itemElement, "Condition")
          };

          foreach (var attr in itemElement.Attributes())
          {
            if (attr.IsNamespaceDeclaration) continue;
            var name = attr.Name.LocalName;
            if (name is "Include" or "Remove" or "Update" or "Exclude" or "Condition") continue;
            item.MetadataAttributes[name] = attr.Value;
          }

          foreach (var metadataElement in itemElement.Elements())
            item.Metadata[metadataElement.Name.LocalName] = metadataElement.Value;

          group.Items.Add(item);
          group.Entries.Add(item);
          break;
        }
      }
    }

    return group;
  }

  private static MsBuildUsingTask ParseUsingTask(XElement element)
  {
    var taskName = Attr(element, "TaskName") ?? throw new InvalidOperationException("UsingTask is missing TaskName.");
    return new MsBuildUsingTask
    {
      TaskName = taskName,
      AssemblyFile = Attr(element, "AssemblyFile"),
      AssemblyName = Attr(element, "AssemblyName"),
      TaskFactory = Attr(element, "TaskFactory"),
      Condition = Attr(element, "Condition")
    };
  }

  private static MsBuildChoose ParseChoose(XElement element)
  {
    var choose = new MsBuildChoose();

    foreach (var child in element.Elements())
    {
      switch (child.Name.LocalName)
      {
        case "When":
          choose.Whens.Add(ParseWhen(child));
          break;
        case "Otherwise":
          choose.Otherwise = ParseOtherwise(child);
          break;
        default:
          throw new NotSupportedException($"Unsupported Choose child: {child.Name.LocalName}");
      }
    }

    return choose;
  }

  private static MsBuildWhen ParseWhen(XElement element)
  {
    var condition = Attr(element, "Condition") ?? throw new InvalidOperationException("When is missing Condition.");
    var when = new MsBuildWhen { Condition = condition };

    foreach (var child in element.Elements())
    {
      switch (child.Name.LocalName)
      {
        case "PropertyGroup":
          when.PropertyGroups.Add(ParsePropertyGroup(child));
          break;
        case "ItemGroup":
          when.ItemGroups.Add(ParseItemGroup(child));
          break;
        default:
          throw new NotSupportedException($"Unsupported When child: {child.Name.LocalName}");
      }
    }

    return when;
  }

  private static MsBuildOtherwise ParseOtherwise(XElement element)
  {
    var otherwise = new MsBuildOtherwise();

    foreach (var child in element.Elements())
    {
      switch (child.Name.LocalName)
      {
        case "PropertyGroup":
          otherwise.PropertyGroups.Add(ParsePropertyGroup(child));
          break;
        case "ItemGroup":
          otherwise.ItemGroups.Add(ParseItemGroup(child));
          break;
        default:
          throw new NotSupportedException($"Unsupported Otherwise child: {child.Name.LocalName}");
      }
    }

    return otherwise;
  }

  private static MsBuildTarget ParseTarget(XElement element)
  {
    var name = Attr(element, "Name") ?? throw new InvalidOperationException("Target is missing Name.");
    var target = new MsBuildTarget
    {
      Name = name,
      Condition = Attr(element, "Condition"),
      BeforeTargets = Attr(element, "BeforeTargets"),
      AfterTargets = Attr(element, "AfterTargets"),
      DependsOnTargets = Attr(element, "DependsOnTargets"),
      Inputs = Attr(element, "Inputs"),
      Outputs = Attr(element, "Outputs"),
      Label = Attr(element, "Label")
    };

    foreach (var node in element.Nodes())
    {
      switch (node)
      {
        case XComment comment:
          target.Elements.Add(new MsBuildTargetComment { Text = comment.Value });
          break;
        case XElement child:
          switch (child.Name.LocalName)
          {
            case "PropertyGroup":
              target.Elements.Add(new MsBuildPropertyGroupElement(ParsePropertyGroup(child)));
              break;
            case "ItemGroup":
              target.Elements.Add(new MsBuildItemGroupElement(ParseItemGroup(child)));
              break;
            case "Message":
              target.Elements.Add(ParseMessage(child));
              break;
            case "Exec":
              target.Elements.Add(ParseExec(child));
              break;
            case "Error":
              target.Elements.Add(ParseError(child));
              break;
            case "Warning":
              target.Elements.Add(ParseWarning(child));
              break;
            default:
              target.Elements.Add(ParseTask(child));
              break;
          }
          break;
      }
    }

    return target;
  }

  private static MsBuildMessageStep ParseMessage(XElement element)
  {
    var text = Attr(element, "Text") ?? string.Empty;
    return new MsBuildMessageStep
    {
      Text = text,
      Importance = Attr(element, "Importance") ?? "High",
      Condition = Attr(element, "Condition")
    };
  }

  private static MsBuildExecStep ParseExec(XElement element)
  {
    var command = Attr(element, "Command") ?? string.Empty;
    return new MsBuildExecStep
    {
      Command = command,
      WorkingDirectory = Attr(element, "WorkingDirectory"),
      Condition = Attr(element, "Condition")
    };
  }

  private static MsBuildErrorStep ParseError(XElement element)
  {
    var text = Attr(element, "Text") ?? string.Empty;
    return new MsBuildErrorStep
    {
      Text = text,
      Code = Attr(element, "Code"),
      Condition = Attr(element, "Condition")
    };
  }

  private static MsBuildWarningStep ParseWarning(XElement element)
  {
    var text = Attr(element, "Text") ?? string.Empty;
    return new MsBuildWarningStep
    {
      Text = text,
      Code = Attr(element, "Code"),
      Condition = Attr(element, "Condition")
    };
  }

  private static MsBuildTaskStep ParseTask(XElement element)
  {
    var step = new MsBuildTaskStep
    {
      TaskName = element.Name.LocalName,
      Condition = Attr(element, "Condition")
    };

    foreach (var attr in element.Attributes())
    {
      if (attr.IsNamespaceDeclaration) continue;
      var name = attr.Name.LocalName;
      if (name == "Condition") continue;
      step.Parameters[name] = attr.Value;
    }

    foreach (var child in element.Elements())
    {
      if (child.Name.LocalName != "Output")
        throw new NotSupportedException($"Unsupported task child element: {child.Name.LocalName}");
      step.Outputs.Add(ParseOutput(child));
    }

    return step;
  }

  private static MsBuildTaskOutput ParseOutput(XElement element)
  {
    var taskParameter = Attr(element, "TaskParameter") ?? throw new InvalidOperationException("Output is missing TaskParameter.");
    return new MsBuildTaskOutput
    {
      TaskParameter = taskParameter,
      PropertyName = Attr(element, "PropertyName"),
      ItemName = Attr(element, "ItemName"),
      Condition = Attr(element, "Condition")
    };
  }

  private static string? Attr(XElement element, string name)
    => (string?)element.Attribute(name);

  private static void Add(MsBuildProject project, IMsBuildProjectElement element)
  {
    project.Elements.Add(element);
    switch (element)
    {
      case MsBuildImport import:
        project.Imports.Add(import);
        break;
      case MsBuildPropertyGroup pg:
        project.PropertyGroups.Add(pg);
        break;
      case MsBuildItemGroup ig:
        project.ItemGroups.Add(ig);
        break;
      case MsBuildUsingTask ut:
        project.UsingTasks.Add(ut);
        break;
      case MsBuildTarget target:
        project.Targets.Add(target);
        break;
      case MsBuildChoose choose:
        project.Chooses.Add(choose);
        break;
    }
  }
}
