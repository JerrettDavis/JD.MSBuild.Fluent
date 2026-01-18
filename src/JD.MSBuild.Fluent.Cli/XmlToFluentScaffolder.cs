using System.Text;
using System.Xml.Linq;

namespace JD.MSBuild.Fluent.Cli;

/// <summary>
/// Scaffolds fluent API code from MSBuild XML files.
/// </summary>
public class XmlToFluentScaffolder
{
    private readonly StringBuilder _code = new();
    private int _indent;
    private readonly HashSet<string> _propertyNames = new();
    private readonly HashSet<string> _itemTypes = new();
    private readonly HashSet<string> _targetNames = new();

    /// <summary>
    /// Scaffolds fluent API C# code from an MSBuild XML file.
    /// </summary>
    /// <param name="xmlFilePath">Path to the MSBuild XML file (.props or .targets)</param>
    /// <param name="packageId">Package ID (defaults to filename without extension)</param>
    /// <param name="factoryClassName">Factory class name (defaults to "DefinitionFactory")</param>
    /// <returns>Generated C# code as a string</returns>
    public string Scaffold(string xmlFilePath, string? packageId = null, string? factoryClassName = null)
    {
        if (!File.Exists(xmlFilePath))
            throw new FileNotFoundException($"XML file not found: {xmlFilePath}", xmlFilePath);

        var xml = XDocument.Load(xmlFilePath);
        var root = xml.Root ?? throw new InvalidOperationException("No root element found");

        var fileName = Path.GetFileNameWithoutExtension(xmlFilePath);
        var inferredPackageId = packageId ?? fileName;
        var className = factoryClassName ?? "DefinitionFactory";

        GenerateFactoryClass(root, inferredPackageId, className, fileName);

        return _code.ToString();
    }

    private void GenerateFactoryClass(XElement root, string packageId, string className, string fileName)
    {
        // Generate file header
        AppendLine("using JD.MSBuild.Fluent;");
        AppendLine("using JD.MSBuild.Fluent.Fluent;");
        AppendLine();
        AppendLine($"namespace {packageId.Replace(".", "")};");
        AppendLine();
        AppendLine($"/// <summary>");
        AppendLine($"/// MSBuild package definition scaffolded from {fileName}.xml");
        AppendLine($"/// </summary>");
        AppendLine($"public static class {className}");
        AppendLine("{");
        _indent++;

        // Generate factory method
        AppendLine("public static PackageDefinition Create()");
        AppendLine("{");
        _indent++;
        AppendLine($"return Package.Define(\"{packageId}\")");
        _indent++;

        bool hasProps = HasPropsElements(root);
        bool hasTargets = HasTargetsElements(root);

        if (hasProps)
        {
            AppendLine(".Props(p =>");
            AppendLine("{");
            _indent++;
            GeneratePropsContent(root);
            _indent--;
            AppendLine("})");
        }

        if (hasTargets)
        {
            AppendLine(".Targets(t =>");
            AppendLine("{");
            _indent++;
            GenerateTargetsContent(root);
            _indent--;
            AppendLine("})");
        }

        AppendLine(".Build();");
        _indent--;
        _indent--;
        AppendLine("}");

        // Generate strongly-typed name structs if any were collected
        if (_propertyNames.Count > 0 || _itemTypes.Count > 0 || _targetNames.Count > 0)
        {
            AppendLine();
            AppendLine("// Strongly-typed names (optional - uncomment to use)");
            GenerateStronglyTypedStructs();
        }

        _indent--;
        AppendLine("}");
    }

    private bool HasPropsElements(XElement root)
    {
        return root.Elements("PropertyGroup").Any() ||
               root.Elements("ItemGroup").Any() ||
               root.Elements("Choose").Any() ||
               root.Elements("Import").Any();
    }

    private bool HasTargetsElements(XElement root)
    {
        return root.Elements("Target").Any() ||
               root.Elements("UsingTask").Any();
    }

    private void GeneratePropsContent(XElement root)
    {
        foreach (var element in root.Elements())
        {
            switch (element.Name.LocalName)
            {
                case "PropertyGroup":
                    GeneratePropertyGroup(element);
                    break;
                case "ItemGroup":
                    GenerateItemGroup(element);
                    break;
                case "Choose":
                    GenerateChoose(element);
                    break;
                case "Import":
                    GenerateImport(element);
                    break;
            }
        }
    }

