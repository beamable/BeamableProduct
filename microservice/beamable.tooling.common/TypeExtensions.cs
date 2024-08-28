namespace Beamable.Server.Common;

public static class TypeExtensions
{
	public static bool IsAssignableTo(this Type type, Type other)
	{
		return other.IsAssignableFrom(type);
	}
}
