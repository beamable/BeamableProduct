
using System;
using System.Text;

namespace Beamable.Common
{
	public static class TypeExtensions
	{
		public static string GetTypeString(this Type type)
		{
			StringBuilder retType = new StringBuilder();

			if (type.IsGenericType)
			{
				string[] parentType = type.FullName.Split('`');

				Type[] arguments = type.GetGenericArguments();

				StringBuilder argList = new StringBuilder();
				foreach (Type t in arguments)
				{
					string arg = GetTypeString(t);
					if (argList.Length > 0)
						argList.AppendFormat("_{0}", arg);
					else
						argList.Append(arg);
				}

				if (argList.Length > 0)
					retType.AppendFormat("{0}_{1}", parentType[0], argList.ToString());
			}
			else if (type.IsArray)
			{
				retType.AppendFormat("{0}_{1}", type.BaseType, GetTypeString(type.GetElementType()));
			}
			else
			{
				return type.ToString();
			}

			return retType.ToString();
		}
	}
}
