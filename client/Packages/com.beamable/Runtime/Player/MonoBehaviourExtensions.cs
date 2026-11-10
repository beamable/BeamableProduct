// unset

using UnityEngine;

namespace Beamable
{
	/// <summary>Provides context lookup helpers for Unity components.</summary>
	public static class MonoBehaviourExtensions
	{
		/// <summary>Gets the nearest Beamable context on this component's GameObject or parents.</summary>
		/// <param name="self">The Unity component from which to search.</param>
		/// <returns>The nearest <see cref="BeamableBehaviour.Context"/>, or <see cref="BeamContext.Default"/>.</returns>
		/// <remarks>The returned context may still be initializing. Await its <see cref="BeamContext.Instance"/>
		/// before using player services.</remarks>
		/// <seealso cref="BeamContext.InParent"/>
		public static BeamContext GetBeamable(this Component self)
		{
			return self.GetComponentInParent<BeamableBehaviour>()?.Context ?? BeamContext.Default;
		}
	}
}
