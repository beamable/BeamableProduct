// this file was copied from nuget package Beamable.Common@4.3.0-PREVIEW.RC2
// https://www.nuget.org/packages/Beamable.Common/4.3.0-PREVIEW.RC2

using System;

namespace Beamable.Common.Player
{
	public class Lazy<T> where T : new()
	{
		private readonly Func<T> _initializer;
		private T _value;

		public T Value => _value == null ? (_value = _initializer()) : _value;

		public Lazy(Func<T> initializer)
		{
			_initializer = initializer;
		}

		public Lazy() : this(() => new T())
		{

		}

		public static implicit operator T(Lazy<T> self) => self.Value;
	}
}
