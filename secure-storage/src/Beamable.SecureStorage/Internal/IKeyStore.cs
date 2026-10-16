namespace Beamable.Security.Internal
{
	internal interface IKeyStore
	{
		// Never create a replacement key when an encrypted snapshot already exists.
		byte[] GetKey(string storageDirectory, bool allowCreate);
	}
}
