using Beamable.Common.Reflection;
using System;
using UnityEngine;

namespace Beamable.Editor.Reflection
{
	public abstract class ReflectionCacheUserSystemObject : ScriptableObject
	{
		public abstract IReflectionCacheUserSystem UserSystem {
			get;
		}

		public abstract IReflectionCacheTypeProvider UserTypeProvider {
			get;
		}

		public abstract Type UserSystemType {
			get;
		}
	}
}
