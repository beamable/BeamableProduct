using System.Collections.Generic;

namespace Beamable.UI.Buss
{
	public interface IComputedProperty : IBussProperty
	{
		IEnumerable<IBussProperty> Members { get; }
	}
}
