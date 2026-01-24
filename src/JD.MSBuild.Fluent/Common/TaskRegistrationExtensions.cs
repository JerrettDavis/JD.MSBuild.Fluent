using JD.MSBuild.Fluent.Fluent;

namespace JD.MSBuild.Fluent.Common;

/// <summary>
/// Extension methods for registering MSBuild tasks in a fluent manner.
/// Provides automatic registration of multiple tasks from assemblies.
/// </summary>
public static class TaskRegistrationExtensions
{
    /// <summary>
    /// Registers multiple tasks from a namespace using an explicit list of task names.
    /// Recommended approach for generation-time scenarios.
    /// </summary>
    /// <param name="builder">The targets builder to register tasks with.</param>
    /// <param name="assemblyPathProperty">MSBuild property containing the task assembly path (e.g., "$(_MyTaskAssembly)").</param>
    /// <param name="taskNamespace">Namespace of the tasks (e.g., "MyCompany.Build.Tasks").</param>
    /// <param name="taskNames">Array of task class names to register.</param>
    /// <returns>The targets builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// t.RegisterTasks(
    ///     "$(_EfcptTaskAssembly)",
    ///     "JD.Efcpt.Build.Tasks",
    ///     "DetectSqlProject",
    ///     "RunEfcpt",
    ///     "ComputeFingerprint"
    /// );
    /// </code>
    /// </example>
    public static TargetsBuilder RegisterTasks(
        this TargetsBuilder builder,
        string assemblyPathProperty,
        string taskNamespace,
        params string[] taskNames)
    {
        if (taskNames == null || taskNames.Length == 0)
            throw new ArgumentException("At least one task name must be provided", nameof(taskNames));

        foreach (var taskName in taskNames)
        {
            var fullName = string.IsNullOrEmpty(taskNamespace) 
                ? taskName 
                : $"{taskNamespace}.{taskName}";
            
            builder.UsingTask(fullName, assemblyPathProperty);
        }

        return builder;
    }

    /// <summary>
    /// Registers multiple tasks from a task name collection.
    /// Useful when task names are defined as a constant array or list.
    /// </summary>
    /// <param name="builder">The targets builder to register tasks with.</param>
    /// <param name="assemblyPathProperty">MSBuild property containing the task assembly path.</param>
    /// <param name="taskNamespace">Namespace of the tasks.</param>
    /// <param name="taskNames">Collection of task class names to register.</param>
    /// <returns>The targets builder for method chaining.</returns>
    public static TargetsBuilder RegisterTasks(
        this TargetsBuilder builder,
        string assemblyPathProperty,
        string taskNamespace,
        IEnumerable<string> taskNames)
    {
        return RegisterTasks(builder, assemblyPathProperty, taskNamespace, taskNames.ToArray());
    }
}
