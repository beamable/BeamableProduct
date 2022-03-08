namespace Beamable.Common.Api
{
	public interface IBeamableFilesystemAccessor
	{
		string GetPersistentDataPathWithoutTrailingSlash();
	}
}
