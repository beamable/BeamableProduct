namespace cli.Services.Web.CodeGen;

[Flags]
public enum TsModifier
{
	None = 0, // 0000 0000 0000

	// ── Module-level ─────────────────────────────────────────────
	Export = 1 << 0, // 0000 0000 0001
	Default = 1 << 1, // 0000 0000 0010
	Declare = 1 << 2, // 0000 0000 0100

	// ── Accessibility ───────────────────────────────────────────
	Public = 1 << 3, // 0000 0000 1000
	Protected = 1 << 4, // 0000 0001 0000
	Private = 1 << 5, // 0000 0010 0000

	// ── Member-level details ────────────────────────────────────
	Static = 1 << 6, // 0000 0100 0000
	Override = 1 << 7, // 0000 1000 0000
	Readonly = 1 << 8, // 0001 0000 0000
	Abstract = 1 << 9, // 0010 0000 0000
	Async = 1 << 10 // 0100 0000 0000
}

public static class TsModifierExtensions
{
	public static string Export => TsModifier.Export.ToString().ToLower();

	public static string Default => TsModifier.Default.ToString().ToLower();

	public static string Declare => TsModifier.Declare.ToString().ToLower();

	public static string Public => TsModifier.Public.ToString().ToLower();

	public static string Protected => TsModifier.Protected.ToString().ToLower();

	public static string Private => TsModifier.Private.ToString().ToLower();

	public static string Static => TsModifier.Static.ToString().ToLower();

	public static string Override => TsModifier.Override.ToString().ToLower();

	public static string Readonly => TsModifier.Readonly.ToString().ToLower();

	public static string Abstract => TsModifier.Abstract.ToString().ToLower();

	public static string Async => TsModifier.Async.ToString().ToLower();
}
