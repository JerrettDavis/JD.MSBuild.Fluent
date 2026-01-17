using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace JD.MSBuild.Fluent.Generators;

/// <summary>
/// Source generator that emits strongly-typed MSBuild task helpers.
/// </summary>
[Generator]
public sealed class MsBuildTaskGenerator : IIncrementalGenerator
{
  private const string TaskAttributeName = "JD.MSBuild.Fluent.Typed.MsBuildTaskAttribute";
  private const string IgnoreAttributeName = "JD.MSBuild.Fluent.Typed.MsBuildIgnoreAttribute";
  private const string OutputAttributeName = "Microsoft.Build.Framework.OutputAttribute";

  /// <inheritdoc />
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    var tasks = context.SyntaxProvider.ForAttributeWithMetadataName(
        TaskAttributeName,
        static (node, _) => node is ClassDeclarationSyntax,
        static (ctx, _) => new TaskCandidate((INamedTypeSymbol)ctx.TargetSymbol, ctx.Attributes))
      .Collect();

    var compilationAndTasks = context.CompilationProvider.Combine(tasks);

    context.RegisterSourceOutput(compilationAndTasks, static (spc, pair) =>
    {
      Execute(pair.Left, pair.Right, spc);
    });
  }

  private static void Execute(Compilation compilation, IReadOnlyList<TaskCandidate> candidates, SourceProductionContext context)
  {
    if (candidates.Count == 0)
      return;

    var distinct = new List<TaskCandidate>();
    var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
    foreach (var candidate in candidates)
    {
      if (seen.Add(candidate.Symbol))
        distinct.Add(candidate);
    }

    foreach (var candidate in distinct)
    {
      var info = BuildTaskInfo(compilation, candidate);
      if (info is null)
        continue;

      var source = Render(info);
      context.AddSource($"MsBuildTask_{info.GeneratedTypeName}.g.cs", source);
    }
  }

  private static TaskInfo? BuildTaskInfo(Compilation compilation, TaskCandidate candidate)
  {
    var symbol = candidate.Symbol;
    var attr = candidate.Attributes.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == TaskAttributeName);
    if (attr is null)
      return null;

    var data = ParseAttribute(attr, compilation);
    var taskName = BuildTaskName(symbol, data.Name, data.NameStyle);

    var parameters = new List<string>();
    var outputs = new List<string>();

    foreach (var property in symbol.GetMembers().OfType<IPropertySymbol>())
    {
      if (property.IsStatic)
        continue;
      if (property.DeclaredAccessibility != Accessibility.Public)
        continue;
      if (property.SetMethod is null)
        continue;
      if (HasAttribute(property, IgnoreAttributeName))
        continue;

      parameters.Add(property.Name);

      if (HasAttribute(property, OutputAttributeName))
        outputs.Add(property.Name);
    }

    parameters = parameters.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
    outputs = outputs.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();

    var containingNamespace = symbol.ContainingNamespace;
    var ns = containingNamespace is null || containingNamespace.IsGlobalNamespace
      ? null
      : containingNamespace.ToDisplayString();

    var generatedTypeName = BuildGeneratedTypeName(symbol);
    var qualifiedTypeName = string.IsNullOrWhiteSpace(ns) ? generatedTypeName : $"{ns}.{generatedTypeName}";
    var extensionNamespace = BuildExtensionNamespace(ns, BuildTaskExtensionSuffix(symbol));

    return new TaskInfo(
      ns,
      generatedTypeName,
      qualifiedTypeName,
      extensionNamespace,
      taskName,
      data.AssemblyFile,
      data.AssemblyName,
      data.TaskFactory,
      parameters,
      outputs);
  }

  private static TaskAttributeData ParseAttribute(AttributeData attribute, Compilation compilation)
  {
    var name = GetNamedString(attribute, "Name");
    var assemblyFile = GetNamedString(attribute, "AssemblyFile");
    var assemblyName = GetNamedString(attribute, "AssemblyName");
    var taskFactory = GetNamedString(attribute, "TaskFactory");
    var useAssemblyName = GetNamedBool(attribute, "UseAssemblyName");
    var nameStyle = GetNamedEnum(attribute, "NameStyle", defaultValue: 1);

    if (AttributeValuesMissing(name, assemblyFile, assemblyName, taskFactory, useAssemblyName, nameStyle))
      ParseAttributeSyntax(attribute, compilation, ref name, ref assemblyFile, ref assemblyName, ref taskFactory, ref useAssemblyName, ref nameStyle);

    if (useAssemblyName && string.IsNullOrWhiteSpace(assemblyName))
      assemblyName = compilation.AssemblyName;

    return new TaskAttributeData(name, nameStyle, assemblyFile, assemblyName, taskFactory);
  }

  private static bool AttributeValuesMissing(string? name, string? assemblyFile, string? assemblyName, string? taskFactory, bool useAssemblyName, int nameStyle)
  {
    return name is null
      && assemblyFile is null
      && assemblyName is null
      && taskFactory is null
      && !useAssemblyName
      && nameStyle == 1;
  }

  private static void ParseAttributeSyntax(
    AttributeData attribute,
    Compilation compilation,
    ref string? name,
    ref string? assemblyFile,
    ref string? assemblyName,
    ref string? taskFactory,
    ref bool useAssemblyName,
    ref int nameStyle)
  {
    if (attribute.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax syntax)
      return;

    if (syntax.ArgumentList is null)
      return;

    var model = compilation.GetSemanticModel(syntax.SyntaxTree);
    foreach (var argument in syntax.ArgumentList.Arguments)
    {
      if (argument.NameEquals is null)
        continue;

      var key = argument.NameEquals.Name.Identifier.ValueText;
      var constant = model.GetConstantValue(argument.Expression);
      if (!constant.HasValue)
        continue;

      switch (key)
      {
        case "Name":
          name ??= constant.Value as string;
          break;
        case "AssemblyFile":
          assemblyFile ??= constant.Value as string;
          break;
        case "AssemblyName":
          assemblyName ??= constant.Value as string;
          break;
        case "TaskFactory":
          taskFactory ??= constant.Value as string;
          break;
        case "UseAssemblyName":
          if (!useAssemblyName && constant.Value is bool useValue)
            useAssemblyName = useValue;
          break;
        case "NameStyle":
          if (nameStyle == 1 && constant.Value is int enumValue)
            nameStyle = enumValue;
          break;
      }
    }
  }

  private static string BuildTaskName(INamedTypeSymbol symbol, string? overrideName, int nameStyle)
  {
    if (!string.IsNullOrWhiteSpace(overrideName))
      return overrideName!;

    if (nameStyle == 0)
      return symbol.Name;

    var format = new SymbolDisplayFormat(
      globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
      typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
      genericsOptions: SymbolDisplayGenericsOptions.None);

    return symbol.ToDisplayString(format);
  }

  private static string BuildGeneratedTypeName(INamedTypeSymbol symbol)
  {
    var typeNames = new Stack<string>();
    var containing = symbol.ContainingType;
    while (containing is not null)
    {
      typeNames.Push(containing.Name);
      containing = containing.ContainingType;
    }

    var prefix = typeNames.Count == 0 ? string.Empty : string.Join("_", typeNames) + "_";
    return prefix + symbol.Name + "MsBuild";
  }

  private static string BuildTaskExtensionSuffix(INamedTypeSymbol symbol)
  {
    var typeNames = new Stack<string>();
    var containing = symbol.ContainingType;
    while (containing is not null)
    {
      typeNames.Push(containing.Name);
      containing = containing.ContainingType;
    }

    var prefix = typeNames.Count == 0 ? string.Empty : string.Join("_", typeNames) + "_";
    return prefix + symbol.Name;
  }

  private static string BuildExtensionNamespace(string? ns, string suffix)
  {
    if (string.IsNullOrWhiteSpace(ns))
      return $"JD.MSBuild.Fluent.Extensions.{suffix}";

    return $"{ns}.Extensions.{suffix}";
  }

  private static bool HasAttribute(ISymbol symbol, string metadataName)
    => symbol.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == metadataName);

  private static string? GetNamedString(AttributeData attribute, string name)
  {
    foreach (var pair in attribute.NamedArguments)
    {
      if (pair.Key == name)
        return pair.Value.Value as string;
    }
    return null;
  }

  private static bool GetNamedBool(AttributeData attribute, string name)
  {
    foreach (var pair in attribute.NamedArguments)
    {
      if (pair.Key == name && pair.Value.Value is bool value)
        return value;
    }
    return false;
  }

  private static int GetNamedEnum(AttributeData attribute, string name, int defaultValue)
  {
    foreach (var pair in attribute.NamedArguments)
    {
      if (pair.Key == name && pair.Value.Value is int value)
        return value;
    }
    return defaultValue;
  }

  private static SourceText Render(TaskInfo info)
  {
    var sb = new StringBuilder();
    sb.AppendLine("// <auto-generated />");
    sb.AppendLine("#nullable enable");
    sb.AppendLine("using JD.MSBuild.Fluent.Typed;");
    sb.AppendLine("using JD.MSBuild.Fluent.Fluent;");
    sb.AppendLine();

    if (!string.IsNullOrWhiteSpace(info.Namespace))
    {
      sb.Append("namespace ").Append(info.Namespace).AppendLine();
      sb.AppendLine("{");
    }

    var indent = string.IsNullOrWhiteSpace(info.Namespace) ? string.Empty : "  ";
    sb.Append(indent).Append("public static class ").Append(info.GeneratedTypeName).AppendLine();
    sb.Append(indent).AppendLine("{");

    AppendTaskName(sb, indent + "  ", info.TaskName);
    AppendReference(sb, indent + "  ", info.TaskName, info.AssemblyFile, info.AssemblyName, info.TaskFactory);
    AppendParameters(sb, indent + "  ", info.Parameters);
    AppendOutputs(sb, indent + "  ", info.Outputs);

    sb.Append(indent).AppendLine("}");

    if (!string.IsNullOrWhiteSpace(info.Namespace))
      sb.AppendLine("}");

    AppendExtensions(sb, info);

    return SourceText.From(sb.ToString(), Encoding.UTF8);
  }

  private static void AppendTaskName(StringBuilder sb, string indent, string taskName)
  {
    sb.Append(indent).AppendLine("public readonly struct TaskName : IMsBuildTaskName");
    sb.Append(indent).AppendLine("{");
    sb.Append(indent).Append("  public string Name => \"").Append(EscapeString(taskName)).AppendLine("\";");
    sb.Append(indent).AppendLine("}");
    sb.AppendLine();
  }

  private static void AppendReference(StringBuilder sb, string indent, string taskName, string? assemblyFile, string? assemblyName, string? taskFactory)
  {
    var args = new List<string> { $"\"{EscapeString(taskName)}\"" };
    if (!string.IsNullOrWhiteSpace(assemblyFile))
      args.Add($"assemblyFile: \"{EscapeString(assemblyFile!)}\"");
    if (!string.IsNullOrWhiteSpace(assemblyName))
      args.Add($"assemblyName: \"{EscapeString(assemblyName!)}\"");
    if (!string.IsNullOrWhiteSpace(taskFactory))
      args.Add($"taskFactory: \"{EscapeString(taskFactory!)}\"");

    sb.Append(indent).Append("public static MsBuildTaskReference Reference => new MsBuildTaskReference(");
    sb.Append(string.Join(", ", args));
    sb.AppendLine(");");
  }

  private static void AppendParameters(StringBuilder sb, string indent, IReadOnlyList<string> parameters)
  {
    if (parameters.Count == 0)
      return;

    sb.AppendLine();
    sb.Append(indent).AppendLine("public static class Parameters");
    sb.Append(indent).AppendLine("{");

    foreach (var name in parameters)
    {
      var identifier = EscapeIdentifier(name);
      sb.Append(indent).Append("  public readonly struct ").Append(identifier).AppendLine(" : IMsBuildTaskParameterName");
      sb.Append(indent).AppendLine("  {");
      sb.Append(indent).Append("    public string Name => \"").Append(EscapeString(name)).AppendLine("\";");
      sb.Append(indent).AppendLine("  }");
    }

    sb.Append(indent).AppendLine("}");
  }

  private static void AppendOutputs(StringBuilder sb, string indent, IReadOnlyList<string> outputs)
  {
    if (outputs.Count == 0)
      return;

    sb.AppendLine();
    sb.Append(indent).AppendLine("public static class Outputs");
    sb.Append(indent).AppendLine("{");

    foreach (var name in outputs)
    {
      var identifier = EscapeIdentifier(name);
      sb.Append(indent).Append("  public readonly struct ").Append(identifier).AppendLine(" : IMsBuildTaskParameterName");
      sb.Append(indent).AppendLine("  {");
      sb.Append(indent).Append("    public string Name => \"").Append(EscapeString(name)).AppendLine("\";");
      sb.Append(indent).AppendLine("  }");
    }

    sb.Append(indent).AppendLine("}");
  }

  private static void AppendExtensions(StringBuilder sb, TaskInfo info)
  {
    if (info.Parameters.Count == 0 && info.Outputs.Count == 0)
      return;

    sb.AppendLine();
    sb.Append("namespace ").Append(info.ExtensionNamespace).AppendLine();
    sb.AppendLine("{");
    sb.AppendLine("  public static class TaskInvocationExtensions");
    sb.AppendLine("  {");

    if (info.Parameters.Count > 0)
      AppendParameterExtensions(sb, info);
    if (info.Parameters.Count > 0 && info.Outputs.Count > 0)
      sb.AppendLine();
    if (info.Outputs.Count > 0)
      AppendOutputExtensions(sb, info);

    sb.AppendLine("  }");
    sb.AppendLine("}");
  }

  private static void AppendParameterExtensions(StringBuilder sb, TaskInfo info)
  {
    for (var i = 0; i < info.Parameters.Count; i++)
    {
      var name = info.Parameters[i];
      var identifier = EscapeIdentifier(name);
      sb.Append("    public static TaskInvocationBuilder ").Append(identifier)
        .AppendLine("(this TaskInvocationBuilder task, string value)");
      sb.Append("      => task.Param<global::").Append(info.QualifiedTypeName)
        .Append(".Parameters.").Append(identifier).AppendLine(">(value);");

      if (i < info.Parameters.Count - 1)
        sb.AppendLine();
    }
  }

  private static void AppendOutputExtensions(StringBuilder sb, TaskInfo info)
  {
    for (var i = 0; i < info.Outputs.Count; i++)
    {
      var name = info.Outputs[i];
      var propertyMethodName = EscapeIdentifier(name + "Property");
      sb.Append("    public static TaskInvocationBuilder ").Append(propertyMethodName).Append("<TProperty>")
        .AppendLine("(this TaskInvocationBuilder task, string? condition = null)");
      sb.AppendLine("      where TProperty : IMsBuildPropertyName, new()");
      sb.Append("      => task.OutputProperty<global::").Append(info.QualifiedTypeName)
        .Append(".Outputs.").Append(EscapeIdentifier(name)).AppendLine(", TProperty>(condition);");
      sb.AppendLine();

      var itemMethodName = EscapeIdentifier(name + "Item");
      sb.Append("    public static TaskInvocationBuilder ").Append(itemMethodName).Append("<TItem>")
        .AppendLine("(this TaskInvocationBuilder task, string? condition = null)");
      sb.AppendLine("      where TItem : IMsBuildItemTypeName, new()");
      sb.Append("      => task.OutputItem<global::").Append(info.QualifiedTypeName)
        .Append(".Outputs.").Append(EscapeIdentifier(name)).AppendLine(", TItem>(condition);");

      if (i < info.Outputs.Count - 1)
        sb.AppendLine();
    }
  }

  private static string EscapeString(string value)
    => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

  private static string EscapeIdentifier(string name)
  {
    return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None
      ? "@" + name
      : name;
  }

  private sealed class TaskCandidate
  {
    public TaskCandidate(INamedTypeSymbol symbol, IReadOnlyList<AttributeData> attributes)
    {
      Symbol = symbol;
      Attributes = attributes;
    }

    public INamedTypeSymbol Symbol { get; }
    public IReadOnlyList<AttributeData> Attributes { get; }
  }

  private sealed class TaskAttributeData
  {
    public TaskAttributeData(string? name, int nameStyle, string? assemblyFile, string? assemblyName, string? taskFactory)
    {
      Name = name;
      NameStyle = nameStyle;
      AssemblyFile = assemblyFile;
      AssemblyName = assemblyName;
      TaskFactory = taskFactory;
    }

    public string? Name { get; }
    public int NameStyle { get; }
    public string? AssemblyFile { get; }
    public string? AssemblyName { get; }
    public string? TaskFactory { get; }
  }

  private sealed class TaskInfo
  {
    public TaskInfo(
      string? ns,
      string generatedTypeName,
      string qualifiedTypeName,
      string extensionNamespace,
      string taskName,
      string? assemblyFile,
      string? assemblyName,
      string? taskFactory,
      IReadOnlyList<string> parameters,
      IReadOnlyList<string> outputs)
    {
      Namespace = ns;
      GeneratedTypeName = generatedTypeName;
      QualifiedTypeName = qualifiedTypeName;
      ExtensionNamespace = extensionNamespace;
      TaskName = taskName;
      AssemblyFile = assemblyFile;
      AssemblyName = assemblyName;
      TaskFactory = taskFactory;
      Parameters = parameters;
      Outputs = outputs;
    }

    public string? Namespace { get; }
    public string GeneratedTypeName { get; }
    public string QualifiedTypeName { get; }
    public string ExtensionNamespace { get; }
    public string TaskName { get; }
    public string? AssemblyFile { get; }
    public string? AssemblyName { get; }
    public string? TaskFactory { get; }
    public IReadOnlyList<string> Parameters { get; }
    public IReadOnlyList<string> Outputs { get; }
  }
}
