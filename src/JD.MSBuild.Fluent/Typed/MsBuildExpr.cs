namespace JD.MSBuild.Fluent.Typed;

/// <summary>
/// Helper methods for building common MSBuild expressions from typed names.
/// </summary>
public static class MsBuildExpr
{
  /// <summary>
  /// Formats a property reference like $(PropertyName).
  /// </summary>
  public static string Prop<T>() where T : IMsBuildPropertyName, new()
    => $"$({new T().Name})";

  /// <summary>
  /// Formats an item list reference like @(ItemName).
  /// </summary>
  public static string Item<T>() where T : IMsBuildItemTypeName, new()
    => $"@({new T().Name})";

  /// <summary>
  /// Builds a condition for an empty property value without spaces.
  /// </summary>
  public static string IsEmpty<T>() where T : IMsBuildPropertyName, new()
    => $"'{Prop<T>()}'==''";

  /// <summary>
  /// Builds a condition for an empty property value with spaces around ==.
  /// </summary>
  public static string IsEmptyWithSpace<T>() where T : IMsBuildPropertyName, new()
    => $"'{Prop<T>()}' == ''";

  /// <summary>
  /// Builds a condition for a non-empty property value.
  /// </summary>
  public static string NotEmpty<T>() where T : IMsBuildPropertyName, new()
    => $"'{Prop<T>()}'!=''";

  /// <summary>
  /// Builds a condition for a property value equal to true.
  /// </summary>
  public static string IsTrue<T>() where T : IMsBuildPropertyName, new()
    => $"'{Prop<T>()}' == 'true'";

  /// <summary>
  /// Builds a condition for a property value not equal to true.
  /// </summary>
  public static string IsNotTrue<T>() where T : IMsBuildPropertyName, new()
    => $"'{Prop<T>()}' != 'true'";

  /// <summary>
  /// Joins condition fragments with " and ".
  /// </summary>
  public static string And(params string[] conditions)
    => string.Join(" and ", conditions.Where(c => !string.IsNullOrWhiteSpace(c)));

  /// <summary>
  /// Joins condition fragments with " or ".
  /// </summary>
  public static string Or(params string[] conditions)
    => string.Join(" or ", conditions.Where(c => !string.IsNullOrWhiteSpace(c)));
}
