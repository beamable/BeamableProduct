namespace cli.Services.Web.CodeGen;

/// <summary>
/// Common TypeScript Utility Types.
/// </summary>
public static class TsUtilityType
{
	/// <summary>
	/// Builds a type expression for the given partial type.
	/// </summary>
	/// <param name="t"> The partial type.</param>
	/// <returns>A type expression for the given partial type.</returns>
	/// <example><c>TsType.Partial(TsType.Of("Foo"))</c> equals <c>Partial&lt;Foo&gt;</c>.</example>
	public static TsType Partial(TsType t) => TsType.Generic("Partial", t);

	/// <summary>
	/// Builds a type expression for the given required type.
	/// </summary>
	/// <param name="t">The required type.</param>
	/// <returns>A type expression for the given required type.</returns>
	/// <example><c>TsType.Required(TsType.Of("Foo"))</c> equals <c>Required&lt;Foo&gt;</c>.</example>
	public static TsType Required(TsType t) => TsType.Generic("Required", t);

	/// <summary>
	/// Builds a type expression for the given readonly type.
	/// </summary>
	/// <param name="t">The readonly type.</param>
	/// <returns>A type expression for the given readonly type.</returns>
	/// <example><c>TsType.Readonly(TsType.Of("Foo"))</c> equals <c>Readonly&lt;Foo&gt;</c>.</example>
	public static TsType Readonly(TsType t) => TsType.Generic("Readonly", t);

	/// <summary>
	/// Builds a type expression for the given record type.
	/// </summary>
	/// <param name="k">The type of the record keys.</param>
	/// <param name="v">The type of the record values.</param>
	/// <returns>A type expression for the given record type.</returns>
	/// <example><c>TsType.Record(TsType.String, TsType.Number)</c> equals <c>Record&lt;string, number&gt;</c>.</example>
	public static TsType Record(TsType k, TsType v) => TsType.Generic("Record", k, v);

	/// <summary>
	/// Builds a type expression for the given pick type.
	/// </summary>
	/// <param name="t">The pick type.</param>
	/// <param name="k">The pick keys.</param>
	/// <returns>A type expression for the given pick type.</returns>
	/// <example>
	/// <c>TsType.Pick(TsType.Of("Foo"), "key1", "key2")</c> equals <c>Pick&lt;Foo, "key1" | "key2"&gt;</c>.
	/// </example>
	public static TsType Pick(TsType t, params string[] k) =>
		TsType.Generic("Pick", t, TsType.Union(k.Select(TsType.Of).ToArray()));

	/// <summary>
	/// Builds a type expression for the given omit type.
	/// </summary>
	/// <param name="t">The omit type.</param>
	/// <param name="k">The omit keys.</param>
	/// <returns>A type expression for the given omit type.</returns>
	/// <example>
	/// <c>TsType.Omit(TsType.Of("Foo"), "key1", "key2")</c> equals <c>Omit&lt;Foo, "key1" | "key2"&gt;</c>.
	/// </example>
	public static TsType Omit(TsType t, params string[] k) =>
		TsType.Generic("Omit", t, TsType.Union(k.Select(TsType.Of).ToArray()));

	/// <summary>
	/// Builds a type expression for the given exclude type.
	/// </summary>
	/// <param name="t">The union type to exclude from.</param>
	/// <param name="u">The union type to exclude.</param>
	/// <returns>A type expression for the given exclude type.</returns>
	/// <example>
	/// <c>TsType.Exclude(TsType.Union(TsType.Of("Foo"), TsType.Of("Bar")), TsType.Of("Bar"))</c>
	/// equals <c>Exclude&lt;Foo | Bar, Bar&gt;</c>.
	/// </example>
	public static TsType Exclude(TsType t, TsType u) => TsType.Generic("Exclude", t, u);

	/// <summary>
	/// Builds a type expression for the given extract type.
	/// </summary>
	/// <param name="t">The union type to extract from.</param>
	/// <param name="u">The union type to extract.</param>
	/// <returns>A type expression for the given extract type.</returns>
	/// <example>
	/// <c>TsType.Extract(TsType.Union(TsType.Of("Foo"), TsType.Of("Bar")), TsType.Of("Bar"))</c>
	/// equals <c>Extract&lt;Foo | Bar, Bar&gt;</c>.
	/// </example>
	public static TsType Extract(TsType t, TsType u) => TsType.Generic("Extract", t, u);

	/// <summary>
	/// Builds a type expression that excludes null and undefined from a type.
	/// </summary>
	/// <param name="t">The type to make non-nullable.</param>
	/// <returns>A type expression for the non-nullable type.</returns>
	/// <example>
	/// <c>TsType.NonNullable(TsType.Union(TsType.String, TsType.Undefined))</c>
	/// equals <c>NonNullable&lt;string | undefined&gt;</c>.
	/// </example>
	public static TsType NonNullable(TsType t) => TsType.Generic("NonNullable", t);
}
