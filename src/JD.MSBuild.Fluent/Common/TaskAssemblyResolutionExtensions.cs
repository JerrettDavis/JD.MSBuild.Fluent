using JD.MSBuild.Fluent.Fluent;

namespace JD.MSBuild.Fluent.Common;

/// <summary>
/// Extension methods for resolving multi-targeted task assemblies in MSBuild packages.
/// Handles version-based selection of the correct .NET framework for task execution.
/// </summary>
public static class TaskAssemblyResolutionExtensions
{
    /// <summary>
    /// Configures automatic resolution of a multi-targeted task assembly based on MSBuild version.
    /// Generates cascading property conditions to select the correct target framework folder.
    /// </summary>
    /// <param name="group">The property group builder to add resolution logic to.</param>
    /// <param name="tasksFolderProperty">Name of property to store selected framework folder (e.g., "_MyTasksFolder").</param>
    /// <param name="taskAssemblyProperty">Name of property to store full assembly path (e.g., "_MyTaskAssembly").</param>
    /// <param name="assemblyFileName">Task assembly filename (e.g., "My.Tasks.dll").</param>
    /// <param name="packageId">Package ID for constructing NuGet package path.</param>
    /// <param name="frameworkMappings">Array of (targetFramework, minMSBuildVersion) tuples in priority order.</param>
    /// <remarks>
    /// <para><strong>Resolution Strategy:</strong></para>
    /// <list type="bullet">
    /// <item>Evaluates framework mappings in order until a match is found</item>
    /// <item>.NET Core MSBuild matches when MSBuildRuntimeType='Core' and version >= minMSBuildVersion</item>
    /// <item>.NET Framework MSBuild matches any remaining cases (typically net472)</item>
    /// </list>
    /// <para><strong>Path Resolution:</strong></para>
    /// <list type="number">
    /// <item>NuGet package: $(MSBuildThisFileDirectory)../tasks/{framework}/{assemblyFileName}</item>
    /// <item>Local build (with $(Configuration)): $(MSBuildThisFileDirectory)../../{packageId}.Tasks/bin/$(Configuration)/{framework}/{assemblyFileName}</item>
    /// <item>Local Debug build: $(MSBuildThisFileDirectory)../../{packageId}.Tasks/bin/Debug/{framework}/{assemblyFileName}</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// group.ResolveMultiTargetedTaskAssembly(
    ///     "_MyTasksFolder", 
    ///     "_MyTaskAssembly",
    ///     "My.Tasks.dll",
    ///     "My.Package",
    ///     ("net10.0", "18.0"),   // VS 2026+
    ///     ("net9.0", "17.12"),   // VS 2024 Update 12+
    ///     ("net8.0", "15.0"),    // Older .NET MSBuild
    ///     ("net472", "15.0")     // .NET Framework MSBuild
    /// );
    /// </code>
    /// </example>
    public static PropsGroupBuilder ResolveMultiTargetedTaskAssembly(
        this PropsGroupBuilder group,
        string tasksFolderProperty,
        string taskAssemblyProperty,
        string assemblyFileName,
        string packageId,
        params (string targetFramework, string minMSBuildVersion)[] frameworkMappings)
    {
        if (frameworkMappings == null || frameworkMappings.Length == 0)
            throw new ArgumentException("At least one framework mapping must be provided", nameof(frameworkMappings));

        // Generate cascading framework selection
        for (int i = 0; i < frameworkMappings.Length; i++)
        {
            var (framework, minVersion) = frameworkMappings[i];
            string condition;

            // First framework: no prefix condition needed
            // Subsequent frameworks: only if not already set
            var prefix = i == 0 ? "" : $"'$({tasksFolderProperty})' == '' and ";

            // For .NET Framework targets (usually the last one), match everything remaining
            if (framework.StartsWith("net4", StringComparison.OrdinalIgnoreCase))
            {
                // .NET Framework MSBuild - catch-all for anything not matched yet
                condition = $"{prefix}'$({tasksFolderProperty})' == ''";
            }
            else
            {
                // .NET Core MSBuild with version check
                condition = $"{prefix}'$(MSBuildRuntimeType)' == 'Core' and $([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '{minVersion}'))";
            }

            group.Property(tasksFolderProperty, framework, condition);
        }

        // Resolve assembly path with fallbacks
        // 1. NuGet package path (for consumers)
        group.Property(taskAssemblyProperty, 
            $"$(MSBuildThisFileDirectory)..\\tasks\\$({tasksFolderProperty})\\{assemblyFileName}");

        // 2. Local build with $(Configuration) (for development builds)
        group.Property(taskAssemblyProperty,
            $"$(MSBuildThisFileDirectory)..\\..\\{packageId}.Tasks\\bin\\$(Configuration)\\$({tasksFolderProperty})\\{assemblyFileName}",
            $"!Exists('$({taskAssemblyProperty})')");

        // 3. Local Debug build (fallback when Configuration is not set)
        group.Property(taskAssemblyProperty,
            $"$(MSBuildThisFileDirectory)..\\..\\{packageId}.Tasks\\bin\\Debug\\$({tasksFolderProperty})\\{assemblyFileName}",
            $"!Exists('$({taskAssemblyProperty})') and '$(Configuration)' == ''");

        return group;
    }
}
