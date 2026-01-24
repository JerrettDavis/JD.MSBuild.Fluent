using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.Typed;

namespace JD.MSBuild.Fluent.Common;

/// <summary>
/// Extension methods for creating targets with common hook patterns.
/// Simplifies conditional target execution at build lifecycle points.
/// </summary>
public static class TargetHookExtensions
{
    /// <summary>
    /// Creates a target that runs before the Build target with an optional condition.
    /// Common pattern for initialization and validation tasks.
    /// </summary>
    /// <param name="builder">The targets builder to add the target to.</param>
    /// <param name="targetName">Name of the target to create.</param>
    /// <param name="condition">Optional condition for target execution.</param>
    /// <param name="configure">Action to configure the target contents.</param>
    /// <returns>The targets builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// t.BeforeBuildTarget("_MyInitialize", 
    ///     condition: "'$(MyEnabled)' == 'true'",
    ///     target => {
    ///         target.Message("Initializing...", "high");
    ///         target.Task("MyTask", task => { ... });
    ///     });
    /// </code>
    /// </example>
    public static TargetsBuilder BeforeBuildTarget(
        this TargetsBuilder builder,
        string targetName,
        string? condition = null,
        Action<TargetBuilder>? configure = null)
    {
        return builder.Target(targetName, target =>
        {
            target.BeforeTargets("Build");
            if (!string.IsNullOrEmpty(condition))
                target.Condition(condition!);
            configure?.Invoke(target);
        });
    }

    /// <summary>
    /// Creates a target that runs after the Build target with an optional condition.
    /// Common pattern for post-build tasks and finalization.
    /// </summary>
    /// <param name="builder">The targets builder to add the target to.</param>
    /// <param name="targetName">Name of the target to create.</param>
    /// <param name="condition">Optional condition for target execution.</param>
    /// <param name="configure">Action to configure the target contents.</param>
    /// <returns>The targets builder for method chaining.</returns>
    public static TargetsBuilder AfterBuildTarget(
        this TargetsBuilder builder,
        string targetName,
        string? condition = null,
        Action<TargetBuilder>? configure = null)
    {
        return builder.Target(targetName, target =>
        {
            target.AfterTargets("Build");
            if (!string.IsNullOrEmpty(condition))
                target.Condition(condition!);
            configure?.Invoke(target);
        });
    }

    /// <summary>
    /// Creates a target that runs before multiple targets with an optional condition.
    /// </summary>
    /// <param name="builder">The targets builder to add the target to.</param>
    /// <param name="targetName">Name of the target to create.</param>
    /// <param name="beforeTargets">Semicolon-separated target names to run before.</param>
    /// <param name="condition">Optional condition for target execution.</param>
    /// <param name="configure">Action to configure the target contents.</param>
    /// <returns>The targets builder for method chaining.</returns>
    public static TargetsBuilder BeforeTargets(
        this TargetsBuilder builder,
        string targetName,
        string beforeTargets,
        string? condition = null,
        Action<TargetBuilder>? configure = null)
    {
        return builder.Target(targetName, target =>
        {
            target.BeforeTargets(beforeTargets);
            if (!string.IsNullOrEmpty(condition))
                target.Condition(condition!);
            configure?.Invoke(target);
        });
    }

    /// <summary>
    /// Creates a target that runs after multiple targets with an optional condition.
    /// </summary>
    /// <param name="builder">The targets builder to add the target to.</param>
    /// <param name="targetName">Name of the target to create.</param>
    /// <param name="afterTargets">Semicolon-separated target names to run after.</param>
    /// <param name="condition">Optional condition for target execution.</param>
    /// <param name="configure">Action to configure the target contents.</param>
    /// <returns>The targets builder for method chaining.</returns>
    public static TargetsBuilder AfterTargets(
        this TargetsBuilder builder,
        string targetName,
        string afterTargets,
        string? condition = null,
        Action<TargetBuilder>? configure = null)
    {
        return builder.Target(targetName, target =>
        {
            target.AfterTargets(afterTargets);
            if (!string.IsNullOrEmpty(condition))
                target.Condition(condition!);
            configure?.Invoke(target);
        });
    }

    /// <summary>
    /// Creates a target that runs before CoreCompile with an optional condition.
    /// Common pattern for code generation tasks.
    /// </summary>
    /// <param name="builder">The targets builder to add the target to.</param>
    /// <param name="targetName">Name of the target to create.</param>
    /// <param name="condition">Optional condition for target execution.</param>
    /// <param name="configure">Action to configure the target contents.</param>
    /// <returns>The targets builder for method chaining.</returns>
    public static TargetsBuilder BeforeCompileTarget(
        this TargetsBuilder builder,
        string targetName,
        string? condition = null,
        Action<TargetBuilder>? configure = null)
    {
        return builder.Target(targetName, target =>
        {
            target.BeforeTargets("CoreCompile");
            if (!string.IsNullOrEmpty(condition))
                target.Condition(condition!);
            configure?.Invoke(target);
        });
    }

    /// <summary>
    /// Creates a target that runs after Clean with an optional condition.
    /// Common pattern for cleanup tasks.
    /// </summary>
    /// <param name="builder">The targets builder to add the target to.</param>
    /// <param name="targetName">Name of the target to create.</param>
    /// <param name="condition">Optional condition for target execution.</param>
    /// <param name="configure">Action to configure the target contents.</param>
    /// <returns>The targets builder for method chaining.</returns>
    public static TargetsBuilder AfterCleanTarget(
        this TargetsBuilder builder,
        string targetName,
        string? condition = null,
        Action<TargetBuilder>? configure = null)
    {
        return builder.Target(targetName, target =>
        {
            target.AfterTargets("Clean");
            if (!string.IsNullOrEmpty(condition))
                target.Condition(condition!);
            configure?.Invoke(target);
        });
    }
}