    private void GenerateTargetsContent(XElement root)
    {
        foreach (var element in root.Elements())
        {
            switch (element.Name.LocalName)
            {
                case "UsingTask":
                    GenerateUsingTask(element);
                    break;
                case "Target":
                    GenerateTarget(element);
                    break;
            }
        }
    }

    private void GeneratePropertyGroup(XElement propertyGroup)
    {
        var condition = propertyGroup.Attribute("Condition")?.Value;

        if (propertyGroup.Elements().Count() == 1)
        {
            // Single property - inline
            var prop = propertyGroup.Elements().First();
            var propCondition = prop.Attribute("Condition")?.Value;
            var effectiveCondition = propCondition ?? condition;

            if (effectiveCondition != null)
                AppendLine($"p.Property(\"{prop.Name.LocalName}\", \"{EscapeString(prop.Value)}\", \"{EscapeString(effectiveCondition)}\");");
            else
                AppendLine($"p.Property(\"{prop.Name.LocalName}\", \"{EscapeString(prop.Value)}\");");

            _propertyNames.Add(prop.Name.LocalName);
        }
        else if (propertyGroup.Elements().Any())
        {
            // Multiple properties - use PropertyGroup
            if (condition != null)
                AppendLine($"p.PropertyGroup(\"{EscapeString(condition)}\", group =>");
            else
                AppendLine("p.PropertyGroup(null, group =>");

            AppendLine("{");
            _indent++;

            foreach (var prop in propertyGroup.Elements())
            {
                var propCondition = prop.Attribute("Condition")?.Value;
                if (propCondition != null)
                    AppendLine($"group.Property(\"{prop.Name.LocalName}\", \"{EscapeString(prop.Value)}\", \"{EscapeString(propCondition)}\");");
                else
                    AppendLine($"group.Property(\"{prop.Name.LocalName}\", \"{EscapeString(prop.Value)}\");");

                _propertyNames.Add(prop.Name.LocalName);
            }

            _indent--;
            AppendLine("});");
        }
    }

    private void GenerateItemGroup(XElement itemGroup)
    {
        var condition = itemGroup.Attribute("Condition")?.Value;

        if (condition != null)
            AppendLine($"p.ItemGroup(\"{EscapeString(condition)}\", group =>");
        else
            AppendLine("p.ItemGroup(null, group =>");

        AppendLine("{");
        _indent++;

        foreach (var item in itemGroup.Elements())
        {
            GenerateItem(item);
        }

        _indent--;
        AppendLine("});");
    }

    private void GenerateItem(XElement item)
    {
        var itemType = item.Name.LocalName;
        var include = item.Attribute("Include")?.Value;
        var exclude = item.Attribute("Exclude")?.Value;
        var remove = item.Attribute("Remove")?.Value;
        var update = item.Attribute("Update")?.Value;
        var condition = item.Attribute("Condition")?.Value;

        _itemTypes.Add(itemType);

        string operation;
        string value;
        if (include != null)
        {
            operation = "Include";
            value = include;
        }
        else if (exclude != null)
        {
            operation = "Exclude";
            value = exclude;
        }
        else if (remove != null)
        {
            operation = "Remove";
            value = remove;
        }
        else if (update != null)
        {
            operation = "Update";
            value = update;
        }
        else
        {
            AppendLine($"// Skipped {itemType} with no Include/Exclude/Remove/Update");
            return;
        }

        var hasMetadata = item.Elements().Any();

        if (hasMetadata)
        {
            AppendLine($"group.{operation}(\"{itemType}\", \"{EscapeString(value)}\", item =>");

            AppendLine("{");
            _indent++;

            foreach (var meta in item.Elements())
            {
                var metaCondition = meta.Attribute("Condition")?.Value;
                if (metaCondition != null)
                    AppendLine($"item.Meta(\"{meta.Name.LocalName}\", \"{EscapeString(meta.Value)}\", \"{EscapeString(metaCondition)}\");");
                else
                    AppendLine($"item.Meta(\"{meta.Name.LocalName}\", \"{EscapeString(meta.Value)}\");");
            }

            _indent--;
            
            // For includes with metadata, pass condition as 4th parameter after the configure lambda
            if (condition != null)
                AppendLine($"}}, \"{EscapeString(condition)}\");");
            else
                AppendLine("});");
        }
        else
        {
            // No metadata - pass null for configure and condition as 4th parameter
            if (condition != null)
                AppendLine($"group.{operation}(\"{itemType}\", \"{EscapeString(value)}\", null, \"{EscapeString(condition)}\");");
            else
                AppendLine($"group.{operation}(\"{itemType}\", \"{EscapeString(value)}\");");
        }
    }

