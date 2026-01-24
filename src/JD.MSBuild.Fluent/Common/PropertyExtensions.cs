using JD.MSBuild.Fluent.Fluent;

namespace JD.MSBuild.Fluent.Common;

/// <summary>
/// Extension methods for working with MSBuild properties in a fluent manner.
/// Provides common patterns for property configuration and cascading.
/// </summary>
public static class PropertyExtensions
{
    /// <summary>
    /// Sets a property with a fallback value if the first choice is empty.
    /// Common pattern: User override → Default value.
    /// </summary>
    /// <param name="builder">The props builder to add properties to.</param>
    /// <param name="propertyName">Name of the property to set.</param>
    /// <param name="firstChoice">Value to use if available (typically a user-provided property).</param>
    /// <param name="fallback">Fallback value to use if firstChoice is empty.</param>
    /// <returns>The props builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Instead of:
    /// p.Property("MyProp", "$(UserValue)", "'$(UserValue)' != ''");
    /// p.Property("MyProp", "defaultValue", "'$(MyProp)' == ''");
    /// 
    /// // Use:
    /// p.PropertyWithFallback("MyProp", "$(UserValue)", "defaultValue");
    /// </code>
    /// </example>
    public static PropsBuilder PropertyWithFallback(
        this PropsBuilder builder,
        string propertyName,
        string firstChoice,
        string fallback)
    {
        // Try first choice if it's not empty
        builder.Property(propertyName, firstChoice, $"'{firstChoice}' != ''");
        
        // Fallback if property still not set
        builder.Property(propertyName, fallback, $"'$({propertyName})' == ''");

        return builder;
    }

    /// <summary>
    /// Sets a property with a fallback value if the first choice is empty.
    /// Overload for use within PropertyGroup.
    /// </summary>
    /// <param name="builder">The property group builder to add properties to.</param>
    /// <param name="propertyName">Name of the property to set.</param>
    /// <param name="firstChoice">Value to use if available (typically a user-provided property).</param>
    /// <param name="fallback">Fallback value to use if firstChoice is empty.</param>
    /// <returns>The property group builder for method chaining.</returns>
    public static PropsGroupBuilder PropertyWithFallback(
        this PropsGroupBuilder builder,
        string propertyName,
        string firstChoice,
        string fallback)
    {
        // Try first choice if it's not empty
        builder.Property(propertyName, firstChoice, $"'{firstChoice}' != ''");
        
        // Fallback if property still not set
        builder.Property(propertyName, fallback, $"'$({propertyName})' == ''");

        return builder;
    }

    /// <summary>
    /// Begins a property cascade that tries multiple values in order.
    /// Useful for complex fallback chains: User → Config → Environment → Default.
    /// </summary>
    /// <param name="builder">The props builder to add the cascade to.</param>
    /// <param name="propertyName">Name of the property to set.</param>
    /// <returns>A cascade builder for specifying value attempts.</returns>
    /// <example>
    /// <code>
    /// p.PropertyCascade("MyProp")
    ///     .TryValue("$(UserOverride)")
    ///     .TryValue("$(ConfigValue)", "'$(ConfigExists)' == 'true'")
    ///     .TryValue("$(EnvValue)", "'$(EnvValue)' != ''")
    ///     .Default("hardcoded-fallback");
    /// </code>
    /// </example>
    public static PropertyCascadeBuilder PropertyCascade(
        this PropsBuilder builder,
        string propertyName)
    {
        return new PropertyCascadeBuilder(builder, propertyName);
    }
}

/// <summary>
/// Builder for constructing property value cascades with multiple fallback attempts.
/// </summary>
public class PropertyCascadeBuilder
{
    private readonly PropsBuilder _builder;
    private readonly string _propertyName;

    internal PropertyCascadeBuilder(PropsBuilder builder, string propertyName)
    {
        _builder = builder;
        _propertyName = propertyName;
    }

    /// <summary>
    /// Attempts to use a value with an optional additional condition.
    /// </summary>
    /// <param name="value">The value to try.</param>
    /// <param name="additionalCondition">Optional additional condition (will be AND'd with "value not empty" check).</param>
    /// <returns>This builder for chaining.</returns>
    public PropertyCascadeBuilder TryValue(string value, string? additionalCondition = null)
    {
        var condition = $"'$({_propertyName})' == '' and '{value}' != ''";
        if (!string.IsNullOrEmpty(additionalCondition))
        {
            condition += $" and {additionalCondition}";
        }

        _builder.Property(_propertyName, value, condition);
        return this;
    }

    /// <summary>
    /// Sets the default value to use if no previous attempts succeeded.
    /// </summary>
    /// <param name="defaultValue">The default value to use.</param>
    /// <returns>The original props builder for continued fluent usage.</returns>
    public PropsBuilder Default(string defaultValue)
    {
        _builder.Property(_propertyName, defaultValue, $"'$({_propertyName})' == ''");
        return _builder;
    }
}
