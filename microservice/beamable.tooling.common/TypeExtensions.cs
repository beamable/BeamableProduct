namespace Beamable.Server.Common;

/// <summary>
/// A utility class that offers the <see cref="IsAssignableTo"/> shim
/// </summary>
public static class TypeExtensions
{
	/// <summary>
	/// Similar to <see cref="Type.IsAssignableFrom"/>, but in the opposite direction.
	/// </summary>
	/// <param name="type"></param>
	/// <param name="other"></param>
	/// <returns></returns>
	public static bool IsAssignableTo(this Type type, Type other)
	{
		return other.IsAssignableFrom(type);
	}

	public static string GetSanitizedFullName(this Type type)
	{
		if (type.FullName != null && type.FullName.Contains("`"))
			return type.FullName.Split('`')[0];
		return type.FullName;
	}
}
