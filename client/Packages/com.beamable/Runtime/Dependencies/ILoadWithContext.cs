namespace Beamable
{
	/// <summary>
	/// Any type that implements this interface, and has been added to the <see cref="Beamable.Common.Dependencies.IDependencyBuilder"/>,
	/// will be instantiated when the <see cref="BeamContext"/> is initializing its services.
	///
	/// </summary>
	public interface ILoadWithContext
	{

	}
}
