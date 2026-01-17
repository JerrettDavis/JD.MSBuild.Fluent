using JD.MSBuild.Fluent.Typed;

namespace Contoso.Build.Tasks
{
  [MsBuildTask(AssemblyFile = "$(_EfcptTaskAssembly)")]
  public sealed class ResolveInputs
  {
    public string ProjectDirectory { get; set; } = string.Empty;
    public string OutputDir { get; set; } = string.Empty;
    public string LogVerbosity { get; set; } = string.Empty;

    [Microsoft.Build.Framework.Output]
    public string ResolvedConfig { get; set; } = string.Empty;

    [Microsoft.Build.Framework.Output]
    public string ResolvedRenaming { get; set; } = string.Empty;
  }

  [MsBuildTask(AssemblyFile = "$(_EfcptTaskAssembly)")]
  public sealed class DetectSqlProject
  {
    public string ProjectPath { get; set; } = string.Empty;
    public string SqlServerVersion { get; set; } = string.Empty;
    public string DSP { get; set; } = string.Empty;

    [Microsoft.Build.Framework.Output]
    public string IsSqlProject { get; set; } = string.Empty;
  }
}

namespace JD.MSBuild.Fluent.Tests.Tasks
{
  [MsBuildTask(NameStyle = MsBuildTaskNameStyle.Name)]
  public sealed class Copy
  {
    public string SourceFiles { get; set; } = string.Empty;
    public string DestinationFolder { get; set; } = string.Empty;

    [Microsoft.Build.Framework.Output]
    public string CopiedFiles { get; set; } = string.Empty;
  }

  [MsBuildTask(NameStyle = MsBuildTaskNameStyle.Name)]
  public sealed class WriteLinesToFile
  {
    public string Lines { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
  }

  [MsBuildTask(NameStyle = MsBuildTaskNameStyle.FullName)]
  public sealed class SampleTask
  {
    public string Param { get; set; } = string.Empty;

    [Microsoft.Build.Framework.Output]
    public string Out { get; set; } = string.Empty;

    [Microsoft.Build.Framework.Output]
    public string Items { get; set; } = string.Empty;
  }
}
