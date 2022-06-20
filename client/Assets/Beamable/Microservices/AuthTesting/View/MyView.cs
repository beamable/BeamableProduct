using Beamable.Server;

namespace Beamable.Microservices.View
{
	[MicroView("sample", MicroViewSlot.PLAYER)]
	public class MyView : SvelteView
	{
		/*
		 * Mostly a no-op. This class serves as a flag to Beamable to let it know that it should treat this folder as a svelte view.
		 */
	}
}
