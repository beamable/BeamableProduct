// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using System;

namespace Beamable.Common.Dependencies
{
	/// <summary>
	/// Any service that is in a <see cref="IDependencyProvider"/> can have this interface.
	/// When the <see cref="IDependencyProvider"/> is disposed via the <see cref="IDependencyProviderScope.Dispose"/> method,
	/// this service's <see cref="IBeamableDisposable.OnDispose"/> method will trigger.
	/// </summary>
	public interface IBeamableDisposable
	{
		/// <summary>
		/// Used as a way to allow services attached to a <see cref="IDependencyProvider"/> to clean up their state when
		/// the <see cref="IDependencyProviderScope.Dispose"/> method is called
		/// </summary>
		/// <returns>A promise that represents when the service's clean up is complete. This promise will hang the entire disposal process of the <see cref="IDependencyProviderScope"/></returns>
		Promise OnDispose();
	}

	/// <summary>
	/// <inheritdoc cref="IBeamableDisposable"/>
	/// </summary>
	public interface IBeamableDisposableOrder : IBeamableDisposable
	{
		/// <summary>
		/// By default, all services are disposed in one group, and therefor, cannot have dependencies on eachother
		/// when disposing.
		/// However, if the service is critical and other services require it for shutdown, then the <see cref="DisposeOrder"/>
		/// flag can be used. The higher the number, the more important the service is, and the later in the dispose cycle it will occur.
		/// </summary>
		int DisposeOrder { get; }
	}

	/// <summary>
	/// Describes how to create a service instance
	/// </summary>
	public class ServiceDescriptor
	{
		public Type Interface, Implementation;
		public Func<IDependencyProvider, object> Factory;

		public ServiceDescriptor Clone()
		{
			return new ServiceDescriptor { Interface = Interface, Implementation = Implementation, Factory = Factory };
		}
	}
}