    private void GenerateChoose(XElement choose)
    {
        AppendLine("p.Choose(choose =>");
        AppendLine("{");
        _indent++;

        foreach (var element in choose.Elements())
        {
            if (element.Name.LocalName == "When")
            {
                var condition = element.Attribute("Condition")?.Value ?? "";
                AppendLine($"choose.When(\"{EscapeString(condition)}\", whenProps =>");
                AppendLine("{");
                _indent++;

                foreach (var child in element.Elements())
                {
                    if (child.Name.LocalName == "PropertyGroup")
                    {
                        foreach (var prop in child.Elements())
                        {
                            AppendLine($"whenProps.Property(\"{prop.Name.LocalName}\", \"{EscapeString(prop.Value)}\");");
                            _propertyNames.Add(prop.Name.LocalName);
                        }
                    }
                    else if (child.Name.LocalName == "ItemGroup")
                    {
                        foreach (var item in child.Elements())
                        {
                            GenerateItem(item);
                        }
                    }
                }

                _indent--;
                AppendLine("});");
            }
            else if (element.Name.LocalName == "Otherwise")
            {
                AppendLine("choose.Otherwise(otherwiseProps =>");
                AppendLine("{");
                _indent++;

                foreach (var child in element.Elements())
                {
                    if (child.Name.LocalName == "PropertyGroup")
                    {
                        foreach (var prop in child.Elements())
                        {
                            AppendLine($"otherwiseProps.Property(\"{prop.Name.LocalName}\", \"{EscapeString(prop.Value)}\");");
                            _propertyNames.Add(prop.Name.LocalName);
                        }
                    }
                    else if (child.Name.LocalName == "ItemGroup")
                    {
                        foreach (var item in child.Elements())
                        {
                            GenerateItem(item);
                        }
                    }
                }

                _indent--;
                AppendLine("});");
            }
        }

        _indent--;
        AppendLine("});");
    }

    private void GenerateImport(XElement import)
    {
        var project = import.Attribute("Project")?.Value;
        var condition = import.Attribute("Condition")?.Value;

        if (project != null)
        {
            if (condition != null)
                AppendLine($"p.Import(\"{EscapeString(project)}\", \"{EscapeString(condition)}\");");
            else
                AppendLine($"p.Import(\"{EscapeString(project)}\");");
        }
    }

    private void GenerateUsingTask(XElement usingTask)
    {
        var taskName = usingTask.Attribute("TaskName")?.Value;
        var assemblyFile = usingTask.Attribute("AssemblyFile")?.Value;
        var assemblyName = usingTask.Attribute("AssemblyName")?.Value;
        var condition = usingTask.Attribute("Condition")?.Value;

        if (taskName == null) return;

        if (condition != null)
            AppendLine($"t.UsingTask(\"{taskName}\", \"{EscapeString(assemblyFile ?? "")}\", \"{EscapeString(condition)}\", \"{EscapeString(assemblyName ?? "")}\");");
        else if (assemblyFile != null && assemblyName != null)
            AppendLine($"t.UsingTask(\"{taskName}\", \"{EscapeString(assemblyFile)}\", assemblyName: \"{EscapeString(assemblyName)}\");");
        else if (assemblyFile != null)
            AppendLine($"t.UsingTask(\"{taskName}\", \"{EscapeString(assemblyFile)}\");");
        else if (assemblyName != null)
            AppendLine($"t.UsingTask(\"{taskName}\", null, assemblyName: \"{EscapeString(assemblyName)}\");");
    }

