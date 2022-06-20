using Beamable.Server;
using Beamable.Server.Editor;
using System;
using System.Collections.Generic;

namespace Beamable.Server
{

	[Serializable]
	public class MicroViewManifest
	{
		public List<MicroViewEntry> Views;
	}

	[Serializable]
	public class MicroViewEntry
	{
		public string Name;
		public MicroViewSlot Slot;
		public string Path;

		// this should not be serialized, because leaking the source code's position isn't a good idea.
		public ViewDescriptor ViewDescriptor { get; set; }
	}
}
