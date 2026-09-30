using Beamable.Tooling.Common.OpenAPI.Utils;

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
		if (type.IsGenericType)
		{
			string typeName = type.FullName?.Split('`')[0] ?? type.Name.Split('`')[0];
		
			var genericArgs = type.GetGenericArguments();
			string args = string.Join(", ", genericArgs.Select(GetSanitizedFullName));

			return $"{typeName}<{args}>".Replace("+",".");
		}
		if(type.IsBasicType() && OpenApiUtils.OpenApiCSharpNameMap.TryGetValue(type.Name.ToLower(), out string shortName))
		{
			return shortName;
		}

		if (type.FullName == null)
		{
			return type.Name.Replace("+",".");
		}

		if(type.FullName.Contains("`"))
			return type.FullName.Split('`')[0].Replace("+",".");
		return type.FullName.Replace("+",".");
	}
	
	/// <summary>
	/// Determines whether the specified <see cref="Type"/> represents a basic or primitive value type,
	/// including common types such as <see cref="System.Int32"/>, <see cref="string"/>, <see cref="bool"/>,
	/// numeric types, and characters.
	/// </summary>
	/// <param name="t">The <see cref="Type"/> to evaluate.</param>
	/// <returns>
	/// <c>true</c> if the given <paramref name="t"/> is considered a basic or primitive type; otherwise, <c>false</c>.
	/// </returns>
	public static bool IsBasicType(this Type t)
	{
		switch (Type.GetTypeCode(t))
		{
			case TypeCode.Boolean:
			case TypeCode.Byte:
			case TypeCode.SByte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			case TypeCode.UInt32:
			case TypeCode.Int64:
			case TypeCode.UInt64:
			case TypeCode.Single:
			case TypeCode.Double:
			case TypeCode.Decimal:
			case TypeCode.String:
			case TypeCode.Char:
				return true;

			default:
				return t.IsPrimitive;
		}
	}
	
}
