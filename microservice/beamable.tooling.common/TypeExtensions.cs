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
		if (type.FullName != null && type.FullName.Contains("`"))
			return type.FullName.Split('`')[0];
		return type.FullName;
	}
	
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
				return false;
		}
	}
	
	public static string GetGenericSanitizedFullName(this Type type)
	{
		if (!type.IsGenericType)
		{
			if(type.IsBasicType() && OpenApiUtils.OpenApiCSharpNameMap.TryGetValue(type.Name.ToLower(), out string shortName))
			{
				return shortName;
			}
			return type.FullName ?? type.Name;
		}
		
		string typeName = type.FullName?.Split('`')[0] ?? type.Name.Split('`')[0];
		
		var genericArgs = type.GetGenericArguments();
		string args = string.Join(", ", genericArgs.Select(GetGenericSanitizedFullName));

		return $"{typeName}<{args}>";
	}

	public static string GetGenericQualifiedTypeName(this Type type)
	{
		if (!type.IsGenericType)
		{
			return type.FullName ?? type.Name;
		}
		
		string typeName = type.FullName?.Split('`')[0] ?? type.Name.Split('`')[0];
		
		var genericArgs = type.GetGenericArguments();
		string args = string.Join(", ", genericArgs.Select(GetGenericQualifiedTypeName));

		return $"{typeName}.{args}";
	}
	
	
}