    private void GenerateTarget(XElement target)
    {
        var name = target.Attribute("Name")?.Value;
        if (name == null) return;

        _targetNames.Add(name);

        AppendLine($"t.Target(\"{name}\", target =>");
        AppendLine("{");
        _indent++;

        // Generate target attributes
        var beforeTargets = target.Attribute("BeforeTargets")?.Value;
        var afterTargets = target.Attribute("AfterTargets")?.Value;
        var dependsOnTargets = target.Attribute("DependsOnTargets")?.Value;
        var inputs = target.Attribute("Inputs")?.Value;
        var outputs = target.Attribute("Outputs")?.Value;
        var condition = target.Attribute("Condition")?.Value;
        var returns = target.Attribute("Returns")?.Value;

        if (beforeTargets != null)
            AppendLine($"target.BeforeTargets(\"{EscapeString(beforeTargets)}\");");
        if (afterTargets != null)
            AppendLine($"target.AfterTargets(\"{EscapeString(afterTargets)}\");");
        if (dependsOnTargets != null)
            AppendLine($"target.DependsOnTargets(\"{EscapeString(dependsOnTargets)}\");");
        if (inputs != null)
            AppendLine($"target.Inputs(\"{EscapeString(inputs)}\");");
        if (outputs != null)
            AppendLine($"target.Outputs(\"{EscapeString(outputs)}\");");
        if (condition != null)
            AppendLine($"target.Condition(\"{EscapeString(condition)}\");");
        if (returns != null)
            AppendLine($"target.Returns(\"{EscapeString(returns)}\");");

        // Generate target body
        foreach (var element in target.Elements())
        {
            GenerateTargetElement(element);
        }

        _indent--;
        AppendLine("});");
    }

    private void GenerateTargetElement(XElement element)
    {
        switch (element.Name.LocalName)
        {
            case "PropertyGroup":
                GenerateTargetPropertyGroup(element);
                break;
            case "ItemGroup":
                GenerateTargetItemGroup(element);
                break;
            case "Message":
                GenerateMessage(element);
                break;
            case "Exec":
                GenerateExec(element);
                break;
            case "Error":
                GenerateError(element);
                break;
            case "Warning":
                GenerateWarning(element);
                break;
            default:
                GenerateGenericTask(element);
                break;
        }
    }

    private void GenerateTargetPropertyGroup(XElement propertyGroup)
    {
        var condition = propertyGroup.Attribute("Condition")?.Value;

        if (condition != null)
            AppendLine($"target.PropertyGroup(\"{EscapeString(condition)}\", group =>");
        else
            AppendLine("target.PropertyGroup(null, group =>");

        AppendLine("{");
        _indent++;

        foreach (var prop in propertyGroup.Elements())
        {
            AppendLine($"group.Property(\"{prop.Name.LocalName}\", \"{EscapeString(prop.Value)}\");");
            _propertyNames.Add(prop.Name.LocalName);
        }

        _indent--;
        AppendLine("});");
    }

    private void GenerateTargetItemGroup(XElement itemGroup)
    {
        var condition = itemGroup.Attribute("Condition")?.Value;

        if (condition != null)
            AppendLine($"target.ItemGroup(\"{EscapeString(condition)}\", group =>");
        else
            AppendLine("target.ItemGroup(null, group =>");

        AppendLine("{");
        _indent++;

        foreach (var item in itemGroup.Elements())
        {
            GenerateItem(item);
        }

        _indent--;
        AppendLine("});");
    }

    private void GenerateMessage(XElement message)
    {
        var text = message.Attribute("Text")?.Value ?? message.Value;
        var importance = message.Attribute("Importance")?.Value;
        var condition = message.Attribute("Condition")?.Value;

        if (importance != null && condition != null)
            AppendLine($"target.Message(\"{EscapeString(text)}\", \"{importance}\", \"{EscapeString(condition)}\");");
        else if (importance != null)
            AppendLine($"target.Message(\"{EscapeString(text)}\", \"{importance}\");");
        else if (condition != null)
            AppendLine($"target.Message(\"{EscapeString(text)}\", condition: \"{EscapeString(condition)}\");");
        else
            AppendLine($"target.Message(\"{EscapeString(text)}\");");
    }

    private void GenerateExec(XElement exec)
    {
        var command = exec.Attribute("Command")?.Value;
        var workingDirectory = exec.Attribute("WorkingDirectory")?.Value;
        var condition = exec.Attribute("Condition")?.Value;

        if (command == null) return;

        if (workingDirectory != null && condition != null)
            AppendLine($"target.Exec(\"{EscapeString(command)}\", \"{EscapeString(workingDirectory)}\", \"{EscapeString(condition)}\");");
        else if (workingDirectory != null)
            AppendLine($"target.Exec(\"{EscapeString(command)}\", \"{EscapeString(workingDirectory)}\");");
        else if (condition != null)
            AppendLine($"target.Exec(\"{EscapeString(command)}\", condition: \"{EscapeString(condition)}\");");
        else
            AppendLine($"target.Exec(\"{EscapeString(command)}\");");
    }

