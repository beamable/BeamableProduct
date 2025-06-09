
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Beamable.Server
{
	[Serializable]
	public class AssemblyDefinitionInfo : IEquatable<AssemblyDefinitionInfo>
	{
		public string Name;
		public string[] References = new string[] { };
		public string[] DllReferences = new string[] { };
		public string Location;

		public string[] IncludePlatforms = new string[] { };
		public bool AutoReferenced = false;

		public bool Equals(AssemblyDefinitionInfo other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Name == other.Name;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((AssemblyDefinitionInfo)obj);
		}

		public override int GetHashCode()
		{
			return (Name != null ? Name.GetHashCode() : 0);
		}
	}

	public class AssemblyDefinitionInfoGroup
	{
		public AssemblyDefinitionInfoCollection ToCopy;
		public AssemblyDefinitionInfoCollection Stubbed;
		public AssemblyDefinitionInfoCollection Invalid;

		public HashSet<string> DllReferences = new HashSet<string>();

	}

	public class AssemblyDefinitionNotFoundException : Exception
	{
		public AssemblyDefinitionNotFoundException(string assemblyName) : base($"Cannot find unity assembly {assemblyName}") { }
	}

	public class DllReferenceNotFoundException : Exception
	{
		public DllReferenceNotFoundException(string dllReference) : base($"Cannot find dll reference {dllReference}") { }
	}

	public class AssemblyDefinitionInfoCollection : IEnumerable<AssemblyDefinitionInfo>
	{
		private Dictionary<string, AssemblyDefinitionInfo> _assemblies = new Dictionary<string, AssemblyDefinitionInfo>();

		public AssemblyDefinitionInfoCollection(IEnumerable<AssemblyDefinitionInfo> assemblies)
		{
			_assemblies = assemblies.ToDictionary(a => a.Name);
		}


		public AssemblyDefinitionInfo Find(string assemblyName)
		{
			const string guidPrefix = "GUID:";

			if (assemblyName.StartsWith(guidPrefix))
			{
				var path = AssetDatabase.GUIDToAssetPath(assemblyName.Replace(guidPrefix, string.Empty));
				var assemblyDefinitionInfo = _assemblies.Where(pair => pair.Value.Location == path)
													.Select(pair => pair.Value).FirstOrDefault();
				if (assemblyDefinitionInfo != null)
				{
					return assemblyDefinitionInfo;
				}
			}

			if (!_assemblies.TryGetValue(assemblyName, out var unityAssembly))
			{
				throw new AssemblyDefinitionNotFoundException(assemblyName);
			}

			return unityAssembly;
		}

		public AssemblyDefinitionInfo Find(Type type)
		{
			return Find(type.Assembly);
		}

		public AssemblyDefinitionInfo Find<T>()
		{
			return Find(typeof(T).Assembly);
		}

		public bool Contains(Type t)
		{
			try
			{
				Find(t);
				return true;
			}
			catch
			{
				var dll = System.IO.Path.GetFileName(t.Assembly.Location);
				var reference = _assemblies.Values.FirstOrDefault(asm => asm.DllReferences.Contains(dll));
				var inAnyReference = reference != null;
				return inAnyReference;
			}
		}

		public AssemblyDefinitionInfo Find(Assembly assembly)
		{
			// make sure this assembly exists in the Unity assembly definition list.
			var hasMoreThanOneModule = assembly.Modules.Count() > 1;
			if (hasMoreThanOneModule)
			{
				throw new Exception("Cannot handle multi-module assemblies");
			}

			var moduleName = assembly.Modules.FirstOrDefault().Name.Replace(".dll", "");
			if (!_assemblies.TryGetValue(moduleName, out var unityAssembly))
			{
				throw new Exception($"Cannot handle non unity assemblies yet. moduleName=[{moduleName}]");
			}

			return unityAssembly;
		}

		public IEnumerator<AssemblyDefinitionInfo> GetEnumerator()
		{
			return _assemblies.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

}
