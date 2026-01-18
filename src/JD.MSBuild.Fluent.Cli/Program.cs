using System.CommandLine;
using System.Reflection;
using System.Runtime.Loader;
using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.Packaging;
using JD.MSBuild.Fluent.Typed;

var root = new RootCommand("JD.MSBuild.Fluent CLI - generate MSBuild assets from fluent definitions");

var generate = new Command("generate", "Generate build/buildTransitive/Sdk assets from a definition factory method.");
var assemblyOpt = new Option<FileInfo?>("--assembly")
{
  Description = "Path to an assembly containing a factory method that returns PackageDefinition."
};
var typeOpt = new Option<string?>("--type")
{
  Description = "Fully qualified type name containing the factory method."
};
var methodOpt = new Option<string>("--method")
{
  Description = "Factory method name (static, parameterless, returns PackageDefinition)",
  DefaultValueFactory = _ => "Create"
};
var outOpt = new Option<DirectoryInfo>("--output")
{
  Description = "Output directory for generated assets",
  DefaultValueFactory = _ => new DirectoryInfo("artifacts/msbuild")
};
var exampleOpt = new Option<bool>("--example")
{
  Description = "Use an internal example definition (ignores --assembly/--type)"
};

generate.Options.Add(assemblyOpt);
generate.Options.Add(typeOpt);
generate.Options.Add(methodOpt);
generate.Options.Add(outOpt);
generate.Options.Add(exampleOpt);

generate.SetAction(parseResult =>
{
  var asmFile = parseResult.GetValue(assemblyOpt);
  var typeName = parseResult.GetValue(typeOpt);
  var methodName = parseResult.GetValue(methodOpt)!;
  var output = parseResult.GetValue(outOpt);
  var example = parseResult.GetValue(exampleOpt);

  var def = example
    ? CreateExample()
    : LoadFromFactory(asmFile, typeName, methodName);

  var emitter = new MsBuildPackageEmitter();
  emitter.Emit(def, output!.FullName);

  Console.WriteLine($"Generated MSBuild assets for '{def.Id}' to: {output.FullName}");
});

root.Subcommands.Add(generate);

// Scaffold command - convert XML to fluent API
var scaffold = new Command("scaffold", "Convert MSBuild XML (.props/.targets) to fluent API C# code.");
var xmlFileOpt = new Option<FileInfo>("--xml")
{
  Description = "Path to MSBuild XML file (.props or .targets) to scaffold",
  Arity = ArgumentArity.ExactlyOne
};
var scaffoldOutOpt = new Option<FileInfo?>("--output")
{
  Description = "Output C# file path (default: DefinitionFactory.cs in current directory)"
};
var packageIdOpt = new Option<string?>("--package-id")
{
  Description = "Package ID for the definition (default: derived from filename)"
};
var classNameOpt = new Option<string?>("--class-name")
{
  Description = "Factory class name (default: DefinitionFactory)"
};
var returnProjectOpt = new Option<bool>("--return-project")
{
  Description = "Generate factory returning MsBuildProject instead of PackageDefinition (for individual .props/.targets files)"
};

scaffold.Options.Add(xmlFileOpt);
scaffold.Options.Add(scaffoldOutOpt);
scaffold.Options.Add(packageIdOpt);
scaffold.Options.Add(classNameOpt);
scaffold.Options.Add(returnProjectOpt);

scaffold.SetAction(parseResult =>
{
  var xmlFile = parseResult.GetValue(xmlFileOpt)!;
  var outputFile = parseResult.GetValue(scaffoldOutOpt);
  var packageId = parseResult.GetValue(packageIdOpt);
  var className = parseResult.GetValue(classNameOpt);
  var returnProject = parseResult.GetValue(returnProjectOpt);

  var scaffolder = new JD.MSBuild.Fluent.Cli.XmlToFluentScaffolder();
  var code = scaffolder.Scaffold(xmlFile.FullName, packageId, className, returnProject);

  var outPath = outputFile?.FullName ?? Path.Combine(Directory.GetCurrentDirectory(), "DefinitionFactory.cs");
  File.WriteAllText(outPath, code);

  Console.WriteLine($"Scaffolded fluent API code to: {outPath}");
  Console.WriteLine($"Review and adjust the generated code as needed.");
});

root.Subcommands.Add(scaffold);

return root.Parse(args).Invoke();

static PackageDefinition LoadFromFactory(FileInfo? asmFile, string? typeName, string methodName)
{
  if (asmFile is null) throw new ArgumentException("--assembly is required unless --example is used.");
  if (string.IsNullOrWhiteSpace(typeName)) throw new ArgumentException("--type is required unless --example is used.");
  if (!asmFile.Exists) throw new FileNotFoundException("Assembly not found", asmFile.FullName);

  var alc = new AssemblyLoadContext("JD.MSBuild.Fluent.Dynamic", isCollectible: true);
  var asm = alc.LoadFromAssemblyPath(asmFile.FullName);

  var t = asm.GetType(typeName, throwOnError: true)!;
  var m = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
          ?? throw new MissingMethodException(typeName, methodName);

  if (m.GetParameters().Length != 0)
    throw new InvalidOperationException($"Factory method {typeName}.{methodName} must be parameterless.");

  if (!typeof(PackageDefinition).IsAssignableFrom(m.ReturnType))
    throw new InvalidOperationException($"Factory method {typeName}.{methodName} must return {nameof(PackageDefinition)}.");

  var result = (PackageDefinition?)m.Invoke(null, null);
  if (result is null) throw new InvalidOperationException("Factory method returned null.");

  alc.Unload();
  return result;
}

static PackageDefinition CreateExample()
{
  return Package.Define("JD.MSBuild.Fluent.Example")
    .Description("Example package emitted by jdmsbuild --example")
    .Props(p => p
      .Property<JdMsbuildFluentEnabled>("true")
      .ItemGroup(null, ig => ig
        .Include<MsBuildItemTypes.None>("README.md", i => i.Meta<PackMetadata>("true"))))
    .Targets(t => t
      .Target<JdMsbuildFluentExampleTarget>(tgt => tgt
        .BeforeTargets(new MsBuildTargets.Build())
        .Condition($"'$({new JdMsbuildFluentEnabled().Name})' == 'true'")
        .Message("Hello from JD.MSBuild.Fluent")
        .Exec("dotnet --info")))
    .Pack(o => { o.BuildTransitive = true; o.EmitSdk = true; })
    .Build();
}

readonly struct JdMsbuildFluentEnabled : IMsBuildPropertyName
{
  public string Name => "JdMsbuildFluentEnabled";
}

readonly struct JdMsbuildFluentExampleTarget : IMsBuildTargetName
{
  public string Name => "JdMsbuildFluent_Example";
}

readonly struct PackMetadata : IMsBuildMetadataName
{
  public string Name => "Pack";
}