    private void GenerateError(XElement error)
    {
        var text = error.Attribute("Text")?.Value ?? error.Value;
        var condition = error.Attribute("Condition")?.Value;

        if (condition != null)
            AppendLine($"target.Error(\"{EscapeString(text)}\", \"{EscapeString(condition)}\");");
        else
            AppendLine($"target.Error(\"{EscapeString(text)}\");");
    }

    private void GenerateWarning(XElement warning)
    {
        var text = warning.Attribute("Text")?.Value ?? warning.Value;
        var condition = warning.Attribute("Condition")?.Value;

        if (condition != null)
            AppendLine($"target.Warning(\"{EscapeString(text)}\", \"{EscapeString(condition)}\");");
        else
            AppendLine($"target.Warning(\"{EscapeString(text)}\");");
    }

    private void GenerateGenericTask(XElement task)
    {
        var taskName = task.Name.LocalName;
        var condition = task.Attribute("Condition")?.Value;

        if (condition != null)
        {
            AppendLine($"target.Task(\"{taskName}\", task =>");
        }
        else
        {
            AppendLine($"target.Task(\"{taskName}\", task =>");
        }
        
        AppendLine("{");
        _indent++;

        foreach (var attr in task.Attributes())
        {
            if (attr.Name.LocalName == "Condition") continue;
            AppendLine($"task.Param(\"{attr.Name.LocalName}\", \"{EscapeString(attr.Value)}\");");
        }

        // Handle Output elements
        foreach (var output in task.Elements().Where(e => e.Name.LocalName == "Output"))
        {
            var taskParam = output.Attribute("TaskParameter")?.Value;
            var propName = output.Attribute("PropertyName")?.Value;
            var itemName = output.Attribute("ItemName")?.Value;
            var outputCondition = output.Attribute("Condition")?.Value;

            if (taskParam != null && propName != null)
            {
                if (outputCondition != null)
                    AppendLine($"task.OutputProperty(\"{taskParam}\", \"{propName}\", \"{EscapeString(outputCondition)}\");");
                else
                    AppendLine($"task.OutputProperty(\"{taskParam}\", \"{propName}\");");
            }
            else if (taskParam != null && itemName != null)
            {
                if (outputCondition != null)
                    AppendLine($"task.OutputItem(\"{taskParam}\", \"{itemName}\", \"{EscapeString(outputCondition)}\");");
                else
                    AppendLine($"task.OutputItem(\"{taskParam}\", \"{itemName}\");");
            }
        }

        _indent--;
        
        if (condition != null)
            AppendLine($"}}, \"{EscapeString(condition)}\");");
        else
            AppendLine("});");
    }

    private void GenerateStronglyTypedStructs()
    {
        if (_propertyNames.Count > 0)
        {
            AppendLine();
            AppendLine("// Property names:");
            foreach (var name in _propertyNames.OrderBy(n => n))
            {
                AppendLine($"// public readonly struct {MakeSafeName(name)} : IMsBuildPropertyName");
                AppendLine($"// {{");
                AppendLine($"//     public string Name => \"{name}\";");
                AppendLine($"// }}");
            }
        }

        if (_itemTypes.Count > 0)
        {
            AppendLine();
            AppendLine("// Item types:");
            foreach (var name in _itemTypes.OrderBy(n => n))
            {
                AppendLine($"// public readonly struct {MakeSafeName(name)}Item : IMsBuildItemTypeName");
                AppendLine($"// {{");
                AppendLine($"//     public string Name => \"{name}\";");
                AppendLine($"// }}");
            }
        }

        if (_targetNames.Count > 0)
        {
            AppendLine();
            AppendLine("// Target names:");
            foreach (var name in _targetNames.OrderBy(n => n))
            {
                AppendLine($"// public readonly struct {MakeSafeName(name)}Target : IMsBuildTargetName");
                AppendLine($"// {{");
                AppendLine($"//     public string Name => \"{name}\";");
                AppendLine($"// }}");
            }
        }
    }

    private string MakeSafeName(string name)
    {
        // Convert to PascalCase and remove invalid characters
        var parts = name.Split(new[] { '_', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
        var result = string.Concat(parts.Select(p => char.ToUpper(p[0]) + p.Substring(1)));
        return string.IsNullOrEmpty(result) ? "Property" : result;
    }

    private string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    private void Append(string text)
    {
        _code.Append(text);
    }

    private void AppendLine(string text = "")
    {
        if (!string.IsNullOrEmpty(text))
            _code.Append(new string(' ', _indent * 4)).AppendLine(text);
        else
            _code.AppendLine();
    }
}
