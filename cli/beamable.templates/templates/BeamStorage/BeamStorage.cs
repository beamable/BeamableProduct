using Beamable.Server;

namespace Beamable.Server
{
	/// <summary>
	/// This class represents the existence of the BeamStorage database.
	/// Use it for type safe access to the database.
	/// <code>
	/// var db = await Storage.GetDatabase&lt;BeamStorage&gt;();
	/// </code>
	/// </summary>
	[StorageObject("BeamStorage")]
	public class BeamStorage : MongoStorageObject
	{
		
	}
}
