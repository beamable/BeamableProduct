// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

using System.Collections.Generic;
using System.Linq;

namespace Beamable.Common.Dependencies
{
	public abstract class DependencyList<T>
	{
		public T[] Elements { get; private set; }

		protected DependencyList(IDependencyProviderScope scope, IEnumerable<ServiceDescriptor> services)
		{
			var descriptors = services.Where(service => typeof(T).IsAssignableFrom(service.Interface)).ToList();
			Elements = descriptors.Select(descriptor => scope.GetService(descriptor.Interface)).Cast<T>().ToArray();
		}
	}

	public class SingletonDependencyList<T> : DependencyList<T>
	{
		public SingletonDependencyList(IDependencyProviderScope scope) : base(scope, scope.SingletonServices) { }
	}
}
