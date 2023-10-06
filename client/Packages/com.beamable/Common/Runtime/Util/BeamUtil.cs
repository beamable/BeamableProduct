using System.Runtime.CompilerServices;

namespace Beamable.Common
{
	public static class BeamUtil
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int CombineHashCodes(int h1, int h2)
		{
			return (((h1 << 5) + h1) ^ h2);
		}
	}
}
