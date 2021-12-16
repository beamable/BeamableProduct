using System;
using UnityEngine;

namespace Beamable.Common
{
	public abstract class ReflectionCacheUserSystemObject : ScriptableObject
	{
		public abstract IReflectionCacheUserSystem UserSystem
		{
			get;
		}

		public abstract IReflectionCacheTypeProvider UserTypeProvider
		{
			get;
		}

		public abstract Type UserSystemType
		{
			get;
		}
		
	}
}
