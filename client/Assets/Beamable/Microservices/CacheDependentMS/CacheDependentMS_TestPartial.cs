using Beamable.Common;
using System.Threading.Tasks;

namespace Beamable.Server
{
	public partial class CacheDependentMS
	{
		/// <summary>
		/// Just a trivial example of a ClientCallable that access the cached data. 
		/// </summary>
		/// <returns></returns>
		[ClientCallable]
		public void TestUnsupportedParameters2(Task testTask, Promise testPromise) { }
	}
}
